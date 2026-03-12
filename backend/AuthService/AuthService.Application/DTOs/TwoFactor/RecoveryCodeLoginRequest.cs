namespace AuthService.Application.DTOs.TwoFactor;

/// <summary>
/// Request for logging in using a recovery code when 2FA device is unavailable.
/// Industry standard: separate endpoint for recovery codes vs regular 2FA codes.
/// </summary>
public record RecoveryCodeLoginRequest(
    /// <summary>
    /// Temporary token received from initial login attempt
    /// </summary>
    string TempToken,

    /// <summary>
    /// One of the 8-character alphanumeric recovery codes (e.g., "H29S41MV")
    /// Each code can only be used ONCE
    /// </summary>
    string RecoveryCode
);
