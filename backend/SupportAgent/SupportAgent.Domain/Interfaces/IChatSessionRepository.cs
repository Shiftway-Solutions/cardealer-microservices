using SupportAgent.Domain.Entities;

namespace SupportAgent.Domain.Interfaces;

public interface IChatSessionRepository
{
    Task<ChatSession?> GetBySessionIdAsync(string sessionId, CancellationToken ct = default);
    Task<ChatSession> CreateAsync(ChatSession session, CancellationToken ct = default);
    Task UpdateAsync(ChatSession session, CancellationToken ct = default);
    Task<IEnumerable<ChatMessage>> GetMessagesAsync(Guid sessionId, int limit = 10, CancellationToken ct = default);
    Task AddMessageAsync(ChatMessage message, CancellationToken ct = default);
}
