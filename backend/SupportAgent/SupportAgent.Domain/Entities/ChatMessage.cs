namespace SupportAgent.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public string Role { get; set; } = "user"; // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
    public string? DetectedModule { get; set; } // "soporte_tecnico" | "orientacion_comprador" | "mixto" | "fuera_alcance" | "conversacional"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public int? LatencyMs { get; set; }
    public ChatSession? Session { get; set; }
}
