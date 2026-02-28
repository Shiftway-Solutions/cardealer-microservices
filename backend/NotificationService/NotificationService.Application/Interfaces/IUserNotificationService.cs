using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

/// <summary>
/// Service for creating in-app user notifications (shown in header and /cuenta/notificaciones).
/// Consumers and handlers should use this service to persist notifications when
/// sending emails, SMS or push notifications.
/// </summary>
public interface IUserNotificationService
{
    /// <summary>
    /// Creates a persisted in-app notification for a user.
    /// Does NOT throw — logs and ignores errors to avoid interrupting the calling flow.
    /// </summary>
    Task CreateAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string? icon = null,
        string? link = null,
        Guid? dealerId = null,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default);
}
