using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SearchAgent.Application.DTOs;
using SearchAgent.Domain.Interfaces;

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

    public ClaudeSearchService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ClaudeSearchService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["Claude:ApiKey"]
            ?? throw new InvalidOperationException("Claude API key not configured. Set 'Claude:ApiKey' in configuration.");
    }

    public async Task<SearchAgentResponse> ProcessQueryAsync(
        string userQuery,
        string systemPrompt,
        float temperature,
        int maxTokens,
        CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("ClaudeApi");

        var requestBody = new ClaudeRequest
        {
            Model = "claude-haiku-4-5-20251001",
            MaxTokens = maxTokens,
            Temperature = temperature,
            System = systemPrompt,
            Messages =
            [
                new ClaudeMessage { Role = "user", Content = userQuery }
            ]
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var requestJson = JsonSerializer.Serialize(requestBody, jsonOptions);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        // Set Anthropic-specific headers
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = content;

        _logger.LogDebug("Sending query to Claude: {Query}", userQuery);

        var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Claude API error {StatusCode}: {Error}", response.StatusCode, errorBody);
            throw new HttpRequestException($"Claude API returned {response.StatusCode}: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseBody, jsonOptions);

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
                "Claude processed query successfully. Confidence={Confidence}, Level={Level}",
                searchResponse.Confianza, searchResponse.NivelFiltrosActivo);

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

    [JsonPropertyName("system")]
    public string System { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<ClaudeMessage> Messages { get; set; } = [];
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
}

#endregion
