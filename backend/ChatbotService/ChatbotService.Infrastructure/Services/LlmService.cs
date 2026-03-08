using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace ChatbotService.Infrastructure.Services;

/// <summary>
/// Configuración del servidor LLM.
/// </summary>
public class LlmSettings
{
    public string ServerUrl { get; set; } = "http://llm-server:8000";
    public string ModelId { get; set; } = "okla-llama3-8b";
    public string LanguageCode { get; set; } = "es";
    public int TimeoutSeconds { get; set; } = 60;
    public float Temperature { get; set; } = 0.3f;
    public float TopP { get; set; } = 0.9f;
    public int MaxTokens { get; set; } = 600;
    public float RepetitionPenalty { get; set; } = 1.15f;
    public string SystemPrompt { get; set; } = "Eres el asistente virtual de OKLA Marketplace, la plataforma de compraventa de vehículos de República Dominicana. Responde en español dominicano, breve y amigable (2-4 oraciones). Ayuda con inventario, precios de referencia y financiamiento. NUNCA inventes datos de vehículos ni precios. NUNCA solicites cédula ni datos personales. Los precios son de referencia y no incluyen traspaso ni impuestos (DGII).";

    /// <summary>
    /// API Key para autenticación con proveedores externos (HuggingFace, OpenAI, Groq, etc.).
    /// Si está vacío, no se envía header Authorization (modo self-hosted llm-server).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Ruta del endpoint de chat completions. Default: /v1/chat/completions.
    /// HuggingFace Inference Endpoints usa esta misma ruta.
    /// </summary>
    public string CompletionsPath { get; set; } = "/v1/chat/completions";
}

/// <summary>
/// Servicio de inferencia LLM usando llama.cpp server.
/// Implementa el patrón de resiliencia con Polly (retry + circuit breaker).
/// </summary>
public class LlmService : ILlmService
{
    private readonly LlmSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IChatMessageRepository _messageRepository;
    private readonly ILogger<LlmService> _logger;
    private readonly ChatbotMetrics _metrics;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
    private DateTime? _lastSuccessfulCall;
    private int _failedCallsLast24h;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LlmService(
        IOptions<LlmSettings> settings,
        IHttpClientFactory httpClientFactory,
        IChatMessageRepository messageRepository,
        ILogger<LlmService> logger,
        ChatbotMetrics metrics)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _messageRepository = messageRepository;
        _logger = logger;
        _metrics = metrics;

        // NO retries — LLM inference takes 2-5 min on CPU.
        // Retrying would send duplicate requests and crash the server.
        _retryPolicy = Policy.NoOpAsync();

        // Circuit breaker: abre después de 10 fallos, espera 2 minutos
        _circuitBreakerPolicy = Policy.Handle<Exception>()
            .CircuitBreakerAsync(10, TimeSpan.FromMinutes(2),
                (exception, duration) =>
                {
                    _logger.LogError(exception, "LLM circuit breaker opened for {Duration}s", duration.TotalSeconds);
                    _metrics.RecordCircuitBreakerTrip();
                },
                () => _logger.LogInformation("LLM circuit breaker reset"));
    }

    public async Task<LlmDetectionResult> GenerateResponseAsync(
        string sessionId,
        string text,
        string? languageCode = null,
        string? systemPrompt = null,
        CancellationToken ct = default)
    {
        var result = new LlmDetectionResult
        {
            QueryText = text,
            LanguageCode = languageCode ?? _settings.LanguageCode
        };

        var sw = Stopwatch.StartNew();

        // Usar un CancellationToken separado basado en TimeoutSeconds para que
        // la llamada al LLM NO se cancele si el cliente HTTP (browser) desconecta.
        // Esto es importante porque inferencia CPU toma 2-5 minutos.
        using var llmCts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        try
        {
            return await _circuitBreakerPolicy.ExecuteAsync(async () =>
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    var client = _httpClientFactory.CreateClient("LlmServer");

                    // Construir mensajes con contexto de sesión
                    // Use per-dealer system prompt if provided, otherwise fall back to settings
                    var effectiveSystemPrompt = !string.IsNullOrWhiteSpace(systemPrompt) 
                        ? systemPrompt 
                        : _settings.SystemPrompt;
                    var messages = new List<LlmChatMessage>
                    {
                        new() { Role = "system", Content = effectiveSystemPrompt }
                    };

                    // Agregar historial de conversación para contexto
                    var history = await GetSessionContextAsync(sessionId, 6, llmCts.Token);
                    messages.AddRange(history);

                    // Agregar mensaje actual del usuario
                    messages.Add(new LlmChatMessage { Role = "user", Content = text });

                    // ──────────────────────────────────────────────────────────
                    // TOKEN BUDGET MANAGEMENT (REC-7)
                    // Rough estimate: 1 token ≈ 4 chars (Spanish).
                    // Context window = 4096 tokens. Reserve MaxTokens for output.
                    // If prompt exceeds budget, trim history (keep system prompt).
                    // ──────────────────────────────────────────────────────────
                    const int CONTEXT_WINDOW = 4096;
                    var maxPromptTokens = CONTEXT_WINDOW - _settings.MaxTokens;
                    var estimatedPromptTokens = messages.Sum(m => m.Content.Length / 4);

                    if (estimatedPromptTokens > maxPromptTokens)
                    {
                        _logger.LogWarning(
                            "Token budget exceeded: ~{Estimated} > {Max}. Trimming history.",
                            estimatedPromptTokens, maxPromptTokens);

                        // Keep system prompt (index 0) and user message (last)
                        // Remove oldest history messages until within budget
                        while (messages.Count > 2 && estimatedPromptTokens > maxPromptTokens)
                        {
                            var removed = messages[1]; // First history message after system
                            estimatedPromptTokens -= removed.Content.Length / 4;
                            messages.RemoveAt(1);
                        }
                    }

                    // Request al servidor llama.cpp (OpenAI-compatible)
                    var request = new
                    {
                        model = _settings.ModelId,
                        messages = messages.Select(m => new { role = m.Role, content = m.Content }),
                        temperature = _settings.Temperature,
                        top_p = _settings.TopP,
                        max_tokens = _settings.MaxTokens,
                        repetition_penalty = _settings.RepetitionPenalty,
                        stop = new[] { "</s>", "<|eot_id|>" }
                    };

                    // Agregar Authorization header si hay ApiKey configurada
                    // (HuggingFace, OpenAI, Groq, etc.)
                    var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.CompletionsPath);
                    if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
                    {
                        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
                    }
                    httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

                    var response = await client.SendAsync(httpRequest, llmCts.Token);
                    response.EnsureSuccessStatusCode();

                    var completion = await response.Content.ReadFromJsonAsync<LlmCompletionResponse>(_jsonOptions, llmCts.Token);
                    sw.Stop();

                    if (completion?.Choices?.FirstOrDefault()?.Message?.Content is string content)
                    {
                        // Intentar parsear respuesta JSON del modelo
                        var parsed = ParseLlmResponse(content);

                        result.FulfillmentText = parsed.Response ?? content;
                        result.DetectedIntent = parsed.Intent ?? "general_response";
                        result.ConfidenceScore = parsed.Confidence ?? 0.8f;

                        // Use model's trained isFallback signal instead of recalculating
                        // The model was fine-tuned to output isFallback accurately
                        result.IsFallback = parsed.IsFallback ?? (parsed.Intent == null || parsed.Intent == "fallback");

                        result.ResponseId = completion.Id;
                        result.TokensUsed = completion.Usage?.TotalTokens ?? 0;
                        result.ResponseTimeMs = sw.ElapsedMilliseconds;
                        result.LeadSignals = parsed.LeadSignals;
                        result.SuggestedAction = parsed.SuggestedAction;
                        result.QuickReplies = parsed.QuickReplies;

                        if (parsed.Parameters != null)
                            result.Parameters = parsed.Parameters;
                    }
                    else
                    {
                        result.IsFallback = true;
                        result.FulfillmentText = GetIntelligentFallback(text);
                    }

                    _lastSuccessfulCall = DateTime.UtcNow;
                    _logger.LogInformation(
                        "LLM response: intent={Intent}, confidence={Confidence:F2}, tokens={Tokens}, time={Time}ms",
                        result.DetectedIntent, result.ConfidenceScore, result.TokensUsed, sw.ElapsedMilliseconds);

                    _metrics.RecordLlmCall(success: true, responseTimeMs: sw.ElapsedMilliseconds, tokensUsed: result.TokensUsed);

                    return result;
                }));
        }
        catch (Exception ex)
        {
            sw.Stop();
            _failedCallsLast24h++;
            _logger.LogError(ex, "Failed to get LLM response for session {SessionId}", sessionId);
            _metrics.RecordLlmCall(success: false, responseTimeMs: sw.ElapsedMilliseconds);
            result.IsFallback = true;
            result.FulfillmentText = GetIntelligentFallback(text);
            result.ResponseTimeMs = sw.ElapsedMilliseconds;
            return result;
        }
    }

    /// <summary>
    /// Returns a context-aware fallback response based on the user's message.
    /// Instead of a generic "try again", attempts to route the user appropriately.
    /// </summary>
    private static string GetIntelligentFallback(string userMessage)
    {
        var lower = userMessage.ToLowerInvariant();

        // Pricing / budget related
        if (lower.Contains("precio") || lower.Contains("cuanto") || lower.Contains("cuesta") ||
            lower.Contains("presupuesto") || lower.Contains("financ"))
            return "No pude procesar tu consulta de precio en este momento. " +
                   "¿Podrías indicarme tu presupuesto aproximado y el tipo de vehículo que te interesa? " +
                   "Así puedo ayudarte mejor cuando me recupere. 💰";

        // Vehicle search
        if (lower.Contains("suv") || lower.Contains("sedan") || lower.Contains("camioneta") ||
            lower.Contains("carro") || lower.Contains("yipeta") || lower.Contains("pickup"))
            return "Estoy teniendo dificultades técnicas, pero tenemos un amplio inventario. " +
                   "¿Podrías intentar de nuevo en unos segundos? Mientras tanto, puedes revisar " +
                   "nuestro catálogo en la sección de vehículos. 🚗";

        // Contact / appointment
        if (lower.Contains("contacto") || lower.Contains("llamar") || lower.Contains("visitar") ||
            lower.Contains("cita") || lower.Contains("agente") || lower.Contains("persona"))
            return "No pude procesar tu mensaje, pero puedo transferirte a un agente humano. " +
                   "¿Te gustaría hablar con un asesor de ventas directamente? 📞";

        // Default
        return "Disculpa, estoy experimentando dificultades técnicas momentáneas. " +
               "¿Podrías reformular tu pregunta? Estoy aquí para ayudarte con " +
               "información sobre vehículos, precios y financiamiento. 😊";
    }

    public async Task<LlmModelInfo> GetModelInfoAsync(CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LlmServer");
            var response = await client.GetFromJsonAsync<LlmModelInfoResponse>("/info", _jsonOptions, ct);

            return new LlmModelInfo
            {
                ModelId = response?.ModelId,
                ModelPath = response?.ModelPath,
                ModelType = response?.ModelType,
                Quantization = response?.Quantization,
                ContextLength = response?.ContextLength ?? 2048,
                GpuLayers = response?.GpuLayers ?? 0,
                Threads = response?.NThreads ?? 4,
                BaseModel = response?.BaseModel,
                FineTunedFor = response?.FineTunedFor,
                IsHealthy = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get LLM model info");
            return new LlmModelInfo { IsHealthy = false, HealthMessage = ex.Message };
        }
    }

    public async Task<bool> TestConnectivityAsync(CancellationToken ct = default)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var client = _httpClientFactory.CreateClient("LlmServer");
            var response = await client.GetAsync("/health", ct);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("LLM connectivity test passed in {Ms}ms", sw.ElapsedMilliseconds);
                _lastSuccessfulCall = DateTime.UtcNow;
                return true;
            }

            _logger.LogWarning("LLM health check returned {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM connectivity test failed");
            return false;
        }
    }

    public async Task<LlmHealthStatus> GetHealthStatusAsync(CancellationToken ct = default)
    {
        var status = new LlmHealthStatus
        {
            IsConnected = _lastSuccessfulCall.HasValue && _lastSuccessfulCall.Value > DateTime.UtcNow.AddMinutes(-5),
            LastSuccessfulCall = _lastSuccessfulCall,
            FailedCallsLast24h = _failedCallsLast24h
        };

        try
        {
            var client = _httpClientFactory.CreateClient("LlmServer");
            var response = await client.GetFromJsonAsync<LlmHealthResponse>("/health", _jsonOptions, ct);
            if (response != null)
            {
                status.IsConnected = response.Status == "healthy";
                status.ModelLoaded = response.ModelLoaded;
                status.UptimeSeconds = response.UptimeSeconds;
                status.TotalRequests = response.TotalRequests;
                status.AvgResponseTimeMs = response.AvgResponseTimeMs;
                status.ModelInfo = await GetModelInfoAsync(ct);
                _lastSuccessfulCall = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            status.IsConnected = false;
            status.LastError = ex.Message;
        }

        return status;
    }

    public async Task<IEnumerable<LlmChatMessage>> GetSessionContextAsync(
        string sessionId,
        int maxMessages = 10,
        CancellationToken ct = default)
    {
        try
        {
            var messages = await _messageRepository.GetRecentBySessionTokenAsync(sessionId, maxMessages, ct);
            return messages.Select(m => new LlmChatMessage
            {
                Role = m.IsFromBot ? "assistant" : "user",
                Content = m.IsFromBot ? (m.BotResponse ?? m.Content) : m.Content
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get session context for {SessionId}", sessionId);
            return Enumerable.Empty<LlmChatMessage>();
        }
    }

    // ======================================================================
    // PARSING del JSON de respuesta del modelo LLM fine-tuned
    // ======================================================================

    private LlmParsedResponse ParseLlmResponse(string rawContent)
    {
        // ── 1. Try full JSON parse ──────────────────────────────────────
        try
        {
            var jsonStart = rawContent.IndexOf('{');
            var jsonEnd = rawContent.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = rawContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<LlmParsedResponse>(jsonStr, _jsonOptions);
                if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Response))
                    return parsed;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Could not parse LLM response as JSON, trying truncated extraction");
        }

        // ── 2. Handle TRUNCATED JSON (max_tokens cut mid-response) ─────
        // LLM may output: {"response": "¡Excelente!... (truncated, no closing })
        // Use regex to extract the "response" value.
        try
        {
            var responseMatch = System.Text.RegularExpressions.Regex.Match(
                rawContent,
                @"""response""\s*:\s*""((?:[^""\\]|\\.)*)""?",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            if (responseMatch.Success && responseMatch.Groups[1].Length > 0)
            {
                var extractedText = responseMatch.Groups[1].Value
                    .Replace("\\n", "\n")
                    .Replace("\\\"", "\"")
                    .TrimEnd();

                _logger.LogInformation("Extracted response from truncated JSON ({Length} chars)", extractedText.Length);
                return new LlmParsedResponse { Response = extractedText };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Regex extraction of truncated JSON failed");
        }

        // ── 3. Fallback: strip any JSON prefix and use raw text ────────
        var cleaned = rawContent.Trim();
        // Remove leading {"response": " if present but incomplete
        var prefixPattern = new System.Text.RegularExpressions.Regex(@"^\{\s*""response""\s*:\s*""");
        if (prefixPattern.IsMatch(cleaned))
        {
            cleaned = prefixPattern.Replace(cleaned, "");
            // Also remove trailing "} if present
            cleaned = cleaned.TrimEnd().TrimEnd('"').TrimEnd('}').TrimEnd('"').TrimEnd();
        }

        return new LlmParsedResponse { Response = cleaned };
    }

    // ======================================================================
    // DTOs internos para comunicación con el servidor llama.cpp
    // ======================================================================

    private class LlmCompletionResponse
    {
        public string? Id { get; set; }
        [JsonPropertyName("object")]
        public string? ObjectType { get; set; }
        public int Created { get; set; }
        public string? Model { get; set; }
        public List<LlmChoice>? Choices { get; set; }
        public LlmUsage? Usage { get; set; }
    }

    private class LlmChoice
    {
        public int Index { get; set; }
        public LlmMessageDto? Message { get; set; }
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private class LlmMessageDto
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    private class LlmUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    private class LlmHealthResponse
    {
        public string Status { get; set; } = string.Empty;
        [JsonPropertyName("model_loaded")]
        public bool ModelLoaded { get; set; }
        [JsonPropertyName("model_path")]
        public string? ModelPath { get; set; }
        [JsonPropertyName("uptime_seconds")]
        public double UptimeSeconds { get; set; }
        [JsonPropertyName("total_requests")]
        public int TotalRequests { get; set; }
        [JsonPropertyName("avg_response_time_ms")]
        public double AvgResponseTimeMs { get; set; }
    }

    private class LlmModelInfoResponse
    {
        [JsonPropertyName("model_id")]
        public string? ModelId { get; set; }
        [JsonPropertyName("model_path")]
        public string? ModelPath { get; set; }
        [JsonPropertyName("model_type")]
        public string? ModelType { get; set; }
        public string? Quantization { get; set; }
        [JsonPropertyName("context_length")]
        public int ContextLength { get; set; }
        [JsonPropertyName("gpu_layers")]
        public int GpuLayers { get; set; }
        [JsonPropertyName("n_threads")]
        public int NThreads { get; set; }
        [JsonPropertyName("base_model")]
        public string? BaseModel { get; set; }
        [JsonPropertyName("fine_tuned_for")]
        public string? FineTunedFor { get; set; }
    }

    /// <summary>
    /// Internal DTO matching the FULL 8-field JSON schema the model was fine-tuned to output:
    /// { response, intent, confidence, isFallback, parameters, leadSignals, suggestedAction, quickReplies }
    /// </summary>
    private class LlmParsedResponse
    {
        public string? Response { get; set; }
        public string? Intent { get; set; }
        public float? Confidence { get; set; }
        [JsonPropertyName("isFallback")]
        public bool? IsFallback { get; set; }
        public Dictionary<string, string>? Parameters { get; set; }
        [JsonPropertyName("leadSignals")]
        public LlmLeadSignals? LeadSignals { get; set; }
        [JsonPropertyName("suggestedAction")]
        public string? SuggestedAction { get; set; }
        [JsonPropertyName("quickReplies")]
        public List<string>? QuickReplies { get; set; }
    }
}
