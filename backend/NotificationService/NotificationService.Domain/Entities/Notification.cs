using CarDealer.Shared.MultiTenancy;
using NotificationService.Domain.Enums;
using System.Text.Json;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Notification entity with optional multi-tenant support.
/// When DealerId is null, the notification is global/system-wide.
/// When DealerId has a value, the notification belongs to a specific dealer.
/// </summary>
public class Notification : IOptionalTenantEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Optional dealer ID for tenant-specific notifications.
    /// Null = system/global notification, Value = dealer-specific notification.
    /// </summary>
    public Guid? DealerId { get; set; }

    public NotificationType Type { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public NotificationProvider Provider { get; set; }
    public PriorityLevel Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TemplateName { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// true = marketing/promotional message (requires user opt-in consent).
    /// false = transactional message (always allowed: confirmations, receipts, security alerts).
    /// </summary>
    public bool IsMarketing { get; set; }

    /// <summary>
    /// The recipient's user ID in UserService (for consent lookup).
    /// Null if the notification is system-generated or recipient is not a registered user.
    /// </summary>
    public Guid? RecipientUserId { get; set; }

    // Constructor
    public Notification()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Status = NotificationStatus.Pending;
        Priority = PriorityLevel.Medium;
        RetryCount = 0;
        Metadata = new Dictionary<string, object>();
    }

    // Factory methods
    public static Notification CreateEmailNotification(string to, string subject, string body,
        NotificationProvider provider = NotificationProvider.SendGrid,
        PriorityLevel priority = PriorityLevel.Medium,
        Dictionary<string, object>? metadata = null,
        Guid? dealerId = null)
    {
        return new Notification
        {
            Type = NotificationType.Email,
            Recipient = to,
            Subject = subject,
            Content = body,
            Provider = provider,
            Priority = priority,
            Metadata = metadata ?? new Dictionary<string, object>(),
            DealerId = dealerId
        };
    }

    public static Notification CreateSmsNotification(string to, string message,
        NotificationProvider provider = NotificationProvider.Twilio,
        PriorityLevel priority = PriorityLevel.Medium,
        Dictionary<string, object>? metadata = null,
        Guid? dealerId = null)
    {
        return new Notification
        {
            Type = NotificationType.Sms,
            Recipient = to,
            Content = message,
            Provider = provider,
            Priority = priority,
            Metadata = metadata ?? new Dictionary<string, object>(),
            DealerId = dealerId
        };
    }

    public static Notification CreatePushNotification(string deviceToken, string title, string body,
        NotificationProvider provider = NotificationProvider.Firebase,
        PriorityLevel priority = PriorityLevel.Medium,
        Dictionary<string, object>? metadata = null,
        Guid? dealerId = null)
    {
        return new Notification
        {
            Type = NotificationType.Push,
            Recipient = deviceToken,
            Subject = title,
            Content = body,
            Provider = provider,
            Priority = priority,
            Metadata = metadata ?? new Dictionary<string, object>(),
            DealerId = dealerId
        };
    }

    public static Notification CreateWhatsAppNotification(string to, string message,
        NotificationProvider provider = NotificationProvider.TwilioWhatsApp,
        PriorityLevel priority = PriorityLevel.Medium,
        string? templateName = null,
        Dictionary<string, object>? metadata = null,
        Guid? dealerId = null)
    {
        return new Notification
        {
            Type = NotificationType.WhatsApp,
            Recipient = to,
            Subject = templateName ?? "WhatsApp Message",
            Content = message,
            Provider = provider,
            Priority = priority,
            TemplateName = templateName,
            Metadata = metadata ?? new Dictionary<string, object>(),
            DealerId = dealerId
        };
    }

    // Business methods
    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
    }

    public void MarkAsDelivered()
    {
        Status = NotificationStatus.Delivered;
    }

    public bool CanRetry()
    {
        return Status == NotificationStatus.Failed && RetryCount < 3;
    }

    public void UpdateMetadata(string key, object value)
    {
        Metadata ??= new Dictionary<string, object>();
        Metadata[key] = value;
    }
}