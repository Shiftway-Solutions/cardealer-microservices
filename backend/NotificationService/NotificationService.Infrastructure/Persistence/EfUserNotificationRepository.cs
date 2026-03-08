using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces.Repositories;

namespace NotificationService.Infrastructure.Persistence;

public class EfUserNotificationRepository : IUserNotificationRepository
{
    private readonly ApplicationDbContext _context;

    public EfUserNotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserNotification?> GetByIdAsync(Guid id)
    {
        return await _context.UserNotifications.FindAsync(id);
    }

    public async Task<(IEnumerable<UserNotification> Items, int Total, int UnreadCount)> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly = false)
    {
        var now = DateTime.UtcNow;

        var baseQuery = _context.UserNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .Where(n => n.ExpiresAt == null || n.ExpiresAt > now);

        if (unreadOnly)
            baseQuery = baseQuery.Where(n => !n.IsRead);

        var total = await baseQuery.CountAsync();

        var unreadCount = await _context.UserNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead && (n.ExpiresAt == null || n.ExpiresAt > now))
            .CountAsync();

        var items = await baseQuery
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total, unreadCount);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        return await _context.UserNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead && (n.ExpiresAt == null || n.ExpiresAt > now))
            .CountAsync();
    }

    public async Task AddAsync(UserNotification notification)
    {
        await _context.UserNotifications.AddAsync(notification);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<UserNotification> notifications)
    {
        await _context.UserNotifications.AddRangeAsync(notifications);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserNotification notification)
    {
        _context.UserNotifications.Update(notification);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var notification = await _context.UserNotifications.FindAsync(id);
        if (notification != null)
        {
            _context.UserNotifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return false;

        notification.MarkAsRead();
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId)
    {
        // Performance: Use ExecuteUpdateAsync instead of loading all entities into memory
        return await _context.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow));
    }

    public async Task<int> DeleteReadAsync(Guid userId)
    {
        // Performance: Use ExecuteDeleteAsync instead of loading all entities into memory
        return await _context.UserNotifications
            .Where(n => n.UserId == userId && n.IsRead)
            .ExecuteDeleteAsync();
    }

    public async Task<int> DeleteExpiredAsync()
    {
        // Performance: Use ExecuteDeleteAsync instead of loading all entities into memory
        return await _context.UserNotifications
            .Where(n => n.ExpiresAt != null && n.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync();
    }
}
