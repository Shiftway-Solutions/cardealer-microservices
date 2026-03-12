using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Domain.Interfaces;

public interface IDealerAnalyticsRepository
{
    // DealerAnalytic methods
    Task<DealerAnalytic?> GetDealerAnalyticsAsync(Guid dealerId, DateTime date);
    Task<IEnumerable<DealerAnalytic>> GetDealerAnalyticsRangeAsync(Guid dealerId, DateTime fromDate, DateTime toDate);
    Task<DealerAnalytic> CreateOrUpdateAnalyticsAsync(DealerAnalytic analytics);
    Task DeleteDealerAnalyticsAsync(Guid dealerId, DateTime date);

    // Aggregate methods
    Task<DealerAnalytic> GetDealerAnalyticsSummaryAsync(Guid dealerId, DateTime fromDate, DateTime toDate);
    Task<decimal> GetDealerConversionRateAsync(Guid dealerId, DateTime fromDate, DateTime toDate);
    Task<decimal> GetDealerRevenueAsync(Guid dealerId, DateTime fromDate, DateTime toDate);
}
