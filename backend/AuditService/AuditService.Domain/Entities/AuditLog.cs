using AuditService.Domain.Common;
using AuditService.Shared.Enums;
using System.Text.Json;

namespace AuditService.Domain.Entities;

/// <summary>
/// Represents an audit log entry that records user actions and system events
/// </summary>
public class AuditLog : EntityBase, IAggregateRoot
{
    /// <summary>
    /// ID of the user who performed the action
    /// </summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>
    /// The action performed
    /// </summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>
    /// The resource that was affected by the action
    /// </summary>
    public string Resource { get; private set; } = string.Empty;

    /// <summary>
    /// IP address of the user who performed the action
    /// </summary>
    public string UserIp { get; private set; } = string.Empty;

    /// <summary>
    /// User agent string from the request
    /// </summary>
    public string UserAgent { get; private set; } = string.Empty;

    /// <summary>
    /// Additional context data stored as JSON
    /// </summary>
    public string AdditionalDataJson { get; private set; } = "{}";

    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool Success { get; private set; } = true;

    /// <summary>
    /// Error message if the action failed
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Duration of the operation in milliseconds
    /// </summary>
    public long? DurationMs { get; private set; }

    /// <summary>
    /// Correlation ID for tracing requests across services
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// Name of the service that generated the audit event
    /// </summary>
    public string ServiceName { get; private set; } = "Unknown";

    /// <summary>
    /// Severity level of the audit event
    /// </summary>
    public AuditSeverity Severity { get; private set; } = AuditSeverity.Information;

    // Private constructor for Entity Framework
    private AuditLog() { }

    /// <summary>
    /// Creates a new audit log entry
    /// </summary>
    public AuditLog(
        string userId,
        string action,
        string resource,
        string userIp,
        string userAgent,
        Dictionary<string, object> additionalData,
        bool success = true,
        string? errorMessage = null,
        long? durationMs = null,
        string? correlationId = null,
        string serviceName = "Unknown",
        AuditSeverity severity = AuditSeverity.Information)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or empty", nameof(action));
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource cannot be null or empty", nameof(resource));
        if (string.IsNullOrWhiteSpace(userIp))
            throw new ArgumentException("UserIp cannot be null or empty", nameof(userIp));
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("UserAgent cannot be null or empty", nameof(userAgent));

        UserId = userId;
        Action = action;
        Resource = resource;
        UserIp = userIp;
        UserAgent = userAgent;
        Success = success;
        ErrorMessage = errorMessage;
        DurationMs = durationMs;
        CorrelationId = correlationId;
        ServiceName = serviceName;
        Severity = severity;

        // Serialize additional data
        AdditionalDataJson = JsonSerializer.Serialize(additionalData ?? new Dictionary<string, object>());

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a successful audit log entry
    /// </summary>
    public static AuditLog CreateSuccess(
        string userId,
        string action,
        string resource,
        string userIp,
        string userAgent,
        Dictionary<string, object>? additionalData = null,
        long? durationMs = null,
        string? correlationId = null,
        string serviceName = "Unknown")
    {
        return new AuditLog(
            userId,
            action,
            resource,
            userIp,
            userAgent,
            additionalData ?? new Dictionary<string, object>(),
            true,
            null,
            durationMs,
            correlationId,
            serviceName,
            AuditSeverity.Information);
    }

    /// <summary>
    /// Creates a failed audit log entry
    /// </summary>
    public static AuditLog CreateFailure(
        string userId,
        string action,
        string resource,
        string userIp,
        string userAgent,
        string errorMessage,
        Dictionary<string, object>? additionalData = null,
        long? durationMs = null,
        string? correlationId = null,
        string serviceName = "Unknown",
        AuditSeverity severity = AuditSeverity.Error)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("ErrorMessage cannot be null or empty for failed audit log", nameof(errorMessage));

        return new AuditLog(
            userId,
            action,
            resource,
            userIp,
            userAgent,
            additionalData ?? new Dictionary<string, object>(),
            false,
            errorMessage,
            durationMs,
            correlationId,
            serviceName,
            severity);
    }

    /// <summary>
    /// Creates a system audit log entry
    /// </summary>
    public static AuditLog CreateSystem(
        string action,
        string resource,
        Dictionary<string, object>? additionalData = null,
        bool success = true,
        string? errorMessage = null,
        AuditSeverity severity = AuditSeverity.Information)
    {
        return new AuditLog(
            "system",
            action,
            resource,
            "127.0.0.1",
            "System",
            additionalData ?? new Dictionary<string, object>(),
            success,
            errorMessage,
            null,
            null,
            "AuditService",
            severity);
    }

    /// <summary>
    /// Creates an anonymous audit log entry
    /// </summary>
    public static AuditLog CreateAnonymous(
        string action,
        string resource,
        string userIp,
        string userAgent,
        Dictionary<string, object>? additionalData = null,
        bool success = true,
        string? errorMessage = null,
        AuditSeverity severity = AuditSeverity.Information)
    {
        return new AuditLog(
            "anonymous",
            action,
            resource,
            userIp,
            userAgent,
            additionalData ?? new Dictionary<string, object>(),
            success,
            errorMessage,
            null,
            null,
            "AuditService",
            severity);
    }

    /// <summary>
    /// Marks the audit log as failed with an error message
    /// </summary>
    public void MarkAsFailed(string errorMessage, AuditSeverity severity = AuditSeverity.Error)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("ErrorMessage cannot be null or empty", nameof(errorMessage));

        Success = false;
        ErrorMessage = errorMessage;
        Severity = severity;
        MarkAsUpdated();
    }

    /// <summary>
    /// Marks the audit log as successful
    /// </summary>
    public void MarkAsSuccess()
    {
        Success = true;
        ErrorMessage = null;
        Severity = AuditSeverity.Information;
        MarkAsUpdated();
    }

    /// <summary>
    /// Sets the duration of the operation
    /// </summary>
    public void SetDuration(long durationMs)
    {
        if (durationMs < 0)
            throw new ArgumentException("Duration cannot be negative", nameof(durationMs));

        DurationMs = durationMs;
        MarkAsUpdated();
    }

    /// <summary>
    /// Sets the correlation ID for distributed tracing
    /// </summary>
    public void SetCorrelationId(string correlationId)
    {
        CorrelationId = correlationId;
        MarkAsUpdated();
    }

    /// <summary>
    /// Sets the service name that generated the event
    /// </summary>
    public void SetServiceName(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("ServiceName cannot be null or empty", nameof(serviceName));

        ServiceName = serviceName;
        MarkAsUpdated();
    }

    /// <summary>
    /// Sets the severity level of the audit event
    /// </summary>
    public void SetSeverity(AuditSeverity severity)
    {
        Severity = severity;
        MarkAsUpdated();
    }

    /// <summary>
    /// Additional data as a dictionary (deserialized from JSON)
    /// </summary>
    public Dictionary<string, object> AdditionalData
    {
        get
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(AdditionalDataJson)
                    ?? new Dictionary<string, object>();
            }
            catch (JsonException)
            {
                // Log the error but don't throw - return empty dictionary
                return new Dictionary<string, object>();
            }
        }
    }

    /// <summary>
    /// Adds additional data to the audit log
    /// </summary>
    public void AddAdditionalData(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value), "Value cannot be null");

        var currentData = AdditionalData;
        currentData[key] = value;
        AdditionalDataJson = JsonSerializer.Serialize(currentData);
        MarkAsUpdated();
    }

    /// <summary>
    /// Removes additional data from the audit log
    /// </summary>
    public void RemoveAdditionalData(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var currentData = AdditionalData;
        if (currentData.Remove(key))
        {
            AdditionalDataJson = JsonSerializer.Serialize(currentData);
            MarkAsUpdated();
        }
    }

    /// <summary>
    /// Checks if the audit log is for a system event
    /// </summary>
    public bool IsSystemEvent() => UserId == "system";

    /// <summary>
    /// Checks if the audit log is for an anonymous user
    /// </summary>
    public bool IsAnonymous() => UserId == "anonymous";

    /// <summary>
    /// Gets the display name for the user (system, anonymous, or actual user ID)
    /// </summary>
    public string GetUserDisplayName()
    {
        return UserId switch
        {
            "system" => "System",
            "anonymous" => "Anonymous",
            _ => UserId
        };
    }

    /// <summary>
    /// Validates the audit log entry
    /// </summary>
    public override bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Id) &&
               !string.IsNullOrWhiteSpace(UserId) &&
               !string.IsNullOrWhiteSpace(Action) &&
               !string.IsNullOrWhiteSpace(Resource) &&
               !string.IsNullOrWhiteSpace(UserIp) &&
               !string.IsNullOrWhiteSpace(UserAgent) &&
               !string.IsNullOrWhiteSpace(ServiceName) &&
               CreatedAt <= DateTime.UtcNow &&
               Enum.IsDefined(typeof(AuditSeverity), Severity);
    }

    /// <summary>
    /// Gets a summary of the audit log for display purposes
    /// </summary>
    public string GetSummary()
    {
        var status = Success ? "SUCCESS" : "FAILED";
        var user = GetUserDisplayName();
        var duration = DurationMs.HasValue ? $" in {DurationMs}ms" : "";
        return $"{Action} on {Resource} by {user} - {status}{duration}";
    }

    /// <summary>
    /// Gets a detailed description of the audit log
    /// </summary>
    public string GetDetailedDescription()
    {
        var summary = GetSummary();
        var severity = Severity.ToString().ToUpper();

        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            return $"{summary} | Severity: {severity} | Error: {ErrorMessage}";
        }

        return $"{summary} | Severity: {severity}";
    }

    /// <summary>
    /// Checks if this audit log represents a security-related event
    /// </summary>
    public bool IsSecurityEvent()
    {
        return Severity == AuditSeverity.Error ||
               Severity == AuditSeverity.Critical ||
               !Success;
    }

    /// <summary>
    /// Checks if this audit log requires immediate attention
    /// </summary>
    public bool RequiresAttention()
    {
        return Severity == AuditSeverity.Critical ||
               (Severity == AuditSeverity.Error && !Success);
    }

    /// <summary>
    /// Gets the age of the audit log in days
    /// </summary>
    public double GetAgeInDays()
    {
        return (DateTime.UtcNow - CreatedAt).TotalDays;
    }

    /// <summary>
    /// Creates a shallow copy of the audit log for reporting purposes
    /// </summary>

    public new AuditLog ShallowCopy()
    {
        return (AuditLog)MemberwiseClone();
    }

    /// <summary>
    /// Updates the audit log timestamp (used for batch operations)
    /// </summary>
    public void UpdateTimestamp()
    {
        MarkAsUpdated();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
        yield return UserId;
        yield return Action;
        yield return Resource;
        yield return CreatedAt;
    }

    public override string ToString()
    {
        return $"{GetType().Name} [Id={Id}, Action={Action}, User={GetUserDisplayName()}, Success={Success}]";
    }


}