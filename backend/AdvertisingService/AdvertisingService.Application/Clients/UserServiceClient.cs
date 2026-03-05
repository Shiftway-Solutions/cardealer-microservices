using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AdvertisingService.Application.Clients;

/// <summary>
/// HTTP client for resolving user information from UserService.
/// Used to get user email addresses for sending advertising reports.
/// </summary>
public class UserServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserServiceClient> _logger;

    public UserServiceClient(HttpClient httpClient, ILogger<UserServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets the email address for a user by their ID.
    /// Returns null if the user is not found or the request fails.
    /// </summary>
    public async Task<string?> GetUserEmailAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/users/{userId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get user {UserId} from UserService: {StatusCode}",
                    userId, response.StatusCode);
                return null;
            }

            var userData = await response.Content.ReadFromJsonAsync<UserInfoResponse>(cancellationToken: ct);
            return userData?.Email;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting user email for {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Gets basic user information by their ID.
    /// </summary>
    public async Task<UserInfoResponse?> GetUserInfoAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/users/{userId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get user {UserId}: {StatusCode}", userId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<UserInfoResponse>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting user info for {UserId}", userId);
            return null;
        }
    }
}

public class UserInfoResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
