namespace AuthService.Application.DTOs.TwoFactor;

/// <summary>
/// Response for recovery code login.
/// Extends standard login response with recovery code usage information.
/// </summary>
public record RecoveryCodeLoginResponse(
    string UserId,
    string Email,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,

    /// <summary>
    /// Number of unused recovery codes remaining.
    /// If this is low (e.g., 3 or less), prompt user to regenerate codes.
    /// </summary>
    int RemainingRecoveryCodes,

    /// <summary>
    /// Warning message if recovery codes are running low
    /// </summary>
    string? Warning = null
);
