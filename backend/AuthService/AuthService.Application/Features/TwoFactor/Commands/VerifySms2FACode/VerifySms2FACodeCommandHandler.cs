using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Domain.Enums;
using AuthService.Shared.Exceptions;
using AuthService.Application.Common.Interfaces;

namespace AuthService.Application.Features.TwoFactor.Commands.VerifySms2FACode;

/// <summary>
/// Handler for verifying SMS 2FA code and completing login.
/// 
/// Flow:
/// 1. Validate temp token → get user ID
/// 2. Check for lockout (too many failed attempts)
/// 3. Retrieve stored code from Redis
/// 4. Compare with provided code
/// 5. If valid → generate access + refresh tokens
/// 6. If invalid → track failed attempt, lockout after 5 failures
/// 
/// US-18.2: Sends security alert email after 3+ failed attempts
/// </summary>
public class VerifySms2FACodeCommandHandler : IRequestHandler<VerifySms2FACodeCommand, VerifySms2FACodeResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IDistributedCache _cache;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IAuthNotificationService _notificationService;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<VerifySms2FACodeCommandHandler> _logger;

    private const int MAX_FAILED_ATTEMPTS = 5;
    private const int LOCKOUT_MINUTES = 15;
    private const int SECURITY_ALERT_THRESHOLD = 3;

    public VerifySms2FACodeCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IDistributedCache cache,
        IJwtGenerator jwtGenerator,
        IAuthNotificationService notificationService,
        IRequestContext requestContext,
        ILogger<VerifySms2FACodeCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _cache = cache;
        _jwtGenerator = jwtGenerator;
        _notificationService = notificationService;
        _requestContext = requestContext;
        _logger = logger;
    }

    public async Task<VerifySms2FACodeResponse> Handle(VerifySms2FACodeCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate temp token and get user ID
        var tempTokenData = _jwtGenerator.ValidateTempToken(request.TempToken);
        if (tempTokenData == null)
        {
            _logger.LogWarning("Invalid or expired temp token for SMS 2FA verification");
            throw new UnauthorizedException("Invalid or expired temp token.");
        }

        if (!Guid.TryParse(tempTokenData.Value.userId, out var userId))
        {
            _logger.LogError("Invalid user ID format in temp token");
            throw new UnauthorizedException("Invalid temp token.");
        }

        // 2. Check if user is locked out
        var lockoutKey = $"sms_2fa_lockout:{userId}";
        var lockoutData = await _cache.GetStringAsync(lockoutKey, cancellationToken);
        if (!string.IsNullOrEmpty(lockoutData))
        {
            _logger.LogWarning("User {UserId} is locked out from SMS 2FA verification", userId);
            throw new ValidationException($"Too many failed attempts. Please try again in {LOCKOUT_MINUTES} minutes.");
        }

        // 3. Get user
        var user = await _userRepository.GetByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        // 4. Get 2FA settings
        var twoFactorAuth = await _userRepository.GetTwoFactorAuthAsync(userId.ToString());
        if (twoFactorAuth == null || twoFactorAuth.Status != TwoFactorAuthStatus.Enabled)
        {
            _logger.LogWarning("2FA not enabled for user {UserId}", userId);
            throw new ValidationException("Two-factor authentication is not enabled.");
        }

        // 5. Get stored code from Redis
        var cacheKey = $"sms_2fa_code:{userId}";
        var storedCode = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (string.IsNullOrEmpty(storedCode))
        {
            _logger.LogWarning("No SMS code found or expired for user {UserId}", userId);
            throw new ValidationException("Verification code expired. Please request a new code.");
        }

        // 6. Verify code with constant-time comparison to prevent timing attacks
        var codeBytes = Encoding.UTF8.GetBytes(request.Code.PadRight(6, '0'));
        var storedBytes = Encoding.UTF8.GetBytes(storedCode.PadRight(6, '0'));
        var isValidCode = CryptographicOperations.FixedTimeEquals(codeBytes, storedBytes);

        if (!isValidCode)
        {
            // Track failed attempt and send security alert if threshold reached (US-18.2)
            await TrackFailedAttemptAsync(userId, user.Email!, cancellationToken);

            _logger.LogWarning("Invalid SMS 2FA code for user {UserId}", userId);
            throw new UnauthorizedException("Invalid verification code.");
        }

        // 7. Code is valid - remove from cache
        await _cache.RemoveAsync(cacheKey, cancellationToken);

        // Clear any failed attempt counter
        var failedAttemptsKey = $"sms_2fa_failed:{userId}";
        await _cache.RemoveAsync(failedAttemptsKey, cancellationToken);

        // 8. Generate tokens
        var accessToken = _jwtGenerator.GenerateToken(user);
        var refreshTokenValue = _jwtGenerator.GenerateRefreshToken();

        // 9. Save refresh token with real IP (US-13.1)
        var refreshTokenEntity = new RefreshToken(
            user.Id,
            refreshTokenValue,
            DateTime.UtcNow.AddDays(7),
            _requestContext.IpAddress
        );
        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        _logger.LogInformation("SMS 2FA verification successful for user {UserId}", userId);

        return new VerifySms2FACodeResponse(
            UserId: userId,
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            Message: "SMS verification successful. Login complete."
        );
    }

    /// <summary>
    /// US-18.2: Tracks failed attempts and sends security alert after SECURITY_ALERT_THRESHOLD.
    /// Applies lockout after MAX_FAILED_ATTEMPTS.
    /// </summary>
    private async Task TrackFailedAttemptAsync(Guid userId, string userEmail, CancellationToken cancellationToken)
    {
        var failedAttemptsKey = $"sms_2fa_failed:{userId}";
        var attemptsStr = await _cache.GetStringAsync(failedAttemptsKey, cancellationToken);

        int attempts = 1;
        if (!string.IsNullOrEmpty(attemptsStr) && int.TryParse(attemptsStr, out var existing))
        {
            attempts = existing + 1;
        }

        // US-18.2: Send security alert email after 3+ failed attempts
        if (attempts >= SECURITY_ALERT_THRESHOLD)
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
                _logger.LogInformation("Security alert sent to {Email} after {Attempts} failed SMS 2FA attempts", userEmail, attempts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send security alert for user {UserId}", userId);
            }
        }

        if (attempts >= MAX_FAILED_ATTEMPTS)
        {
            // Lock out user
            var lockoutKey = $"sms_2fa_lockout:{userId}";
            await _cache.SetStringAsync(lockoutKey, "locked", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(LOCKOUT_MINUTES)
            }, cancellationToken);

            // Clear failed attempts counter
            await _cache.RemoveAsync(failedAttemptsKey, cancellationToken);

            // US-18.2: Send lockout notification
            try
            {
                var lockoutAlert = new SecurityAlertDto(
                    AlertType: "AccountLockout",
                    IpAddress: _requestContext.IpAddress,
                    AttemptCount: attempts,
                    Timestamp: DateTime.UtcNow,
                    DeviceInfo: _requestContext.UserAgent,
                    LockoutDuration: TimeSpan.FromMinutes(LOCKOUT_MINUTES)
                );
                await _notificationService.SendSecurityAlertAsync(userEmail, lockoutAlert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send lockout notification for user {UserId}", userId);
            }

            _logger.LogWarning("User {UserId} locked out from SMS 2FA after {Attempts} failed attempts",
                userId, MAX_FAILED_ATTEMPTS);
        }
        else
        {
            // Update failed attempts counter
            await _cache.SetStringAsync(failedAttemptsKey, attempts.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(LOCKOUT_MINUTES)
            }, cancellationToken);
        }
    }
}
