using NotificationService.Domain.Enums;
using System.Text.Json;

namespace NotificationService.Domain.Entities;

public class NotificationLog
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
    public string? ProviderResponse { get; set; }
    public string? ProviderMessageId { get; set; }
    public decimal? Cost { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    // Constructor
    public NotificationLog()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }

    // Factory methods
    public static NotificationLog CreateSent(Guid notificationId, string? providerMessageId = null,
        string? providerResponse = null, decimal? cost = null)
    {
        return new NotificationLog
        {
            NotificationId = notificationId,
            Action = "SENT",
            Details = "Notification sent successfully",
            ProviderMessageId = providerMessageId,
            ProviderResponse = providerResponse,
            Cost = cost
        };
    }

    public static NotificationLog CreateFailed(Guid notificationId, string errorMessage,
        string? providerResponse = null)
    {
        return new NotificationLog
        {
            NotificationId = notificationId,
            Action = "FAILED",
            ErrorMessage = errorMessage,
            ProviderResponse = providerResponse,
            Details = "Failed to send notification"
        };
    }

    public static NotificationLog CreateDelivered(Guid notificationId, string? providerResponse = null)
    {
        return new NotificationLog
        {
            NotificationId = notificationId,
            Action = "DELIVERED",
            Details = "Notification delivered to recipient",
            ProviderResponse = providerResponse
        };
    }

    public static NotificationLog CreateOpened(Guid notificationId, string? ipAddress = null,
        string? userAgent = null)
    {
        return new NotificationLog
        {
            NotificationId = notificationId,
            Action = "OPENED",
            Details = "Notification was opened by recipient",
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    public static NotificationLog CreateClicked(Guid notificationId, string linkUrl,
        string? ipAddress = null, string? userAgent = null)
    {
        return new NotificationLog
        {
            NotificationId = notificationId,
            Action = "CLICKED",
            Details = $"Recipient clicked link: {linkUrl}",
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    // Business methods
    public bool IsSuccess()
    {
        return Action == "SENT" || Action == "DELIVERED";
    }

    public bool IsFailure()
    {
        return Action == "FAILED";
    }

    public void UpdateDetails(string details)
    {
        Details = details;
        Timestamp = DateTime.UtcNow;
    }
}