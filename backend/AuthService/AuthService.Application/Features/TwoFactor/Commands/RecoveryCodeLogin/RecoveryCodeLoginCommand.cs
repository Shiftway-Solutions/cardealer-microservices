using MediatR;
using AuthService.Application.DTOs.TwoFactor;

namespace AuthService.Application.Features.TwoFactor.Commands.RecoveryCodeLogin;

/// <summary>
/// Command for logging in using a recovery code.
/// Industry Standard: Recovery codes are separate from regular 2FA codes.
/// Used when user has lost access to their 2FA device (phone lost, app deleted, etc.)
/// </summary>
public record RecoveryCodeLoginCommand(
    /// <summary>
    /// Temporary token from initial login attempt
    /// </summary>
    string TempToken,

    /// <summary>
    /// 8-character alphanumeric recovery code (e.g., "H29S41MV")
    /// Each code is ONE-TIME USE only
    /// </summary>
    string RecoveryCode
) : IRequest<RecoveryCodeLoginResponse>;
