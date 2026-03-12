using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace KYCService.Application.Clients;

/// <summary>
/// Client for communicating with the centralized AuditService microservice
/// </summary>
public interface IAuditServiceClient
{
    /// <summary>
    /// Log an audit event to the centralized audit service
    /// </summary>
    Task<bool> LogAsync(AuditLogRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Log a KYC-specific audit event
    /// </summary>
    Task<bool> LogKYCEventAsync(
        string userId,
        string action,
        string resource,
        string ipAddress,
        string? userAgent = null,
        bool success = true,
        string? errorMessage = null,
        long? durationMs = null,
        Dictionary<string, object>? additionalData = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for creating audit logs
/// </summary>
public class AuditLogRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string UserIp { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public long? DurationMs { get; set; }
    public string? CorrelationId { get; set; }
    public string ServiceName { get; set; } = "KYCService";
    /// <summary>
    /// Severity level: 1=Debug, 2=Information, 3=Warning, 4=Error, 5=Critical
    /// </summary>
    public int Severity { get; set; } = 2; // Information
}

/// <summary>
/// Implementation of AuditService client using HttpClient
/// </summary>
public class AuditServiceClient : IAuditServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuditServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string ServiceName = "KYCService";

    public AuditServiceClient(
        HttpClient httpClient,
        ILogger<AuditServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<bool> LogAsync(AuditLogRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            request.ServiceName = ServiceName;

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/audit", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Audit log sent successfully: {Action} on {Resource}",
                    request.Action, request.Resource);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to send audit log: {StatusCode} - {Error}",
                response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            // Don't let audit failures affect the main operation
            _logger.LogError(ex, "Error sending audit log to AuditService: {Action}", request.Action);
            return false;
        }
    }

    public async Task<bool> LogKYCEventAsync(
        string userId,
        string action,
        string resource,
        string ipAddress,
        string? userAgent = null,
        bool success = true,
        string? errorMessage = null,
        long? durationMs = null,
        Dictionary<string, object>? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        var request = new AuditLogRequest
        {
            UserId = userId,
            Action = $"KYC.{action}",
            Resource = resource,
            UserIp = ipAddress,
            UserAgent = userAgent ?? "Unknown",
            Success = success,
            ErrorMessage = errorMessage,
            DurationMs = durationMs,
            AdditionalData = additionalData ?? new Dictionary<string, object>(),
            ServiceName = ServiceName,
            Severity = success ? 2 : 3, // 2=Information, 3=Warning
            CorrelationId = Guid.NewGuid().ToString()
        };

        return await LogAsync(request, cancellationToken);
    }
}

/// <summary>
/// KYC-specific audit actions
/// </summary>
public static class KYCAuditActions
{
    // Profile actions
    public const string ProfileCreated = "Profile.Created";
    public const string ProfileUpdated = "Profile.Updated";
    public const string ProfileDeleted = "Profile.Deleted";
    public const string ProfileViewed = "Profile.Viewed";
    public const string ProfileSubmittedForReview = "Profile.SubmittedForReview";
    public const string ProfileApproved = "Profile.Approved";
    public const string ProfileRejected = "Profile.Rejected";
    public const string ProfileSuspended = "Profile.Suspended";

    // Document actions
    public const string DocumentUploaded = "Document.Uploaded";
    public const string DocumentDeleted = "Document.Deleted";
    public const string DocumentVerified = "Document.Verified";
    public const string DocumentRejected = "Document.Rejected";
    public const string DocumentExpired = "Document.Expired";

    // Identity verification
    public const string IdentityVerificationStarted = "Identity.VerificationStarted";
    public const string IdentityVerificationCompleted = "Identity.VerificationCompleted";
    public const string IdentityVerificationFailed = "Identity.VerificationFailed";
    public const string FaceComparisonPassed = "Identity.FaceComparisonPassed";
    public const string FaceComparisonFailed = "Identity.FaceComparisonFailed";
    public const string LivenessCheckPassed = "Identity.LivenessCheckPassed";
    public const string LivenessCheckFailed = "Identity.LivenessCheckFailed";

    // Security events
    public const string RateLimitExceeded = "Security.RateLimitExceeded";
    public const string DuplicateRequestBlocked = "Security.DuplicateRequestBlocked";
    public const string UnauthorizedAccess = "Security.UnauthorizedAccess";
    public const string SuspiciousActivity = "Security.SuspiciousActivity";

    // Risk assessment
    public const string RiskAssessmentCompleted = "Risk.AssessmentCompleted";
    public const string HighRiskDetected = "Risk.HighRiskDetected";
    public const string WatchlistMatch = "Risk.WatchlistMatch";
}
