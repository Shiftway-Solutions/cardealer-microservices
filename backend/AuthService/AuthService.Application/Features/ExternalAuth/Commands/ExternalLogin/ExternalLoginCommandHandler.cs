using AuthService.Application.DTOs.ExternalAuth;
using AuthService.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.ExternalAuth.Commands.ExternalLogin;

public class ExternalLoginCommandHandler : IRequestHandler<ExternalLoginCommand, ExternalLoginResponse>
{
    private readonly IExternalAuthService _externalAuthService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalLoginCommandHandler> _logger;

    public ExternalLoginCommandHandler(
        IExternalAuthService externalAuthService,
        IConfiguration configuration,
        ILogger<ExternalLoginCommandHandler> logger)
    {
        _externalAuthService = externalAuthService;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<ExternalLoginResponse> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var authorizationUrl = GenerateAuthorizationUrl(request.Provider, request.RedirectUri);
            var response = new ExternalLoginResponse(authorizationUrl);

            _logger.LogInformation("Generated authorization URL for provider {Provider}", request.Provider);

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating authorization URL for provider {Provider}", request.Provider);
            throw;
        }
    }

    private string GenerateAuthorizationUrl(string provider, string redirectUri)
    {
        var encodedRedirectUri = Uri.EscapeDataString(redirectUri);

        return provider.ToLower() switch
        {
            "google" => GenerateGoogleAuthUrl(encodedRedirectUri),
            "microsoft" => GenerateMicrosoftAuthUrl(encodedRedirectUri),
            "facebook" => GenerateFacebookAuthUrl(encodedRedirectUri),
            "apple" => GenerateAppleAuthUrl(encodedRedirectUri),
            _ => throw new ArgumentException($"Unsupported provider: {provider}")
        };
    }

    private string GenerateGoogleAuthUrl(string redirectUri)
    {
        var clientId = _configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Google Client ID is not configured");

        return $"https://accounts.google.com/o/oauth2/v2/auth?" +
               $"client_id={clientId}&" +
               $"redirect_uri={redirectUri}&" +
               "response_type=code&" +
               "scope=openid%20email%20profile&" +
               "access_type=offline&" +
               "prompt=consent";
    }

    private string GenerateMicrosoftAuthUrl(string redirectUri)
    {
        var clientId = _configuration["Authentication:Microsoft:ClientId"]
            ?? throw new InvalidOperationException("Microsoft Client ID is not configured");

        return $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?" +
               $"client_id={clientId}&" +
               $"redirect_uri={redirectUri}&" +
               "response_type=code&" +
               "scope=openid%20email%20profile";
    }

    private string GenerateFacebookAuthUrl(string redirectUri)
    {
        var clientId = _configuration["Authentication:Facebook:AppId"]
            ?? throw new InvalidOperationException("Facebook App ID is not configured");

        return $"https://www.facebook.com/v18.0/dialog/oauth?" +
               $"client_id={clientId}&" +
               $"redirect_uri={redirectUri}&" +
               "response_type=code&" +
               "scope=email,public_profile";
    }

    private string GenerateAppleAuthUrl(string redirectUri)
    {
        var clientId = _configuration["Authentication:Apple:ClientId"]
            ?? throw new InvalidOperationException("Apple Client ID is not configured");

        return $"https://appleid.apple.com/auth/authorize?" +
               $"client_id={clientId}&" +
               $"redirect_uri={redirectUri}&" +
               "response_type=code%20id_token&" +
               "scope=name%20email&" +
               "response_mode=form_post";
    }
}
