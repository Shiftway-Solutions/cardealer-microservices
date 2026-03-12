using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Services;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using AuthService.Shared.Exceptions;
using System.Text.Json;

namespace AuthService.Infrastructure.Services.ExternalAuth;

public class ExternalAuthService : IExternalAuthService
{
    private readonly IExternalTokenValidator _tokenValidator;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ExternalAuthService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalAuthService(
        IExternalTokenValidator tokenValidator,
        IUserRepository userRepository,
        ILogger<ExternalAuthService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _tokenValidator = tokenValidator;
        _userRepository = userRepository;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(ApplicationUser user, bool isNewUser)> AuthenticateAsync(ExternalAuthProvider provider, string idToken)
    {
        try
        {
            // Validar token
            var validationResult = await ValidateTokenAndGetUserInfo(provider, idToken);
            if (!validationResult.IsValid)
                throw new UnauthorizedException("Invalid external token");

            // Buscar usuario existente por external ID
            var existingUser = await FindUserByExternalIdAsync(provider, validationResult.UserId);
            if (existingUser != null)
            {
                // Update profile info if missing
                await UpdateUserProfileIfNeededAsync(existingUser, validationResult);

                _logger.LogInformation("External user found for {Email} with provider {Provider}",
                    validationResult.Email, provider);
                return (existingUser, false);
            }

            // Buscar usuario por email
            existingUser = await _userRepository.GetByEmailAsync(validationResult.Email);
            if (existingUser != null)
            {
                // Vincular cuenta externa a usuario existente
                if (!existingUser.IsExternalUser)
                {
                    existingUser.LinkExternalAccount(provider, validationResult.UserId);
                    await UpdateUserProfileIfNeededAsync(existingUser, validationResult);
                    await _userRepository.UpdateAsync(existingUser);
                    _logger.LogInformation("Linked external account to existing user {Email}", validationResult.Email);
                }
                return (existingUser, false);
            }

            // Crear nuevo usuario con toda la información del perfil
            var userName = GenerateUserName(validationResult.Name, validationResult.Email);
            var newUser = ApplicationUser.CreateExternalUser(
                userName,
                validationResult.Email,
                provider,
                validationResult.UserId,
                validationResult.FirstName,
                validationResult.LastName,
                validationResult.ProfilePictureUrl
            );

            await _userRepository.AddAsync(newUser);
            _logger.LogInformation("Created new external user for {Email} with provider {Provider} (FirstName: {FirstName}, LastName: {LastName})",
                validationResult.Email, provider, validationResult.FirstName, validationResult.LastName);

            return (newUser, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during external authentication with provider {Provider}", provider);
            throw;
        }
    }

    private async Task UpdateUserProfileIfNeededAsync(ApplicationUser user, ExternalTokenValidationResult validationResult)
    {
        bool updated = false;

        if (string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(validationResult.FirstName))
        {
            user.FirstName = validationResult.FirstName;
            updated = true;
        }

        if (string.IsNullOrEmpty(user.LastName) && !string.IsNullOrEmpty(validationResult.LastName))
        {
            user.LastName = validationResult.LastName;
            updated = true;
        }

        if (string.IsNullOrEmpty(user.ProfilePictureUrl) && !string.IsNullOrEmpty(validationResult.ProfilePictureUrl))
        {
            user.ProfilePictureUrl = validationResult.ProfilePictureUrl;
            updated = true;
        }

        if (updated)
        {
            user.MarkAsUpdated();
            await _userRepository.UpdateAsync(user);
        }
    }

    public async Task<ApplicationUser?> FindUserByExternalIdAsync(ExternalAuthProvider provider, string externalUserId)
    {
        return await _userRepository.GetByExternalIdAsync(provider, externalUserId);
    }

    public async Task<bool> ValidateTokenAsync(ExternalAuthProvider provider, string idToken)
    {
        var result = await ValidateTokenAndGetUserInfo(provider, idToken);
        return result.IsValid;
    }

    public async Task<string> GetUserInfoAsync(ExternalAuthProvider provider, string accessToken)
    {
        try
        {
            _logger.LogInformation("Getting user info for provider: {Provider}", provider);

            var userInfo = provider switch
            {
                ExternalAuthProvider.Google => await GetGoogleUserInfoAsync(accessToken),
                ExternalAuthProvider.Microsoft => await GetMicrosoftUserInfoAsync(accessToken),
                ExternalAuthProvider.Facebook => await GetFacebookUserInfoAsync(accessToken),
                ExternalAuthProvider.Apple => await GetAppleUserInfoAsync(accessToken),
                _ => throw new ArgumentOutOfRangeException(nameof(provider), $"Unsupported provider: {provider}")
            };

            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info for provider {Provider}", provider);
            throw new InvalidOperationException($"Failed to get user info from {provider}", ex);
        }
    }

    public async Task<ExternalUserInfo> GetUserInfoTypedAsync(ExternalAuthProvider provider, string accessToken)
    {
        var userInfoJson = await GetUserInfoAsync(provider, accessToken);

        return provider switch
        {
            ExternalAuthProvider.Google => ParseGoogleUserInfo(userInfoJson),
            ExternalAuthProvider.Microsoft => ParseMicrosoftUserInfo(userInfoJson),
            ExternalAuthProvider.Facebook => ParseFacebookUserInfo(userInfoJson),
            ExternalAuthProvider.Apple => ParseAppleUserInfo(userInfoJson),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), $"Unsupported provider: {provider}")
        };
    }

    private async Task<ExternalTokenValidationResult> ValidateTokenAndGetUserInfo(
        ExternalAuthProvider provider, string idToken)
    {
        return provider switch
        {
            ExternalAuthProvider.Google => await _tokenValidator.ValidateGoogleTokenAsync(idToken),
            ExternalAuthProvider.Microsoft => await _tokenValidator.ValidateMicrosoftTokenAsync(idToken),
            ExternalAuthProvider.Facebook => await _tokenValidator.ValidateFacebookTokenAsync(idToken),
            ExternalAuthProvider.Apple => await _tokenValidator.ValidateAppleTokenAsync(idToken),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), $"Unsupported provider: {provider}")
        };
    }

    private async Task<string> GetGoogleUserInfoAsync(string accessToken)
    {
        using var httpClient = _httpClientFactory.CreateClient("ExternalAuth");
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Google API returned {response.StatusCode}: {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync();

        // Parse and enrich the response
        var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(content);
        var enrichedInfo = new
        {
            userInfo?.sub,
            userInfo?.name,
            userInfo?.given_name,
            userInfo?.family_name,
            userInfo?.picture,
            userInfo?.email,
            userInfo?.email_verified,
            userInfo?.locale,
            Provider = "Google",
            RetrievedAt = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(enrichedInfo, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> GetMicrosoftUserInfoAsync(string accessToken)
    {
        using var httpClient = _httpClientFactory.CreateClient("ExternalAuth");
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Microsoft Graph API returned {response.StatusCode}: {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync();

        // Parse and enrich the response
        var userInfo = JsonSerializer.Deserialize<MicrosoftUserInfo>(content);
        var enrichedInfo = new
        {
            userInfo?.id,
            userInfo?.displayName,
            userInfo?.givenName,
            userInfo?.surname,
            userInfo?.mail,
            userInfo?.userPrincipalName,
            userInfo?.mobilePhone,
            userInfo?.jobTitle,
            userInfo?.officeLocation,
            Provider = "Microsoft",
            RetrievedAt = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(enrichedInfo, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> GetFacebookUserInfoAsync(string accessToken)
    {
        using var httpClient = _httpClientFactory.CreateClient("ExternalAuth");

        var url = $"https://graph.facebook.com/me?fields=id,name,email,first_name,last_name,picture.type(large)&access_token={accessToken}";
        var response = await httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Facebook Graph API returned {response.StatusCode}: {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync();

        // Parse and enrich the response
        var userInfo = JsonSerializer.Deserialize<FacebookUserInfo>(content);
        var enrichedInfo = new
        {
            userInfo?.id,
            userInfo?.name,
            userInfo?.email,
            userInfo?.first_name,
            userInfo?.last_name,
            picture = userInfo?.picture?.data?.url,
            Provider = "Facebook",
            RetrievedAt = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(enrichedInfo, new JsonSerializerOptions { WriteIndented = true });
    }

    private Task<string> GetAppleUserInfoAsync(string idToken)
    {
        // Apple doesn't have a userinfo endpoint like Google/Microsoft/Facebook
        // User info is embedded in the ID token itself
        // Parse the JWT to extract claims
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(idToken);

            var userInfo = new
            {
                sub = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
                email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty,
                email_verified = jwtToken.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value ?? "false",
                is_private_email = jwtToken.Claims.FirstOrDefault(c => c.Type == "is_private_email")?.Value ?? "false",
                Provider = "Apple",
                RetrievedAt = DateTime.UtcNow
            };

            return Task.FromResult(JsonSerializer.Serialize(userInfo, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Apple ID token");
            throw new HttpRequestException($"Failed to parse Apple ID token: {ex.Message}");
        }
    }

    private ExternalUserInfo ParseGoogleUserInfo(string userInfoJson)
    {
        var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(userInfoJson);

        if (userInfo == null)
            throw new InvalidOperationException("Failed to parse Google user info");

        var additionalData = new Dictionary<string, object>
        {
            ["email_verified"] = userInfo.email_verified,
            ["locale"] = userInfo.locale,
            ["picture"] = userInfo.picture
        };

        return new ExternalUserInfo(
            UserId: userInfo.sub,
            Email: userInfo.email,
            Name: userInfo.name,
            FirstName: userInfo.given_name,
            LastName: userInfo.family_name,
            PictureUrl: userInfo.picture,
            Provider: "Google",
            AdditionalData: additionalData
        );
    }

    private ExternalUserInfo ParseMicrosoftUserInfo(string userInfoJson)
    {
        var userInfo = JsonSerializer.Deserialize<MicrosoftUserInfo>(userInfoJson);

        if (userInfo == null)
            throw new InvalidOperationException("Failed to parse Microsoft user info");

        var additionalData = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(userInfo.mobilePhone))
            additionalData["mobile_phone"] = userInfo.mobilePhone;
        if (!string.IsNullOrEmpty(userInfo.jobTitle))
            additionalData["job_title"] = userInfo.jobTitle;
        if (!string.IsNullOrEmpty(userInfo.officeLocation))
            additionalData["office_location"] = userInfo.officeLocation;

        return new ExternalUserInfo(
            UserId: userInfo.id,
            Email: userInfo.mail ?? userInfo.userPrincipalName,
            Name: userInfo.displayName,
            FirstName: userInfo.givenName,
            LastName: userInfo.surname,
            PictureUrl: null,
            Provider: "Microsoft",
            AdditionalData: additionalData
        );
    }

    private ExternalUserInfo ParseFacebookUserInfo(string userInfoJson)
    {
        var userInfo = JsonSerializer.Deserialize<FacebookUserInfo>(userInfoJson);

        if (userInfo == null)
            throw new InvalidOperationException("Failed to parse Facebook user info");

        var additionalData = new Dictionary<string, object>();

        if (userInfo.picture?.data?.url != null)
            additionalData["picture_url"] = userInfo.picture.data.url;

        return new ExternalUserInfo(
            UserId: userInfo.id,
            Email: userInfo.email ?? $"{userInfo.id}@facebook.com",
            Name: userInfo.name,
            FirstName: userInfo.first_name,
            LastName: userInfo.last_name,
            PictureUrl: userInfo.picture?.data?.url,
            Provider: "Facebook",
            AdditionalData: additionalData
        );
    }

    private ExternalUserInfo ParseAppleUserInfo(string userInfoJson)
    {
        var userInfo = JsonSerializer.Deserialize<AppleUserInfo>(userInfoJson);

        if (userInfo == null)
            throw new InvalidOperationException("Failed to parse Apple user info");

        var additionalData = new Dictionary<string, object>
        {
            ["email_verified"] = userInfo.email_verified,
            ["is_private_email"] = userInfo.is_private_email
        };

        return new ExternalUserInfo(
            UserId: userInfo.sub,
            Email: userInfo.email,
            Name: string.Empty, // Apple doesn't always provide name in token
            FirstName: string.Empty,
            LastName: string.Empty,
            PictureUrl: null,
            Provider: "Apple",
            AdditionalData: additionalData
        );
    }

    private string GenerateUserName(string? name, string email)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            // Limpiar el nombre para usarlo como username
            var cleanName = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '.' || c == '_').ToArray());
            if (!string.IsNullOrWhiteSpace(cleanName))
                return cleanName;
        }

        // Usar la parte del email antes del @ como username
        return email.Split('@')[0];
    }

    // Clases internas privadas para deserialización
    private class GoogleUserInfo
    {
        public string sub { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string given_name { get; set; } = string.Empty;
        public string family_name { get; set; } = string.Empty;
        public string picture { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public bool email_verified { get; set; }
        public string locale { get; set; } = string.Empty;
    }

    private class MicrosoftUserInfo
    {
        public string id { get; set; } = string.Empty;
        public string displayName { get; set; } = string.Empty;
        public string givenName { get; set; } = string.Empty;
        public string surname { get; set; } = string.Empty;
        public string mail { get; set; } = string.Empty;
        public string userPrincipalName { get; set; } = string.Empty;
        public string mobilePhone { get; set; } = string.Empty;
        public string jobTitle { get; set; } = string.Empty;
        public string officeLocation { get; set; } = string.Empty;
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
        public string url { get; set; } = string.Empty;
        public int height { get; set; }
        public int width { get; set; }
        public bool is_silhouette { get; set; }
    }

    private class AppleUserInfo
    {
        public string sub { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string email_verified { get; set; } = string.Empty;
        public string is_private_email { get; set; } = string.Empty;
    }
}
