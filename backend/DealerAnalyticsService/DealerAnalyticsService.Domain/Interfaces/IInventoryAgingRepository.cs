using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Domain.Interfaces;

/// <summary>
/// Repositorio para análisis de antigüedad del inventario
/// </summary>
public interface IInventoryAgingRepository
{
    // CRUD Operations
    Task<InventoryAging?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<InventoryAging?> GetLatestAsync(Guid dealerId, CancellationToken ct = default);
    Task<InventoryAging?> GetByDateAsync(Guid dealerId, DateTime date, CancellationToken ct = default);
    Task<IEnumerable<InventoryAging>> GetHistoryAsync(Guid dealerId, int days, CancellationToken ct = default);
    Task<InventoryAging> CreateAsync(InventoryAging aging, CancellationToken ct = default);
    Task<InventoryAging> UpdateAsync(InventoryAging aging, CancellationToken ct = default);

    // Analysis
    Task<InventoryAging> CalculateCurrentAsync(Guid dealerId, CancellationToken ct = default);
    Task<List<AgingBucket>> GetAgingBucketsAsync(Guid dealerId, CancellationToken ct = default);

    // At-Risk Inventory
    Task<decimal> GetAtRiskValueAsync(Guid dealerId, int daysThreshold = 60, CancellationToken ct = default);
    Task<int> GetAtRiskCountAsync(Guid dealerId, int daysThreshold = 60, CancellationToken ct = default);

    // Trends
    Task<Dictionary<DateTime, double>> GetAverageDaysOnMarketTrendAsync(Guid dealerId, int days, CancellationToken ct = default);
}
