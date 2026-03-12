using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Domain.Interfaces;

/// <summary>
/// Repositorio para snapshots diarios de dealers
/// </summary>
public interface IDealerSnapshotRepository
{
    // CRUD Operations
    Task<DealerSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DealerSnapshot?> GetLatestAsync(Guid dealerId, CancellationToken ct = default);
    Task<DealerSnapshot?> GetByDateAsync(Guid dealerId, DateTime date, CancellationToken ct = default);
    Task<IEnumerable<DealerSnapshot>> GetRangeAsync(Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<DealerSnapshot> CreateAsync(DealerSnapshot snapshot, CancellationToken ct = default);
    Task<DealerSnapshot> UpdateAsync(DealerSnapshot snapshot, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Bulk Operations
    Task BulkInsertAsync(IEnumerable<DealerSnapshot> snapshots, CancellationToken ct = default);

    // Aggregation
    Task<DealerSnapshot> AggregateAsync(Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    // Comparison
    Task<(DealerSnapshot? current, DealerSnapshot? previous)> GetComparisonAsync(
        Guid dealerId,
        DateTime currentDate,
        int compareDays,
        CancellationToken ct = default);

    // Statistics
    Task<double> GetAverageMetricAsync(Guid dealerId, string metricName, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<Dictionary<DateTime, double>> GetMetricTrendAsync(Guid dealerId, string metricName, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
}
