using AuthService.Application.DTOs.ExternalAuth;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Shared.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using AuthService.Application.Common.Interfaces;

namespace AuthService.Application.Features.ExternalAuth.Commands.LinkExternalAccount;

/// <summary>
/// Handler for LinkExternalAccountCommand (AUTH-EXT-005)
/// 
/// Allows authenticated users to link an external OAuth provider to their existing account.
/// This enables login via both password and external provider.
/// 
/// Security considerations:
/// - User must already be authenticated
/// - User cannot have another external account already linked (one at a time)
/// - Email from external provider should match or be verified
/// </summary>
public class LinkExternalAccountCommandHandler : IRequestHandler<LinkExternalAccountCommand, ExternalAuthResponse>
{
    private readonly IExternalAuthService _externalAuthService;
    private readonly IUserRepository _userRepository;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<LinkExternalAccountCommandHandler> _logger;
    private readonly IRequestContext _requestContext;

    public LinkExternalAccountCommandHandler(
        IExternalAuthService externalAuthService,
        IUserRepository userRepository,
        IJwtGenerator jwtGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<LinkExternalAccountCommandHandler> logger,
        IRequestContext requestContext)
    {
        _externalAuthService = externalAuthService;
        _userRepository = userRepository;
        _jwtGenerator = jwtGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
        _requestContext = requestContext;
    }

    public async Task<ExternalAuthResponse> Handle(LinkExternalAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate provider enum
            if (!Enum.TryParse<ExternalAuthProvider>(request.Provider, true, out var provider))
                throw new BadRequestException($"Unsupported provider: {request.Provider}. Supported: Google, Microsoft, Facebook, Apple");

            // Get user
            var user = await _userRepository.GetByIdAsync(request.UserId)
                ?? throw new NotFoundException("User not found.");

            // Check if user already has an external account linked
            if (user.IsExternalUser)
            {
                throw new BadRequestException(
                    $"You already have a {user.ExternalAuthProvider} account linked. " +
                    "Please unlink it first before linking a new provider.");
            }

            // Authenticate with external provider to verify the token and get user info
            var (externalUser, _) = await _externalAuthService.AuthenticateAsync(provider, request.IdToken);

            // Security: Verify email matches (optional but recommended)
            if (!string.Equals(externalUser.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Email mismatch during account linking. User email: {UserEmail}, External email: {ExternalEmail}",
                    user.Email, externalUser.Email);

                // We allow linking even with different email, but log it for security
                // In stricter environments, you might want to throw an exception here
            }

            // Link the external account to existing user
            user.LinkExternalAccount(provider, externalUser.ExternalUserId!);

            // Update user
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Generate new tokens (to include updated claims)
            var accessToken = _jwtGenerator.GenerateToken(user);
            var refreshTokenValue = _jwtGenerator.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(60);

            var refreshTokenEntity = new RefreshToken(
                user.Id,
                refreshTokenValue,
                DateTime.UtcNow.AddDays(7),
                _requestContext.IpAddress
            );

            await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

            _logger.LogInformation(
                "External account linked successfully. UserId: {UserId}, Provider: {Provider}, ExternalUserId: {ExternalUserId}",
                request.UserId, request.Provider, externalUser.ExternalUserId);

            // TODO: Publish domain event for audit (ExternalAccountLinkedEvent)
            // This should trigger:
            // 1. Confirmation email to user
            // 2. Audit log entry
            // 3. Analytics tracking

            return new ExternalAuthResponse(
                user.Id,
                user.UserName!,
                user.Email!,
                accessToken,
                refreshTokenValue,
                expiresAt,
                false, // isNewUser is false since we're linking to existing account
                user.FirstName,
                user.LastName,
                user.ProfilePictureUrl
            );
        }
        catch (BadRequestException)
        {
            throw; // Re-throw validation errors as-is
        }
        catch (NotFoundException)
        {
            throw; // Re-throw not found errors as-is
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking external account for user {UserId}", request.UserId);
            throw new ApplicationException("An error occurred while linking the external account. Please try again.", ex);
        }
    }
}
