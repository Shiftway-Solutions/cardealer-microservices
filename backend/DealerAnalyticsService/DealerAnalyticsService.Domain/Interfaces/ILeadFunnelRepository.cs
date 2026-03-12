using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Domain.Interfaces;

/// <summary>
/// Repositorio para métricas del funnel de leads
/// </summary>
public interface ILeadFunnelRepository
{
    // CRUD Operations
    Task<LeadFunnelMetrics?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<LeadFunnelMetrics?> GetByPeriodAsync(Guid dealerId, DateTime periodStart, DateTime periodEnd, string periodType, CancellationToken ct = default);
    Task<IEnumerable<LeadFunnelMetrics>> GetHistoryAsync(Guid dealerId, int months, CancellationToken ct = default);
    Task<LeadFunnelMetrics> CreateAsync(LeadFunnelMetrics metrics, CancellationToken ct = default);
    Task<LeadFunnelMetrics> UpdateAsync(LeadFunnelMetrics metrics, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Aggregation
    Task<LeadFunnelMetrics> AggregateAsync(Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    // Comparison
    Task<IEnumerable<LeadFunnelMetrics>> GetMonthlyTrendAsync(Guid dealerId, int months, CancellationToken ct = default);
    Task<(LeadFunnelMetrics? current, LeadFunnelMetrics? previous)> GetComparisonAsync(
        Guid dealerId,
        DateTime currentPeriodStart,
        DateTime currentPeriodEnd,
        CancellationToken ct = default);

    // Funnel Analysis
    Task<List<FunnelStage>> GetFunnelStagesAsync(Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<Dictionary<string, double>> GetConversionRatesAsync(Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    // Source Analysis
    Task<Dictionary<string, int>> GetLeadsBySourceAsync(Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<Dictionary<string, double>> GetConversionBySourceAsync(Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
}
