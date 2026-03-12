using AuthService.Application.Features.Auth.Commands.RequestPasswordSetup;
using AuthService.Shared.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.Commands.ValidatePasswordSetupToken;

/// <summary>
/// Handler for ValidatePasswordSetupTokenCommand (AUTH-PWD-001)
/// 
/// Validates the token from the password setup email link without consuming it.
/// The token remains valid until used to set the password or until it expires.
/// </summary>
public class ValidatePasswordSetupTokenCommandHandler : IRequestHandler<ValidatePasswordSetupTokenCommand, ValidatePasswordSetupTokenResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<ValidatePasswordSetupTokenCommandHandler> _logger;

    private const string TOKEN_PREFIX = "password_setup:token:";
    private static readonly TimeSpan TOKEN_TTL = TimeSpan.FromHours(1);

    public ValidatePasswordSetupTokenCommandHandler(
        IDistributedCache cache,
        ILogger<ValidatePasswordSetupTokenCommandHandler> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<ValidatePasswordSetupTokenResponse> Handle(ValidatePasswordSetupTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return new ValidatePasswordSetupTokenResponse(
                    IsValid: false,
                    Message: "Token is required."
                );
            }

            var tokenKey = $"{TOKEN_PREFIX}{request.Token}";
            var tokenDataJson = await _cache.GetStringAsync(tokenKey, cancellationToken);

            if (string.IsNullOrEmpty(tokenDataJson))
            {
                _logger.LogWarning("Invalid or expired password setup token attempted");

                return new ValidatePasswordSetupTokenResponse(
                    IsValid: false,
                    Message: "This link has expired or is invalid. Please request a new password setup email."
                );
            }

            var tokenData = System.Text.Json.JsonSerializer.Deserialize<PasswordSetupTokenData>(tokenDataJson);
            if (tokenData == null)
            {
                return new ValidatePasswordSetupTokenResponse(
                    IsValid: false,
                    Message: "Invalid token data. Please request a new password setup email."
                );
            }

            // Calculate remaining time
            var expiresAt = tokenData.CreatedAt.Add(TOKEN_TTL);
            if (DateTime.UtcNow > expiresAt)
            {
                // Clean up expired token
                await _cache.RemoveAsync(tokenKey, cancellationToken);

                return new ValidatePasswordSetupTokenResponse(
                    IsValid: false,
                    Message: "This link has expired. Please request a new password setup email."
                );
            }

            _logger.LogInformation(
                "Password setup token validated. UserId: {UserId}, Email: {Email}",
                tokenData.UserId, tokenData.Email);

            return new ValidatePasswordSetupTokenResponse(
                IsValid: true,
                Message: "Token is valid. You can now set your password.",
                Email: tokenData.Email,
                Provider: tokenData.Provider,
                ExpiresAt: expiresAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password setup token");
            throw new ApplicationException("An error occurred while validating the token. Please try again.", ex);
        }
    }
}
