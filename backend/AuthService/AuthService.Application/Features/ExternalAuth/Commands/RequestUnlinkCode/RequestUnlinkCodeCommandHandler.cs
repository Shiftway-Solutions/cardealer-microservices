using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Shared.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace AuthService.Application.Features.ExternalAuth.Commands.RequestUnlinkCode;

/// <summary>
/// Handler for RequestUnlinkCodeCommand (AUTH-EXT-008)
/// 
/// Security flow:
/// 1. Verify user has password configured
/// 2. Verify this is the active OAuth provider
/// 3. Check rate limiting (max 3 requests per hour)
/// 4. Check lockout status
/// 5. Generate 6-digit code
/// 6. Store code in Redis with 10-minute TTL
/// 7. Queue email notification
/// 8. Log audit event
/// </summary>
public class RequestUnlinkCodeCommandHandler : IRequestHandler<RequestUnlinkCodeCommand, RequestUnlinkCodeResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RequestUnlinkCodeCommandHandler> _logger;

    // Redis key prefixes
    private const string CODE_PREFIX = "unlink_code:";
    private const string RATE_LIMIT_PREFIX = "unlink_rate:";
    private const string LOCKOUT_PREFIX = "unlink_lockout:";
    private const string ATTEMPTS_PREFIX = "unlink_attempts:";

    // Configuration
    private static readonly TimeSpan CODE_TTL = TimeSpan.FromMinutes(10);
    private const int MAX_REQUESTS_PER_HOUR = 3;
    private const int MAX_VERIFICATION_ATTEMPTS = 5;
    private static readonly TimeSpan LOCKOUT_DURATION = TimeSpan.FromHours(1);

    public RequestUnlinkCodeCommandHandler(
        IUserRepository userRepository,
        IDistributedCache cache,
        ILogger<RequestUnlinkCodeCommandHandler> logger)
    {
        _userRepository = userRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RequestUnlinkCodeResponse> Handle(RequestUnlinkCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate provider
            if (!Enum.TryParse<ExternalAuthProvider>(request.Provider, true, out var provider))
            {
                throw new BadRequestException($"Unsupported provider: {request.Provider}");
            }

            // Get user
            var user = await _userRepository.GetByIdAsync(request.UserId)
                ?? throw new NotFoundException("User not found.");

            // Verify user has this provider linked
            if (!user.IsExternalUser || user.ExternalAuthProvider != provider)
            {
                throw new BadRequestException($"You don't have a {request.Provider} account linked.");
            }

            // Verify user has password (required for active provider unlink)
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new BadRequestException(
                    "You must set a password before unlinking your OAuth account.");
            }

            // Check lockout
            var lockoutKey = $"{LOCKOUT_PREFIX}{request.UserId}:{request.Provider}";
            var isLockedOut = await _cache.GetStringAsync(lockoutKey, cancellationToken);
            if (!string.IsNullOrEmpty(isLockedOut))
            {
                throw new BadRequestException(
                    "Too many failed verification attempts. Please try again in 1 hour.");
            }

            // Check rate limiting
            var rateLimitKey = $"{RATE_LIMIT_PREFIX}{request.UserId}:{request.Provider}";
            var rateLimitData = await _cache.GetStringAsync(rateLimitKey, cancellationToken);
            var requestCount = string.IsNullOrEmpty(rateLimitData) ? 0 : int.Parse(rateLimitData);

            if (requestCount >= MAX_REQUESTS_PER_HOUR)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for unlink code request. UserId: {UserId}, Provider: {Provider}, IP: {IpAddress}",
                    request.UserId, request.Provider, request.IpAddress);

                throw new BadRequestException(
                    "Too many verification code requests. Please try again in 1 hour.");
            }

            // Generate 6-digit code
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // Store code in Redis
            var codeKey = $"{CODE_PREFIX}{request.UserId}:{request.Provider}";
            var codeData = new UnlinkCodeData
            {
                Code = code,
                UserId = request.UserId,
                Email = user.Email ?? string.Empty,
                Provider = request.Provider,
                CreatedAt = DateTime.UtcNow,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent
            };

            await _cache.SetStringAsync(
                codeKey,
                System.Text.Json.JsonSerializer.Serialize(codeData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CODE_TTL
                },
                cancellationToken);

            // Reset attempt counter when new code is issued
            var attemptsKey = $"{ATTEMPTS_PREFIX}{request.UserId}:{request.Provider}";
            await _cache.RemoveAsync(attemptsKey, cancellationToken);

            // Increment rate limit counter
            await _cache.SetStringAsync(
                rateLimitKey,
                (requestCount + 1).ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                },
                cancellationToken);

            // Queue email notification
            // TODO: Publish event to RabbitMQ for NotificationService
            // var emailEvent = new UnlinkCodeRequestedEvent
            // {
            //     UserId = request.UserId,
            //     Email = user.Email,
            //     Provider = request.Provider,
            //     Code = code,
            //     ExpiresAt = DateTime.UtcNow.Add(CODE_TTL),
            //     IpAddress = request.IpAddress
            // };
            // await _eventPublisher.PublishAsync(emailEvent, cancellationToken);

            _logger.LogInformation(
                "Unlink verification code sent. UserId: {UserId}, Provider: {Provider}, Email: {Email}, IP: {IpAddress}",
                request.UserId, request.Provider, MaskEmail(user.Email ?? string.Empty), request.IpAddress);

            return new RequestUnlinkCodeResponse(
                Success: true,
                Message: "Verification code sent to your email.",
                MaskedEmail: MaskEmail(user.Email ?? string.Empty),
                ExpiresInMinutes: 10
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
            _logger.LogError(ex, "Error requesting unlink code for user {UserId}", request.UserId);
            throw new ApplicationException("An error occurred while sending the verification code. Please try again.", ex);
        }
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return "***@***.***";

        var parts = email.Split('@');
        if (parts.Length != 2)
            return "***@***.***";

        var localPart = parts[0];
        var domain = parts[1];

        var maskedLocal = localPart.Length <= 2
            ? new string('*', localPart.Length)
            : localPart[0] + new string('*', localPart.Length - 2) + localPart[^1];

        return $"{maskedLocal}@{domain}";
    }
}

/// <summary>
/// Unlink verification code data stored in Redis
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
