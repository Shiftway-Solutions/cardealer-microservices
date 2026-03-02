using Microsoft.EntityFrameworkCore;
using SupportAgent.Domain.Entities;
using SupportAgent.Domain.Interfaces;
using SupportAgent.Infrastructure.Persistence;

namespace SupportAgent.Infrastructure.Repositories;

public class ChatSessionRepository : IChatSessionRepository
{
    private readonly SupportAgentDbContext _context;

    public ChatSessionRepository(SupportAgentDbContext context)
    {
        _context = context;
    }

    public async Task<ChatSession?> GetBySessionIdAsync(string sessionId, CancellationToken ct = default)
    {
        return await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);
    }

    public async Task<ChatSession> CreateAsync(ChatSession session, CancellationToken ct = default)
    {
        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync(ct);
        return session;
    }

    public async Task UpdateAsync(ChatSession session, CancellationToken ct = default)
    {
        _context.ChatSessions.Update(session);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(Guid sessionId, int limit = 10, CancellationToken ct = default)
    {
        return await _context.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task AddMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync(ct);
    }
}
