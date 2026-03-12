using AuthService.Api.Helpers;
using AuthService.Application.DTOs.TwoFactor;
using AuthService.Application.Features.TwoFactor.Commands.Disable2FA;
using AuthService.Application.Features.TwoFactor.Commands.Enable2FA;
using AuthService.Application.Features.TwoFactor.Commands.GenerateRecoveryCodes;
using AuthService.Application.Features.TwoFactor.Commands.RecoveryAccountWithAllCodes;
using AuthService.Application.Features.TwoFactor.Commands.RecoveryCodeLogin;
using AuthService.Application.Features.TwoFactor.Commands.SendSms2FACode;
using AuthService.Application.Features.TwoFactor.Commands.VerifySms2FACode;
using AuthService.Application.Features.TwoFactor.Commands.TwoFactorLogin;
using AuthService.Application.Features.TwoFactor.Commands.Verify2FA;
using AuthService.Application.Features.TwoFactor.Commands.VerifyRecoveryCode;
using AuthService.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.Api.Controllers;

/// <summary>
/// Rate-limited to prevent brute-force attacks on 2FA verification/recovery codes (OWASP API4:2023)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("AuthPolicy")]
public class TwoFactorController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TwoFactorController> _logger;

    public TwoFactorController(IMediator mediator, ILogger<TwoFactorController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Enable two-factor authentication for the current user
    /// </summary>
    [HttpPost("enable")]
    public async Task<ActionResult<ApiResponse<Enable2FAResponse>>> Enable2FA([FromBody] Enable2FARequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<Enable2FAResponse>.Fail("User not authenticated"));
            }

            var command = new Enable2FACommand(userId, request.Type);
            var result = await _mediator.Send(command);

            _logger.LogInformation("2FA enabled successfully for user {UserId} with type {Type}", userId, request.Type);
            return Ok(ApiResponse<Enable2FAResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling 2FA for user");
            return BadRequest(ApiResponse<Enable2FAResponse>.Fail("Failed to enable two-factor authentication."));
        }
    }

    /// <summary>
    /// Verify two-factor authentication setup
    /// </summary>
    [HttpPost("verify")]
    public async Task<ActionResult<ApiResponse<Verify2FAResponse>>> Verify2FA([FromBody] Verify2FARequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<Verify2FAResponse>.Fail("User not authenticated"));
            }

            var command = new Verify2FACommand(userId, request.Code, request.Type);
            var result = await _mediator.Send(command);

            if (result.Success)
            {
                _logger.LogInformation("2FA verified successfully for user {UserId}", userId);
                return Ok(ApiResponse<Verify2FAResponse>.Ok(result));
            }
            else
            {
                return BadRequest(ApiResponse<Verify2FAResponse>.Fail(result.Message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying 2FA for user");
            return BadRequest(ApiResponse<Verify2FAResponse>.Fail("Failed to verify two-factor code."));
        }
    }

    /// <summary>
    /// Disable two-factor authentication for the current user
    /// </summary>
    [HttpPost("disable")]
    public async Task<ActionResult<ApiResponse<Verify2FAResponse>>> Disable2FA([FromBody] Disable2FARequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<Verify2FAResponse>.Fail("User not authenticated"));
            }

            var command = new Disable2FACommand(userId, request.Password);
            var result = await _mediator.Send(command);

            _logger.LogInformation("2FA disabled successfully for user {UserId}", userId);
            return Ok(ApiResponse<Verify2FAResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling 2FA for user");
            return BadRequest(ApiResponse<Verify2FAResponse>.Fail("Failed to disable two-factor authentication."));
        }
    }

    /// <summary>
    /// Generate new recovery codes for two-factor authentication
    /// </summary>
    [HttpPost("generate-recovery-codes")]
    public async Task<ActionResult<ApiResponse<GenerateRecoveryCodesResponse>>> GenerateRecoveryCodes([FromBody] GenerateRecoveryCodesRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<GenerateRecoveryCodesResponse>.Fail("User not authenticated"));
            }

            var command = new GenerateRecoveryCodesCommand(userId, request.Password);
            var result = await _mediator.Send(command);

            _logger.LogInformation("Recovery codes generated successfully for user {UserId}", userId);
            return Ok(ApiResponse<GenerateRecoveryCodesResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recovery codes for user");
            return BadRequest(ApiResponse<GenerateRecoveryCodesResponse>.Fail("Failed to generate recovery codes."));
        }
    }

    /// <summary>
    /// Verify a recovery code for two-factor authentication
    /// </summary>
    [HttpPost("verify-recovery-code")]
    public async Task<ActionResult<ApiResponse<Verify2FAResponse>>> VerifyRecoveryCode([FromBody] VerifyRecoveryCodeRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<Verify2FAResponse>.Fail("User not authenticated"));
            }

            var command = new VerifyRecoveryCodeCommand(userId, request.Code);
            var result = await _mediator.Send(command);

            if (result.Success)
            {
                _logger.LogInformation("Recovery code verified successfully for user {UserId}", userId);
                return Ok(ApiResponse<Verify2FAResponse>.Ok(result));
            }
            else
            {
                return BadRequest(ApiResponse<Verify2FAResponse>.Fail(result.Message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying recovery code for user");
            return BadRequest(ApiResponse<Verify2FAResponse>.Fail("Failed to verify recovery code."));
        }
    }

    /// <summary>
    /// Complete two-factor authentication login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TwoFactorLoginResponse>>> TwoFactorLogin([FromBody] TwoFactorLoginRequest request)
    {
        try
        {
            var command = new TwoFactorLoginCommand(request.TempToken, request.TwoFactorCode);
            var result = await _mediator.Send(command);

            _logger.LogInformation("2FA login completed successfully for user {UserId}", result.UserId);

            // Security (CWE-922): Set tokens as HttpOnly cookies
            if (!string.IsNullOrEmpty(result.AccessToken))
            {
                AuthCookieHelper.SetAuthCookies(Response, result.AccessToken, result.RefreshToken, result.ExpiresAt);
            }

            return Ok(ApiResponse<TwoFactorLoginResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 2FA login");
            return BadRequest(ApiResponse<TwoFactorLoginResponse>.Fail("Two-factor authentication failed."));
        }
    }

    /// <summary>
    /// Login using a recovery code when 2FA device is unavailable.
    /// 
    /// Industry Standard (Google, GitHub, Microsoft):
    /// - Separate endpoint for recovery codes
    /// - Each recovery code can only be used ONCE
    /// - Response includes remaining codes count and warning if low
    /// 
    /// Use Case: User lost their phone, app was deleted, can't access authenticator
    /// </summary>
    /// <remarks>
    /// Recovery codes are 8-character alphanumeric codes (e.g., "H29S41MV").
    /// Users receive 10 codes when enabling 2FA. Each code is consumed after use.
    /// When codes run low (3 or less), a warning is included in the response.
    /// </remarks>
    [HttpPost("login-with-recovery")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<RecoveryCodeLoginResponse>>> LoginWithRecoveryCode([FromBody] RecoveryCodeLoginRequest request)
    {
        try
        {
            var command = new RecoveryCodeLoginCommand(request.TempToken, request.RecoveryCode);
            var result = await _mediator.Send(command);

            _logger.LogInformation(
                "Recovery code login completed for user {UserId}. Remaining codes: {RemainingCodes}",
                result.UserId,
                result.RemainingRecoveryCodes
            );

            // Security (CWE-922): Set tokens as HttpOnly cookies
            if (!string.IsNullOrEmpty(result.AccessToken))
            {
                AuthCookieHelper.SetAuthCookies(Response, result.AccessToken, result.RefreshToken, result.ExpiresAt);
            }

            return Ok(ApiResponse<RecoveryCodeLoginResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during recovery code login");
            return BadRequest(ApiResponse<RecoveryCodeLoginResponse>.Fail("Recovery code login failed."));
        }
    }

    /// <summary>
    /// EMERGENCY: Recover account by providing ALL 10 original recovery codes.
    /// 
    /// Use this endpoint when:
    /// - You lost access to your authenticator app
    /// - Individual recovery codes in cache have expired
    /// - You still have ALL 10 original recovery codes
    /// 
    /// What happens:
    /// 1. All 10 codes are verified against the database
    /// 2. If ALL match → account ownership is proven
    /// 3. New authenticator secret + QR code is generated
    /// 4. New 10 recovery codes are generated
    /// 5. Full access tokens are returned
    /// 
    /// Industry Pattern: "Full Recovery Code Verification" (Google, Microsoft, GitHub)
    /// </summary>
    /// <param name="request">TempToken + all 10 recovery codes</param>
    /// <returns>New 2FA setup (QR code, secret) + new recovery codes + access tokens</returns>
    [HttpPost("recover-with-all-codes")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RecoveryAccountWithAllCodesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RecoveryAccountWithAllCodesResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RecoveryAccountWithAllCodesResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<RecoveryAccountWithAllCodesResponse>>> RecoverAccountWithAllCodes(
        [FromBody] RecoveryAccountWithAllCodesRequest request)
    {
        try
        {
            var command = new RecoveryAccountWithAllCodesCommand(request.TempToken, request.RecoveryCodes);
            var result = await _mediator.Send(command);

            _logger.LogInformation(
                "Account recovery successful for user {UserId}. New 2FA setup created.",
                result.UserId
            );

            return Ok(ApiResponse<RecoveryAccountWithAllCodesResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full account recovery");
            return BadRequest(ApiResponse<RecoveryAccountWithAllCodesResponse>.Fail("Account recovery failed. Please verify your recovery codes."));
        }
    }

    /// <summary>
    /// Send SMS 2FA verification code after login
    /// </summary>
    /// <remarks>
    /// This endpoint sends a 6-digit verification code via SMS to the user's registered phone number.
    /// The code is valid for 5 minutes. Requires a valid tempToken from the login response.
    /// </remarks>
    /// <param name="request">TempToken from login response</param>
    /// <returns>Masked phone number and expiration info</returns>
    [HttpPost("send-sms-code")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SendSms2FACodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SendSms2FACodeResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SendSms2FACodeResponse>>> SendSmsCode(
        [FromBody] SendSms2FACodeRequest request)
    {
        try
        {
            var command = new SendSms2FACodeCommand(request.TempToken);
            var result = await _mediator.Send(command);

            _logger.LogInformation(
                "SMS 2FA code sent to user {UserId}, phone {MaskedPhone}",
                result.UserId,
                result.MaskedPhoneNumber
            );

            return Ok(ApiResponse<SendSms2FACodeResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS 2FA code");
            return BadRequest(ApiResponse<SendSms2FACodeResponse>.Fail("Failed to send SMS verification code."));
        }
    }

    /// <summary>
    /// Verify SMS 2FA code and complete login
    /// </summary>
    /// <remarks>
    /// Verifies the 6-digit code sent via SMS. After 5 failed attempts, the user will be locked out for 15 minutes.
    /// On success, returns full access tokens to complete the login process.
    /// </remarks>
    /// <param name="request">TempToken + 6-digit SMS code</param>
    /// <returns>Access token, refresh token, and user info</returns>
    [HttpPost("verify-sms-code")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<VerifySms2FACodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VerifySms2FACodeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VerifySms2FACodeResponse>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<VerifySms2FACodeResponse>>> VerifySmsCode(
        [FromBody] VerifySms2FACodeRequest request)
    {
        try
        {
            var command = new VerifySms2FACodeCommand(request.TempToken, request.Code);
            var result = await _mediator.Send(command);

            _logger.LogInformation("SMS 2FA verification successful for user {UserId}", result.UserId);
            return Ok(ApiResponse<VerifySms2FACodeResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying SMS 2FA code");

            if (ex.Message.Contains("Too many failed attempts"))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    ApiResponse<VerifySms2FACodeResponse>.Fail("Too many failed attempts. Please try again later."));
            }

            return BadRequest(ApiResponse<VerifySms2FACodeResponse>.Fail("SMS verification failed."));
        }
    }
}
