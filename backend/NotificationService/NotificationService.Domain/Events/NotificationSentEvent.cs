using MediatR;

namespace NotificationService.Domain.Events;

public class NotificationSentEvent : INotification
{
    public Guid NotificationId { get; }
    public string Recipient { get; }
    public string Type { get; }
    public DateTime SentAt { get; }
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public string? ProviderMessageId { get; }

    public NotificationSentEvent(Guid notificationId, string recipient, string type,
        DateTime sentAt, bool success, string? errorMessage = null, string? providerMessageId = null)
    {
        NotificationId = notificationId;
        Recipient = recipient;
        Type = type;
        SentAt = sentAt;
        Success = success;
        ErrorMessage = errorMessage;
        ProviderMessageId = providerMessageId;
    }

    // Factory methods
    public static NotificationSentEvent CreateSuccess(Guid notificationId, string recipient,
        string type, string? providerMessageId = null)
    {
        return new NotificationSentEvent(
            notificationId,
            recipient,
            type,
            DateTime.UtcNow,
            true,
            null,
            providerMessageId);
    }

    public static NotificationSentEvent CreateFailure(Guid notificationId, string recipient,
        string type, string errorMessage)
    {
        return new NotificationSentEvent(
            notificationId,
            recipient,
            type,
            DateTime.UtcNow,
            false,
            errorMessage);
    }
}