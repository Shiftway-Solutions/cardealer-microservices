using AuthService.Application.Features.Auth.Commands.RequestPasswordSetup;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Shared.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.Commands.SetPasswordForOAuthUser;

/// <summary>
/// Handler for SetPasswordForOAuthUserCommand (AUTH-PWD-001)
/// 
/// Security flow:
/// 1. Validate token exists and is not expired
/// 2. Validate passwords match and meet requirements
/// 3. Get user and verify still OAuth-only
/// 4. Hash and save password
/// 5. Delete token from Redis (single-use)
/// 6. Send confirmation email
/// 7. Log audit event
/// </summary>
public class SetPasswordForOAuthUserCommandHandler : IRequestHandler<SetPasswordForOAuthUserCommand, SetPasswordForOAuthUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SetPasswordForOAuthUserCommandHandler> _logger;

    private const string TOKEN_PREFIX = "password_setup:token:";
    private static readonly TimeSpan TOKEN_TTL = TimeSpan.FromHours(1);

    public SetPasswordForOAuthUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IDistributedCache cache,
        ILogger<SetPasswordForOAuthUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SetPasswordForOAuthUserResponse> Handle(SetPasswordForOAuthUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate token
            var tokenKey = $"{TOKEN_PREFIX}{request.Token}";
            var tokenDataJson = await _cache.GetStringAsync(tokenKey, cancellationToken);

            if (string.IsNullOrEmpty(tokenDataJson))
            {
                throw new BadRequestException(
                    "This link has expired or is invalid. Please request a new password setup email.");
            }

            var tokenData = System.Text.Json.JsonSerializer.Deserialize<PasswordSetupTokenData>(tokenDataJson);
            if (tokenData == null)
            {
                throw new BadRequestException("Invalid token data. Please request a new password setup email.");
            }

            // Check expiration
            var expiresAt = tokenData.CreatedAt.Add(TOKEN_TTL);
            if (DateTime.UtcNow > expiresAt)
            {
                await _cache.RemoveAsync(tokenKey, cancellationToken);
                throw new BadRequestException("This link has expired. Please request a new password setup email.");
            }

            // 2. Validate passwords match
            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new BadRequestException("Passwords do not match.");
            }

            // 3. Get user and verify state
            var user = await _userRepository.GetByIdAsync(tokenData.UserId)
                ?? throw new NotFoundException("User not found.");

            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                // User already has password (race condition or already set)
                await _cache.RemoveAsync(tokenKey, cancellationToken);

                return new SetPasswordForOAuthUserResponse(
                    Success: true,
                    Message: "Password is already configured for your account.",
                    Email: user.Email,
                    CanNowUnlinkProvider: true
                );
            }

            // 4. Hash and save password
            var hashedPassword = _passwordHasher.HashPassword(user, request.NewPassword);
            user.PasswordHash = hashedPassword;
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.MarkAsUpdated();

            await _userRepository.UpdateAsync(user, cancellationToken);

            // 5. Delete token (single-use)
            await _cache.RemoveAsync(tokenKey, cancellationToken);

            // 6. Send confirmation email
            // TODO: Publish event to RabbitMQ for NotificationService
            // var confirmEvent = new PasswordSetForOAuthUserEvent
            // {
            //     UserId = user.Id,
            //     Email = user.Email,
            //     Provider = tokenData.Provider,
            //     IpAddress = request.IpAddress,
            //     Timestamp = DateTime.UtcNow
            // };
            // await _eventPublisher.PublishAsync(confirmEvent, cancellationToken);

            _logger.LogInformation(
                "Password set successfully for OAuth user. UserId: {UserId}, Email: {Email}, Provider: {Provider}, IP: {IpAddress}",
                user.Id, user.Email, tokenData.Provider, request.IpAddress);

            return new SetPasswordForOAuthUserResponse(
                Success: true,
                Message: "Password configured successfully! You can now unlink your OAuth provider if desired.",
                Email: user.Email,
                CanNowUnlinkProvider: true
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
            _logger.LogError(ex, "Error setting password for OAuth user");
            throw new ApplicationException("An error occurred while setting your password. Please try again.", ex);
        }
    }
}
