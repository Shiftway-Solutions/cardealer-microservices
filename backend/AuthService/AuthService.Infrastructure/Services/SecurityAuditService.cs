using AuthService.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// US-18.5: Security audit logging service for SIEM integration.
/// Uses structured logging (Serilog) with security-specific properties
/// that can be ingested by Splunk, Elasticsearch, Datadog, etc.
/// 
/// Log format follows Common Event Format (CEF) principles:
/// - Consistent event types for correlation
/// - Structured properties for filtering/alerting
/// - Timestamp, severity, and source identification
/// </summary>
public class SecurityAuditService : ISecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;

    // Event type constants for SIEM correlation
    private const string EVENT_LOGIN_SUCCESS = "AUTH_LOGIN_SUCCESS";
    private const string EVENT_LOGIN_FAILURE = "AUTH_LOGIN_FAILURE";
    private const string EVENT_2FA_SUCCESS = "AUTH_2FA_SUCCESS";
    private const string EVENT_2FA_FAILURE = "AUTH_2FA_FAILURE";
    private const string EVENT_PASSWORD_CHANGE = "AUTH_PASSWORD_CHANGE";
    private const string EVENT_ACCOUNT_LOCKOUT = "AUTH_ACCOUNT_LOCKOUT";
    private const string EVENT_NEW_DEVICE = "AUTH_NEW_DEVICE";
    private const string EVENT_RECOVERY_CODES_GENERATED = "AUTH_RECOVERY_CODES_GEN";
    private const string EVENT_RECOVERY_CODE_USED = "AUTH_RECOVERY_CODE_USED";
    private const string EVENT_2FA_STATUS_CHANGE = "AUTH_2FA_STATUS_CHANGE";
    private const string EVENT_SESSIONS_REVOKED = "AUTH_SESSIONS_REVOKED";
    private const string EVENT_SUSPICIOUS_ACTIVITY = "AUTH_SUSPICIOUS_ACTIVITY";

    public SecurityAuditService(ILogger<SecurityAuditService> logger)
    {
        _logger = logger;
    }

    public Task LogLoginSuccessAsync(string userId, string email, string ipAddress,
        string? userAgent = null, string? deviceFingerprint = null, bool usedTwoFactor = false)
    {
        _logger.LogInformation(
            "[SECURITY_AUDIT] {EventType} | UserId={UserId} | Email={Email} | IP={IpAddress} | " +
            "UserAgent={UserAgent} | DeviceFingerprint={DeviceFingerprint} | UsedTwoFactor={UsedTwoFactor}",
            EVENT_LOGIN_SUCCESS,
            userId,
            MaskEmail(email),
            ipAddress,
            userAgent ?? "unknown",
            deviceFingerprint ?? "none",
            usedTwoFactor);

        return Task.CompletedTask;
    }

    public Task LogLoginFailureAsync(string email, string ipAddress, string? userAgent = null,
        string? reason = null, int attemptCount = 1)
    {
        _logger.LogWarning(
            "[SECURITY_AUDIT] {EventType} | Email={Email} | IP={IpAddress} | " +
            "UserAgent={UserAgent} | Reason={Reason} | AttemptCount={AttemptCount}",
            EVENT_LOGIN_FAILURE,
            MaskEmail(email),
            ipAddress,
            userAgent ?? "unknown",
            reason ?? "invalid_credentials",
            attemptCount);

        return Task.CompletedTask;
    }

    public Task LogTwoFactorSuccessAsync(string userId, string method, string ipAddress, string? userAgent = null)
    {
        _logger.LogInformation(
            "[SECURITY_AUDIT] {EventType} | UserId={UserId} | Method={Method} | " +
            "IP={IpAddress} | UserAgent={UserAgent}",
            EVENT_2FA_SUCCESS,
            userId,
            method,
            ipAddress,
            userAgent ?? "unknown");

        return Task.CompletedTask;
    }

    public Task LogTwoFactorFailureAsync(string userId, string method, string ipAddress,
        string? userAgent = null, int attemptCount = 1)
    {
        _logger.LogWarning(
            "[SECURITY_AUDIT] {EventType} | UserId={UserId} | Method={Method} | " +
            "IP={IpAddress} | UserAgent={UserAgent} | AttemptCount={AttemptCount}",
            EVENT_2FA_FAILURE,
            userId,
            method,
            ipAddress,
            userAgent ?? "unknown",
            attemptCount);

        return Task.CompletedTask;
    }

    public Task LogPasswordChangeAsync(string userId, string email, string ipAddress,
        bool wasForced = false, string? reason = null)
    {
        _logger.LogInformation(
            "[SECURITY_AUDIT] {EventType} | UserId={UserId} | Email={Email} | " +
            "IP={IpAddress} | WasForced={WasForced} | Reason={Reason}",
            EVENT_PASSWORD_CHANGE,
            userId,
            MaskEmail(email),
            ipAddress,
            wasForced,
            reason ?? "user_initiated");

        return Task.CompletedTask;
    }

    public Task LogAccountLockoutAsync(string userId, string email, string ipAddress,
        TimeSpan lockoutDuration, int failedAttempts)
    {
        _logger.LogWarning(
            "[SECURITY_AUDIT] {EventType} | UserId={UserId} | Email={Email} | " +
            "IP={IpAddress} | LockoutMinutes={LockoutMinutes} | FailedAttempts={FailedAttempts}",
            EVENT_ACCOUNT_LOCKOUT,
            userId,
            MaskEmail(email),
            ipAddress,
            (int)lockoutDuration.TotalMinutes,
            failedAttempts);

        return Task.CompletedTask;
    }

    public Task LogNewDeviceLoginAsync(string userId, string email, string deviceName,
        string ipAddress, string? location = null)
    {
        _logger.LogInformation(
            "[SECURITY_AUDIT] {EventType} | UserId={UserId} | Email={Email} | " +
            "DeviceName={DeviceName} | IP={IpAddress} | Location={Location}",
            EVENT_NEW_DEVICE,
            userId,
            MaskEmail(email),
            deviceName,
            ipAddress,
            location ?? "unknown");

        return Task.CompletedTask;
    }

    public Task LogRecoveryCodesGeneratedAsync(string userId, string email, int codeCount)
    {
        _logger.LogInformation(
            "[SECURITY_AUDIT] {EventType} | UserId={UserId} | Email={Email} | CodeCount={CodeCount}",
            EVENT_RECOVERY_CODES_GENERATED,
            userId,
            MaskEmail(email),
            codeCount);

        return Task.CompletedTask;
    }

    public Task LogRecoveryCodeUsedAsync(string userId, string email, string ipAddress, int remainingCodes)
    {
        _logger.LogWarning(
            "[SECURITY_AUDIT] {EventType} | UserId={UserId} | Email={Email} | " +
            "IP={IpAddress} | RemainingCodes={RemainingCodes}",
            EVENT_RECOVERY_CODE_USED,
            userId,
            MaskEmail(email),
            ipAddress,
            remainingCodes);

        return Task.CompletedTask;
    }

    public Task LogTwoFactorStatusChangeAsync(string userId, string email, string method,
        bool enabled, string ipAddress)
    {
        _logger.LogInformation(
            "[SECURITY_AUDIT] {EventType} | UserId={UserId} | Email={Email} | " +
            "Method={Method} | Enabled={Enabled} | IP={IpAddress}",
            EVENT_2FA_STATUS_CHANGE,
            userId,
            MaskEmail(email),
            method,
            enabled,
            ipAddress);

        return Task.CompletedTask;
    }

    public Task LogSessionsRevokedAsync(string userId, string email, string reason, string ipAddress)
    {
        _logger.LogWarning(
            "[SECURITY_AUDIT] {EventType} | UserId={UserId} | Email={Email} | " +
            "Reason={Reason} | IP={IpAddress}",
            EVENT_SESSIONS_REVOKED,
            userId,
            MaskEmail(email),
            reason,
            ipAddress);

        return Task.CompletedTask;
    }

    public Task LogSuspiciousActivityAsync(string userId, string email, string activityType,
        string description, string ipAddress, string? userAgent = null)
    {
        _logger.LogError(
            "[SECURITY_AUDIT] {EventType} | UserId={UserId} | Email={Email} | " +
            "ActivityType={ActivityType} | Description={Description} | IP={IpAddress} | UserAgent={UserAgent}",
            EVENT_SUSPICIOUS_ACTIVITY,
            userId,
            MaskEmail(email),
            activityType,
            description,
            ipAddress,
            userAgent ?? "unknown");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Mask email for privacy in logs (show first 2 chars + domain).
    /// Example: jo***@example.com
    /// </summary>
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return "unknown";

        var parts = email.Split('@');
        if (parts.Length != 2)
            return "invalid_email";

        var local = parts[0];
        var domain = parts[1];

        if (local.Length <= 2)
            return $"{local}***@{domain}";

        return $"{local[..2]}***@{domain}";
    }
}
