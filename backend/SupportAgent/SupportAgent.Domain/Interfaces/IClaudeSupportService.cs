namespace SupportAgent.Domain.Interfaces;

public interface IClaudeSupportService
{
    Task<ClaudeSupportResponse> SendMessageAsync(
        string userMessage,
        List<ConversationMessage> conversationHistory,
        string systemPrompt,
        float temperature,
        int maxTokens,
        CancellationToken ct = default);
}

public record ClaudeSupportResponse(
    string Response,
    int InputTokens,
    int OutputTokens,
    string? StopReason);

public record ConversationMessage(string Role, string Content);
