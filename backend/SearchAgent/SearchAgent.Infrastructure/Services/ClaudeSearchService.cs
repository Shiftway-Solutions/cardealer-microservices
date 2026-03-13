using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SearchAgent.Domain.Interfaces;
using SearchAgent.Domain.Models;

namespace SearchAgent.Infrastructure.Services;

/// <summary>
/// Claude AI service that calls the Anthropic Messages API.
/// Sends user search queries and receives structured JSON filters.
/// </summary>
public class ClaudeSearchService : IClaudeSearchService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ClaudeSearchService> _logger;
    private readonly string _apiKey;

    // ── P1-2 FIX: Reuse static JsonSerializerOptions (avoid per-request allocation) ──
    private static readonly JsonSerializerOptions _snakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Docker secret file path for the Claude API key (mounted by compose/K8s).</summary>
    private const string DockerSecretPath = "/run/secrets/claude_api_key";

    public ClaudeSearchService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ClaudeSearchService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // Priority: 1) env var Claude__ApiKey / appsettings  2) Docker secret file
        var apiKey = configuration["Claude:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey) && File.Exists(DockerSecretPath))
        {
            apiKey = File.ReadAllText(DockerSecretPath).Trim();
            logger.LogInformation("Claude API key loaded from Docker secret file.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Claude API key not configured. Set 'Claude:ApiKey' env var or mount secret at /run/secrets/claude_api_key.");

        _apiKey = apiKey;
    }

    public async Task<SearchAgentResponse> ProcessQueryAsync(
        string userQuery,
        string systemPrompt,
        float temperature,
        int maxTokens,
        CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("ClaudeApi");

        var systemBlocks = new List<ClaudeSystemBlock>
        {
            new() { Text = systemPrompt, CacheControl = new ClaudeCacheControl() }
        };

        var requestBody = new ClaudeRequest
        {
            Model = "claude-haiku-4-5-20251001",
            MaxTokens = maxTokens,
            Temperature = temperature,
            System = systemBlocks,
            Messages =
            [
                new ClaudeMessage { Role = "user", Content = userQuery }
            ]
        };

        var requestJson = JsonSerializer.Serialize(requestBody, _snakeCaseOptions);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        // Set Anthropic-specific headers
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        // Enable prompt caching — search system prompt cached server-side
        request.Headers.Add("anthropic-beta", "prompt-caching-2024-07-31");
        request.Content = content;

        _logger.LogDebug("Sending query to Claude: {Query}", userQuery);

        var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(ct);

            // Transient errors: 429 Rate-Limited, 529 Overloaded (Anthropic-specific)
            // Return a zero-confidence response so callers can degrade gracefully
            // without propagating a 500 to the client.
            if (statusCode is 429 or 529)
            {
                _logger.LogWarning(
                    "Claude API temporarily unavailable ({StatusCode}). Returning zero-confidence fallback for query: {Query}",
                    statusCode, userQuery);

                return new SearchAgentResponse
                {
                    FiltrosExactos = null,
                    FiltrosRelajados = null,
                    Confianza = 0.0f,
                    ResultadoMinimoGarantizado = 0,
                    NivelFiltrosActivo = 0,
                    Advertencias = ["Servicio de IA temporalmente no disponible."],
                    MensajeUsuario = "La búsqueda inteligente está temporalmente ocupada. Por favor intenta de nuevo en unos segundos.",
                    QueryReformulada = userQuery,
                };
            }

            _logger.LogError("Claude API error {StatusCode}: {Error}", response.StatusCode, errorBody);
            throw new HttpRequestException($"Claude API returned {response.StatusCode}: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseBody, _snakeCaseOptions);

        if (claudeResponse?.Content == null || claudeResponse.Content.Count == 0)
        {
            _logger.LogWarning("Empty response from Claude for query: {Query}", userQuery);
            return CreateFallbackResponse(userQuery);
        }

        // Extract the JSON text from Claude's response
        var textContent = claudeResponse.Content.FirstOrDefault(c => c.Type == "text");
        if (textContent?.Text == null)
        {
            return CreateFallbackResponse(userQuery);
        }

        // Parse the JSON response from Claude
        try
        {
            // Claude may wrap JSON in markdown code blocks — strip them
            var jsonText = textContent.Text.Trim();
            if (jsonText.StartsWith("```"))
            {
                jsonText = jsonText
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();
            }

            var searchResponse = JsonSerializer.Deserialize<SearchAgentResponse>(jsonText);

            if (searchResponse == null)
            {
                _logger.LogWarning("Failed to parse Claude response as SearchAgentResponse");
                return CreateFallbackResponse(userQuery);
            }

            _logger.LogInformation(
                "Claude processed query successfully. Confidence={Confidence}, Level={Level}, CacheCreated={CacheCreated}, CacheRead={CacheRead}",
                searchResponse.Confianza, searchResponse.NivelFiltrosActivo,
                claudeResponse.Usage?.CacheCreationInputTokens ?? 0,
                claudeResponse.Usage?.CacheReadInputTokens ?? 0);

            return searchResponse;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Claude JSON response: {Response}", textContent.Text);
            return CreateFallbackResponse(userQuery);
        }
    }

    private static SearchAgentResponse CreateFallbackResponse(string query)
    {
        return new SearchAgentResponse
        {
            FiltrosExactos = new SearchFilters { Condicion = "usado" },
            FiltrosRelajados = new SearchFilters(),
            ResultadoMinimoGarantizado = 8,
            NivelFiltrosActivo = 5,
            PatrocinadosConfig = new SponsoredConfig
            {
                UmbralAfinidad = 0.45f,
                MaxPorcentajeResultados = 0.25f,
                PosicionesFijas = [1, 5, 10],
                Etiqueta = "Patrocinado"
            },
            OrdenarPor = "okla_score",
            Confianza = 0.1f,
            QueryReformulada = query,
            Advertencias = ["No se pudo procesar la consulta con IA. Mostrando resultados generales."],
            MensajeRelajamiento = "Mostrando los vehículos mejor puntuados disponibles."
        };
    }
}

#region Claude API Models

internal class ClaudeRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "claude-haiku-4-5-20251001";

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 1024;

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.2f;

    /// <summary>Structured system blocks — required format for Anthropic prompt caching.</summary>
    [JsonPropertyName("system")]
    public List<ClaudeSystemBlock> System { get; set; } = [];

    [JsonPropertyName("messages")]
    public List<ClaudeMessage> Messages { get; set; } = [];
}

/// <summary>
/// Represents a system content block with optional cache_control.
/// Send as array even for a single block so Anthropic can honour cache_control.
/// </summary>
internal class ClaudeSystemBlock
{
    [JsonPropertyName("type")] public string Type { get; set; } = "text";
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
    [JsonPropertyName("cache_control")] public ClaudeCacheControl? CacheControl { get; set; }
}

/// <summary>Ephemeral cache control — caches the block for ~5 min on Anthropic servers.</summary>
internal class ClaudeCacheControl
{
    [JsonPropertyName("type")] public string Type { get; set; } = "ephemeral";
}

internal class ClaudeMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

internal class ClaudeResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public List<ClaudeContentBlock> Content { get; set; } = [];

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("usage")]
    public ClaudeUsage? Usage { get; set; }
}

internal class ClaudeContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

internal class ClaudeUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }

    /// <summary>Tokens written to cache (first call). Non-zero = cache was CREATED.</summary>
    [JsonPropertyName("cache_creation_input_tokens")]
    public int CacheCreationInputTokens { get; set; }

    /// <summary>Tokens read from cache (subsequent calls). Non-zero = cache HIT.</summary>
    [JsonPropertyName("cache_read_input_tokens")]
    public int CacheReadInputTokens { get; set; }
}

#endregion
