namespace NotificationService.Domain.Entities;

/// <summary>
/// Persists the schedule for a 7-day onboarding report for a new dealer.
/// Survives pod restarts (stored in PostgreSQL).
/// Includes retry logic with exponential backoff.
/// </summary>
public class OnboardingReportSchedule
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public DateTime ApprovedAt { get; set; }
    public DateTime DueAt { get; set; }

    /// <summary>
    /// Scheduled, Processing, Sent, Failed
    /// </summary>
    public string Status { get; set; } = "Scheduled";

    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string? ErrorMessage { get; set; }

    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Idempotency key to prevent duplicate schedules for the same dealer.
    /// Format: "onboarding_report:{DealerId}"
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    public OnboardingReportSchedule()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public static OnboardingReportSchedule Create(Guid dealerId, Guid? ownerUserId, DateTime approvedAt, TimeSpan delay)
    {
        return new OnboardingReportSchedule
        {
            DealerId = dealerId,
            OwnerUserId = ownerUserId,
            ApprovedAt = approvedAt,
            DueAt = approvedAt.Add(delay),
            IdempotencyKey = $"onboarding_report:{dealerId}"
        };
    }

    public void MarkAsProcessing()
    {
        Status = "Processing";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsSent()
    {
        Status = "Sent";
        SentAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        RetryCount++;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;

        if (RetryCount >= MaxRetries)
        {
            Status = "Failed";
        }
        else
        {
            // Exponential backoff: 15min, 1h, 4h
            var backoff = TimeSpan.FromMinutes(15 * Math.Pow(4, RetryCount - 1));
            DueAt = DateTime.UtcNow.Add(backoff);
            Status = "Scheduled";
        }
    }

    public bool IsDue() => Status == "Scheduled" && DueAt <= DateTime.UtcNow;
    public bool CanRetry() => RetryCount < MaxRetries;
}
