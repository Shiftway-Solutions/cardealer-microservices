namespace SupportAgent.Domain.Entities;

public class SupportAgentConfig
{
    public int Id { get; set; } = 1;
    public string ModelId { get; set; } = "claude-haiku-4-5-20251001";
    public int MaxTokens { get; set; } = 512;
    public float Temperature { get; set; } = 0.3f;
    public int MaxConversationHistory { get; set; } = 10;
    public int SessionTimeoutMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public string UpdatedBy { get; set; } = "system";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
