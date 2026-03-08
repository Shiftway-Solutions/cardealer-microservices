using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller for managing in-app user notifications (header bell + /cuenta/notificaciones).
/// All data is stored in the database — no mock data.
///
/// Security:
///  - [Authorize] required for all endpoints.
///  - UserId is extracted from JWT claims — NEVER trusted from request body.
///  - Notification content is sanitized at write time (UserNotificationService).
/// </summary>
[ApiController]
[Authorize]
[Route("api/notifications")]
public class UserNotificationsController : ControllerBase
{
    private readonly IUserNotificationRepository _repository;
    private readonly ILogger<UserNotificationsController> _logger;

    public UserNotificationsController(
        IUserNotificationRepository repository,
        ILogger<UserNotificationsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    // ─── GET /api/notifications ──────────────────────────────────────────────────

    /// <summary>
    /// Get paginated notifications for the authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<UserNotificationsListResponse>> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "User ID not found in token" });

        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        _logger.LogDebug(
            "Getting notifications for UserId={UserId} page={Page} pageSize={PageSize} unreadOnly={UnreadOnly}",
            userId, page, pageSize, unreadOnly);

        var (items, total, unreadCount) = await _repository.GetByUserIdAsync(
            userId.Value, page, pageSize, unreadOnly);

        var notifications = items.Select(MapToDto).ToList();

        return Ok(new UserNotificationsListResponse(
            Notifications: notifications,
            Total: total,
            UnreadCount: unreadCount,
            Page: page,
            PageSize: pageSize,
            TotalPages: (int)Math.Ceiling((double)total / pageSize)
        ));
    }

    // ─── GET /api/notifications/unread/count ─────────────────────────────────────

    /// <summary>
    /// Get unread notification count (used by header bell badge).
    /// </summary>
    [HttpGet("unread/count")]
    public async Task<ActionResult<UnreadCountResponse>> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "User ID not found in token" });

        var count = await _repository.GetUnreadCountAsync(userId.Value);
        return Ok(new UnreadCountResponse(count));
    }

    // ─── PATCH /api/notifications/{id}/read ──────────────────────────────────────

    /// <summary>
    /// Mark a specific notification as read.
    /// Only the owning user can mark their notification.
    /// </summary>
    [HttpPatch("{notificationId}/read")]
    public async Task<ActionResult> MarkAsRead(string notificationId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "User ID not found in token" });

        if (!Guid.TryParse(notificationId, out var notifId))
            return BadRequest(new { error = "Invalid notification ID" });

        var updated = await _repository.MarkAsReadAsync(notifId, userId.Value);
        if (!updated)
            return NotFound(new { error = "Notification not found" });

        _logger.LogDebug("Marked notification {NotifId} as read for UserId={UserId}", notifId, userId);
        return Ok(new { message = "Notification marked as read" });
    }

    // ─── PATCH /api/notifications/read-all ───────────────────────────────────────

    /// <summary>
    /// Mark all unread notifications as read for the authenticated user.
    /// </summary>
    [HttpPatch("read-all")]
    public async Task<ActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "User ID not found in token" });

        var count = await _repository.MarkAllAsReadAsync(userId.Value);
        _logger.LogInformation("Marked {Count} notifications as read for UserId={UserId}", count, userId);
        return Ok(new { message = "All notifications marked as read", count });
    }

    // ─── DELETE /api/notifications/{id} ──────────────────────────────────────────

    /// <summary>
    /// Delete a specific notification.
    /// Only the owning user can delete their own notification.
    /// </summary>
    [HttpDelete("{notificationId}")]
    public async Task<ActionResult> DeleteNotification(string notificationId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "User ID not found in token" });

        if (!Guid.TryParse(notificationId, out var notifId))
            return BadRequest(new { error = "Invalid notification ID" });

        // Ownership check: load and validate before delete
        var notification = await _repository.GetByIdAsync(notifId);
        if (notification == null || notification.UserId != userId.Value)
            return NotFound(new { error = "Notification not found" });

        await _repository.DeleteAsync(notifId);
        _logger.LogDebug("Deleted notification {NotifId} for UserId={UserId}", notifId, userId);
        return Ok(new { message = "Notification deleted" });
    }

    // ─── DELETE /api/notifications/read ──────────────────────────────────────────

    /// <summary>
    /// Delete all read notifications for the authenticated user.
    /// </summary>
    [HttpDelete("read")]
    public async Task<ActionResult> DeleteAllRead()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "User ID not found in token" });

        var count = await _repository.DeleteReadAsync(userId.Value);
        _logger.LogInformation("Deleted {Count} read notifications for UserId={UserId}", count, userId);
        return Ok(new { message = $"Deleted {count} read notifications", count });
    }

    // ─── Mapping ─────────────────────────────────────────────────────────────────

    private static UserNotificationDto MapToDto(Domain.Entities.UserNotification n)
    {
        return new UserNotificationDto(
            Id: n.Id.ToString(),
            UserId: n.UserId.ToString(),
            Type: n.Type,
            Title: n.Title,
            Message: n.Message,
            Icon: n.Icon,
            Link: n.Link,
            IsRead: n.IsRead,
            CreatedAt: n.CreatedAt.ToString("o"),
            ReadAt: n.ReadAt?.ToString("o"),
            ExpiresAt: n.ExpiresAt?.ToString("o")
        );
    }
}
