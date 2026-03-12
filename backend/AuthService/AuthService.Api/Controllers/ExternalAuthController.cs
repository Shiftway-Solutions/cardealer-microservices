using AuthService.Api.Helpers;
using AuthService.Application.DTOs.ExternalAuth;
using AuthService.Application.Features.ExternalAuth.Commands.ExternalAuth;
using AuthService.Application.Features.ExternalAuth.Commands.ExternalAuthCallback;
using AuthService.Application.Features.ExternalAuth.Commands.ExternalLogin;
using AuthService.Application.Features.ExternalAuth.Commands.LinkExternalAccount;
using AuthService.Application.Features.ExternalAuth.Commands.UnlinkExternalAccount;
using AuthService.Application.Features.ExternalAuth.Commands.ValidateUnlinkAccount;
using AuthService.Application.Features.ExternalAuth.Commands.RequestUnlinkCode;
using AuthService.Application.Features.ExternalAuth.Commands.UnlinkActiveProvider;
using AuthService.Application.Features.ExternalAuth.Queries.GetLinkedAccounts;
using AuthService.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("AuthPolicy")]
public class ExternalAuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ExternalAuthController> _logger;

    public ExternalAuthController(IMediator mediator, ILogger<ExternalAuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate with external provider using ID token
    /// </summary>
    [HttpPost("authenticate")]
    public async Task<ActionResult<ApiResponse<ExternalAuthResponse>>> Authenticate(
        [FromBody] ExternalAuthRequest request)
    {
        try
        {
            _logger.LogInformation("External authentication attempt with provider: {Provider}", request.Provider);

            var command = new ExternalAuthCommand(request.Provider, request.IdToken);
            var result = await _mediator.Send(command);

            _logger.LogInformation("External authentication successful for user {UserId} with provider {Provider}",
                result.UserId, request.Provider);

            // Security (CWE-922): Set tokens as HttpOnly cookies
            if (!string.IsNullOrEmpty(result.AccessToken))
            {
                AuthCookieHelper.SetAuthCookies(Response, result.AccessToken, result.RefreshToken, result.ExpiresAt);
            }

            return Ok(ApiResponse<ExternalAuthResponse>.Ok(result, new Dictionary<string, object>
            {
                ["isNewUser"] = result.IsNewUser,
                ["provider"] = request.Provider
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External authentication failed for provider {Provider}", request.Provider);
            return BadRequest(ApiResponse<ExternalAuthResponse>.Fail("External authentication failed. Please try again."));
        }
    }

    /// <summary>
    /// Initiate external login flow and get authorization URL
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<ExternalLoginResponse>>> Login(
        [FromBody] ExternalLoginRequest request)
    {
        try
        {
            _logger.LogInformation("Initiating external login flow for provider: {Provider}", request.Provider);

            var command = new ExternalLoginCommand(request.Provider, request.RedirectUri);
            var result = await _mediator.Send(command);

            _logger.LogInformation("External login URL generated for provider {Provider}", request.Provider);

            return Ok(ApiResponse<ExternalLoginResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External login initiation failed for provider {Provider}", request.Provider);
            return BadRequest(ApiResponse<ExternalLoginResponse>.Fail("Failed to initiate external login. Please try again."));
        }
    }

    /// <summary>
    /// Link external account to existing user (AUTH-EXT-005)
    /// Allows authenticated users to connect an OAuth provider to their account.
    /// User can only have one external provider linked at a time.
    /// </summary>
    /// <param name="request">Provider and ID token from OAuth flow</param>
    /// <returns>New tokens with updated claims</returns>
    [HttpPost("link-account")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ExternalAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ExternalAuthResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ExternalAuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ExternalAuthResponse>>> LinkAccount(
        [FromBody] ExternalAuthRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<ExternalAuthResponse>.Fail("User not authenticated"));
            }

            _logger.LogInformation("Linking external account for user {UserId} with provider {Provider}",
                userId, request.Provider);

            var command = new LinkExternalAccountCommand(userId, request.Provider, request.IdToken);
            var result = await _mediator.Send(command);

            _logger.LogInformation("External account linked successfully for user {UserId}, provider {Provider}",
                userId, request.Provider);

            return Ok(ApiResponse<ExternalAuthResponse>.Ok(result, new Dictionary<string, object>
            {
                ["isLinked"] = true,
                ["provider"] = request.Provider,
                ["linkedAt"] = DateTime.UtcNow
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to link external account for user");
            return BadRequest(ApiResponse<ExternalAuthResponse>.Fail("Failed to link external account. Please try again."));
        }
    }

    /// <summary>
    /// Unlink external account from user (AUTH-EXT-006)
    /// Security: Requires user to have a password set before unlinking
    /// </summary>
    /// <param name="request">The provider to unlink</param>
    /// <returns>Success response with unlink details</returns>
    [HttpDelete("unlink-account")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UnlinkExternalAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UnlinkExternalAccountResponse>>> UnlinkAccount(
        [FromBody] UnlinkExternalAccountRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<UnlinkExternalAccountResponse>.Fail("User not authenticated"));
            }

            _logger.LogInformation("Unlinking external account for user {UserId} with provider {Provider}",
                userId, request.Provider);

            var command = new UnlinkExternalAccountCommand(userId, request.Provider);
            var result = await _mediator.Send(command);

            _logger.LogInformation("External account unlinked successfully for user {UserId}, provider {Provider}",
                userId, request.Provider);

            return Ok(ApiResponse<UnlinkExternalAccountResponse>.Ok(result, new Dictionary<string, object>
            {
                ["provider"] = result.Provider,
                ["unlinkedAt"] = result.UnlinkedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlink external account for user");
            return BadRequest(ApiResponse<UnlinkExternalAccountResponse>.Fail("Failed to unlink external account. Please try again."));
        }
    }

    /// <summary>
    /// Get linked external accounts for current user
    /// </summary>
    [HttpGet("linked-accounts")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<LinkedAccountResponse>>>> GetLinkedAccounts()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<LinkedAccountResponse>>.Fail("User not authenticated"));
            }

            _logger.LogInformation("Getting linked accounts for user {UserId}", userId);

            var query = new GetLinkedAccountsQuery(userId);
            var result = await _mediator.Send(query);

            return Ok(ApiResponse<List<LinkedAccountResponse>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get linked accounts for user");
            return BadRequest(ApiResponse<List<LinkedAccountResponse>>.Fail("Failed to retrieve linked accounts."));
        }
    }

    /// <summary>
    /// Validate unlink account request (AUTH-EXT-008)
    /// Checks if user can unlink, has password, and if it's the active provider.
    /// </summary>
    [HttpPost("unlink-account/validate")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ValidateUnlinkAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ValidateUnlinkAccountResponse>>> ValidateUnlinkAccount(
        [FromBody] ValidateUnlinkRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<ValidateUnlinkAccountResponse>.Fail("User not authenticated"));
            }

            _logger.LogInformation("Validating unlink account for user {UserId}, provider {Provider}",
                userId, request.Provider);

            var command = new ValidateUnlinkAccountCommand(userId, request.Provider);
            var result = await _mediator.Send(command);

            return Ok(ApiResponse<ValidateUnlinkAccountResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate unlink account");
            return BadRequest(ApiResponse<ValidateUnlinkAccountResponse>.Fail("Failed to validate unlink request."));
        }
    }

    /// <summary>
    /// Request verification code for unlinking active provider (AUTH-EXT-008)
    /// Sends a 6-digit code to the user's email for verification.
    /// </summary>
    [HttpPost("unlink-account/request-code")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<RequestUnlinkCodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<RequestUnlinkCodeResponse>>> RequestUnlinkCode(
        [FromBody] RequestUnlinkCodeRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<RequestUnlinkCodeResponse>.Fail("User not authenticated"));
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();

            _logger.LogInformation("Requesting unlink code for user {UserId}, provider {Provider}",
                userId, request.Provider);

            var command = new RequestUnlinkCodeCommand(userId, request.Provider, ipAddress, userAgent);
            var result = await _mediator.Send(command);

            return Ok(ApiResponse<RequestUnlinkCodeResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request unlink code");
            return BadRequest(ApiResponse<RequestUnlinkCodeResponse>.Fail("Failed to send verification code. Please try again."));
        }
    }

    /// <summary>
    /// Unlink active OAuth provider with verification code (AUTH-EXT-008)
    /// Verifies the code, unlinks the provider, and revokes all sessions.
    /// </summary>
    [HttpPost("unlink-account/confirm")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UnlinkActiveProviderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UnlinkActiveProviderResponse>>> UnlinkActiveProvider(
        [FromBody] UnlinkActiveProviderRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<UnlinkActiveProviderResponse>.Fail("User not authenticated"));
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();

            _logger.LogWarning("Unlinking active provider for user {UserId}, provider {Provider}",
                userId, request.Provider);

            var command = new UnlinkActiveProviderCommand(
                userId,
                request.Provider,
                request.VerificationCode,
                ipAddress,
                userAgent);
            var result = await _mediator.Send(command);

            _logger.LogWarning("Active provider unlinked for user {UserId}, provider {Provider}, sessions revoked: {SessionsRevoked}",
                userId, request.Provider, result.SessionsRevoked);

            return Ok(ApiResponse<UnlinkActiveProviderResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlink active provider");
            return BadRequest(ApiResponse<UnlinkActiveProviderResponse>.Fail("Failed to unlink provider. Please try again."));
        }
    }

    /// <summary>
    /// Handle external authentication callback
    /// </summary>
    [HttpPost("callback")]
    public async Task<ActionResult<ApiResponse<ExternalAuthResponse>>> Callback(
        [FromBody] ExternalAuthCallbackRequest request)
    {
        try
        {
            _logger.LogInformation("Processing external auth callback for provider: {Provider}", request.Provider);

            var command = new ExternalAuthCallbackCommand(
                request.Provider,
                request.Code,
                request.IdToken,
                request.RedirectUri,
                request.State);

            var result = await _mediator.Send(command);

            _logger.LogInformation("External auth callback processed successfully for user {UserId}",
                result.UserId);

            // Security (CWE-922): Set tokens as HttpOnly cookies
            if (!string.IsNullOrEmpty(result.AccessToken))
            {
                AuthCookieHelper.SetAuthCookies(Response, result.AccessToken, result.RefreshToken, result.ExpiresAt);
            }

            return Ok(ApiResponse<ExternalAuthResponse>.Ok(result, new Dictionary<string, object>
            {
                ["isNewUser"] = result.IsNewUser,
                ["provider"] = request.Provider
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External auth callback failed for provider {Provider}", request.Provider);
            return BadRequest(ApiResponse<ExternalAuthResponse>.Fail("External authentication callback failed. Please try again."));
        }
    }
}
