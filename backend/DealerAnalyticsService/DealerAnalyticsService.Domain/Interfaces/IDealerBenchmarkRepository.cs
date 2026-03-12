using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Domain.Interfaces;

/// <summary>
/// Repositorio para benchmarks y rankings de dealers
/// </summary>
public interface IDealerBenchmarkRepository
{
    // CRUD Operations
    Task<DealerBenchmark?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DealerBenchmark?> GetLatestAsync(Guid dealerId, CancellationToken ct = default);
    Task<DealerBenchmark?> GetByDateAsync(Guid dealerId, DateTime date, CancellationToken ct = default);
    Task<IEnumerable<DealerBenchmark>> GetHistoryAsync(Guid dealerId, int months, CancellationToken ct = default);
    Task<DealerBenchmark> CreateAsync(DealerBenchmark benchmark, CancellationToken ct = default);
    Task<DealerBenchmark> UpdateAsync(DealerBenchmark benchmark, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Rankings
    Task<IEnumerable<DealerBenchmark>> GetTopDealersAsync(int limit, string? period = null, CancellationToken ct = default);
    Task<int> GetDealerRankAsync(Guid dealerId, string? period = null, CancellationToken ct = default);
    Task<IEnumerable<DealerBenchmark>> GetRankingsAsync(int page, int pageSize, string? sortBy = null, CancellationToken ct = default);

    // Market Averages
    Task<DealerBenchmark> GetMarketAveragesAsync(DateTime date, CancellationToken ct = default);
    Task<Dictionary<string, double>> GetMarketMetricsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    // Tier Analysis
    Task<Dictionary<DealerTier, int>> GetDealersByTierAsync(CancellationToken ct = default);
    Task<DealerTier> CalculateDealerTierAsync(Guid dealerId, CancellationToken ct = default);

    // Bulk Operations
    Task BulkUpdateRankingsAsync(DateTime date, CancellationToken ct = default);
    Task CalculateAllBenchmarksAsync(DateTime date, CancellationToken ct = default);
}
