using Microsoft.EntityFrameworkCore;
using RecoAgent.Domain.Entities;
using RecoAgent.Domain.Interfaces;
using RecoAgent.Infrastructure.Persistence;

namespace RecoAgent.Infrastructure.Repositories;

public class RecommendationLogRepository : IRecommendationLogRepository
{
    private readonly RecoAgentDbContext _context;

    public RecommendationLogRepository(RecoAgentDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(RecommendationLog log, CancellationToken ct = default)
    {
        _context.RecommendationLogs.Add(log);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<RecommendationLog>> GetRecentByUserAsync(string userId, int count = 10, CancellationToken ct = default)
    {
        return await _context.RecommendationLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
    }
}
