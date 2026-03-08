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

            // Add session history for context with conversation prefix caching.
            // Mark the last history message with cache_control=ephemeral so Anthropic
            // caches the entire conversation prefix (system + history up to that point).
            // On multi-turn conversations this avoids re-tokenizing previous turns (~200ms savings).
            var history = await GetSessionContextAsync(sessionId, 10, llmCts.Token);
            var historyList = history.ToList();
            for (int i = 0; i < historyList.Count; i++)
            {
                var msg = historyList[i];
                if (i == historyList.Count - 1 && historyList.Count >= 4)
                {
                    // Last history message → structured content with cache_control for prefix caching
                    messages.Add(new ClaudeMessage
                    {
                        Role = msg.Role,
                        Content = new List<SystemPromptBlock>
                        {
                            new() { Type = "text", Text = msg.Content, CacheControl = new CacheControl { Type = "ephemeral" } }
                        }
                    });
                }
                else
                {
                    messages.Add(new ClaudeMessage { Role = msg.Role, Content = msg.Content });
                }
            }

            // Add current user message
            messages.Add(new ClaudeMessage { Role = "user", Content = text });

            // ──────────────────────────────────────────────────────────
            // TOKEN BUDGET MANAGEMENT
            // Claude Sonnet 4.5 has 200K context. Reserve MaxTokens for output.
            // Rough estimate: 1 token ≈ 4 chars (Spanish).
            // ──────────────────────────────────────────────────────────
            var maxPromptTokens = CONTEXT_WINDOW - _settings.MaxTokens;
            var estimatedPromptTokens = messages.Sum(m => EstimateContentTokens(m.Content)) +
                                        (effectiveSystemPrompt.Length / 4);

            if (estimatedPromptTokens > maxPromptTokens)
            {
                _logger.LogWarning(
                    "Token budget exceeded: ~{Estimated} > {Max}. Trimming history.",
                    estimatedPromptTokens, maxPromptTokens);

                while (messages.Count > 1 && estimatedPromptTokens > maxPromptTokens)
                {
                    var removed = messages[0];
                    estimatedPromptTokens -= EstimateContentTokens(removed.Content);
                    messages.RemoveAt(0);
                }
            }

            // Ensure first message is from user (Anthropic requirement)
            if (messages.Count > 0 && messages[0].Role == "assistant")
            {
                messages.RemoveAt(0);
            }

            // Build Anthropic API request with prompt caching
            // Uses BuildSystemBlocks() to split on <!-- CACHE_BREAK --> marker:
            //   Block 1 (static rules): cache_control = ephemeral → cached server-side for 5 min
            //   Block 2 (dynamic dealer/RAG context): no cache_control → re-tokenized each call
            var request = new ClaudeRequest
            {
                Model = _settings.ModelId,
                MaxTokens = _settings.MaxTokens,
                System = BuildSystemBlocks(effectiveSystemPrompt),
                Messages = messages,
                Temperature = _settings.Temperature
            };

            // Create HTTP request with Anthropic headers
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.CompletionsPath);
            httpRequest.Headers.Add("x-api-key", _settings.ApiKey);
            httpRequest.Headers.Add("anthropic-version", ANTHROPIC_VERSION);
            // Enable prompt caching — system prompt cached server-side for 5 min
            // Reduces ~90% cost on cached tokens and improves latency by ~200ms
            httpRequest.Headers.Add("anthropic-beta", "prompt-caching-2024-07-31");
            httpRequest.Content = JsonContent.Create(request, options: _snakeCaseOptions);

            var response = await client.SendAsync(httpRequest, llmCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(llmCts.Token);
                var statusCode = (int)response.StatusCode;
                
                // 429 (rate limit) and 529 (overloaded) are transient — graceful degradation
                if (statusCode == 429 || statusCode == 529)
                {
                    _logger.LogWarning(
                        "Claude API transient error {StatusCode}: {Error}. Returning fallback.",
                        statusCode, errorBody);
                    sw.Stop();
                    _metrics.RecordLlmCall(success: false, responseTimeMs: sw.ElapsedMilliseconds);
                    result.IsFallback = true;
                    result.FulfillmentText = GetIntelligentFallback(text);
                    result.ResponseTimeMs = sw.ElapsedMilliseconds;
                    return result;
                }

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
                "Claude response: intent={Intent}, score={IntentScore}, clasificacion={Clasificacion}, tokens={Tokens}, time={Time}ms, cache_read={CacheRead}, cache_write={CacheWrite}",
                result.DetectedIntent, result.IntentScore, result.Clasificacion,
                result.TokensUsed, sw.ElapsedMilliseconds,
                completion?.Usage?.CacheReadInputTokens ?? 0,
                completion?.Usage?.CacheCreationInputTokens ?? 0);

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
    /// Estimates token count for a message content (string or structured blocks).
    /// Rough estimate: 1 token ≈ 4 chars (Spanish).
    /// </summary>
    private static int EstimateContentTokens(object content) => content switch
    {
        string s => s.Length / 4,
        IList<SystemPromptBlock> blocks => blocks.Sum(b => b.Text.Length) / 4,
        _ => content.ToString()?.Length / 4 ?? 0
    };

    /// <summary>
    /// Splits a system prompt on the <!-- CACHE_BREAK --> marker into two SystemPromptBlocks.
    /// The static portion (rules, personality, legal) gets cache_control=ephemeral for
    /// Anthropic server-side caching (reused across ALL dealers, ~75% token cost savings).
    /// The dynamic portion (dealer info, RAG context) is re-tokenized each call.
    /// If no marker exists, caches the entire prompt as a single block.
    /// </summary>
    private static object BuildSystemBlocks(string systemPrompt)
    {
        const string CACHE_BREAK_MARKER = "<!-- CACHE_BREAK -->";
        var markerIndex = systemPrompt.IndexOf(CACHE_BREAK_MARKER, StringComparison.Ordinal);

        if (markerIndex >= 0)
        {
            var staticPart = systemPrompt[..markerIndex].TrimEnd();
            var dynamicPart = systemPrompt[(markerIndex + CACHE_BREAK_MARKER.Length)..].TrimStart();

            return new List<SystemPromptBlock>
            {
                new() { Type = "text", Text = staticPart, CacheControl = new CacheControl { Type = "ephemeral" } },
                new() { Type = "text", Text = dynamicPart }
            };
        }

        // No marker found — cache the entire prompt (GeneralChat, SingleVehicle modes)
        return new List<SystemPromptBlock>
        {
            new() { Type = "text", Text = systemPrompt, CacheControl = new CacheControl { Type = "ephemeral" } }
        };
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
        /// <summary>
        /// System prompt — supports both string and structured format for prompt caching.
        /// Using structured format with cache_control for Anthropic prompt caching.
        /// </summary>
        public object System { get; set; } = string.Empty;
        public List<ClaudeMessage> Messages { get; set; } = new();
        public float Temperature { get; set; }
    }

    /// <summary>
    /// Structured system prompt block with cache control for Anthropic prompt caching.
    /// </summary>
    private class SystemPromptBlock
    {
        public string Type { get; set; } = "text";
        public string Text { get; set; } = string.Empty;
        [JsonPropertyName("cache_control")]
        public CacheControl? CacheControl { get; set; }
    }

    private class CacheControl
    {
        public string Type { get; set; } = "ephemeral";
    }

    private class ClaudeMessage
    {
        public string Role { get; set; } = string.Empty;
        /// <summary>
        /// Content can be a plain string or a List&lt;SystemPromptBlock&gt; for messages
        /// that need cache_control (conversation prefix caching).
        /// </summary>
        public object Content { get; set; } = string.Empty;
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
        /// <summary>
        /// Tokens written to Anthropic's server-side prompt cache on this request.
        /// Non-zero means a new cache entry was created (costs 25% more than base input).
        /// </summary>
        [JsonPropertyName("cache_creation_input_tokens")]
        public int CacheCreationInputTokens { get; set; }
        /// <summary>
        /// Tokens read from Anthropic's server-side prompt cache on this request.
        /// Non-zero means cache HIT — these tokens cost 90% less than base input.
        /// High ratio of CacheRead/Input = good cache utilization.
        /// </summary>
        [JsonPropertyName("cache_read_input_tokens")]
        public int CacheReadInputTokens { get; set; }
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
