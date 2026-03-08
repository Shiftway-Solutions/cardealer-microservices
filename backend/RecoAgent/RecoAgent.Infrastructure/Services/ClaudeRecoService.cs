using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecoAgent.Domain.Interfaces;

namespace RecoAgent.Infrastructure.Services;

/// <summary>
/// Claude AI service that calls the Anthropic Messages API with Sonnet 4.5.
/// Sends user profiles and vehicle candidates, receives raw JSON string for deserialization by caller.
/// </summary>
public class ClaudeRecoService : IClaudeRecoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ClaudeRecoService> _logger;
    private readonly string _apiKey;

    // ── Reuse static JsonSerializerOptions (avoid per-request allocation) ──
    private static readonly JsonSerializerOptions _snakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ClaudeRecoService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ClaudeRecoService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["Claude:ApiKey"]
            ?? throw new InvalidOperationException("Claude API key not configured. Set 'Claude:ApiKey' in configuration.");
    }

    public async Task<string> GenerateRecommendationsAsync(
        string userProfileJson,
        string systemPrompt,
        float temperature,
        int maxTokens,
        CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("ClaudeApi");

        var requestBody = new ClaudeRequest
        {
            Model = "claude-sonnet-4-5-20251022",
            MaxTokens = maxTokens,
            Temperature = temperature,
            System =
            [
                new { type = "text", text = systemPrompt, cache_control = new { type = "ephemeral" } }
            ],
            Messages =
            [
                new ClaudeMessage { Role = "user", Content = userProfileJson }
            ]
        };

        var requestJson = JsonSerializer.Serialize(requestBody, _snakeCaseOptions);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Headers.Add("anthropic-beta", "prompt-caching-2024-07-31");
        request.Content = content;

        _logger.LogDebug("Sending recommendation request to Claude Sonnet 4.5");

        var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(ct);

            // Transient errors: 429 Rate-Limited, 529 Overloaded (Anthropic-specific)
            // Return empty JSON so the handler uses fallback response instead of propagating 500
            if (statusCode is 429 or 529)
            {
                _logger.LogWarning(
                    "Claude API temporarily unavailable ({StatusCode}). Returning empty fallback for recommendations.",
                    statusCode);
                return "{}";
            }

            _logger.LogError("Claude API error {StatusCode}: {Error}", response.StatusCode, errorBody);
            throw new HttpRequestException($"Claude API returned {response.StatusCode}: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseBody, _snakeCaseOptions);

        if (claudeResponse?.Content == null || claudeResponse.Content.Count == 0)
        {
            _logger.LogWarning("Empty response from Claude for recommendation request");
            return "{}";
        }

        var textContent = claudeResponse.Content.FirstOrDefault(c => c.Type == "text");
        if (textContent?.Text == null)
        {
            return "{}";
        }

        // Claude may wrap JSON in markdown code blocks — strip them
        var jsonText = textContent.Text.Trim();
        if (jsonText.StartsWith("```"))
        {
            jsonText = jsonText
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();
        }

        if (claudeResponse?.Usage != null)
        {
            _logger.LogInformation(
                "RecoAgent tokens: input={Input}, output={Output}, cache_creation={CC}, cache_read={CR}",
                claudeResponse.Usage.InputTokens,
                claudeResponse.Usage.OutputTokens,
                claudeResponse.Usage.CacheCreationInputTokens,
                claudeResponse.Usage.CacheReadInputTokens);
        }

        _logger.LogInformation("Claude Sonnet 4.5 returned recommendation response");
        return jsonText;
    }
}

#region Claude API Models

internal class ClaudeRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "claude-sonnet-4-5-20251022";

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 1536;

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.5f;

    [JsonPropertyName("system")]
    public List<object> System { get; set; } = [];

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

    [JsonPropertyName("cache_creation_input_tokens")]
    public int CacheCreationInputTokens { get; set; }

    [JsonPropertyName("cache_read_input_tokens")]
    public int CacheReadInputTokens { get; set; }
}

#endregion
