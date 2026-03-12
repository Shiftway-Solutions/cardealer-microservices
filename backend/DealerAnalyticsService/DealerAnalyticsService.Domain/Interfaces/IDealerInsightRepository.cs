using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Domain.Interfaces;

public interface IDealerInsightRepository
{
    Task<DealerInsight?> GetInsightAsync(Guid id);
    Task<IEnumerable<DealerInsight>> GetDealerInsightsAsync(Guid dealerId, bool onlyUnread = false);
    Task<IEnumerable<DealerInsight>> GetInsightsByTypeAsync(Guid dealerId, InsightType type);
    Task<IEnumerable<DealerInsight>> GetInsightsByPriorityAsync(Guid dealerId, InsightPriority priority);

    Task<DealerInsight> CreateInsightAsync(DealerInsight insight);
    Task<DealerInsight> UpdateInsightAsync(DealerInsight insight);
    Task DeleteInsightAsync(Guid id);

    // Bulk operations
    Task MarkInsightsAsReadAsync(Guid dealerId, IEnumerable<Guid> insightIds);
    Task MarkInsightAsActedUponAsync(Guid insightId, DateTime actionDate);
    Task DeleteExpiredInsightsAsync();
}
