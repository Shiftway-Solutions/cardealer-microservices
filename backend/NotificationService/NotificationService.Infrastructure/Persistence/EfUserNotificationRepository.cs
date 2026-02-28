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
        var now = DateTime.UtcNow;
        var unread = await _context.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }

        await _context.SaveChangesAsync();
        return unread.Count;
    }

    public async Task<int> DeleteReadAsync(Guid userId)
    {
        var read = await _context.UserNotifications
            .Where(n => n.UserId == userId && n.IsRead)
            .ToListAsync();

        _context.UserNotifications.RemoveRange(read);
        await _context.SaveChangesAsync();
        return read.Count;
    }

    public async Task<int> DeleteExpiredAsync()
    {
        var now = DateTime.UtcNow;
        var expired = await _context.UserNotifications
            .Where(n => n.ExpiresAt != null && n.ExpiresAt < now)
            .ToListAsync();

        _context.UserNotifications.RemoveRange(expired);
        await _context.SaveChangesAsync();
        return expired.Count;
    }
}
