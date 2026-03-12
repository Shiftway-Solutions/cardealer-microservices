using AdminService.Domain.Entities;

namespace AdminService.Domain.Interfaces;

/// <summary>
/// Interface para repositorio de moderación
/// </summary>
public interface IModerationRepository
{
    Task<IEnumerable<PendingListingInfo>> GetPendingListingsAsync(int limit);
    Task<IEnumerable<PendingReportInfo>> GetPendingReportsAsync(int limit);

    /// <summary>
    /// Get paginated moderation queue
    /// </summary>
    Task<(IReadOnlyList<ModerationItem> Items, int Total)> GetModerationQueueAsync(
        string? type = null,
        string? priority = null,
        string? status = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific moderation item by ID
    /// </summary>
    Task<ModerationItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get moderation statistics
    /// </summary>
    Task<ModerationStats> GetStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Process moderation action (approve, reject, escalate)
    /// </summary>
    Task<bool> ProcessActionAsync(
        Guid itemId,
        string action,
        string reviewerId,
        string? reason = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new item to the moderation queue
    /// </summary>
    Task<ModerationItem> AddAsync(ModerationItem item, CancellationToken cancellationToken = default);
}