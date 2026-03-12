namespace NotificationService.Domain.Entities;

/// <summary>
/// User notification entity for in-app notifications shown in the UI
/// These are the notifications that appear in the header dropdown
/// </summary>
public class UserNotification
{
    public Guid Id { get; set; }

    /// <summary>
    /// The user ID this notification belongs to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Optional dealer ID for dealer-specific notifications
    /// </summary>
    public Guid? DealerId { get; set; }

    /// <summary>
    /// Type of notification for icon/styling
    /// </summary>
    public string Type { get; set; } = "system";

    /// <summary>
    /// Short title displayed in notification list
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed message content
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional emoji/icon to display
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Optional link when clicking the notification
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// Whether the user has read this notification
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// When the notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the notification was read (if applicable)
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Optional expiration date for the notification
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    public UserNotification()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsRead = false;
        Metadata = new Dictionary<string, object>();
    }

    public static UserNotification Create(
        Guid userId,
        string type,
        string title,
        string message,
        string? icon = null,
        string? link = null,
        Guid? dealerId = null,
        DateTime? expiresAt = null)
    {
        return new UserNotification
        {
            UserId = userId,
            DealerId = dealerId,
            Type = type,
            Title = title,
            Message = message,
            Icon = icon,
            Link = link,
            ExpiresAt = expiresAt
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
