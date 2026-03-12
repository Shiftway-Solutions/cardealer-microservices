using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Domain.Interfaces;

/// <summary>
/// Repositorio para métricas de performance de vehículos
/// </summary>
public interface IVehiclePerformanceRepository
{
    // CRUD Operations
    Task<VehiclePerformance?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<VehiclePerformance?> GetByVehicleAndDateAsync(Guid vehicleId, DateTime date, CancellationToken ct = default);
    Task<IEnumerable<VehiclePerformance>> GetByDealerAsync(Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<IEnumerable<VehiclePerformance>> GetByVehicleAsync(Guid vehicleId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<VehiclePerformance> CreateAsync(VehiclePerformance performance, CancellationToken ct = default);
    Task<VehiclePerformance> UpdateAsync(VehiclePerformance performance, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Top Performers
    Task<IEnumerable<VehiclePerformance>> GetTopPerformersAsync(Guid dealerId, int limit, CancellationToken ct = default);
    Task<IEnumerable<VehiclePerformance>> GetTopByViewsAsync(Guid dealerId, int limit, DateTime? fromDate = null, CancellationToken ct = default);
    Task<IEnumerable<VehiclePerformance>> GetTopByContactsAsync(Guid dealerId, int limit, DateTime? fromDate = null, CancellationToken ct = default);
    Task<IEnumerable<VehiclePerformance>> GetTopByEngagementAsync(Guid dealerId, int limit, DateTime? fromDate = null, CancellationToken ct = default);

    // Low Performers
    Task<IEnumerable<VehiclePerformance>> GetLowPerformersAsync(Guid dealerId, int limit, CancellationToken ct = default);

    // Aggregation
    Task<VehiclePerformance> AggregateByVehicleAsync(Guid vehicleId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<Dictionary<Guid, VehiclePerformance>> AggregateByDealerAsync(Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    // Statistics
    Task<double> GetAverageViewsPerVehicleAsync(Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<double> GetAverageContactRateAsync(Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
}
