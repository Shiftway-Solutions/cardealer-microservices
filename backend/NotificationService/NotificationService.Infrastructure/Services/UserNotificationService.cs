using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces.Repositories;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Persists in-app user notifications to the database.
/// These notifications appear in the header bell and /cuenta/notificaciones.
/// 
/// Security: Title and Message are sanitized to prevent XSS/injection in the UI.
/// </summary>
public class UserNotificationService : IUserNotificationService
{
    private readonly IUserNotificationRepository _repository;
    private readonly ILogger<UserNotificationService> _logger;

    public UserNotificationService(
        IUserNotificationRepository repository,
        ILogger<UserNotificationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task CreateAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string? icon = null,
        string? link = null,
        Guid? dealerId = null,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Security: strip tags from title/message to prevent XSS when rendered in frontend
            var safeTitle = SanitizeText(title, 200);
            var safeMessage = SanitizeText(message, 500);

            // Security: sanitize link to prevent javascript: URIs
            var safeLink = SanitizeLink(link);

            var notification = UserNotification.Create(
                userId: userId,
                type: type,
                title: safeTitle,
                message: safeMessage,
                icon: icon,
                link: safeLink,
                dealerId: dealerId,
                expiresAt: expiresAt);

            await _repository.AddAsync(notification);

            _logger.LogDebug(
                "Created UserNotification {Id} for UserId={UserId} Type={Type}",
                notification.Id, userId, type);
        }
        catch (Exception ex)
        {
            // Fire-and-forget: never interrupt the calling flow (email/SMS sends)
            _logger.LogError(ex,
                "Failed to persist UserNotification for UserId={UserId} Type={Type}",
                userId, type);
        }
    }

    /// <summary>
    /// Strips HTML tags and truncates to maxLength.
    /// </summary>
    private static string SanitizeText(string input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Strip HTML tags
        var stripped = System.Text.RegularExpressions.Regex.Replace(input, "<[^>]*>", string.Empty);
        // Decode HTML entities
        stripped = System.Net.WebUtility.HtmlDecode(stripped).Trim();
        // Truncate
        return stripped.Length > maxLength ? stripped[..maxLength] : stripped;
    }

    /// <summary>
    /// Allows only relative paths and https:// URLs to prevent javascript: and data: URIs.
    /// </summary>
    private static string? SanitizeLink(string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
            return null;

        var trimmed = link.Trim();

        // Allow relative paths (e.g., /cuenta/mis-vehiculos)
        if (trimmed.StartsWith("/"))
            return trimmed.Length > 500 ? trimmed[..500] : trimmed;

        // Allow https:// only
        if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return trimmed.Length > 500 ? trimmed[..500] : trimmed;

        // Reject anything else (javascript:, data:, http:, etc.)
        return null;
    }
}
