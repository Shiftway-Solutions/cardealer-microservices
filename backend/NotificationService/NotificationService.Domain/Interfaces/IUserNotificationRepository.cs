using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for in-app UserNotification entities.
/// These are the notifications shown in the header dropdown and /cuenta/notificaciones page.
/// </summary>
public interface IUserNotificationRepository
{
    Task<UserNotification?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get paginated notifications for a specific user (excludes expired ones).
    /// </summary>
    Task<(IEnumerable<UserNotification> Items, int Total, int UnreadCount)> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly = false);

    /// <summary>
    /// Get total unread count for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId);

    Task AddAsync(UserNotification notification);
    Task AddRangeAsync(IEnumerable<UserNotification> notifications);
    Task UpdateAsync(UserNotification notification);
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Mark a specific notification as read. Returns false if not found.
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);

    /// <summary>
    /// Mark all unread notifications as read for a user.
    /// </summary>
    Task<int> MarkAllAsReadAsync(Guid userId);

    /// <summary>
    /// Delete all read notifications for a user.
    /// </summary>
    Task<int> DeleteReadAsync(Guid userId);

    /// <summary>
    /// Cleanup expired notifications across all users.
    /// </summary>
    Task<int> DeleteExpiredAsync();
}
