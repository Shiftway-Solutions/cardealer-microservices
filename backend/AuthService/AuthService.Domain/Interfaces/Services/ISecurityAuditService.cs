namespace AuthService.Domain.Interfaces.Services;

/// <summary>
/// US-18.5: Service for audit logging to SIEM systems.
/// Logs security-relevant events in structured format compatible with
/// Splunk, Elasticsearch, Datadog, and other SIEM platforms.
/// </summary>
public interface ISecurityAuditService
{
    /// <summary>
    /// Log a successful login event.
    /// </summary>
    Task LogLoginSuccessAsync(string userId, string email, string ipAddress, string? userAgent = null,
        string? deviceFingerprint = null, bool usedTwoFactor = false);

    /// <summary>
    /// Log a failed login attempt.
    /// </summary>
    Task LogLoginFailureAsync(string email, string ipAddress, string? userAgent = null,
        string? reason = null, int attemptCount = 1);

    /// <summary>
    /// Log a successful 2FA verification.
    /// </summary>
    Task LogTwoFactorSuccessAsync(string userId, string method, string ipAddress, string? userAgent = null);

    /// <summary>
    /// Log a failed 2FA attempt.
    /// </summary>
    Task LogTwoFactorFailureAsync(string userId, string method, string ipAddress,
        string? userAgent = null, int attemptCount = 1);

    /// <summary>
    /// Log a password change event.
    /// </summary>
    Task LogPasswordChangeAsync(string userId, string email, string ipAddress,
        bool wasForced = false, string? reason = null);

    /// <summary>
    /// Log an account lockout.
    /// </summary>
    Task LogAccountLockoutAsync(string userId, string email, string ipAddress,
        TimeSpan lockoutDuration, int failedAttempts);

    /// <summary>
    /// Log a new device login.
    /// </summary>
    Task LogNewDeviceLoginAsync(string userId, string email, string deviceName,
        string ipAddress, string? location = null);

    /// <summary>
    /// Log when recovery codes are generated.
    /// </summary>
    Task LogRecoveryCodesGeneratedAsync(string userId, string email, int codeCount);

    /// <summary>
    /// Log when a recovery code is used.
    /// </summary>
    Task LogRecoveryCodeUsedAsync(string userId, string email, string ipAddress, int remainingCodes);

    /// <summary>
    /// Log when 2FA is enabled or disabled.
    /// </summary>
    Task LogTwoFactorStatusChangeAsync(string userId, string email, string method,
        bool enabled, string ipAddress);

    /// <summary>
    /// Log when all sessions are revoked.
    /// </summary>
    Task LogSessionsRevokedAsync(string userId, string email, string reason, string ipAddress);

    /// <summary>
    /// Log suspicious activity detection.
    /// </summary>
    Task LogSuspiciousActivityAsync(string userId, string email, string activityType,
        string description, string ipAddress, string? userAgent = null);
}
