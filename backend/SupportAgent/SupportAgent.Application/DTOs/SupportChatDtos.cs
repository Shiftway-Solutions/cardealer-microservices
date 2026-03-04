namespace SupportAgent.Application.DTOs;

public class SupportChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}

public class SupportChatResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public string DetectedModule { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    /// <summary>Contextual follow-up action chips shown in the chat UI after each bot message.</summary>
    public List<string> SuggestedActions { get; set; } = [];
}

public class SessionHistoryResponse
{
    public string SessionId { get; set; } = string.Empty;
    public List<MessageDto> Messages { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
}

public class MessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? DetectedModule { get; set; }
    public DateTime CreatedAt { get; set; }
}
