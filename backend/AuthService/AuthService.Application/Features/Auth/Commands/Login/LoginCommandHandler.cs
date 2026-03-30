using AuthService.Shared.Exceptions;
using AuthService.Domain.Entities;
using RefreshTokenEntity = AuthService.Domain.Entities.RefreshToken;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Application.Services;
using MediatR;
using AuthService.Application.DTOs.Auth;
using AuthService.Domain.Interfaces;
using CarDealer.Contracts.Events.Auth;
using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Enums;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

// Security settings are read dynamically from ConfigurationService
// (admin panel → ConfigurationService → this handler via ISecurityConfigProvider)

namespace AuthService.Application.Features.Auth.Commands.Login;

/// <summary>
/// US-18.3: Login handler with CAPTCHA verification after 2 failed attempts.
/// AUTH-SEC-005: Revoked device detection and verification.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IRequestContext _requestContext;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ICaptchaService _captchaService;
    private readonly IDistributedCache _cache;
    private readonly IAuthNotificationService _notificationService;
    private readonly IRevokedDeviceService _revokedDeviceService;
    private readonly IGeoLocationService _geoLocationService;
    private readonly ISecurityConfigProvider _securityConfig;
    private readonly ILogger<LoginCommandHandler> _logger;

    private const int CAPTCHA_REQUIRED_AFTER_ATTEMPTS = 2;
    private const int SECURITY_ALERT_THRESHOLD = 3;
    // MAX_FAILED_ATTEMPTS and LOCKOUT_MINUTES are now read from ConfigurationService
    // via ISecurityConfigProvider (admin panel: Seguridad → Intentos de login máximos / Tiempo de bloqueo)

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtGenerator jwtGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IUserSessionRepository sessionRepository,
        IEventPublisher eventPublisher,
        IRequestContext requestContext,
        ITwoFactorService twoFactorService,
        ICaptchaService captchaService,
        IDistributedCache cache,
        IAuthNotificationService notificationService,
        IRevokedDeviceService revokedDeviceService,
        IGeoLocationService geoLocationService,
        ISecurityConfigProvider securityConfig,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtGenerator = jwtGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _sessionRepository = sessionRepository;
        _eventPublisher = eventPublisher;
        _requestContext = requestContext;
        _twoFactorService = twoFactorService;
        _captchaService = captchaService;
        _cache = cache;
        _notificationService = notificationService;
        _revokedDeviceService = revokedDeviceService;
        _geoLocationService = geoLocationService;
        _securityConfig = securityConfig;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // US-18.3: Check if CAPTCHA is required based on failed attempts
        var failedAttemptsKey = $"login_failed:{request.Email.ToLowerInvariant()}";
        string? failedAttemptsStr = null;
        try
        {
            failedAttemptsStr = await _cache.GetStringAsync(failedAttemptsKey, cancellationToken);
        }
        catch (Exception ex)
        {
            // Redis unavailable (MISCONF, connection refused, etc.) — proceed with 0 failed attempts
            _logger.LogWarning(ex, "Redis unavailable reading failed-attempts counter for {Email} — assuming 0", request.Email);
        }
        var failedAttempts = 0;
        if (!string.IsNullOrEmpty(failedAttemptsStr))
        {
            int.TryParse(failedAttemptsStr, out failedAttempts);
        }

        if (_captchaService.IsCaptchaRequired(failedAttempts))
        {
            if (string.IsNullOrEmpty(request.CaptchaToken))
            {
                throw new BadRequestException("Se requiere verificación CAPTCHA. Por favor, completa el desafío.");
            }

            var captchaValid = await _captchaService.VerifyAsync(
                request.CaptchaToken,
                "login",
                _requestContext.IpAddress);

            if (!captchaValid)
            {
                _logger.LogWarning("CAPTCHA verification failed for {Email}. Score: {Score}",
                    request.Email, _captchaService.LastScore);
                throw new BadRequestException("La verificación CAPTCHA falló. Inténtalo de nuevo.");
            }
        }

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null)
        {
            await TrackFailedLoginAttemptAsync(request.Email, null, null, cancellationToken);
            throw new UnauthorizedException("Credenciales inválidas.");
        }

        // Verificar que PasswordHash no sea nulo
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            await TrackFailedLoginAttemptAsync(request.Email, user.Email, user, cancellationToken);
            throw new UnauthorizedException("Credenciales inválidas.");
        }

        // Check lockout BEFORE password verification so locked-out users get the correct error message
        if (user.IsLockedOut())
            throw new UnauthorizedException("La cuenta está temporalmente bloqueada. Inténtalo más tarde.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await TrackFailedLoginAttemptAsync(request.Email, user.Email, user, cancellationToken);
            throw new UnauthorizedException("Credenciales inválidas.");
        }

        if (!user.EmailConfirmed)
            throw new UnauthorizedException("Por favor, verifica tu email antes de iniciar sesión.");


        // Clear failed attempts on successful password verification (Redis + DB)
        try
        {
            await _cache.RemoveAsync(failedAttemptsKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable clearing failed-attempts for {Email} — continuing login", request.Email);
        }
        if (user.AccessFailedCount > 0)
        {
            user.ResetAccessFailedCount();
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        // AUTH-SEC-005: Check if this device was previously revoked
        RevokedDeviceCheckResult? revokedDeviceCheck = null;
        try
        {
            revokedDeviceCheck = await _revokedDeviceService.CheckIfDeviceIsRevokedAsync(
                user.Id,
                _requestContext.IpAddress,
                _requestContext.UserAgent ?? "unknown",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for revoked-device check for user {UserId} — skipping check", user.Id);
        }

        if (revokedDeviceCheck?.IsRevoked == true)
        {
            _logger.LogWarning(
                "AUTH-SEC-005: Login attempt from revoked device. User: {UserId}, Fingerprint: {Fingerprint}",
                user.Id, revokedDeviceCheck.DeviceFingerprint);

            // Return a response indicating revoked device verification is required
            // The frontend will need to handle this and call the revoked device verification flow
            return new LoginResponse(
                user.Id,
                user.Email!,
                string.Empty, // No access token
                string.Empty, // No refresh token
                DateTime.UtcNow.AddMinutes(10), // Short expiration
                false, // requiresTwoFactor
                null, // No temp token (reserved for 2FA)
                null, // No 2FA type
                true, // requiresRevokedDeviceVerification
                revokedDeviceCheck.DeviceFingerprint // Device fingerprint for verification
            );
        }

        // Verificar si requiere 2FA
        if (user.IsTwoFactorEnabled)
        {
            // Generar token temporal para 2FA
            var tempToken = _jwtGenerator.GenerateTempToken(user.Id);

            // Determinar el tipo de 2FA y obtener el string para el frontend
            string twoFactorTypeString = "authenticator"; // default

            if (user.TwoFactorAuth != null)
            {
                var twoFactorType = user.TwoFactorAuth.PrimaryMethod;

                // Map enum to string for frontend
                twoFactorTypeString = twoFactorType switch
                {
                    TwoFactorAuthType.SMS => "sms",
                    TwoFactorAuthType.Email => "email",
                    _ => "authenticator"
                };

                // Enviar código 2FA automáticamente para SMS y Email
                if (twoFactorType == TwoFactorAuthType.SMS || twoFactorType == TwoFactorAuthType.Email)
                {
                    await _twoFactorService.SendTwoFactorCodeAsync(user.Id, twoFactorType);
                }
            }

            return new LoginResponse(
                user.Id,
                user.Email!,
                string.Empty, // No access token yet
                string.Empty, // No refresh token yet
                DateTime.UtcNow.AddMinutes(5), // Short expiration for 2FA
                true, // requiresTwoFactor
                tempToken,
                twoFactorTypeString // Include 2FA type for frontend
            );
        }

        // Flujo normal sin 2FA
        // Read expiration dynamically from ConfigurationService (admin panel: Seguridad)
        var jwtExpiresMinutes = await _securityConfig.GetJwtExpiresMinutesAsync(cancellationToken);
        var refreshTokenDays = await _securityConfig.GetRefreshTokenDaysAsync(cancellationToken);

        var refreshTokenValue = _jwtGenerator.GenerateRefreshToken();

        var expiresAt = DateTime.UtcNow.AddMinutes(jwtExpiresMinutes);
        var sessionExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays);

        var refreshTokenEntity = new RefreshTokenEntity(
            user.Id,
            refreshTokenValue,
            sessionExpiresAt,
            _requestContext.IpAddress
        );

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        // Parse device info once
        var userAgent = _requestContext.UserAgent ?? string.Empty;
        var deviceInfo = ParseDeviceInfo(userAgent);
        var browser = ParseBrowser(userAgent);
        var operatingSystem = ParseOperatingSystem(userAgent);
        var deviceFingerprint = UserSession.ComputeFingerprint(browser, operatingSystem, deviceInfo);

        // Get geolocation from IP address
        string? locationString = null;
        string? country = null;
        string? city = null;
        try
        {
            var geoLocation = await _geoLocationService.GetLocationFromIpAsync(_requestContext.IpAddress);
            if (geoLocation != null)
            {
                country = geoLocation.Country;
                city = geoLocation.City;
                locationString = !string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(country)
                    ? $"{city}, {country}"
                    : country ?? city ?? "Unknown location";
                _logger.LogDebug("Geolocation for IP {IpAddress}: {Location}", _requestContext.IpAddress, locationString);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get geolocation for IP {IpAddress}", _requestContext.IpAddress);
        }

        // Check if there's an existing active session for the same device fingerprint
        // (Browser + OS + DeviceType hash — IP excluded to handle dynamic IPs, VPNs, k8s pod rotation)
        var existingSession = await _sessionRepository.GetActiveSessionByDeviceAsync(
            user.Id,
            deviceFingerprint,
            cancellationToken);

        // Pre-determine session ID BEFORE generating the JWT so it can be embedded as a claim.
        // This allows /api/auth/security/sessions to correctly mark isCurrent = true.
        Guid sessionId;

        if (existingSession != null)
        {
            sessionId = existingSession.Id;
            // Reuse existing session — update with new refresh token and latest IP
            existingSession.RenewSession(refreshTokenEntity.Id.ToString(), sessionExpiresAt, _requestContext.IpAddress);
            // Update location if we got one
            if (!string.IsNullOrEmpty(locationString))
            {
                existingSession.UpdateLocation(locationString, country, city);
            }
            await _sessionRepository.UpdateAsync(existingSession, cancellationToken);
            _logger.LogInformation("Renewed existing session {SessionId} for user {UserId}", existingSession.Id, user.Id);
        }
        else
        {
            // Create new session only if no matching session exists
            var userSession = new UserSession(
                userId: user.Id,
                refreshTokenId: refreshTokenEntity.Id.ToString(),
                deviceInfo: deviceInfo,
                browser: browser,
                operatingSystem: operatingSystem,
                ipAddress: _requestContext.IpAddress,
                expiresAt: sessionExpiresAt
            );
            sessionId = userSession.Id;

            // Set location if available
            if (!string.IsNullOrEmpty(locationString))
            {
                userSession.UpdateLocation(locationString, country, city);
            }

            await _sessionRepository.AddAsync(userSession, cancellationToken);
            _logger.LogInformation("Created new session {SessionId} for user {UserId} from {Location}",
                userSession.Id, user.Id, locationString ?? "Unknown location");

            // Send new session notification email
            try
            {
                var sessionNotification = new NewSessionNotificationDto(
                    DeviceInfo: deviceInfo,
                    Browser: browser,
                    OperatingSystem: operatingSystem,
                    IpAddress: _requestContext.IpAddress,
                    LoginTime: DateTime.UtcNow,
                    Location: locationString
                );
                await _notificationService.SendNewSessionNotificationAsync(user.Email!, sessionNotification);
                _logger.LogInformation("New session notification sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                // Don't fail login if notification fails
                _logger.LogWarning(ex, "Failed to send new session notification to {Email}", user.Email);
            }
        }

        // Generate JWT AFTER session ID is determined so it can be embedded as "SessionId" claim.
        // This allows /api/auth/security/sessions to mark isCurrent = true for the active session.
        var accessToken = _jwtGenerator.GenerateToken(user, jwtExpiresMinutes, sessionId.ToString());

        user.ResetAccessFailedCount();
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Publish UserLoggedInEvent
        var userLoggedInEvent = new UserLoggedInEvent
        {
            UserId = Guid.Parse(user.Id),
            Email = user.Email!,
            LoggedInAt = DateTime.UtcNow,
            IpAddress = _requestContext.IpAddress,
            UserAgent = _requestContext.UserAgent
        };
        await _eventPublisher.PublishAsync(userLoggedInEvent, cancellationToken);

        return new LoginResponse(
            user.Id,
            user.Email!,
            accessToken,
            refreshTokenValue,
            expiresAt,
            false // requiresTwoFactor
        );
    }

    /// <summary>
    /// US-18.2/18.3: Track failed login attempts for CAPTCHA requirement and security alerts.
    /// Sends security alerts after SECURITY_ALERT_THRESHOLD attempts.
    /// Lockout duration is read from ConfigurationService (admin panel: Seguridad → Tiempo de bloqueo).
    /// </summary>
    private async Task TrackFailedLoginAttemptAsync(string email, string? userEmail, ApplicationUser? user, CancellationToken cancellationToken)
    {
        var lockoutMinutes = await _securityConfig.GetLockoutDurationMinutesAsync(cancellationToken);

        var failedAttemptsKey = $"login_failed:{email.ToLowerInvariant()}";
        string? attemptsStr = null;
        try
        {
            attemptsStr = await _cache.GetStringAsync(failedAttemptsKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable reading failed-attempts for {Email}", email);
        }

        int attempts = 1;
        if (!string.IsNullOrEmpty(attemptsStr) && int.TryParse(attemptsStr, out var existing))
        {
            attempts = existing + 1;
        }

        // Update failed attempts counter — cache TTL = lockout window from admin config
        try
        {
            await _cache.SetStringAsync(failedAttemptsKey, attempts.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(lockoutMinutes)
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable writing failed-attempts for {Email}", email);
        }

        _logger.LogWarning("Failed login attempt {Attempts} for {Email} from IP {IP}",
            attempts, email, _requestContext.IpAddress);

        // US-18.2: Send security alert after 3+ failed attempts
        if (attempts >= SECURITY_ALERT_THRESHOLD && !string.IsNullOrEmpty(userEmail))
        {
            try
            {
                var alert = new SecurityAlertDto(
                    AlertType: "FailedLoginAttempts",
                    IpAddress: _requestContext.IpAddress,
                    AttemptCount: attempts,
                    Timestamp: DateTime.UtcNow,
                    DeviceInfo: _requestContext.UserAgent
                );
                await _notificationService.SendSecurityAlertAsync(userEmail, alert);
                _logger.LogInformation("Security alert sent to {Email} after {Attempts} failed login attempts",
                    userEmail, attempts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send security alert for {Email}", email);
            }
        }

        // Log when CAPTCHA will be required
        if (attempts == CAPTCHA_REQUIRED_AFTER_ATTEMPTS)
        {
            _logger.LogInformation("CAPTCHA will be required for next login attempt from {Email}", email);
        }

        // Synchronize failed attempt count to the Identity-tracked field so user.IsLockedOut() activates
        if (user != null)
        {
            var maxAttempts = await _securityConfig.GetMaxLoginAttemptsAsync(cancellationToken);
            user.IncrementAccessFailedCount(maxAttempts, lockoutMinutes);
            await _userRepository.UpdateAsync(user, cancellationToken);
        }
    }

    /// <summary>
    /// Parse device info from User-Agent string
    /// </summary>
    private static string ParseDeviceInfo(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown Device";

        if (userAgent.Contains("Mobile") || userAgent.Contains("Android") || userAgent.Contains("iPhone"))
            return "Mobile";
        if (userAgent.Contains("Tablet") || userAgent.Contains("iPad"))
            return "Tablet";
        return "Desktop";
    }

    /// <summary>
    /// Parse browser name from User-Agent string
    /// </summary>
    private static string ParseBrowser(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";

        if (userAgent.Contains("Edg/")) return "Microsoft Edge";
        if (userAgent.Contains("Chrome/") && !userAgent.Contains("Chromium")) return "Chrome";
        if (userAgent.Contains("Firefox/")) return "Firefox";
        if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome")) return "Safari";
        if (userAgent.Contains("Opera") || userAgent.Contains("OPR")) return "Opera";
        return "Unknown Browser";
    }

    /// <summary>
    /// Parse operating system from User-Agent string
    /// </summary>
    private static string ParseOperatingSystem(string userAgent)
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
}
