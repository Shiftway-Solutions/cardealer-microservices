using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces.Repositories;

namespace NotificationService.Infrastructure.Persistence;

public class EfScheduledNotificationRepository : IScheduledNotificationRepository
{
    private readonly ApplicationDbContext _context;

    public EfScheduledNotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ScheduledNotification?> GetByIdAsync(Guid id)
    {
        return await _context.ScheduledNotifications
            .Include(sn => sn.Notification)
            .FirstOrDefaultAsync(sn => sn.Id == id);
    }

    public async Task<IEnumerable<ScheduledNotification>> GetAllAsync()
    {
        return await _context.ScheduledNotifications
            .AsNoTracking()
            .Include(sn => sn.Notification)
            .OrderBy(sn => sn.ScheduledFor)
            .Take(1000) // Safety limit
            .ToListAsync();
    }

    public async Task<IEnumerable<ScheduledNotification>> GetByStatusAsync(ScheduledNotificationStatus status)
    {
        return await _context.ScheduledNotifications
            .AsNoTracking()
            .Include(sn => sn.Notification)
            .Where(sn => sn.Status == status)
            .OrderBy(sn => sn.ScheduledFor)
            .Take(500)
            .ToListAsync();
    }

    public async Task AddAsync(ScheduledNotification scheduledNotification)
    {
        await _context.ScheduledNotifications.AddAsync(scheduledNotification);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<ScheduledNotification> scheduledNotifications)
    {
        await _context.ScheduledNotifications.AddRangeAsync(scheduledNotifications);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ScheduledNotification scheduledNotification)
    {
        _context.ScheduledNotifications.Update(scheduledNotification);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var scheduledNotification = await GetByIdAsync(id);
        if (scheduledNotification != null)
        {
            _context.ScheduledNotifications.Remove(scheduledNotification);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ScheduledNotification>> GetDueNotificationsAsync()
    {
        return await GetDueNotificationsAsync(DateTime.UtcNow);
    }

    public async Task<IEnumerable<ScheduledNotification>> GetDueNotificationsAsync(DateTime upTo)
    {
        return await _context.ScheduledNotifications
            .Include(sn => sn.Notification)
            .Where(sn => sn.Status == ScheduledNotificationStatus.Pending &&
                        (sn.NextExecution != null ? sn.NextExecution <= upTo : sn.ScheduledFor <= upTo))
            .OrderBy(sn => sn.NextExecution ?? sn.ScheduledFor)
            .Take(200) // Process in batches
            .ToListAsync();
    }

    public async Task<IEnumerable<ScheduledNotification>> GetScheduledForDateRangeAsync(DateTime start, DateTime end)
    {
        return await _context.ScheduledNotifications
            .AsNoTracking()
            .Include(sn => sn.Notification)
            .Where(sn => sn.ScheduledFor >= start && sn.ScheduledFor <= end)
            .OrderBy(sn => sn.ScheduledFor)
            .Take(1000)
            .ToListAsync();
    }

    public async Task<IEnumerable<ScheduledNotification>> GetRecurringNotificationsAsync()
    {
        return await _context.ScheduledNotifications
            .AsNoTracking()
            .Include(sn => sn.Notification)
            .Where(sn => sn.IsRecurring &&
                        (sn.Status == ScheduledNotificationStatus.Pending ||
                         sn.Status == ScheduledNotificationStatus.Executed))
            .Take(500)
            .ToListAsync();
    }

    public async Task<IEnumerable<ScheduledNotification>> GetByNotificationIdAsync(Guid notificationId)
    {
        return await _context.ScheduledNotifications
            .AsNoTracking()
            .Include(sn => sn.Notification)
            .Where(sn => sn.NotificationId == notificationId)
            .OrderBy(sn => sn.ScheduledFor)
            .Take(500) // Safety limit
            .ToListAsync();
    }

    public async Task<IEnumerable<ScheduledNotification>> GetPendingAsync()
    {
        return await GetByStatusAsync(ScheduledNotificationStatus.Pending);
    }

    public async Task<IEnumerable<ScheduledNotification>> GetFailedAsync()
    {
        return await GetByStatusAsync(ScheduledNotificationStatus.Failed);
    }

    public async Task<IEnumerable<ScheduledNotification>> GetExecutedAsync(DateTime since)
    {
        return await _context.ScheduledNotifications
            .AsNoTracking()
            .Include(sn => sn.Notification)
            .Where(sn => sn.Status == ScheduledNotificationStatus.Executed &&
                        sn.LastExecution >= since)
            .OrderByDescending(sn => sn.LastExecution)
            .Take(500)
            .ToListAsync();
    }

    public async Task<IEnumerable<ScheduledNotification>> GetPagedAsync(int pageNumber, int pageSize)
    {
        return await _context.ScheduledNotifications
            .AsNoTracking()
            .Include(sn => sn.Notification)
            .OrderBy(sn => sn.ScheduledFor)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.ScheduledNotifications.AsNoTracking().CountAsync();
    }

    public async Task<int> GetCountByStatusAsync(ScheduledNotificationStatus status)
    {
        return await _context.ScheduledNotifications.AsNoTracking().CountAsync(sn => sn.Status == status);
    }

    public async Task<int> CleanupCompletedAsync(DateTime cutoffDate)
    {
        // Performance: Use ExecuteDeleteAsync instead of loading entities into memory
        return await _context.ScheduledNotifications
            .Where(sn => sn.Status == ScheduledNotificationStatus.Completed &&
                        sn.LastExecution < cutoffDate)
            .ExecuteDeleteAsync();
    }

    public async Task<int> CleanupCancelledAsync(DateTime cutoffDate)
    {
        // Performance: Use ExecuteDeleteAsync instead of loading entities into memory
        return await _context.ScheduledNotifications
            .Where(sn => sn.Status == ScheduledNotificationStatus.Cancelled &&
                        sn.CancelledAt < cutoffDate)
            .ExecuteDeleteAsync();
    }
}
