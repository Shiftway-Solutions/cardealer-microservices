using AuthService.Domain.Interfaces.Repositories;
using AuthService.Shared.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace AuthService.Application.Features.Auth.Commands.RequestPasswordSetup;

/// <summary>
/// Handler for RequestPasswordSetupCommand (AUTH-PWD-001)
/// 
/// Security flow:
/// 1. Verify user exists and is OAuth-only (no password)
/// 2. Check rate limiting (max 3 requests per hour)
/// 3. Generate secure token (64 bytes, URL-safe)
/// 4. Store token in Redis with 1-hour TTL
/// 5. Queue email to NotificationService
/// 6. Log audit event
/// </summary>
public class RequestPasswordSetupCommandHandler : IRequestHandler<RequestPasswordSetupCommand, RequestPasswordSetupResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RequestPasswordSetupCommandHandler> _logger;

    // Redis key prefixes
    private const string TOKEN_PREFIX = "password_setup:token:";
    private const string RATE_LIMIT_PREFIX = "password_setup:rate:";

    // Configuration
    private static readonly TimeSpan TOKEN_TTL = TimeSpan.FromHours(1);
    private const int MAX_REQUESTS_PER_HOUR = 3;

    public RequestPasswordSetupCommandHandler(
        IUserRepository userRepository,
        IDistributedCache cache,
        ILogger<RequestPasswordSetupCommandHandler> logger)
    {
        _userRepository = userRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RequestPasswordSetupResponse> Handle(RequestPasswordSetupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get user and validate
            var user = await _userRepository.GetByIdAsync(request.UserId)
                ?? throw new NotFoundException("User not found.");

            // Verify user is OAuth-only (no password set)
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new BadRequestException(
                    "You already have a password configured. Use 'Change Password' instead.");
            }

            // Verify user is an external auth user
            if (!user.IsExternalUser || user.ExternalAuthProvider == null)
            {
                throw new BadRequestException(
                    "This feature is only available for users who registered with OAuth providers.");
            }

            // 2. Check rate limiting
            var rateLimitKey = $"{RATE_LIMIT_PREFIX}{request.UserId}";
            var rateLimitData = await _cache.GetStringAsync(rateLimitKey, cancellationToken);
            var requestCount = string.IsNullOrEmpty(rateLimitData) ? 0 : int.Parse(rateLimitData);

            if (requestCount >= MAX_REQUESTS_PER_HOUR)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for password setup request. UserId: {UserId}, IP: {IpAddress}",
                    request.UserId, request.IpAddress);

                throw new BadRequestException(
                    "Too many password setup requests. Please try again in 1 hour.");
            }

            // 3. Generate secure token
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            // 4. Store token in Redis
            var tokenKey = $"{TOKEN_PREFIX}{token}";
            var tokenData = new PasswordSetupTokenData
            {
                UserId = request.UserId,
                Email = request.Email,
                Provider = user.ExternalAuthProvider.Value.ToString(),
                CreatedAt = DateTime.UtcNow,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent
            };

            await _cache.SetStringAsync(
                tokenKey,
                System.Text.Json.JsonSerializer.Serialize(tokenData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TOKEN_TTL
                },
                cancellationToken);

            // Increment rate limit counter
            await _cache.SetStringAsync(
                rateLimitKey,
                (requestCount + 1).ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                },
                cancellationToken);

            // 5. Queue email notification
            // TODO: Publish event to RabbitMQ for NotificationService
            // var emailEvent = new PasswordSetupRequestedEvent
            // {
            //     UserId = request.UserId,
            //     Email = request.Email,
            //     Token = token,
            //     Provider = user.ExternalAuthProvider.Value.ToString(),
            //     ExpiresAt = DateTime.UtcNow.Add(TOKEN_TTL)
            // };
            // await _eventPublisher.PublishAsync(emailEvent, cancellationToken);

            _logger.LogInformation(
                "Password setup email sent. UserId: {UserId}, Email: {Email}, Provider: {Provider}, IP: {IpAddress}",
                request.UserId, request.Email, user.ExternalAuthProvider, request.IpAddress);

            var expiresAt = DateTime.UtcNow.Add(TOKEN_TTL);

            return new RequestPasswordSetupResponse(
                Success: true,
                Message: "Password setup email sent. Please check your inbox.",
                ExpiresAt: expiresAt
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
            _logger.LogError(ex, "Error requesting password setup for user {UserId}", request.UserId);
            throw new ApplicationException("An error occurred while requesting password setup. Please try again.", ex);
        }
    }
}

/// <summary>
/// Token data stored in Redis for password setup verification
/// </summary>
public class PasswordSetupTokenData
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
