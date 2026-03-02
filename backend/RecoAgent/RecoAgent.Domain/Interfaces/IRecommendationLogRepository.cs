namespace RecoAgent.Domain.Interfaces;

using RecoAgent.Domain.Entities;

public interface IRecommendationLogRepository
{
    Task SaveAsync(RecommendationLog log, CancellationToken ct = default);
    Task<IEnumerable<RecommendationLog>> GetRecentByUserAsync(string userId, int count = 20, CancellationToken ct = default);
}
