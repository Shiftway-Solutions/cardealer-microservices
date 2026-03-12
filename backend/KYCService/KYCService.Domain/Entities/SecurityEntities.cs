namespace KYCService.Domain.Entities;

/// <summary>
/// Stores idempotency keys to prevent duplicate requests
/// </summary>
public class IdempotencyKey
{
    public Guid Id { get; set; }

    /// <summary>
    /// The idempotency key from the client request header
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// User who made the request
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The HTTP method of the original request
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// The endpoint path of the original request
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The cached response status code
    /// </summary>
    public int ResponseStatusCode { get; set; }

    /// <summary>
    /// The cached response body (JSON)
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// When the request was first processed
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the idempotency key expires (default 24 hours)
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

    /// <summary>
    /// Whether the request is still being processed
    /// </summary>
    public bool IsProcessing { get; set; }
}

/// <summary>
/// Audit log entry for KYC operations
/// </summary>
public class KYCAuditLog
{
    public Guid Id { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Related KYC Profile ID (if applicable)
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Type of action performed
    /// </summary>
    public KYCAuditAction Action { get; set; }

    /// <summary>
    /// Detailed description of the action
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// IP address of the client
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Duration of the operation in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// When the action was performed
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Types of KYC audit actions
/// </summary>
public enum KYCAuditAction
{
    // Profile actions
    ProfileCreated = 1,
    ProfileUpdated = 2,
    ProfileApproved = 3,
    ProfileRejected = 4,
    ProfileExpired = 5,
    ProfileSuspended = 6,

    // Document actions
    DocumentUploaded = 10,
    DocumentVerified = 11,
    DocumentRejected = 12,
    DocumentDeleted = 13,

    // Identity verification
    IdentityVerificationStarted = 20,
    IdentityVerificationCompleted = 21,
    IdentityVerificationFailed = 22,
    LivenessCheckPassed = 23,
    LivenessCheckFailed = 24,
    FaceMatchPassed = 25,
    FaceMatchFailed = 26,

    // Security events
    DuplicateProfileAttempt = 30,
    DuplicateDocumentAttempt = 31,
    RateLimitExceeded = 32,
    SuspiciousActivity = 33,
    UnauthorizedAccess = 34,

    // Review actions
    SubmittedForReview = 40,
    ReviewStarted = 41,
    ReviewCompleted = 42,

    // Admin actions
    AdminOverride = 50,
    ManualVerification = 51,
    RiskLevelChanged = 52
}

/// <summary>
/// Rate limit entry for tracking request rates
/// </summary>
public class RateLimitEntry
{
    public Guid Id { get; set; }

    /// <summary>
    /// The key for rate limiting (userId or IP address)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The endpoint being rate limited
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Number of requests in the current window
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// Start of the current window
    /// </summary>
    public DateTime WindowStart { get; set; }

    /// <summary>
    /// When the window expires
    /// </summary>
    public DateTime WindowEnd { get; set; }
}

/// <summary>
/// Saga state for tracking multi-step KYC operations
/// </summary>
public class KYCSagaState
{
    public Guid Id { get; set; }

    /// <summary>
    /// Correlation ID for tracking the saga across steps
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// User who initiated the saga
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Current status of the saga
    /// </summary>
    public SagaStatus Status { get; set; } = SagaStatus.Started;

    /// <summary>
    /// Current step in the saga
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// Total number of steps
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Completed steps data as JSON for rollback
    /// </summary>
    public string CompletedStepsData { get; set; } = "[]";

    /// <summary>
    /// Profile ID created (for rollback)
    /// </summary>
    public Guid? CreatedProfileId { get; set; }

    /// <summary>
    /// Document IDs created (for rollback)
    /// </summary>
    public List<Guid> CreatedDocumentIds { get; set; } = new();

    /// <summary>
    /// Error message if saga failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Step where the error occurred
    /// </summary>
    public int? FailedAtStep { get; set; }

    /// <summary>
    /// When the saga was started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the saga was completed or failed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When rollback was completed (if applicable)
    /// </summary>
    public DateTime? RolledBackAt { get; set; }
}

/// <summary>
/// Status of a saga operation
/// </summary>
public enum SagaStatus
{
    Started = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    RollingBack = 5,
    RolledBack = 6,
    PartiallyRolledBack = 7
}
