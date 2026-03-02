using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models;

namespace ChatbotService.Infrastructure.Services;

/// <summary>
/// Servicio de inferencia LLM usando Anthropic Claude API.
/// Implementa el DealerChatAgent con Claude Sonnet 4.5.
/// </summary>
public class ClaudeLlmService : ILlmService
{
    private readonly LlmSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IChatMessageRepository _messageRepository;
    private readonly ILogger<ClaudeLlmService> _logger;
    private readonly ChatbotMetrics _metrics;
    private DateTime? _lastSuccessfulCall;
    private int _failedCallsLast24h;

    private const string ANTHROPIC_VERSION = "2023-06-01";
    private const int CONTEXT_WINDOW = 200000; // Claude Sonnet 4.5 — 200K tokens

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions _snakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ClaudeLlmService(
        IOptions<LlmSettings> settings,
        IHttpClientFactory httpClientFactory,
        IChatMessageRepository messageRepository,
        ILogger<ClaudeLlmService> logger,
        ChatbotMetrics metrics)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _messageRepository = messageRepository;
        _logger = logger;
        _metrics = metrics;
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
        using var llmCts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        try
        {
            var client = _httpClientFactory.CreateClient("LlmServer");

            // Build effective system prompt
            var effectiveSystemPrompt = !string.IsNullOrWhiteSpace(systemPrompt)
                ? systemPrompt
                : _settings.SystemPrompt;

            // Build conversation messages (without system — Claude uses top-level system field)
            var messages = new List<ClaudeMessage>();

            // Add session history for context
            var history = await GetSessionContextAsync(sessionId, 10, llmCts.Token);
            foreach (var msg in history)
            {
                messages.Add(new ClaudeMessage { Role = msg.Role, Content = msg.Content });
            }

            // Add current user message
            messages.Add(new ClaudeMessage { Role = "user", Content = text });

            // ──────────────────────────────────────────────────────────
            // TOKEN BUDGET MANAGEMENT
            // Claude Sonnet 4.5 has 200K context. Reserve MaxTokens for output.
            // Rough estimate: 1 token ≈ 4 chars (Spanish).
            // ──────────────────────────────────────────────────────────
            var maxPromptTokens = CONTEXT_WINDOW - _settings.MaxTokens;
            var estimatedPromptTokens = messages.Sum(m => m.Content.Length / 4) +
                                        (effectiveSystemPrompt.Length / 4);

            if (estimatedPromptTokens > maxPromptTokens)
            {
                _logger.LogWarning(
                    "Token budget exceeded: ~{Estimated} > {Max}. Trimming history.",
                    estimatedPromptTokens, maxPromptTokens);

                while (messages.Count > 1 && estimatedPromptTokens > maxPromptTokens)
                {
                    var removed = messages[0];
                    estimatedPromptTokens -= removed.Content.Length / 4;
                    messages.RemoveAt(0);
                }
            }

            // Ensure first message is from user (Anthropic requirement)
            if (messages.Count > 0 && messages[0].Role == "assistant")
            {
                messages.RemoveAt(0);
            }

            // Build Anthropic API request
            var request = new ClaudeRequest
            {
                Model = _settings.ModelId,
                MaxTokens = _settings.MaxTokens,
                System = effectiveSystemPrompt,
                Messages = messages,
                Temperature = _settings.Temperature
            };

            // Create HTTP request with Anthropic headers
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.CompletionsPath);
            httpRequest.Headers.Add("x-api-key", _settings.ApiKey);
            httpRequest.Headers.Add("anthropic-version", ANTHROPIC_VERSION);
            httpRequest.Content = JsonContent.Create(request, options: _snakeCaseOptions);

            var response = await client.SendAsync(httpRequest, llmCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(llmCts.Token);
                _logger.LogError("Claude API error {StatusCode}: {Error}", response.StatusCode, errorBody);
                throw new HttpRequestException($"Claude API returned {response.StatusCode}: {errorBody}");
            }

            var completion = await response.Content.ReadFromJsonAsync<ClaudeResponse>(_snakeCaseOptions, llmCts.Token);
            sw.Stop();

            var rawContent = completion?.Content?.FirstOrDefault(c => c.Type == "text")?.Text;

            if (!string.IsNullOrWhiteSpace(rawContent))
            {
                // Parse the JSON response from Claude
                var parsed = ParseClaudeResponse(rawContent);

                result.FulfillmentText = parsed.Response ?? rawContent;
                result.DetectedIntent = parsed.Intent ?? "general_response";
                result.ConfidenceScore = parsed.Confidence ?? 0.85f;
                result.IsFallback = parsed.IsFallback ?? false;
                result.ResponseId = completion?.Id;
                result.TokensUsed = (completion?.Usage?.InputTokens ?? 0) + (completion?.Usage?.OutputTokens ?? 0);
                result.ResponseTimeMs = sw.ElapsedMilliseconds;
                result.LeadSignals = parsed.LeadSignals;
                result.SuggestedAction = parsed.SuggestedAction;
                result.QuickReplies = parsed.QuickReplies;

                // DealerChatAgent intent scoring
                result.IntentScore = parsed.IntentScore ?? 1;
                result.Clasificacion = parsed.Clasificacion ?? "curioso";
                result.ModuloActivo = parsed.ModuloActivo ?? "qa";
                result.VehiculoInteresId = parsed.VehiculoInteresId;
                result.HandoffActivado = parsed.HandoffActivado ?? false;
                result.RazonHandoff = parsed.RazonHandoff;
                result.TemasConsulta = parsed.TemasConsulta ?? new List<string>();
                result.CitaPropuesta = parsed.CitaPropuesta;

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
                "Claude response: intent={Intent}, score={IntentScore}, clasificacion={Clasificacion}, tokens={Tokens}, time={Time}ms",
                result.DetectedIntent, result.IntentScore, result.Clasificacion,
                result.TokensUsed, sw.ElapsedMilliseconds);

            _metrics.RecordLlmCall(success: true, responseTimeMs: sw.ElapsedMilliseconds, tokensUsed: result.TokensUsed);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _failedCallsLast24h++;
            _logger.LogError(ex, "Failed to get Claude response for session {SessionId}", sessionId);
            _metrics.RecordLlmCall(success: false, responseTimeMs: sw.ElapsedMilliseconds);
            result.IsFallback = true;
            result.FulfillmentText = GetIntelligentFallback(text);
            result.ResponseTimeMs = sw.ElapsedMilliseconds;
            return result;
        }
    }

    /// <summary>
    /// Returns a context-aware fallback response.
    /// </summary>
    private static string GetIntelligentFallback(string userMessage)
    {
        var lower = userMessage.ToLowerInvariant();

        if (lower.Contains("precio") || lower.Contains("cuanto") || lower.Contains("cuesta") ||
            lower.Contains("presupuesto") || lower.Contains("financ"))
            return "No pude procesar tu consulta de precio en este momento. " +
                   "¿Podrías indicarme tu presupuesto aproximado y el tipo de vehículo que te interesa? 💰";

        if (lower.Contains("suv") || lower.Contains("sedan") || lower.Contains("camioneta") ||
            lower.Contains("carro") || lower.Contains("yipeta") || lower.Contains("pickup"))
            return "Estoy teniendo dificultades técnicas, pero tenemos un amplio inventario. " +
                   "¿Podrías intentar de nuevo en unos segundos? 🚗";

        if (lower.Contains("contacto") || lower.Contains("llamar") || lower.Contains("visitar") ||
            lower.Contains("cita") || lower.Contains("agente") || lower.Contains("persona"))
            return "No pude procesar tu mensaje, pero puedo transferirte a un agente humano. " +
                   "¿Te gustaría hablar con un asesor de ventas directamente? 📞";

        if (lower.Contains("disponible") || lower.Contains("hoy") || lower.Contains("ver") ||
            lower.Contains("ir") || lower.Contains("visita"))
            return "Disculpa, tuve un problema técnico. ¿Quieres que te agende una visita para ver el vehículo? 🏪";

        return "Disculpa, estoy experimentando dificultades técnicas momentáneas. " +
               "¿Podrías reformular tu pregunta? Estoy aquí para ayudarte. 😊";
    }

    public Task<LlmModelInfo> GetModelInfoAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new LlmModelInfo
        {
            ModelId = _settings.ModelId,
            ModelType = "Claude Sonnet 4.5",
            ContextLength = CONTEXT_WINDOW,
            BaseModel = "Anthropic Claude",
            FineTunedFor = "DealerChatAgent - OKLA Marketplace",
            IsHealthy = _lastSuccessfulCall.HasValue &&
                        _lastSuccessfulCall.Value > DateTime.UtcNow.AddMinutes(-5)
        });
    }

    public async Task<bool> TestConnectivityAsync(CancellationToken ct = default)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var client = _httpClientFactory.CreateClient("LlmServer");

            var request = new ClaudeRequest
            {
                Model = _settings.ModelId,
                MaxTokens = 10,
                System = "Respond with just 'ok'.",
                Messages = new List<ClaudeMessage>
                {
                    new() { Role = "user", Content = "ping" }
                },
                Temperature = 0
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.CompletionsPath);
            httpRequest.Headers.Add("x-api-key", _settings.ApiKey);
            httpRequest.Headers.Add("anthropic-version", ANTHROPIC_VERSION);
            httpRequest.Content = JsonContent.Create(request, options: _snakeCaseOptions);

            var response = await client.SendAsync(httpRequest, ct);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Claude connectivity test passed in {Ms}ms", sw.ElapsedMilliseconds);
                _lastSuccessfulCall = DateTime.UtcNow;
                return true;
            }

            _logger.LogWarning("Claude health check returned {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude connectivity test failed");
            return false;
        }
    }

    public async Task<LlmHealthStatus> GetHealthStatusAsync(CancellationToken ct = default)
    {
        var isConnected = await TestConnectivityAsync(ct);

        return new LlmHealthStatus
        {
            IsConnected = isConnected,
            LastSuccessfulCall = _lastSuccessfulCall,
            FailedCallsLast24h = _failedCallsLast24h,
            ModelLoaded = true,
            ModelInfo = await GetModelInfoAsync(ct)
        };
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
    // PARSING — Extract structured JSON from Claude's response
    // ======================================================================

    private DealerChatAgentResponse ParseClaudeResponse(string rawContent)
    {
        // 1. Try full JSON parse
        try
        {
            var jsonStart = rawContent.IndexOf('{');
            var jsonEnd = rawContent.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = rawContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<DealerChatAgentResponse>(jsonStr, _snakeCaseOptions);
                if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Response))
                    return parsed;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Could not parse Claude response as JSON, trying regex extraction");
        }

        // 2. Handle truncated JSON — extract "response" field
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
                return new DealerChatAgentResponse { Response = extractedText };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Regex extraction failed");
        }

        // 3. Fallback: use raw text as response
        var cleaned = rawContent.Trim();
        var prefixPattern = new System.Text.RegularExpressions.Regex(@"^\{\s*""response""\s*:\s*""");
        if (prefixPattern.IsMatch(cleaned))
        {
            cleaned = prefixPattern.Replace(cleaned, "");
            cleaned = cleaned.TrimEnd().TrimEnd('"').TrimEnd('}').TrimEnd('"').TrimEnd();
        }

        return new DealerChatAgentResponse { Response = cleaned };
    }

    // ======================================================================
    // DTOs internos para comunicación con Anthropic Claude API
    // ======================================================================

    private class ClaudeRequest
    {
        public string Model { get; set; } = string.Empty;
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
        public string System { get; set; } = string.Empty;
        public List<ClaudeMessage> Messages { get; set; } = new();
        public float Temperature { get; set; }
    }

    private class ClaudeMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private class ClaudeResponse
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Role { get; set; }
        public List<ClaudeContentBlock>? Content { get; set; }
        public string? Model { get; set; }
        [JsonPropertyName("stop_reason")]
        public string? StopReason { get; set; }
        public ClaudeUsage? Usage { get; set; }
    }

    private class ClaudeContentBlock
    {
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    private class ClaudeUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }
        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }

    /// <summary>
    /// Internal DTO for the DealerChatAgent JSON output schema.
    /// Claude is prompted to return this structure in every turn.
    /// </summary>
    private class DealerChatAgentResponse
    {
        public string? Response { get; set; }
        public string? Intent { get; set; }
        public float? Confidence { get; set; }
        [JsonPropertyName("is_fallback")]
        public bool? IsFallback { get; set; }
        public Dictionary<string, string>? Parameters { get; set; }
        [JsonPropertyName("lead_signals")]
        public LlmLeadSignals? LeadSignals { get; set; }
        [JsonPropertyName("suggested_action")]
        public string? SuggestedAction { get; set; }
        [JsonPropertyName("quick_replies")]
        public List<string>? QuickReplies { get; set; }

        // DealerChatAgent-specific fields
        [JsonPropertyName("intent_score")]
        public int? IntentScore { get; set; }
        public string? Clasificacion { get; set; }
        [JsonPropertyName("modulo_activo")]
        public string? ModuloActivo { get; set; }
        [JsonPropertyName("vehiculo_interes_id")]
        public string? VehiculoInteresId { get; set; }
        [JsonPropertyName("handoff_activado")]
        public bool? HandoffActivado { get; set; }
        [JsonPropertyName("razon_handoff")]
        public string? RazonHandoff { get; set; }
        [JsonPropertyName("temas_consulta")]
        public List<string>? TemasConsulta { get; set; }
        [JsonPropertyName("cita_propuesta")]
        public CitaPropuestaDto? CitaPropuesta { get; set; }
    }
}

/// <summary>
/// DTO for appointment proposal from the DealerChatAgent
/// </summary>
public class CitaPropuestaDto
{
    [JsonPropertyName("tipo")]
    public string? Tipo { get; set; }
    [JsonPropertyName("dia_propuesto")]
    public string? DiaPropuesto { get; set; }
    [JsonPropertyName("hora_propuesta")]
    public string? HoraPropuesta { get; set; }
}
