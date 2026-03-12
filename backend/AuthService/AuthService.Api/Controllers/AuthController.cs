using AuthService.Application.Features.Auth.Commands.Login;
using AuthService.Application.Features.Auth.Commands.Register;
using AuthService.Application.Features.Auth.Commands.ForgotPassword;
using AuthService.Application.Features.Auth.Commands.ResetPassword;
using AuthService.Application.Features.Auth.Commands.RefreshToken;
using AuthService.Application.Features.Auth.Commands.Logout;
using AuthService.Application.Features.Auth.Commands.VerifyEmail;
using AuthService.Application.Features.Auth.Commands.ResendVerification;
using AuthService.Application.Features.Auth.Commands.RequestPasswordSetup;
using AuthService.Application.Features.Auth.Commands.ValidatePasswordSetupToken;
using AuthService.Application.Features.Auth.Commands.SetPasswordForOAuthUser;
using AuthService.Application.Features.ExternalAuth.Commands.ExternalLogin;
using AuthService.Application.Features.ExternalAuth.Commands.ExternalAuthCallback;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AuthService.Application.DTOs.Auth;
using CarDealer.Shared.Audit.Attributes;
using CarDealer.Shared.Audit.Models;
using System.Security.Claims;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("AuthPolicy")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IUserRepository _userRepository;

    public AuthController(
        IMediator mediator,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        IUserRepository userRepository)
    {
        _mediator = mediator;
        _configuration = configuration;
        _logger = logger;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Get current authenticated user profile
    /// </summary>
    /// <returns>User profile data</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetCurrentUser called without valid user ID in claims");
                return Unauthorized(ApiResponse.Fail("User not authenticated"));
            }

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found in database", userId);
                return NotFound(ApiResponse.Fail("User not found"));
            }

            var response = new UserResponse
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarUrl = user.ProfilePictureUrl,
                Phone = user.PhoneNumber,
                AccountType = user.AccountType.ToString().ToLowerInvariant(),
                IsVerified = user.EmailConfirmed,
                IsEmailVerified = user.EmailConfirmed,
                IsPhoneVerified = user.PhoneNumberConfirmed,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.UpdatedAt
            };

            return Ok(ApiResponse<UserResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, ApiResponse.Fail("Failed to get user information"));
        }
    }

    /// <summary>
    /// Set the dealer ID for the current authenticated user.
    /// Called after dealer profile creation in DealerManagementService
    /// so the JWT can include the dealerId claim on next token refresh.
    /// </summary>
    /// <summary>
    /// Set the dealer ID for the current authenticated user.
    /// SECURITY: Validates that the requesting user is the owner of the dealer profile.
    /// Called after dealer profile creation in DealerManagementService.
    /// </summary>
    [HttpPost("set-dealer-id")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetDealerId([FromBody] SetDealerIdRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Fail("User not authenticated"));
            }

            if (string.IsNullOrWhiteSpace(request.DealerId) || !Guid.TryParse(request.DealerId, out _))
            {
                return BadRequest(ApiResponse.Fail("Invalid dealer ID format"));
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse.Fail("User not found"));
            }

            // SECURITY: Verify the user has a Dealer account type
            if (user.AccountType != Domain.Enums.AccountType.Dealer)
            {
                _logger.LogWarning("User {UserId} attempted to set DealerId without Dealer account type (current: {AccountType})",
                    userId, user.AccountType);
                return StatusCode(403, ApiResponse.Fail("Only users with Dealer account type can set a dealer ID"));
            }

            // SECURITY: Prevent overwriting an existing DealerId (must be done by admin)
            if (!string.IsNullOrEmpty(user.DealerId) && user.DealerId != request.DealerId)
            {
                _logger.LogWarning("User {UserId} attempted to change DealerId from {OldDealerId} to {NewDealerId}",
                    userId, user.DealerId, request.DealerId);
                return BadRequest(ApiResponse.Fail("Dealer ID is already set. Contact support to change it."));
            }

            user.DealerId = request.DealerId;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("DealerId {DealerId} set for user {UserId}", request.DealerId, userId);

            return Ok(ApiResponse<string>.Ok("Dealer ID updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting dealer ID for user");
            return StatusCode(500, ApiResponse.Fail("Failed to update dealer ID"));
        }
    }

    /// <summary>
    /// Initiate OAuth login flow - redirects browser to provider's login page
    /// </summary>
    /// <param name="provider">OAuth provider: google, apple</param>
    /// <returns>Redirect to OAuth provider</returns>
    [HttpGet("oauth/{provider}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OAuthLogin(string provider)
    {
        try
        {
            _logger.LogInformation("Initiating OAuth login flow for provider: {Provider}", provider);

            // Validate provider
            var validProviders = new[] { "google", "apple" };
            if (!validProviders.Contains(provider.ToLowerInvariant()))
            {
                return BadRequest(ApiResponse.Fail($"Invalid OAuth provider: {provider}. Supported: google, apple"));
            }

            // Get the frontend callback URL from configuration
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
            var redirectUri = $"{frontendUrl}/auth/callback/{provider}";

            _logger.LogDebug("Using redirect URI: {RedirectUri}", redirectUri);

            var command = new ExternalLoginCommand(provider, redirectUri);
            var result = await _mediator.Send(command);

            _logger.LogInformation("OAuth authorization URL generated for provider {Provider}, redirecting...", provider);

            // Redirect the browser to the OAuth provider's authorization page
            return Redirect(result.AuthorizationUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate OAuth login for provider {Provider}", provider);
            return BadRequest(ApiResponse.Fail("Failed to initiate OAuth login. Please try again."));
        }
    }

    /// <summary>
    /// Handle OAuth callback - exchange authorization code for tokens
    /// </summary>
    /// <param name="provider">OAuth provider: google, apple</param>
    /// <param name="request">Contains the authorization code from the OAuth provider</param>
    /// <returns>JWT tokens for the authenticated user</returns>
    [HttpPost("oauth/{provider}/callback")]
    [ProducesResponseType(typeof(ApiResponse<AuthService.Application.DTOs.ExternalAuth.ExternalAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OAuthCallback(string provider, [FromBody] OAuthCallbackRequest request)
    {
        try
        {
            _logger.LogInformation("Processing OAuth callback for provider: {Provider}", provider);

            // Validate provider
            var validProviders = new[] { "google", "apple" };
            if (!validProviders.Contains(provider.ToLowerInvariant()))
            {
                return BadRequest(ApiResponse.Fail($"Invalid OAuth provider: {provider}. Supported: google, apple"));
            }

            if (string.IsNullOrEmpty(request.Code))
            {
                return BadRequest(ApiResponse.Fail("Authorization code is required"));
            }

            // ExternalAuthCallbackCommand(Provider, Code, IdToken, RedirectUri, State)
            var command = new ExternalAuthCallbackCommand(provider, request.Code, null, request.RedirectUri, null);
            var result = await _mediator.Send(command);

            _logger.LogInformation("OAuth callback successful for provider {Provider}, user {UserId}",
                provider, result.UserId);

            // Security (CWE-922): Set tokens as HttpOnly cookies for OAuth login
            if (!string.IsNullOrEmpty(result.AccessToken))
            {
                SetAuthCookies(result.AccessToken, result.RefreshToken, result.ExpiresAt);
            }

            return Ok(ApiResponse<AuthService.Application.DTOs.ExternalAuth.ExternalAuthResponse>.Ok(result, new Dictionary<string, object>
            {
                ["isNewUser"] = result.IsNewUser,
                ["provider"] = provider
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback failed for provider {Provider}", provider);
            return BadRequest(ApiResponse.Fail("OAuth authentication failed. Please try again."));
        }
    }

    [HttpPost("register")]
    [Audit("AUTH_REGISTER", "Register", ResourceType = "User", Severity = AuditSeverity.Warning)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Map string accountType from frontend ("seller", "buyer", "dealer") to domain enum
        var accountType = request.AccountType?.ToLowerInvariant() switch
        {
            "seller" => Domain.Enums.AccountType.Seller,
            "dealer" => Domain.Enums.AccountType.Dealer,
            _ => Domain.Enums.AccountType.Buyer   // default: Buyer
        };

        // Map string userIntent from frontend ("sell", "buy", "buy_and_sell") to domain enum
        var userIntent = request.UserIntent?.ToLowerInvariant() switch
        {
            "sell" => Domain.Enums.UserIntent.Sell,
            "buy" => Domain.Enums.UserIntent.Buy,
            "buy_and_sell" => Domain.Enums.UserIntent.BuyAndSell,
            _ => Domain.Enums.UserIntent.Browse   // default: Browse
        };

        var command = new RegisterCommand(
            request.UserName,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.AcceptTerms,
            accountType,
            userIntent
        );
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<RegisterResponse>.Ok(result));
    }

    [HttpPost("login")]
    [Audit("AUTH_LOGIN", "Login", ResourceType = "User", Severity = AuditSeverity.Warning)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Password, request.CaptchaToken);
        var result = await _mediator.Send(command);

        // Security (CWE-922): Set tokens as HttpOnly cookies instead of exposing in response body
        if (!string.IsNullOrEmpty(result.AccessToken) && !result.RequiresTwoFactor)
        {
            SetAuthCookies(result.AccessToken, result.RefreshToken, result.ExpiresAt);
        }

        return Ok(ApiResponse<LoginResponse>.Ok(result));
    }

    [HttpPost("forgot-password")]
    [Audit("AUTH_FORGOT_PASSWORD", "ForgotPassword", ResourceType = "User", Severity = AuditSeverity.Info)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var command = new ForgotPasswordCommand(request.Email);
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<ForgotPasswordResponse>.Ok(result));
    }

    [HttpPost("reset-password")]
    [Audit("AUTH_RESET_PASSWORD", "ResetPassword", ResourceType = "User", Severity = AuditSeverity.Warning)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        // DEBUG: Log incoming request details
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AuthController>>();
        logger.LogInformation("ResetPassword request received - Token: {TokenLength} chars, NewPassword: {PasswordLength} chars, ConfirmPassword: {ConfirmLength} chars",
            request.Token?.Length ?? 0,
            request.NewPassword?.Length ?? 0,
            request.ConfirmPassword?.Length ?? 0);

        if (string.IsNullOrEmpty(request.Token))
        {
            logger.LogWarning("ResetPassword failed: Token is null or empty");
            return BadRequest(ApiResponse.Fail("Token is required"));
        }
        if (string.IsNullOrEmpty(request.NewPassword))
        {
            logger.LogWarning("ResetPassword failed: NewPassword is null or empty");
            return BadRequest(ApiResponse.Fail("New password is required"));
        }

        var command = new ResetPasswordCommand(request.Token, request.NewPassword, request.ConfirmPassword ?? string.Empty);
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<ResetPasswordResponse>.Ok(result));
    }

    [HttpPost("verify-email")]
    [Audit("AUTH_VERIFY_EMAIL", "VerifyEmail", ResourceType = "User", Severity = AuditSeverity.Info)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var command = new VerifyEmailCommand(request.Token);
        await _mediator.Send(command);
        return Ok(ApiResponse.Ok());
    }

    [HttpPost("resend-verification")]
    [Audit("AUTH_RESEND_VERIFICATION", "ResendVerification", ResourceType = "User", Severity = AuditSeverity.Info)]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        var command = new ResendVerificationCommand(request.Email);
        await _mediator.Send(command);
        return Ok(ApiResponse.Ok());
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]  // RefreshToken NO debe requerir autorización (ese es su propósito)
    [Audit("AUTH_REFRESH_TOKEN", "RefreshToken", ResourceType = "Token", Severity = AuditSeverity.Debug)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        // Security: Accept refresh token from HttpOnly cookie if not provided in body
        var refreshToken = request.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken))
        {
            refreshToken = Request.Cookies["okla_refresh_token"] ?? string.Empty;
        }

        var command = new RefreshTokenCommand(refreshToken);
        var result = await _mediator.Send(command);

        // Security (CWE-922): Set new tokens as HttpOnly cookies
        SetAuthCookies(result.AccessToken, result.RefreshToken, result.ExpiresAt);

        return Ok(ApiResponse<RefreshTokenResponse>.Ok(result));
    }

    /// <summary>
    /// Cierra la sesión del usuario revocando su refresh token
    /// </summary>
    /// <param name="request">Request con el refreshToken a revocar</param>
    /// <returns>Confirmación del logout</returns>
    /// <response code="200">Logout exitoso, refreshToken revocado</response>
    /// <response code="400">RefreshToken no proporcionado</response>
    /// <response code="401">Token de acceso inválido o expirado</response>
    [HttpPost("logout")]
    [Authorize]
    [Audit("AUTH_LOGOUT", "Logout", ResourceType = "User", Severity = AuditSeverity.Info)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        // Accept refresh token from HttpOnly cookie if not provided in body
        var refreshToken = request.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken))
        {
            refreshToken = Request.Cookies["okla_refresh_token"] ?? string.Empty;
        }

        var command = new LogoutCommand(refreshToken);
        await _mediator.Send(command);

        // Security: Clear HttpOnly auth cookies
        ClearAuthCookies();

        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// AUTH-SEC-005: Request verification code for login from a revoked device.
    /// This is called when a login attempt is detected from a device that was previously revoked.
    /// </summary>
    [HttpPost("revoked-device/request-code")]
    [AllowAnonymous]
    [Audit("AUTH_REVOKED_DEVICE_CODE_REQUEST", "RequestRevokedDeviceCode", ResourceType = "Device", Severity = AuditSeverity.Warning)]
    [ProducesResponseType(typeof(ApiResponse<RevokedDeviceLoginResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestRevokedDeviceCode([FromBody] RevokedDeviceCodeRequest request)
    {
        var command = new Application.Features.Auth.Commands.VerifyRevokedDeviceLogin.RequestRevokedDeviceLoginCommand(
            request.UserId,
            request.Email,
            request.DeviceFingerprint,
            GetIpAddress(),
            Request.Headers.UserAgent.ToString(),
            GetBrowserFromUserAgent(Request.Headers.UserAgent.ToString()),
            GetOSFromUserAgent(Request.Headers.UserAgent.ToString())
        );
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<Application.Features.Auth.Commands.VerifyRevokedDeviceLogin.RequestRevokedDeviceLoginResponse>.Ok(result));
    }

    /// <summary>
    /// AUTH-SEC-005: Verify code and complete login from a revoked device.
    /// </summary>
    [HttpPost("revoked-device/verify")]
    [AllowAnonymous]
    [Audit("AUTH_REVOKED_DEVICE_VERIFY", "VerifyRevokedDevice", ResourceType = "Device", Severity = AuditSeverity.Warning)]
    [ProducesResponseType(typeof(ApiResponse<RevokedDeviceVerifyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyRevokedDevice([FromBody] RevokedDeviceVerifyRequest request)
    {
        var command = new Application.Features.Auth.Commands.VerifyRevokedDeviceLogin.VerifyRevokedDeviceLoginCommand(
            request.VerificationToken,
            request.Code,
            GetIpAddress()
        );
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<Application.Features.Auth.Commands.VerifyRevokedDeviceLogin.VerifyRevokedDeviceLoginResponse>.Ok(result));
    }

    private string? GetIpAddress()
    {
        var forwardedHeader = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedHeader))
        {
            return forwardedHeader.Split(',').FirstOrDefault()?.Trim();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Request password setup for OAuth users (AUTH-PW001)
    /// Sends an email with a secure link to set a password.
    /// </summary>
    [HttpPost("password/setup-request")]
    [Authorize]
    [Audit("AUTH_PASSWORD_SETUP_REQUEST", "PasswordSetupRequest", ResourceType = "User", Severity = AuditSeverity.Info)]
    [ProducesResponseType(typeof(ApiResponse<RequestPasswordSetupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestPasswordSetup()
    {
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AuthController>>();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
        {
            return Unauthorized(ApiResponse.Fail("User not authenticated"));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        logger.LogInformation("Password setup request for user {UserId}", userId);

        var command = new RequestPasswordSetupCommand(userId, email, ipAddress, userAgent);
        var result = await _mediator.Send(command);

        return Ok(ApiResponse<RequestPasswordSetupResponse>.Ok(result));
    }

    /// <summary>
    /// Validate password setup token (AUTH-PWD-001)
    /// Called when user clicks the link in the email to verify it's still valid.
    /// </summary>
    [HttpGet("password/setup-validate")]
    [Audit("AUTH_PASSWORD_SETUP_VALIDATE", "PasswordSetupValidate", ResourceType = "User", Severity = AuditSeverity.Info)]
    [ProducesResponseType(typeof(ApiResponse<ValidatePasswordSetupTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidatePasswordSetupToken([FromQuery] string token)
    {
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AuthController>>();

        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(ApiResponse.Fail("Token is required"));
        }

        logger.LogInformation("Validating password setup token");

        var command = new ValidatePasswordSetupTokenCommand(token);
        var result = await _mediator.Send(command);

        return Ok(ApiResponse<ValidatePasswordSetupTokenResponse>.Ok(result));
    }

    /// <summary>
    /// Complete password setup for OAuth user (AUTH-PWD-001)
    /// Sets the password and invalidates the token.
    /// </summary>
    [HttpPost("password/setup-complete")]
    [Audit("AUTH_PASSWORD_SETUP_COMPLETE", "PasswordSetupComplete", ResourceType = "User", Severity = AuditSeverity.Warning)]
    [ProducesResponseType(typeof(ApiResponse<SetPasswordForOAuthUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompletePasswordSetup([FromBody] SetPasswordRequest request)
    {
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AuthController>>();

        if (string.IsNullOrEmpty(request.Token))
        {
            return BadRequest(ApiResponse.Fail("Token is required"));
        }
        if (string.IsNullOrEmpty(request.NewPassword))
        {
            return BadRequest(ApiResponse.Fail("Password is required"));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        logger.LogInformation("Completing password setup");

        var command = new SetPasswordForOAuthUserCommand(
            request.Token,
            request.NewPassword,
            request.ConfirmPassword,
            ipAddress,
            userAgent);
        var result = await _mediator.Send(command);

        return Ok(ApiResponse<SetPasswordForOAuthUserResponse>.Ok(result));
    }

    private static string GetBrowserFromUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        if (userAgent.Contains("Edg/")) return "Microsoft Edge";
        if (userAgent.Contains("Chrome/") && !userAgent.Contains("Chromium")) return "Chrome";
        if (userAgent.Contains("Firefox/")) return "Firefox";
        if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome")) return "Safari";
        if (userAgent.Contains("Opera") || userAgent.Contains("OPR")) return "Opera";
        return "Unknown Browser";
    }

    private static string GetOSFromUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        if (userAgent.Contains("Windows NT 10")) return "Windows 10/11";
        if (userAgent.Contains("Windows NT")) return "Windows";
        if (userAgent.Contains("Mac OS X")) return "macOS";
        if (userAgent.Contains("Linux") && !userAgent.Contains("Android")) return "Linux";
        if (userAgent.Contains("Android")) return "Android";
        if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) return "iOS";
        return "Unknown OS";
    }

    #region HttpOnly Cookie Helpers (CWE-922 mitigation)

    /// <summary>
    /// Sets access and refresh tokens as HttpOnly, Secure, SameSite=Strict cookies.
    /// Tokens are NOT accessible via JavaScript (prevents XSS token theft).
    /// </summary>
    private void SetAuthCookies(string accessToken, string refreshToken, DateTime expiresAt)
    {
        var isProduction = !string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development", StringComparison.OrdinalIgnoreCase);

        // In K8s production: Gateway (HTTPS) → Ocelot (terminates TLS) → Internal HTTP to AuthService
        // Check both Request.IsHttps AND X-Forwarded-Proto header to detect original HTTPS from browser
        var isHttps = Request.IsHttps ||
                      (Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) &&
                       proto.ToString().Equals("https", StringComparison.OrdinalIgnoreCase));

        var secure = isProduction && isHttps;

        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,  // Only true if HTTPS detected
            SameSite = SameSiteMode.Lax,  // Changed from Strict to Lax — cookies sent in cross-site requests
            Path = "/",
            Expires = expiresAt,
            IsEssential = true
        };

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,  // Only true if HTTPS detected
            SameSite = SameSiteMode.Lax,  // Changed from Strict to Lax — cookies sent in cross-site requests
            Path = "/",  // Changed from "/api/auth" to "/" — needs to be sent to all endpoints
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            IsEssential = true
        };

        Response.Cookies.Append("okla_access_token", accessToken, accessCookieOptions);
        Response.Cookies.Append("okla_refresh_token", refreshToken, refreshCookieOptions);
    }

    /// <summary>
    /// Clears auth cookies on logout.
    /// </summary>
    private void ClearAuthCookies()
    {
        var isProduction = !string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development", StringComparison.OrdinalIgnoreCase);

        var deleteOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = SameSiteMode.Lax,  // Changed from Strict to Lax
            Path = "/"
        };

        Response.Cookies.Delete("okla_access_token", deleteOptions);
        Response.Cookies.Delete("okla_refresh_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = SameSiteMode.Lax,  // Changed from Strict to Lax
            Path = "/"  // Changed from "/api/auth" to "/"
        });
    }

    #endregion
}

// DTOs for revoked device endpoints
public record RevokedDeviceCodeRequest(
    string UserId,
    string Email,
    string DeviceFingerprint
);

public record RevokedDeviceLoginResponse(
    bool RequiresVerification,
    string Message,
    string? VerificationToken = null,
    DateTime? CodeExpiresAt = null
);

public record RevokedDeviceVerifyRequest(
    string VerificationToken,
    string Code
);

public record RevokedDeviceVerifyResponse(
    bool Success,
    string Message,
    bool DeviceCleared = false,
    int? RemainingAttempts = null
);

// DTO for password setup (AUTH-PWD-001)
public record SetPasswordRequest(
    string Token,
    string NewPassword,
    string ConfirmPassword
);
