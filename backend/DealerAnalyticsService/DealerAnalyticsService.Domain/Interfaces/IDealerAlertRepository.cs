using DealerAnalyticsService.Domain.Entities;
using DealerAnalyticsService.Domain.Enums;

namespace DealerAnalyticsService.Domain.Interfaces;

/// <summary>
/// Repositorio para alertas de dealers
/// </summary>
public interface IDealerAlertRepository
{
    // CRUD Operations
    Task<DealerAlert?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<DealerAlert>> GetByDealerAsync(Guid dealerId, bool includeRead = false, bool includeDismissed = false, CancellationToken ct = default);
    Task<DealerAlert> CreateAsync(DealerAlert alert, CancellationToken ct = default);
    Task<DealerAlert> UpdateAsync(DealerAlert alert, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(DealerAlert alert, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    // Query
    Task<IEnumerable<DealerAlert>> GetActiveAlertsAsync(Guid dealerId, CancellationToken ct = default);
    Task<IEnumerable<DealerAlert>> GetUnreadAlertsAsync(Guid dealerId, CancellationToken ct = default);
    Task<IEnumerable<DealerAlert>> GetByTypeAsync(Guid dealerId, DealerAlertType type, CancellationToken ct = default);
    Task<IEnumerable<DealerAlert>> GetBySeverityAsync(Guid dealerId, AlertSeverity minSeverity, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid dealerId, CancellationToken ct = default);

    // Paginated query
    Task<(List<DealerAlert> Items, int TotalCount)> GetByDealerIdAsync(
        Guid dealerId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    // Check for existing alerts (to avoid duplicates)
    Task<bool> HasActiveAlertOfTypeAsync(Guid dealerId, DealerAlertType type, CancellationToken ct = default);
    Task<DealerAlert?> GetActiveAlertForVehicleAsync(Guid dealerId, Guid vehicleId, DealerAlertType type, CancellationToken ct = default);
    Task<DealerAlert?> GetActiveAlertForLeadAsync(Guid dealerId, Guid leadId, DealerAlertType type, CancellationToken ct = default);
    Task<DealerAlert?> GetActiveByDealerAndTypeAsync(Guid dealerId, DealerAlertType type, CancellationToken ct = default);

    // Bulk Operations
    Task MarkAllAsReadAsync(Guid dealerId, CancellationToken ct = default);
    Task DismissAllAsync(Guid dealerId, CancellationToken ct = default);
    Task DismissByTypeAsync(Guid dealerId, DealerAlertType type, CancellationToken ct = default);
    Task CleanupExpiredAsync(CancellationToken ct = default);

    // Statistics
    Task<Dictionary<DealerAlertType, int>> GetAlertCountsByTypeAsync(Guid dealerId, CancellationToken ct = default);
    Task<Dictionary<AlertSeverity, int>> GetAlertCountsBySeverityAsync(Guid dealerId, CancellationToken ct = default);
}
