using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

public class NotificationQueue
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;
    public DateTime QueuedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public QueueStatus Status { get; set; }
    public DateTime? NextRetryAt { get; set; }

    // Constructor
    public NotificationQueue()
    {
        Id = Guid.NewGuid();
        QueuedAt = DateTime.UtcNow;
        Status = QueueStatus.Pending;
        RetryCount = 0;
    }

    // Factory method
    public static NotificationQueue Create(Notification notification)
    {
        return new NotificationQueue
        {
            NotificationId = notification.Id,
            Notification = notification
        };
    }

    // Business methods
    public void MarkAsProcessing()
    {
        Status = QueueStatus.Processing;
    }

    public void MarkAsCompleted()
    {
        Status = QueueStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = QueueStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;

        // Calculate next retry time (exponential backoff)
        if (RetryCount < 5)
        {
            NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, RetryCount));
            Status = QueueStatus.Retry;
        }
    }

    public bool CanRetry()
    {
        return Status == QueueStatus.Failed && RetryCount < 5 && NextRetryAt <= DateTime.UtcNow;
    }

    public void MarkForRetry()
    {
        if (CanRetry())
        {
            Status = QueueStatus.Pending;
            ErrorMessage = null;
        }
    }
}