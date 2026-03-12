// AuthService.Application/Features/TwoFactor/Commands/RecoveryAccountWithAllCodes/RecoveryAccountWithAllCodesCommandHandler.cs
using AuthService.Shared.Exceptions;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.TwoFactor.Commands.RecoveryAccountWithAllCodes;

/// <summary>
/// Handler for full account recovery using ALL 10 recovery codes.
/// 
/// Security Flow:
/// 1. Validate temp token → get user ID
/// 2. Get user's stored recovery codes from database (TwoFactorAuth entity)
/// 3. Compare ALL 10 codes (user must provide exact match)
/// 4. If ALL match → reset 2FA, generate new authenticator + new codes
/// 5. Return new setup + new tokens
/// 
/// This is a LAST RESORT recovery method when user has lost access to:
/// - Their authenticator app
/// - Individual recovery codes in Redis (expired)
/// BUT still has the original 10 codes that were shown when enabling 2FA.
/// </summary>
public class RecoveryAccountWithAllCodesCommandHandler
    : IRequestHandler<RecoveryAccountWithAllCodesCommand, RecoveryAccountWithAllCodesResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RecoveryAccountWithAllCodesCommandHandler> _logger;

    // Stricter lockout for full recovery (more sensitive operation)
    private const int MAX_FAILED_ATTEMPTS = 3;
    private const int LOCKOUT_MINUTES = 60;

    public RecoveryAccountWithAllCodesCommandHandler(
        IUserRepository userRepository,
        IJwtGenerator jwtGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        ITwoFactorService twoFactorService,
        IDistributedCache cache,
        ILogger<RecoveryAccountWithAllCodesCommandHandler> logger)
    {
        _userRepository = userRepository;
        _jwtGenerator = jwtGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _twoFactorService = twoFactorService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RecoveryAccountWithAllCodesResponse> Handle(
        RecoveryAccountWithAllCodesCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate temp token
        var tempTokenData = _jwtGenerator.ValidateTempToken(request.TempToken);
        if (tempTokenData == null)
        {
            throw new UnauthorizedException("Invalid or expired temp token.");
        }

        var userId = tempTokenData.Value.userId;
        _logger.LogInformation("Full account recovery attempt for user {UserId}", userId);

        // 2. Check for lockout (brute force protection - stricter for full recovery)
        var lockoutKey = $"full_recovery_lockout:{userId}";
        var lockoutData = await _cache.GetStringAsync(lockoutKey, cancellationToken);
        if (!string.IsNullOrEmpty(lockoutData))
        {
            _logger.LogWarning("User {UserId} is locked out from full account recovery", userId);
            throw new ValidationException($"Too many failed attempts. Please try again in {LOCKOUT_MINUTES} minutes or contact support.");
        }

        // 3. Get user

        // 2. Get user
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        // 3. Get 2FA config from database via UserRepository
        var twoFactorAuth = await _userRepository.GetTwoFactorAuthAsync(userId);
        if (twoFactorAuth == null)
        {
            throw new NotFoundException("Two-factor authentication not configured for this account.");
        }

        // 4. Validate that user provided exactly 10 codes
        if (request.RecoveryCodes == null || request.RecoveryCodes.Count != 10)
        {
            _logger.LogWarning("Invalid recovery code count for user {UserId}. Expected 10, got {Count}",
                userId, request.RecoveryCodes?.Count ?? 0);
            throw new ValidationException("You must provide exactly 10 recovery codes.");
        }

        // 5. Normalize all provided codes
        var normalizedProvidedCodes = request.RecoveryCodes
            .Select(c => c.Replace(" ", "").Replace("-", "").ToUpperInvariant().Trim())
            .ToList();

        // 6. Get stored recovery codes from database
        var storedCodes = twoFactorAuth.RecoveryCodes;
        if (storedCodes == null || storedCodes.Count == 0)
        {
            _logger.LogError("No stored recovery codes found for user {UserId}", userId);
            throw new InvalidOperationException("No recovery codes found. Contact support.");
        }

        // 7. Normalize stored codes
        var normalizedStoredCodes = storedCodes
            .Select(c => c.Replace(" ", "").Replace("-", "").ToUpperInvariant().Trim())
            .ToList();

        // 8. Compare ALL codes - order doesn't matter, but all 10 must match
        var providedSet = new HashSet<string>(normalizedProvidedCodes);
        var storedSet = new HashSet<string>(normalizedStoredCodes);

        if (!providedSet.SetEquals(storedSet))
        {
            // Log the mismatch for security audit
            var matchingCodes = providedSet.Intersect(storedSet).Count();

            _logger.LogWarning(
                "Full recovery failed for user {UserId}. Provided {ProvidedCount} codes, {MatchingCount} matched out of {StoredCount} stored codes",
                userId, normalizedProvidedCodes.Count, matchingCodes, normalizedStoredCodes.Count);

            // Track failed attempt with lockout
            await TrackFailedFullRecoveryAttemptAsync(userId, lockoutKey, cancellationToken);

            throw new UnauthorizedException(
                "Recovery codes do not match. Please provide all original recovery codes exactly as they were given to you.");
        }

        // 9. Clear failed attempts on success
        var failedAttemptsKey = $"full_recovery_failed:{userId}";
        await _cache.RemoveAsync(failedAttemptsKey, cancellationToken);

        _logger.LogInformation("All recovery codes verified for user {UserId}. Proceeding with account recovery.", userId);

        // 10. Generate new authenticator secret
        var setupResult = await _twoFactorService.GenerateAuthenticatorKeyAsync(user.Id.ToString(), user.Email ?? string.Empty);
        var newSecret = setupResult.secret;
        var qrCodeUri = setupResult.qrCodeUri;

        // 10. Generate new recovery codes
        var newRecoveryCodes = await _twoFactorService.GenerateRecoveryCodesAsync(user.Id.ToString());

        // 11. Update 2FA in database with new secret and codes
        twoFactorAuth.ResetAuthenticator(newSecret, newRecoveryCodes);
        await _userRepository.AddOrUpdateTwoFactorAuthAsync(twoFactorAuth);

        _logger.LogInformation("Account recovery successful for user {UserId}. New 2FA setup created.", userId);

        // 12. Generate login tokens (user is now authenticated)
        var accessToken = _jwtGenerator.GenerateToken(user);
        var refreshTokenValue = _jwtGenerator.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        var refreshTokenEntity = new RefreshToken(
            user.Id,
            refreshTokenValue,
            DateTime.UtcNow.AddDays(7),
            "127.0.0.1"
        );

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        return new RecoveryAccountWithAllCodesResponse(
            UserId: userId.ToString(),
            Email: user.Email ?? string.Empty,
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            ExpiresAt: expiresAt,
            NewAuthenticatorSecret: newSecret,
            NewQrCodeUri: qrCodeUri,
            NewRecoveryCodes: newRecoveryCodes,
            Message: "Account recovered successfully. Please set up your new authenticator and save these new recovery codes in a safe place."
        );
    }

    /// <summary>
    /// Tracks failed full recovery attempts and applies stricter lockout.
    /// Full recovery is a sensitive operation so we use fewer attempts and longer lockout.
    /// </summary>
    private async Task TrackFailedFullRecoveryAttemptAsync(string userId, string lockoutKey, CancellationToken cancellationToken)
    {
        var failedAttemptsKey = $"full_recovery_failed:{userId}";
        var attemptsStr = await _cache.GetStringAsync(failedAttemptsKey, cancellationToken);

        int attempts = 1;
        if (!string.IsNullOrEmpty(attemptsStr) && int.TryParse(attemptsStr, out var existing))
        {
            attempts = existing + 1;
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

            _logger.LogWarning("User {UserId} locked out from full account recovery after {Attempts} failed attempts. " +
                "This is a security-sensitive operation.", userId, MAX_FAILED_ATTEMPTS);
        }
        else
        {
            // Update failed attempts counter
            await _cache.SetStringAsync(failedAttemptsKey, attempts.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(LOCKOUT_MINUTES)
            }, cancellationToken);

            _logger.LogWarning("Full recovery attempt {Attempts}/{Max} failed for user {UserId}",
                attempts, MAX_FAILED_ATTEMPTS, userId);
        }
    }
}
