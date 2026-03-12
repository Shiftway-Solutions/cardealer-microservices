using AuthService.Application.DTOs.Security;
using AuthService.Application.Features.Auth.Commands.ChangePassword;
using AuthService.Application.Features.Auth.Commands.RevokeSession;
using AuthService.Application.Features.Auth.Commands.RevokeAllSessions;
using AuthService.Application.Features.Auth.Commands.RequestSessionRevocation;
using AuthService.Application.Features.Auth.Queries.GetActiveSessions;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AuthService.Domain.Entities;
using MediatR;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.Api.Controllers;

/// <summary>
/// Controller for managing user security settings
/// Proceso: AUTH-SEC-001, AUTH-SEC-002, AUTH-SEC-003, AUTH-SEC-004
/// Rate-limited to prevent brute-force attacks on password/session endpoints (OWASP API4:2023)
/// </summary>
[ApiController]
[Route("api/auth/security")]
[Authorize]
[EnableRateLimiting("AuthPolicy")]
public class SecurityController : ControllerBase
{
    private readonly ILogger<SecurityController> _logger;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly ILoginHistoryRepository _loginHistoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMediator _mediator;
    private readonly IGeoLocationService _geoLocationService;

    public SecurityController(
        ILogger<SecurityController> logger,
        IUserSessionRepository sessionRepository,
        ILoginHistoryRepository loginHistoryRepository,
        IUserRepository userRepository,
        IMediator mediator,
        IGeoLocationService geoLocationService)
    {
        _logger = logger;
        _sessionRepository = sessionRepository;
        _loginHistoryRepository = loginHistoryRepository;
        _userRepository = userRepository;
        _mediator = mediator;
        _geoLocationService = geoLocationService;
    }

    /// <summary>
    /// Get security settings for the current user
    /// Incluye: 2FA status, sesiones activas, historial de logins
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SecuritySettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SecuritySettingsDto>> GetSecuritySettings(
        CancellationToken cancellationToken)
    {
        // SECURITY FIX: Always use JWT claim — never trust client headers (IDOR prevention)
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("User not authenticated"));
        }

        _logger.LogInformation("Getting security settings for user {UserId}", userId);

        try
        {
            // Obtener usuario para información de 2FA
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Fail("User not found"));
            }

            // Obtener sesiones activas
            var activeSessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(userId, cancellationToken);
            var currentSessionId = GetCurrentSessionId();

            // Update sessions that don't have location (legacy sessions before geolocation was implemented)
            foreach (var session in activeSessions.Where(s => string.IsNullOrEmpty(s.Location) && string.IsNullOrEmpty(s.Country)))
            {
                try
                {
                    var geoLocation = await _geoLocationService.GetLocationFromIpAsync(session.IpAddress, cancellationToken);
                    if (geoLocation != null)
                    {
                        var locationString = FormatLocationString(geoLocation.City, geoLocation.Country);
                        session.UpdateLocation(locationString, geoLocation.Country, geoLocation.City);
                        await _sessionRepository.UpdateAsync(session, cancellationToken);
                        _logger.LogDebug("Updated location for session {SessionId}: {Location}", session.Id, locationString);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get geolocation for session {SessionId}", session.Id);
                }
            }

            var sessionDtos = activeSessions.Select(s => new ActiveSessionDto(
                Id: s.Id.ToString(),
                Device: s.DeviceInfo,
                Browser: s.Browser,
                Location: s.Location ?? GetLocationString(s.City, s.Country),
                IpAddress: s.IpAddress,
                LastActive: s.LastActiveAt.ToString("o"),
                IsCurrent: s.Id.ToString() == currentSessionId
            )).ToList();

            // Obtener historial de logins (últimos 10)
            var loginHistory = await _loginHistoryRepository.GetByUserIdAsync(userId, 10, cancellationToken);

            var loginDtos = loginHistory.Select(l => new LoginHistoryDto(
                Id: l.Id.ToString(),
                Device: l.DeviceInfo,
                Browser: l.Browser,
                Location: l.Location ?? GetLocationString(l.City, l.Country),
                IpAddress: l.IpAddress,
                LoginTime: l.LoginTime.ToString("o"),
                Success: l.Success
            )).ToList();

            // Obtener fecha de último cambio de contraseña
            var lastPasswordChange = user.UpdatedAt?.ToString("o") ?? user.CreatedAt.ToString("o");

            // Check if user has a password set (OAuth users may not have one)
            var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);

            // Get linked OAuth providers
            var linkedProviders = new List<LinkedProviderDto>();
            if (user.ExternalAuthProvider.HasValue && !string.IsNullOrEmpty(user.ExternalUserId))
            {
                linkedProviders.Add(new LinkedProviderDto(
                    Provider: user.ExternalAuthProvider.Value.ToString(),
                    Email: user.Email ?? "",
                    LinkedAt: user.CreatedAt
                ));
            }

            var settings = new SecuritySettingsDto(
                TwoFactorEnabled: user.IsTwoFactorEnabled,
                TwoFactorType: user.TwoFactorAuth?.PrimaryMethod.ToString(),
                LastPasswordChange: lastPasswordChange,
                ActiveSessions: sessionDtos,
                RecentLogins: loginDtos,
                HasPassword: hasPassword,
                LinkedProviders: linkedProviders.Count > 0 ? linkedProviders : null
            );

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security settings for user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }

    /// <summary>
    /// Change user password
    /// Proceso: AUTH-SEC-001
    /// 
    /// Security measures:
    /// - Requires valid current password
    /// - Validates password complexity (8+ chars, upper, lower, number, special)
    /// - Prevents password reuse (new != current)
    /// - Revokes all active sessions
    /// - Revokes all refresh tokens
    /// - Sends email notification
    /// </summary>
    /// <remarks>
    /// Password requirements:
    /// - Minimum 8 characters
    /// - At least one uppercase letter (A-Z)
    /// - At least one lowercase letter (a-z)
    /// - At least one number (0-9)
    /// - At least one special character (!@#$%^&amp;*(),.?":{}|&lt;&gt;_-+=[]\/~`)
    /// - Cannot be the same as current password
    /// - Cannot contain common passwords (password123, qwerty, etc.)
    /// - Cannot contain more than 3 consecutive identical characters
    /// </remarks>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<ChangePasswordResponse>>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Password change attempt without valid authentication");
            return Unauthorized(ApiResponse<ChangePasswordResponse>.Fail("User not authenticated"));
        }

        _logger.LogInformation(
            "AUTH-SEC-001: Password change initiated for user {UserId} from IP {IpAddress}",
            userId, GetClientIpAddress());

        try
        {
            // Crear comando con información de contexto para auditoría
            var command = new ChangePasswordCommand(
                UserId: userId,
                CurrentPassword: request.CurrentPassword,
                NewPassword: request.NewPassword,
                ConfirmPassword: request.ConfirmPassword,
                IpAddress: GetClientIpAddress(),
                UserAgent: GetUserAgentString()
            );

            // Ejecutar a través de MediatR (incluye validación con FluentValidation)
            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation(
                "AUTH-SEC-001: Password changed successfully for user {UserId}",
                userId);

            return Ok(ApiResponse<ChangePasswordResponse>.Ok(result));
        }
        catch (ValidationException validationEx)
        {
            var errors = validationEx.Errors
                .Select(e => e.ErrorMessage)
                .ToList();

            _logger.LogWarning(
                "AUTH-SEC-001: Password change validation failed for user {UserId}: {Errors}",
                userId, string.Join(", ", errors));

            return BadRequest(ApiResponse<ChangePasswordResponse>.Fail(
                string.Join("; ", errors)));
        }
        catch (Exception ex) when (ex.Message.Contains("incorrect") ||
                                   ex.Message.Contains("wrong") ||
                                   ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            // Errores de contraseña actual incorrecta
            _logger.LogWarning(
                "AUTH-SEC-001: Password change failed - invalid current password for user {UserId}",
                userId);
            return BadRequest(ApiResponse<ChangePasswordResponse>.Fail("The current password is incorrect."));
        }
        catch (Exception ex) when (ex.Message.Contains("different") ||
                                   ex.Message.Contains("same") ||
                                   ex.Message.Contains("common") ||
                                   ex.Message.Contains("locked"))
        {
            // Otros errores de validación de negocio
            _logger.LogWarning(
                "AUTH-SEC-001: Password change business rule violation for user {UserId}: {Message}",
                userId, ex.Message);
            return BadRequest(ApiResponse<ChangePasswordResponse>.Fail("The new password does not meet the security requirements."));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "AUTH-SEC-001: Unexpected error during password change for user {UserId}",
                userId);
            return StatusCode(500, ApiResponse<ChangePasswordResponse>.Fail(
                "An unexpected error occurred. Please try again later."));
        }
    }

    /// <summary>
    /// Get active sessions for the current user
    /// Proceso: AUTH-SEC-002
    /// 
    /// Returns a list of all active sessions with device info, location, and activity status.
    /// IPs are partially masked for privacy. Current session is marked.
    /// </summary>
    /// <remarks>
    /// Security features:
    /// - Only returns sessions belonging to the authenticated user
    /// - IP addresses are partially masked (e.g., 192.168.1.***)
    /// - Strings are sanitized to prevent XSS
    /// - Current session is marked for UI differentiation
    /// </remarks>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<GetActiveSessionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GetActiveSessionsResponse>>> GetActiveSessions(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("AUTH-SEC-002: Attempt to get sessions without authentication");
            return Unauthorized(ApiResponse<GetActiveSessionsResponse>.Fail("User not authenticated"));
        }

        _logger.LogInformation(
            "AUTH-SEC-002: Getting active sessions for user {UserId} from IP {IpAddress}",
            userId, GetClientIpAddress());

        try
        {
            var query = new GetActiveSessionsQuery(
                UserId: userId,
                CurrentSessionId: GetCurrentSessionId()
            );

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.Success)
            {
                return StatusCode(500, ApiResponse<GetActiveSessionsResponse>.Fail(
                    result.Message ?? "Error retrieving sessions"));
            }

            return Ok(ApiResponse<GetActiveSessionsResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AUTH-SEC-002: Error getting sessions for user {UserId}", userId);
            return StatusCode(500, ApiResponse<GetActiveSessionsResponse>.Fail(
                "An error occurred while retrieving sessions."));
        }
    }

    /// <summary>
    /// Request a verification code to revoke a session
    /// Proceso: AUTH-SEC-003-A
    /// 
    /// Sends a 6-digit code to the user's email that must be provided to revoke the session.
    /// Code expires in 5 minutes. Maximum 3 requests per hour.
    /// </summary>
    /// <remarks>
    /// Security features:
    /// - Cannot request code for current session (must use logout)
    /// - Rate limited: 3 requests per hour
    /// - Code expires in 5 minutes
    /// - Maximum 3 verification attempts before lockout
    /// </remarks>
    /// <param name="sessionId">The GUID of the session to request revocation for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("sessions/{sessionId}/request-revoke")]
    [ProducesResponseType(typeof(ApiResponse<RequestSessionRevocationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<RequestSessionRevocationResponse>>> RequestSessionRevocation(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning(
                "AUTH-SEC-003-A: Attempt to request session revocation without authentication");
            return Unauthorized(ApiResponse<RequestSessionRevocationResponse>.Fail("User not authenticated"));
        }

        _logger.LogInformation(
            "AUTH-SEC-003-A: Session revocation code request from user {UserId} for session {SessionId}",
            userId, sessionId);

        try
        {
            var command = new RequestSessionRevocationCommand(
                UserId: userId,
                SessionId: sessionId,
                CurrentSessionId: GetCurrentSessionId(),
                IpAddress: GetClientIpAddress(),
                UserAgent: GetUserAgentString()
            );

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<RequestSessionRevocationResponse>.Fail(result.Message));
            }

            return Ok(ApiResponse<RequestSessionRevocationResponse>.Ok(result));
        }
        catch (Exception ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(ApiResponse<RequestSessionRevocationResponse>.Fail("Session not found."));
        }
        catch (Exception ex) when (ex.Message.Contains("Too many", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<RequestSessionRevocationResponse>.Fail("Too many requests. Please try again later."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AUTH-SEC-003-A: Error requesting session revocation for user {UserId}",
                userId);
            return StatusCode(500, ApiResponse<RequestSessionRevocationResponse>.Fail(
                "An error occurred while requesting session revocation."));
        }
    }

    /// <summary>
    /// Revoke a specific session (remote logout)
    /// Proceso: AUTH-SEC-003
    /// 
    /// Terminates a specific session. If code is provided, it verifies the email code first.
    /// If no code is provided, it revokes directly (simplified flow for authenticated users).
    /// The device will be logged out on next request.
    /// </summary>
    /// <remarks>
    /// Security features:
    /// - Cannot revoke current session (must use logout)
    /// - Verifies session belongs to the authenticated user (prevents IDOR)
    /// - Returns 404 even for sessions of other users (prevents enumeration)
    /// - Revokes associated refresh token
    /// - Sends notification email to user
    /// - Logs security audit trail
    /// </remarks>
    /// <param name="sessionId">The GUID of the session to revoke</param>
    /// <param name="code">Optional verification code (if required by security policy)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("sessions/{sessionId}")]
    [ProducesResponseType(typeof(ApiResponse<RevokeSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RevokeSessionResponse>>> RevokeSession(
        string sessionId,
        [FromQuery] string? code = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning(
                "AUTH-SEC-003: Attempt to revoke session {SessionId} without authentication",
                sessionId);
            return Unauthorized(ApiResponse<RevokeSessionResponse>.Fail("User not authenticated"));
        }

        // Code is now optional - direct revocation is allowed for authenticated users
        _logger.LogInformation(
            "AUTH-SEC-003: Session revocation request from user {UserId} for session {SessionId} (code provided: {HasCode})",
            userId, sessionId, !string.IsNullOrEmpty(code));

        try
        {
            var command = new RevokeSessionCommand(
                UserId: userId,
                SessionId: sessionId,
                VerificationCode: code,
                CurrentSessionId: GetCurrentSessionId(),
                IpAddress: GetClientIpAddress(),
                UserAgent: GetUserAgentString()
            );

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<RevokeSessionResponse>.Fail(result.Message));
            }

            return Ok(ApiResponse<RevokeSessionResponse>.Ok(result));
        }
        catch (ValidationException validationEx)
        {
            var errors = validationEx.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning(
                "AUTH-SEC-003: Validation failed for session revocation: {Errors}",
                string.Join(", ", errors));
            return BadRequest(ApiResponse<RevokeSessionResponse>.Fail(string.Join("; ", errors)));
        }
        catch (Exception ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(ApiResponse<RevokeSessionResponse>.Fail("Session not found."));
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<RevokeSessionResponse>.Fail("Invalid verification code."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AUTH-SEC-003: Error revoking session {SessionId} for user {UserId}",
                sessionId, userId);
            return StatusCode(500, ApiResponse<RevokeSessionResponse>.Fail(
                "An error occurred while revoking the session."));
        }
    }

    /// <summary>
    /// Revoke all sessions (sign out from all devices)
    /// Proceso: AUTH-SEC-004
    /// 
    /// Terminates all active sessions and refresh tokens.
    /// By default, keeps the current session active.
    /// </summary>
    /// <remarks>
    /// Security features:
    /// - Option to keep current session (default: true)
    /// - Revokes all associated refresh tokens
    /// - Sends security alert email to user
    /// - Logs audit trail with count of revoked sessions
    /// 
    /// Use cases:
    /// - User suspects account compromise
    /// - User lost a device
    /// - Periodic security hygiene
    /// </remarks>
    /// <param name="keepCurrentSession">Keep the current session active (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("sessions/revoke-all")]
    [ProducesResponseType(typeof(ApiResponse<RevokeAllSessionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<RevokeAllSessionsResponse>>> RevokeAllSessions(
        [FromQuery] bool keepCurrentSession = true,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("AUTH-SEC-004: Attempt to revoke all sessions without authentication");
            return Unauthorized(ApiResponse<RevokeAllSessionsResponse>.Fail("User not authenticated"));
        }

        _logger.LogInformation(
            "AUTH-SEC-004: Revoke all sessions request from user {UserId}. KeepCurrent: {KeepCurrent}",
            userId, keepCurrentSession);

        try
        {
            var command = new RevokeAllSessionsCommand(
                UserId: userId,
                CurrentSessionId: GetCurrentSessionId(),
                KeepCurrentSession: keepCurrentSession,
                RevokeRefreshTokens: true,
                IpAddress: GetClientIpAddress(),
                UserAgent: GetUserAgentString()
            );

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success)
            {
                return StatusCode(500, ApiResponse<RevokeAllSessionsResponse>.Fail(result.Message));
            }

            _logger.LogInformation(
                "AUTH-SEC-004: Successfully revoked {Count} sessions for user {UserId}",
                result.SessionsRevoked, userId);

            return Ok(ApiResponse<RevokeAllSessionsResponse>.Ok(result));
        }
        catch (ValidationException validationEx)
        {
            var errors = validationEx.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<RevokeAllSessionsResponse>.Fail(string.Join("; ", errors)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AUTH-SEC-004: Error revoking all sessions for user {UserId}",
                userId);
            return StatusCode(500, ApiResponse<RevokeAllSessionsResponse>.Fail(
                "An error occurred while revoking sessions."));
        }
    }

    /// <summary>
    /// Get login history for the current user
    /// </summary>
    [HttpGet("login-history")]
    [ProducesResponseType(typeof(List<LoginHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<LoginHistoryDto>>> GetLoginHistory(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("Getting login history for user {UserId}", userId);

        var history = await _loginHistoryRepository.GetByUserIdAsync(userId, Math.Min(limit, 100), cancellationToken);

        var historyDtos = history.Select(l => new LoginHistoryDto(
            Id: l.Id.ToString(),
            Device: l.DeviceInfo,
            Browser: l.Browser,
            Location: l.Location ?? GetLocationString(l.City, l.Country),
            IpAddress: l.IpAddress,
            LoginTime: l.LoginTime.ToString("o"),
            Success: l.Success
        )).ToList();

        return Ok(historyDtos);
    }

    #region Helper Methods

    private string? GetCurrentSessionId()
    {
        return User.FindFirst("SessionId")?.Value;
    }

    /// <summary>
    /// Formats city and country into a location string
    /// </summary>
    private static string FormatLocationString(string? city, string? country)
    {
        if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(country))
            return $"{city}, {country}";
        if (!string.IsNullOrEmpty(country))
            return country;
        if (!string.IsNullOrEmpty(city))
            return city;
        return "Red Local";  // Default for private IPs
    }

    private static string GetLocationString(string? city, string? country)
    {
        if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(country))
            return $"{city}, {country}";
        if (!string.IsNullOrEmpty(country))
            return country;
        if (!string.IsNullOrEmpty(city))
            return city;
        return "Unknown location";
    }

    private string GetDeviceFromUserAgent()
    {
        var userAgent = Request.Headers.UserAgent.ToString();

        if (userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
            return "Mobile iOS";
        if (userAgent.Contains("Android"))
            return "Mobile Android";
        if (userAgent.Contains("Windows"))
            return "Desktop Windows";
        if (userAgent.Contains("Mac"))
            return "Desktop Mac";
        if (userAgent.Contains("Linux"))
            return "Desktop Linux";

        return "Unknown Device";
    }

    private string GetBrowserFromUserAgent()
    {
        var userAgent = Request.Headers.UserAgent.ToString();

        if (userAgent.Contains("Edg/"))
            return "Edge";
        if (userAgent.Contains("Chrome/"))
            return "Chrome";
        if (userAgent.Contains("Firefox/"))
            return "Firefox";
        if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome"))
            return "Safari";

        return "Unknown Browser";
    }

    private string GetOsFromUserAgent()
    {
        var userAgent = Request.Headers.UserAgent.ToString();

        if (userAgent.Contains("Windows NT 10"))
            return "Windows 10/11";
        if (userAgent.Contains("Windows"))
            return "Windows";
        if (userAgent.Contains("Mac OS X"))
            return "macOS";
        if (userAgent.Contains("Linux"))
            return "Linux";
        if (userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
            return "iOS";
        if (userAgent.Contains("Android"))
            return "Android";

        return "Unknown OS";
    }

    private string GetClientIpAddress()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Check for forwarded IP (when behind proxy/load balancer)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            ip = forwardedFor.Split(',').First().Trim();
        }

        return ip ?? "Unknown";
    }

    private string GetUserAgentString()
    {
        return Request.Headers.UserAgent.ToString();
    }

    #endregion
}
