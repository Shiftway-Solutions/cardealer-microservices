using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Shared.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.ExternalAuth.Commands.UnlinkActiveProvider;

/// <summary>
/// Handler for UnlinkActiveProviderCommand (AUTH-EXT-008)
/// 
/// Security flow:
/// 1. Validate verification code
/// 2. Verify user and provider
/// 3. Unlink the OAuth provider
/// 4. Revoke ALL user sessions
/// 5. Clean up Redis keys
/// 6. Send confirmation email
/// 7. Log audit event (WARNING level)
/// </summary>
public class UnlinkActiveProviderCommandHandler : IRequestHandler<UnlinkActiveProviderCommand, UnlinkActiveProviderResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<UnlinkActiveProviderCommandHandler> _logger;

    // Redis key prefixes
    private const string CODE_PREFIX = "unlink_code:";
    private const string ATTEMPTS_PREFIX = "unlink_attempts:";
    private const string LOCKOUT_PREFIX = "unlink_lockout:";

    // Configuration
    private const int MAX_VERIFICATION_ATTEMPTS = 5;
    private static readonly TimeSpan LOCKOUT_DURATION = TimeSpan.FromHours(1);

    public UnlinkActiveProviderCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IDistributedCache cache,
        ILogger<UnlinkActiveProviderCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<UnlinkActiveProviderResponse> Handle(UnlinkActiveProviderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate provider
            if (!Enum.TryParse<ExternalAuthProvider>(request.Provider, true, out var provider))
            {
                throw new BadRequestException($"Unsupported provider: {request.Provider}");
            }

            // Check lockout first
            var lockoutKey = $"{LOCKOUT_PREFIX}{request.UserId}:{request.Provider}";
            var isLockedOut = await _cache.GetStringAsync(lockoutKey, cancellationToken);
            if (!string.IsNullOrEmpty(isLockedOut))
            {
                throw new BadRequestException(
                    "Account is temporarily locked due to too many failed attempts. Please try again in 1 hour.");
            }

            // Get and validate stored code
            var codeKey = $"{CODE_PREFIX}{request.UserId}:{request.Provider}";
            var codeDataJson = await _cache.GetStringAsync(codeKey, cancellationToken);

            if (string.IsNullOrEmpty(codeDataJson))
            {
                throw new BadRequestException(
                    "Verification code has expired or was not requested. Please request a new code.");
            }

            var codeData = System.Text.Json.JsonSerializer.Deserialize<UnlinkCodeData>(codeDataJson);
            if (codeData == null)
            {
                throw new BadRequestException("Invalid verification data. Please request a new code.");
            }

            // Validate code with attempt tracking
            var attemptsKey = $"{ATTEMPTS_PREFIX}{request.UserId}:{request.Provider}";
            var attemptsData = await _cache.GetStringAsync(attemptsKey, cancellationToken);
            var attempts = string.IsNullOrEmpty(attemptsData) ? 0 : int.Parse(attemptsData);

            if (codeData.Code != request.VerificationCode)
            {
                attempts++;

                if (attempts >= MAX_VERIFICATION_ATTEMPTS)
                {
                    // Lock out the user
                    await _cache.SetStringAsync(
                        lockoutKey,
                        "locked",
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = LOCKOUT_DURATION
                        },
                        cancellationToken);

                    // Clean up code and attempts
                    await _cache.RemoveAsync(codeKey, cancellationToken);
                    await _cache.RemoveAsync(attemptsKey, cancellationToken);

                    _logger.LogWarning(
                        "User locked out due to too many failed unlink verification attempts. UserId: {UserId}, Provider: {Provider}, IP: {IpAddress}",
                        request.UserId, request.Provider, request.IpAddress);

                    throw new BadRequestException(
                        "Too many failed verification attempts. Account locked for 1 hour.");
                }

                // Save updated attempt count
                await _cache.SetStringAsync(
                    attemptsKey,
                    attempts.ToString(),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    },
                    cancellationToken);

                var remainingAttempts = MAX_VERIFICATION_ATTEMPTS - attempts;
                throw new BadRequestException(
                    $"Incorrect verification code. {remainingAttempts} attempts remaining.");
            }

            // Code is valid - get user and perform unlink
            var user = await _userRepository.GetByIdAsync(request.UserId)
                ?? throw new NotFoundException("User not found.");

            // Verify user still has this provider linked
            if (!user.IsExternalUser || user.ExternalAuthProvider != provider)
            {
                throw new BadRequestException($"You don't have a {request.Provider} account linked.");
            }

            // Verify user has password (safety check)
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new BadRequestException(
                    "Cannot unlink without a password configured. Please set a password first.");
            }

            // Store provider info before unlinking
            var unlinkedProvider = user.ExternalAuthProvider!.Value.ToString();
            var externalEmail = user.Email; // User's email before unlink

            // Unlink the external account
            user.UnlinkExternalAccount();
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Revoke ALL user sessions
            await _refreshTokenRepository.RevokeAllForUserAsync(
                request.UserId,
                "OAUTH_PROVIDER_UNLINKED",
                cancellationToken);

            // Note: sessionsRevoked count not available from this method
            // If needed, we could query count before revoking

            // Clean up Redis keys
            await _cache.RemoveAsync(codeKey, cancellationToken);
            await _cache.RemoveAsync(attemptsKey, cancellationToken);

            // Queue confirmation email
            // TODO: Publish event to RabbitMQ for NotificationService
            // var confirmEvent = new ActiveProviderUnlinkedEvent
            // {
            //     UserId = request.UserId,
            //     Email = user.Email,
            //     Provider = unlinkedProvider,
            //     ExternalEmail = externalEmail,
            //     SessionsRevoked = sessionsRevoked,
            //     IpAddress = request.IpAddress,
            //     Timestamp = DateTime.UtcNow
            // };
            // await _eventPublisher.PublishAsync(confirmEvent, cancellationToken);

            _logger.LogWarning(
                "Active OAuth provider unlinked. UserId: {UserId}, Provider: {Provider}, ExternalEmail: {ExternalEmail}, IP: {IpAddress}",
                request.UserId, unlinkedProvider, externalEmail, request.IpAddress);

            return new UnlinkActiveProviderResponse(
                Success: true,
                Message: $"Successfully unlinked your {unlinkedProvider} account. All sessions have been closed. Please log in again with your email and password.",
                Provider: unlinkedProvider,
                SessionsRevoked: 0, // Count not available from repository method
                RequiresReLogin: true
            );
        }
        catch (BadRequestException)
        {
            throw;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking active provider for user {UserId}", request.UserId);
            throw new ApplicationException("An error occurred while unlinking your account. Please try again.", ex);
        }
    }
}

/// <summary>
/// Unlink verification code data (same structure as in RequestUnlinkCodeCommandHandler)
/// </summary>
public class UnlinkCodeData
{
    public string Code { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
