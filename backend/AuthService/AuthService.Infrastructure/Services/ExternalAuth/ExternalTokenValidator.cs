using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace AuthService.Infrastructure.Services.ExternalAuth;

public class ExternalTokenValidator : IExternalTokenValidator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalTokenValidator> _logger;

    public ExternalTokenValidator(HttpClient httpClient, ILogger<ExternalTokenValidator> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ExternalTokenValidationResult> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");

            if (!response.IsSuccessStatusCode)
                return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);

            var content = await response.Content.ReadAsStringAsync();
            var tokenInfo = JsonSerializer.Deserialize<GoogleTokenInfo>(content);

            if (tokenInfo == null || string.IsNullOrEmpty(tokenInfo.email))
                return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);

            return new ExternalTokenValidationResult(
                IsValid: true,
                Email: tokenInfo.email,
                UserId: tokenInfo.sub,
                Name: tokenInfo.name ?? string.Empty,
                FirstName: tokenInfo.given_name,
                LastName: tokenInfo.family_name,
                ProfilePictureUrl: tokenInfo.picture
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);
        }
    }

    public async Task<ExternalTokenValidationResult> ValidateMicrosoftTokenAsync(string idToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {idToken}");

            var response = await _httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");

            if (!response.IsSuccessStatusCode)
                return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);

            var content = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<MicrosoftUserInfo>(content);

            if (userInfo == null || (string.IsNullOrEmpty(userInfo.mail) && string.IsNullOrEmpty(userInfo.userPrincipalName)))
                return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);

            var email = userInfo.mail ?? userInfo.userPrincipalName;
            return new ExternalTokenValidationResult(
                IsValid: true,
                Email: email,
                UserId: userInfo.id,
                Name: userInfo.displayName ?? string.Empty,
                FirstName: userInfo.givenName,
                LastName: userInfo.surname
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Microsoft token");
            return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);
        }
    }

    public async Task<ExternalTokenValidationResult> ValidateFacebookTokenAsync(string accessToken)
    {
        try
        {
            // Facebook Graph API to get user info with picture
            var url = $"https://graph.facebook.com/me?fields=id,name,email,first_name,last_name,picture.type(large)&access_token={accessToken}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Facebook token validation failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);
            }

            var content = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<FacebookUserInfo>(content);

            if (userInfo == null || string.IsNullOrEmpty(userInfo.id))
            {
                _logger.LogWarning("Facebook returned invalid user info");
                return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);
            }

            // Email puede ser nulo si el usuario no tiene email o no dio permiso
            var email = userInfo.email ?? $"{userInfo.id}@facebook.com";
            var name = userInfo.name ?? $"{userInfo.first_name} {userInfo.last_name}".Trim();

            _logger.LogInformation("Facebook token validated successfully for user {UserId}", userInfo.id);
            return new ExternalTokenValidationResult(
                IsValid: true,
                Email: email,
                UserId: userInfo.id,
                Name: name,
                FirstName: userInfo.first_name,
                LastName: userInfo.last_name,
                ProfilePictureUrl: userInfo.picture?.data?.url
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Facebook token");
            return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);
        }
    }

    public async Task<ExternalTokenValidationResult> ValidateAppleTokenAsync(string idToken)
    {
        try
        {
            // Apple uses JWT ID tokens that we need to validate
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(idToken))
            {
                _logger.LogWarning("Invalid Apple ID token format");
                return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);
            }

            // Get Apple's public keys for validation
            var appleKeysResponse = await _httpClient.GetAsync("https://appleid.apple.com/auth/keys");
            if (!appleKeysResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch Apple public keys");
                return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);
            }

            var keysJson = await appleKeysResponse.Content.ReadAsStringAsync();
            var appleKeys = JsonSerializer.Deserialize<AppleKeySet>(keysJson);

            // Read the token without validation first to get the key ID
            var jwtToken = handler.ReadJwtToken(idToken);
            var kid = jwtToken.Header.Kid;

            // Find the matching key
            var matchingKey = appleKeys?.keys?.FirstOrDefault(k => k.kid == kid);
            if (matchingKey == null)
            {
                _logger.LogWarning("No matching Apple key found for kid: {Kid}", kid);
                return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);
            }

            // Create RSA key from Apple's public key
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = Base64UrlDecode(matchingKey.n),
                Exponent = Base64UrlDecode(matchingKey.e)
            });

            var securityKey = new RsaSecurityKey(rsa);

            // Validate the token
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://appleid.apple.com",
                ValidateAudience = false, // TODO(SECURITY): Enable audience validation once Apple ClientId is injected into this class.
                                          // Without this, ANY Apple-issued token (from any app) is accepted.
                                          // Fix: Inject IConfiguration, read "Authentication:Apple:ClientId", set ValidAudience.
                ValidateLifetime = true,
                IssuerSigningKey = securityKey
            };

            try
            {
                handler.ValidateToken(idToken, validationParameters, out _);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Apple token validation failed");
                return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);
            }

            // Extract claims from the token
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty;
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty;

            // Apple doesn't always provide name in the token, it's only in the first authentication
            // The name comes from the authorization response, not the ID token
            var name = string.Empty;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Apple token missing required 'sub' claim");
                return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);
            }

            _logger.LogInformation("Apple token validated successfully for user {UserId}", userId);
            return new ExternalTokenValidationResult(
                IsValid: true,
                Email: email,
                UserId: userId,
                Name: name
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Apple token");
            return new ExternalTokenValidationResult(false, string.Empty, string.Empty, string.Empty);
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        return Convert.FromBase64String(output);
    }

    #region Internal DTOs

    private class GoogleTokenInfo
    {
        public string email { get; set; } = string.Empty;
        public string sub { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string given_name { get; set; } = string.Empty;
        public string family_name { get; set; } = string.Empty;
        public string picture { get; set; } = string.Empty;
        public string email_verified { get; set; } = string.Empty;
    }

    private class MicrosoftUserInfo
    {
        public string id { get; set; } = string.Empty;
        public string mail { get; set; } = string.Empty;
        public string displayName { get; set; } = string.Empty;
        public string givenName { get; set; } = string.Empty;
        public string surname { get; set; } = string.Empty;
        public string userPrincipalName { get; set; } = string.Empty;
    }

    private class FacebookUserInfo
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string first_name { get; set; } = string.Empty;
        public string last_name { get; set; } = string.Empty;
        public FacebookPicture? picture { get; set; }
    }

    private class FacebookPicture
    {
        public FacebookPictureData? data { get; set; }
    }

    private class FacebookPictureData
    {
        public string? url { get; set; }
    }

    private class AppleKeySet
    {
        public List<AppleKey>? keys { get; set; }
    }

    private class AppleKey
    {
        public string kty { get; set; } = string.Empty;
        public string kid { get; set; } = string.Empty;
        public string use { get; set; } = string.Empty;
        public string alg { get; set; } = string.Empty;
        public string n { get; set; } = string.Empty;
        public string e { get; set; } = string.Empty;
    }

    #endregion
}
