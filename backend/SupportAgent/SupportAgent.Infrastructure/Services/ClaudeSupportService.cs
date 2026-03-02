using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SupportAgent.Domain.Interfaces;

namespace SupportAgent.Infrastructure.Services;

public class ClaudeSupportService : IClaudeSupportService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ClaudeSupportService> _logger;
    private readonly string _apiKey;

    public ClaudeSupportService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ClaudeSupportService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["Claude:ApiKey"]
            ?? throw new InvalidOperationException("Claude API key not configured. Set 'Claude:ApiKey' in configuration.");
    }

    public async Task<ClaudeSupportResponse> SendMessageAsync(
        string userMessage,
        List<ConversationMessage> conversationHistory,
        string systemPrompt,
        float temperature,
        int maxTokens,
        CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("ClaudeApi");

        // Build messages array with conversation history + new user message
        var messages = new List<ClaudeMessage>();

        foreach (var msg in conversationHistory)
        {
            messages.Add(new ClaudeMessage { Role = msg.Role, Content = msg.Content });
        }

        messages.Add(new ClaudeMessage { Role = "user", Content = userMessage });

        var requestBody = new ClaudeRequest
        {
            Model = "claude-haiku-4-5-20251001",
            MaxTokens = maxTokens,
            Temperature = temperature,
            System = systemPrompt,
            Messages = messages
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var requestJson = JsonSerializer.Serialize(requestBody, jsonOptions);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = content;

        _logger.LogDebug("Sending support message to Claude. History count={HistoryCount}", conversationHistory.Count);

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
            _logger.LogWarning("Empty response from Claude for support message");
            return new ClaudeSupportResponse(
                "Lo siento, no pude procesar tu mensaje en este momento. ¿Podrías intentar de nuevo? 🙏",
                0, 0, null);
        }

        var textContent = claudeResponse.Content.FirstOrDefault(c => c.Type == "text");
        if (textContent?.Text == null)
        {
            return new ClaudeSupportResponse(
                "Lo siento, tuve un problema procesando tu consulta. ¿Puedes reformular tu pregunta? 😊",
                0, 0, null);
        }

        _logger.LogInformation(
            "Claude support response generated. InputTokens={InputTokens}, OutputTokens={OutputTokens}",
            claudeResponse.Usage?.InputTokens ?? 0,
            claudeResponse.Usage?.OutputTokens ?? 0);

        return new ClaudeSupportResponse(
            textContent.Text,
            claudeResponse.Usage?.InputTokens ?? 0,
            claudeResponse.Usage?.OutputTokens ?? 0,
            claudeResponse.StopReason);
    }
}

#region Claude API Models

internal class ClaudeRequest
{
    [JsonPropertyName("model")] public string Model { get; set; } = "claude-haiku-4-5-20251001";
    [JsonPropertyName("max_tokens")] public int MaxTokens { get; set; } = 512;
    [JsonPropertyName("temperature")] public float Temperature { get; set; } = 0.3f;
    [JsonPropertyName("system")] public string System { get; set; } = string.Empty;
    [JsonPropertyName("messages")] public List<ClaudeMessage> Messages { get; set; } = [];
}

internal class ClaudeMessage
{
    [JsonPropertyName("role")] public string Role { get; set; } = "user";
    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
}

internal class ClaudeResponse
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("role")] public string? Role { get; set; }
    [JsonPropertyName("content")] public List<ClaudeContentBlock> Content { get; set; } = [];
    [JsonPropertyName("model")] public string? Model { get; set; }
    [JsonPropertyName("stop_reason")] public string? StopReason { get; set; }
    [JsonPropertyName("usage")] public ClaudeUsage? Usage { get; set; }
}

internal class ClaudeContentBlock
{
    [JsonPropertyName("type")] public string Type { get; set; } = "text";
    [JsonPropertyName("text")] public string? Text { get; set; }
}

internal class ClaudeUsage
{
    [JsonPropertyName("input_tokens")] public int InputTokens { get; set; }
    [JsonPropertyName("output_tokens")] public int OutputTokens { get; set; }
}

#endregion
