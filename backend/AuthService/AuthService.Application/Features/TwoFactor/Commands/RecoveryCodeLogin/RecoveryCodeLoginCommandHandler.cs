// AuthService.Application/Features/TwoFactor/Commands/RecoveryCodeLogin/RecoveryCodeLoginCommandHandler.cs
using AuthService.Shared.Exceptions;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Application.Common.Interfaces;
using MediatR;
using AuthService.Application.DTOs.TwoFactor;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.TwoFactor.Commands.RecoveryCodeLogin;

/// <summary>
/// Handler for recovery code login.
/// 
/// Industry Standard Flow (Google, GitHub, Microsoft):
/// 1. User attempts login → gets tempToken (requires 2FA)
/// 2. User doesn't have access to 2FA device
/// 3. User clicks "Use recovery code" 
/// 4. User enters ONE of their 8-character recovery codes
/// 5. System validates the code exists and is not used
/// 6. System CONSUMES the code (marks as used - one-time only)
/// 7. System generates access + refresh tokens
/// 8. If all codes used → AUTO-REGENERATE and send via email
/// 9. System warns if remaining codes are low
/// </summary>
public class RecoveryCodeLoginCommandHandler : IRequestHandler<RecoveryCodeLoginCommand, RecoveryCodeLoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IAuthNotificationService _notificationService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RecoveryCodeLoginCommandHandler> _logger;
    private readonly IRequestContext _requestContext;

    private const int LOW_CODES_THRESHOLD = 3;
    private const int MAX_FAILED_ATTEMPTS = 5;
    private const int LOCKOUT_MINUTES = 30;
    private const int SECURITY_ALERT_THRESHOLD = 3;

    public RecoveryCodeLoginCommandHandler(
        IUserRepository userRepository,
        IJwtGenerator jwtGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        ITwoFactorService twoFactorService,
        IAuthNotificationService notificationService,
        IDistributedCache cache,
        ILogger<RecoveryCodeLoginCommandHandler> logger,
        IRequestContext requestContext)
    {
        _userRepository = userRepository;
        _jwtGenerator = jwtGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _twoFactorService = twoFactorService;
        _notificationService = notificationService;
        _cache = cache;
        _logger = logger;
        _requestContext = requestContext;
    }

    public async Task<RecoveryCodeLoginResponse> Handle(RecoveryCodeLoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate temp token
        var tempTokenData = _jwtGenerator.ValidateTempToken(request.TempToken);
        if (tempTokenData == null)
        {
            _logger.LogWarning("Recovery code login attempt with invalid temp token");
            throw new UnauthorizedException("Invalid or expired temporary token.");
        }

        var userId = tempTokenData.Value.userId;
        _logger.LogInformation("Recovery code login attempt for user {UserId}", userId);

        // 2. Check for lockout (brute force protection)
        var lockoutKey = $"recovery_code_lockout:{userId}";
        var lockoutData = await _cache.GetStringAsync(lockoutKey, cancellationToken);
        if (!string.IsNullOrEmpty(lockoutData))
        {
            _logger.LogWarning("User {UserId} is locked out from recovery code login", userId);
            throw new ValidationException($"Too many failed attempts. Please try again in {LOCKOUT_MINUTES} minutes.");
        }

        // 3. Get user
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        // 4. Validate 2FA is enabled
        if (!user.IsTwoFactorEnabled)
            throw new BadRequestException("Two-factor authentication is not enabled for this user.");

        var twoFactorAuth = await _userRepository.GetTwoFactorAuthAsync(user.Id);
        if (twoFactorAuth == null)
            throw new NotFoundException("Two-factor authentication configuration not found.");

        // 5. Clean recovery code (remove spaces, dashes, uppercase)
        var cleanedCode = request.RecoveryCode
            .Replace(" ", "")
            .Replace("-", "")
            .ToUpperInvariant()
            .Trim();

        // 6. Verify recovery code (this also CONSUMES the code - marks as used)
        var isValid = await _twoFactorService.VerifyRecoveryCodeAsync(user.Id, cleanedCode);

        if (!isValid)
        {
            // Track failed attempt with lockout - US-18.2: passes email for security alerts
            await TrackFailedRecoveryAttemptAsync(userId, user.Email!, lockoutKey, cancellationToken);

            twoFactorAuth.IncrementFailedAttempts();
            await _userRepository.AddOrUpdateTwoFactorAuthAsync(twoFactorAuth);

            _logger.LogWarning(
                "Invalid recovery code attempt for user {UserId}. Failed attempts tracked.",
                userId
            );

            throw new UnauthorizedException("Invalid or already used recovery code.");
        }

        // 7. Clear failed attempts on success
        var failedAttemptsKey = $"recovery_failed:{userId}";
        await _cache.RemoveAsync(failedAttemptsKey, cancellationToken);

        _logger.LogInformation("Recovery code verified successfully for user {UserId}", userId);

        // 8. Get remaining recovery codes count
        var remainingCodes = await _twoFactorService.GetRemainingRecoveryCodesCountAsync(user.Id);

        // 9. Generate tokens
        var accessToken = _jwtGenerator.GenerateToken(user);
        var refreshTokenValue = _jwtGenerator.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        var refreshTokenEntity = new RefreshToken(
            user.Id,
            refreshTokenValue,
            DateTime.UtcNow.AddDays(7),
            _requestContext.IpAddress  // US-18.2: Use real IP from request context
        );

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        // 8. Reset counters
        user.ResetAccessFailedCount();
        twoFactorAuth.ResetFailedAttempts();
        twoFactorAuth.MarkAsUsed();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.AddOrUpdateTwoFactorAuthAsync(twoFactorAuth);

        // 9. Prepare warning message if codes are running low OR handle if depleted
        string? warning = null;
        if (remainingCodes <= 0)
        {
            // User used their last recovery code
            _logger.LogWarning("User {UserId} used their last recovery code.", userId);

            if (twoFactorAuth.PrimaryMethod == AuthService.Domain.Enums.TwoFactorAuthType.Authenticator)
            {
                // AUTHENTICATOR: Generate NEW secret (QR code) + new recovery codes
                // User lost their device, they need a NEW authenticator setup
                var (newSecret, newQrCodeUri) = await _twoFactorService.GenerateAuthenticatorKeyAsync(user.Id, user.Email!);
                var newCodes = await _twoFactorService.GenerateRecoveryCodesAsync(user.Id);

                // Update 2FA with new secret
                twoFactorAuth.ResetAuthenticator(newSecret, newCodes);
                await _userRepository.AddOrUpdateTwoFactorAuthAsync(twoFactorAuth);

                // Send new QR code + recovery codes via email
                await _notificationService.SendNewAuthenticatorSetupAsync(user.Email!, newSecret, newQrCodeUri, newCodes);

                remainingCodes = newCodes.Count;
                warning = "🔄 Recovery codes exhausted. A NEW Authenticator setup (QR code) and 10 new recovery codes have been sent to your email. Please configure your authenticator app with the new QR code.";

                _logger.LogInformation("New authenticator secret generated and sent to {Email} for user {UserId}", user.Email, userId);
            }
            else if (twoFactorAuth.PrimaryMethod == AuthService.Domain.Enums.TwoFactorAuthType.SMS)
            {
                // SMS: Just generate new recovery codes, user needs to update phone number if lost
                var newCodes = await _twoFactorService.GenerateRecoveryCodesAsync(user.Id);
                twoFactorAuth.UpdateRecoveryCodes(newCodes);
                await _userRepository.AddOrUpdateTwoFactorAuthAsync(twoFactorAuth);
                await _notificationService.SendTwoFactorBackupCodesAsync(user.Email!, newCodes);

                remainingCodes = newCodes.Count;
                warning = "🔄 Recovery codes exhausted. 10 new recovery codes have been sent to your email. If you lost your phone, please update your phone number in account settings.";

                _logger.LogInformation("New recovery codes generated for SMS 2FA user {UserId}", userId);
            }
        }
        else if (remainingCodes <= LOW_CODES_THRESHOLD)
        {
            warning = $"⚠️ Only {remainingCodes} recovery code(s) remaining. Consider regenerating your recovery codes.";
            _logger.LogInformation("User {UserId} has only {RemainingCodes} recovery codes left", userId, remainingCodes);
        }

        _logger.LogInformation(
            "Recovery code login successful for user {UserId}. Remaining codes: {RemainingCodes}",
            userId,
            remainingCodes
        );

        return new RecoveryCodeLoginResponse(
            user.Id,
            user.Email!,
            accessToken,
            refreshTokenValue,
            expiresAt,
            remainingCodes,
            warning
        );
    }

    /// <summary>
    /// US-18.2: Tracks failed recovery code attempts, sends security alerts after SECURITY_ALERT_THRESHOLD,
    /// and applies lockout after MAX_FAILED_ATTEMPTS.
    /// Uses Redis cache for distributed tracking.
    /// </summary>
    private async Task TrackFailedRecoveryAttemptAsync(string odGuidUserId, string userEmail, string lockoutKey, CancellationToken cancellationToken)
    {
        var failedAttemptsKey = $"recovery_failed:{odGuidUserId}";
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
                    AlertType: "FailedRecoveryCodeAttempts",
                    IpAddress: _requestContext.IpAddress,
                    AttemptCount: attempts,
                    Timestamp: DateTime.UtcNow,
                    DeviceInfo: _requestContext.UserAgent
                );
                await _notificationService.SendSecurityAlertAsync(userEmail, alert);
                _logger.LogInformation("Security alert sent to {Email} after {Attempts} failed recovery code attempts", userEmail, attempts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send security alert for user {UserId}", odGuidUserId);
            }
        }

        if (attempts >= MAX_FAILED_ATTEMPTS)
        {
            // Apply lockout
            await _cache.SetStringAsync(lockoutKey, "locked", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(LOCKOUT_MINUTES)
            }, cancellationToken);

            // Clear attempts counter
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
                _logger.LogError(ex, "Failed to send lockout notification for user {UserId}", odGuidUserId);
            }

            _logger.LogWarning("User {UserId} locked out from recovery code login after {Attempts} failed attempts",
                odGuidUserId, MAX_FAILED_ATTEMPTS);
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
