using AuthService.Application.DTOs.ExternalAuth;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Services;
using AuthService.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AuthService.Application.Common.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace AuthService.Application.Features.ExternalAuth.Commands.ExternalAuthCallback;

public class ExternalAuthCallbackCommandHandler : IRequestHandler<ExternalAuthCallbackCommand, ExternalAuthResponse>
{
    private readonly IExternalAuthService _externalAuthService;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly ILogger<ExternalAuthCallbackCommandHandler> _logger;
    private readonly IRequestContext _requestContext;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalAuthCallbackCommandHandler(
        IExternalAuthService externalAuthService,
        IJwtGenerator jwtGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IUserSessionRepository sessionRepository,
        ILogger<ExternalAuthCallbackCommandHandler> logger,
        IRequestContext requestContext,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _externalAuthService = externalAuthService;
        _jwtGenerator = jwtGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _sessionRepository = sessionRepository;
        _logger = logger;
        _requestContext = requestContext;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ExternalAuthResponse> Handle(ExternalAuthCallbackCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate provider
            if (!Enum.TryParse<ExternalAuthProvider>(request.Provider, true, out var provider))
                throw new ArgumentException($"Unsupported provider: {request.Provider}");

            string idToken;

            // If we have a code, we need to exchange it for an ID token
            if (!string.IsNullOrEmpty(request.Code))
            {
                idToken = await ExchangeCodeForIdToken(provider, request.Code, request.RedirectUri);
            }
            else if (!string.IsNullOrEmpty(request.IdToken))
            {
                idToken = request.IdToken;
            }
            else
            {
                throw new ArgumentException("Either Code or IdToken must be provided");
            }

            // Authenticate with external provider
            var (user, isNewUser) = await _externalAuthService.AuthenticateAsync(provider, idToken);

            // Generate tokens
            var accessToken = _jwtGenerator.GenerateToken(user);
            var refreshTokenValue = _jwtGenerator.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(60);

            var refreshTokenEntity = new Domain.Entities.RefreshToken(
                user.Id,
                refreshTokenValue,
                DateTime.UtcNow.AddDays(7),
                _requestContext.IpAddress
            );

            await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

            // Create user session for session management
            var userSession = new UserSession(
                userId: user.Id,
                refreshTokenId: refreshTokenEntity.Id.ToString(),
                deviceInfo: ParseDeviceInfo(_requestContext.UserAgent),
                browser: ParseBrowser(_requestContext.UserAgent),
                operatingSystem: ParseOperatingSystem(_requestContext.UserAgent),
                ipAddress: _requestContext.IpAddress,
                expiresAt: DateTime.UtcNow.AddDays(7)
            );
            await _sessionRepository.AddAsync(userSession, cancellationToken);
            _logger.LogInformation("Created session {SessionId} for user {UserId} via {Provider}",
                userSession.Id, user.Id, request.Provider);

            _logger.LogInformation("External auth callback processed successfully for user {UserId} from IP {IpAddress}",
                user.Id, _requestContext.IpAddress);

            return new ExternalAuthResponse(
                user.Id,
                user.UserName!,
                user.Email!,
                accessToken,
                refreshTokenValue,
                expiresAt,
                isNewUser,
                user.FirstName,
                user.LastName,
                user.ProfilePictureUrl
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing external auth callback for provider {Provider}",
                request.Provider);
            throw;
        }
    }

    private async Task<string> ExchangeCodeForIdToken(ExternalAuthProvider provider, string code, string? redirectUri)
    {
        _logger.LogInformation("Exchanging code for ID token for provider {Provider}", provider);

        return provider switch
        {
            ExternalAuthProvider.Google => await ExchangeGoogleCode(code, redirectUri),
            ExternalAuthProvider.Microsoft => await ExchangeMicrosoftCode(code, redirectUri),
            ExternalAuthProvider.Facebook => await ExchangeFacebookCode(code, redirectUri),
            ExternalAuthProvider.Apple => await ExchangeAppleCode(code, redirectUri),
            _ => throw new NotImplementedException($"OAuth code exchange for provider {provider} is not implemented.")
        };
    }

    private async Task<string> ExchangeGoogleCode(string code, string? redirectUri)
    {
        var clientId = _configuration["Authentication:Google:ClientId"];
        var clientSecret = _configuration["Authentication:Google:ClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            throw new InvalidOperationException("Google OAuth credentials are not configured");

        var client = _httpClientFactory.CreateClient();
        var tokenEndpoint = "https://oauth2.googleapis.com/token";

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri ?? "",
            ["grant_type"] = "authorization_code"
        });

        var response = await client.PostAsync(tokenEndpoint, content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Google token exchange failed: {Response}", json);
            throw new InvalidOperationException($"Google OAuth token exchange failed with status {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(json);
        var idToken = doc.RootElement.GetProperty("id_token").GetString();

        if (string.IsNullOrEmpty(idToken))
            throw new Exception("Google did not return an id_token");

        _logger.LogInformation("Successfully exchanged Google code for ID token");
        return idToken;
    }

    private async Task<string> ExchangeMicrosoftCode(string code, string? redirectUri)
    {
        var clientId = _configuration["Authentication:Microsoft:ClientId"];
        var clientSecret = _configuration["Authentication:Microsoft:ClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            throw new InvalidOperationException("Microsoft OAuth credentials are not configured");

        var client = _httpClientFactory.CreateClient();
        var tokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri ?? "",
            ["grant_type"] = "authorization_code",
            ["scope"] = "openid email profile"
        });

        var response = await client.PostAsync(tokenEndpoint, content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Microsoft token exchange failed: {Response}", json);
            throw new InvalidOperationException($"Microsoft OAuth token exchange failed with status {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(json);
        var idToken = doc.RootElement.GetProperty("id_token").GetString();

        if (string.IsNullOrEmpty(idToken))
            throw new Exception("Microsoft did not return an id_token");

        _logger.LogInformation("Successfully exchanged Microsoft code for ID token");
        return idToken;
    }

    private async Task<string> ExchangeFacebookCode(string code, string? redirectUri)
    {
        var clientId = _configuration["Authentication:Facebook:AppId"];
        var clientSecret = _configuration["Authentication:Facebook:AppSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            throw new InvalidOperationException("Facebook OAuth credentials are not configured");

        var client = _httpClientFactory.CreateClient();

        // Facebook returns access_token, not id_token, so we need to get user info
        var tokenEndpoint = $"https://graph.facebook.com/v18.0/oauth/access_token?" +
            $"client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri ?? "")}" +
            $"&client_secret={clientSecret}&code={code}";

        var response = await client.GetAsync(tokenEndpoint);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Facebook token exchange failed: {Response}", json);
            throw new InvalidOperationException($"Facebook OAuth token exchange failed with status {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(json);
        var accessToken = doc.RootElement.GetProperty("access_token").GetString();

        if (string.IsNullOrEmpty(accessToken))
            throw new Exception("Facebook did not return an access_token");

        _logger.LogInformation("Successfully exchanged Facebook code for access token");
        // For Facebook, we return the access_token which will be used to get user info
        return accessToken;
    }

    private async Task<string> ExchangeAppleCode(string code, string? redirectUri)
    {
        var clientId = _configuration["Authentication:Apple:ClientId"];
        var clientSecret = _configuration["Authentication:Apple:ClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            throw new InvalidOperationException("Apple OAuth credentials are not configured");

        var client = _httpClientFactory.CreateClient();
        var tokenEndpoint = "https://appleid.apple.com/auth/token";

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri ?? "",
            ["grant_type"] = "authorization_code"
        });

        var response = await client.PostAsync(tokenEndpoint, content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Apple token exchange failed: {Response}", json);
            throw new InvalidOperationException($"Apple OAuth token exchange failed with status {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(json);
        var idToken = doc.RootElement.GetProperty("id_token").GetString();

        if (string.IsNullOrEmpty(idToken))
            throw new Exception("Apple did not return an id_token");

        _logger.LogInformation("Successfully exchanged Apple code for ID token");
        return idToken;
    }

    private static string ParseDeviceInfo(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown Device";

        if (userAgent.Contains("Mobile")) return "Mobile Device";
        if (userAgent.Contains("Tablet")) return "Tablet";
        return "Desktop";
    }

    private static string ParseBrowser(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";

        if (userAgent.Contains("Chrome") && !userAgent.Contains("Edg")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox";
        if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome")) return "Safari";
        if (userAgent.Contains("Edg")) return "Edge";
        if (userAgent.Contains("Opera") || userAgent.Contains("OPR")) return "Opera";
        return "Unknown";
    }

    private static string ParseOperatingSystem(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";

        if (userAgent.Contains("Windows NT 10")) return "Windows 10/11";
        if (userAgent.Contains("Windows")) return "Windows";
        if (userAgent.Contains("Mac OS X")) return "macOS";
        if (userAgent.Contains("Linux")) return "Linux";
        if (userAgent.Contains("Android")) return "Android";
        if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) return "iOS";
        return "Unknown";
    }
}
