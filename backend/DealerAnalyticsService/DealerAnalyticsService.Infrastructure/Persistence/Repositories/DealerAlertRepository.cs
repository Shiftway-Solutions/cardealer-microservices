using DealerAnalyticsService.Domain.Entities;
using DealerAnalyticsService.Domain.Enums;
using DealerAnalyticsService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DealerAnalyticsService.Infrastructure.Persistence.Repositories;

public class DealerAlertRepository : IDealerAlertRepository
{
    private readonly DealerAnalyticsDbContext _context;
    private readonly ILogger<DealerAlertRepository> _logger;

    public DealerAlertRepository(
        DealerAnalyticsDbContext context,
        ILogger<DealerAlertRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DealerAlert?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.DealerAlerts.FindAsync(new object[] { id }, ct);
    }

    public async Task<IEnumerable<DealerAlert>> GetByDealerAsync(
        Guid dealerId, bool includeRead = false, bool includeDismissed = false, CancellationToken ct = default)
    {
        var query = _context.DealerAlerts.Where(a => a.DealerId == dealerId);

        if (!includeRead)
            query = query.Where(a => !a.IsRead);

        if (!includeDismissed)
            query = query.Where(a => !a.IsDismissed);

        return await query
            .Where(a => !a.ExpiresAt.HasValue || a.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<DealerAlert> CreateAsync(DealerAlert alert, CancellationToken ct = default)
    {
        _context.DealerAlerts.Add(alert);
        await _context.SaveChangesAsync(ct);
        return alert;
    }

    public async Task<DealerAlert> UpdateAsync(DealerAlert alert, CancellationToken ct = default)
    {
        alert.UpdatedAt = DateTime.UtcNow;
        _context.DealerAlerts.Update(alert);
        await _context.SaveChangesAsync(ct);
        return alert;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var alert = await GetByIdAsync(id, ct);
        if (alert != null)
        {
            _context.DealerAlerts.Remove(alert);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<IEnumerable<DealerAlert>> GetActiveAlertsAsync(Guid dealerId, CancellationToken ct = default)
    {
        return await _context.DealerAlerts
            .Where(a => a.DealerId == dealerId &&
                        a.Status == AlertStatus.Active &&
                        !a.IsDismissed &&
                        (!a.ExpiresAt.HasValue || a.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<DealerAlert>> GetUnreadAlertsAsync(Guid dealerId, CancellationToken ct = default)
    {
        return await _context.DealerAlerts
            .Where(a => a.DealerId == dealerId &&
                        !a.IsRead &&
                        !a.IsDismissed &&
                        (!a.ExpiresAt.HasValue || a.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<DealerAlert>> GetByTypeAsync(
        Guid dealerId, DealerAlertType type, CancellationToken ct = default)
    {
        return await _context.DealerAlerts
            .Where(a => a.DealerId == dealerId && a.Type == type)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<DealerAlert>> GetBySeverityAsync(
        Guid dealerId, AlertSeverity minSeverity, CancellationToken ct = default)
    {
        return await _context.DealerAlerts
            .Where(a => a.DealerId == dealerId &&
                        a.Severity >= minSeverity &&
                        !a.IsDismissed)
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid dealerId, CancellationToken ct = default)
    {
        return await _context.DealerAlerts
            .Where(a => a.DealerId == dealerId &&
                        !a.IsRead &&
                        !a.IsDismissed &&
                        (!a.ExpiresAt.HasValue || a.ExpiresAt > DateTime.UtcNow))
            .CountAsync(ct);
    }

    public async Task<bool> HasActiveAlertOfTypeAsync(
        Guid dealerId, DealerAlertType type, CancellationToken ct = default)
    {
        return await _context.DealerAlerts
            .AnyAsync(a => a.DealerId == dealerId &&
                          a.Type == type &&
                          a.Status == AlertStatus.Active &&
                          !a.IsDismissed &&
                          (!a.ExpiresAt.HasValue || a.ExpiresAt > DateTime.UtcNow), ct);
    }

    public async Task<DealerAlert?> GetActiveAlertForVehicleAsync(
        Guid dealerId, Guid vehicleId, DealerAlertType type, CancellationToken ct = default)
    {
        return await _context.DealerAlerts
            .Where(a => a.DealerId == dealerId &&
                        a.RelatedVehicleId == vehicleId &&
                        a.Type == type &&
                        a.Status == AlertStatus.Active)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<DealerAlert?> GetActiveAlertForLeadAsync(
        Guid dealerId, Guid leadId, DealerAlertType type, CancellationToken ct = default)
    {
        return await _context.DealerAlerts
            .Where(a => a.DealerId == dealerId &&
                        a.RelatedLeadId == leadId &&
                        a.Type == type &&
                        a.Status == AlertStatus.Active)
            .FirstOrDefaultAsync(ct);
    }

    public async Task MarkAllAsReadAsync(Guid dealerId, CancellationToken ct = default)
    {
        var alerts = await _context.DealerAlerts
            .Where(a => a.DealerId == dealerId && !a.IsRead)
            .ToListAsync(ct);

        foreach (var alert in alerts)
        {
            alert.MarkAsRead();
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task DismissAllAsync(Guid dealerId, CancellationToken ct = default)
    {
        var alerts = await _context.DealerAlerts
            .Where(a => a.DealerId == dealerId && !a.IsDismissed)
            .ToListAsync(ct);

        foreach (var alert in alerts)
        {
            alert.Dismiss();
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task DismissByTypeAsync(Guid dealerId, DealerAlertType type, CancellationToken ct = default)
    {
        var alerts = await _context.DealerAlerts
            .Where(a => a.DealerId == dealerId &&
                        a.Type == type &&
                        !a.IsDismissed)
            .ToListAsync(ct);

        foreach (var alert in alerts)
        {
            alert.Dismiss();
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task CleanupExpiredAsync(CancellationToken ct = default)
    {
        var expired = await _context.DealerAlerts
            .Where(a => a.ExpiresAt.HasValue && a.ExpiresAt < DateTime.UtcNow && a.Status != AlertStatus.Expired)
            .ToListAsync(ct);

        foreach (var alert in expired)
        {
            alert.Status = AlertStatus.Expired;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Marked {Count} alerts as expired", expired.Count);
    }

    public async Task<Dictionary<DealerAlertType, int>> GetAlertCountsByTypeAsync(
        Guid dealerId, CancellationToken ct = default)
    {
        return await _context.DealerAlerts
            .Where(a => a.DealerId == dealerId && a.Status == AlertStatus.Active)
            .GroupBy(a => a.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, ct);
    }

    public async Task<Dictionary<AlertSeverity, int>> GetAlertCountsBySeverityAsync(
        Guid dealerId, CancellationToken ct = default)
    {
        return await _context.DealerAlerts
            .Where(a => a.DealerId == dealerId && a.Status == AlertStatus.Active)
            .GroupBy(a => a.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Severity, x => x.Count, ct);
    }

    public async Task AddAsync(DealerAlert alert, CancellationToken ct = default)
    {
        _context.DealerAlerts.Add(alert);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }

    public async Task<(List<DealerAlert> Items, int TotalCount)> GetByDealerIdAsync(
        Guid dealerId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.DealerAlerts
            .Where(a => a.DealerId == dealerId)
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<DealerAlert?> GetActiveByDealerAndTypeAsync(
        Guid dealerId, DealerAlertType type, CancellationToken ct = default)
    {
        return await _context.DealerAlerts
            .Where(a => a.DealerId == dealerId &&
                        a.Type == type &&
                        a.Status == AlertStatus.Active &&
                        !a.IsDismissed &&
                        (!a.ExpiresAt.HasValue || a.ExpiresAt > DateTime.UtcNow))
            .FirstOrDefaultAsync(ct);
    }
}
