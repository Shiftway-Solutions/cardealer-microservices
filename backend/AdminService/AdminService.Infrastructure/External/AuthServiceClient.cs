using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AdminService.Application.Interfaces;

namespace AdminService.Infrastructure.External;

/// <summary>
/// HTTP client for communicating with AuthService
/// Handles admin user creation and security operations
/// </summary>
public class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthServiceClient> _logger;
    private readonly string _authServiceUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuthServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AuthServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // In Docker, use service name; otherwise use localhost
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        _authServiceUrl = configuration["ServiceUrls:AuthService"]
            ?? configuration["Services:AuthService"]
            ?? (isDevelopment ? "http://localhost:15001" : "http://authservice:8080");

        _httpClient.BaseAddress = new Uri(_authServiceUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<CreateAdminUserResult> CreateAdminUserAsync(CreateAdminUserRequest request)
    {
        try
        {
            _logger.LogInformation("Creating admin user {Email} with role {Role}", request.Email, request.Role);

            var response = await _httpClient.PostAsJsonAsync("/api/auth/admin/register", new
            {
                email = request.Email,
                password = request.Password,
                firstName = request.FirstName,
                lastName = request.LastName,
                phoneNumber = request.PhoneNumber,
                role = request.Role
            }, JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CreateAdminUserResponse>(JsonOptions);
                return new CreateAdminUserResult
                {
                    Success = true,
                    UserId = result?.UserId ?? Guid.Empty,
                    AccessToken = result?.AccessToken
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to create admin user {Email}: {StatusCode} - {Error}",
                request.Email, response.StatusCode, errorContent);

            return new CreateAdminUserResult
            {
                Success = false,
                ErrorMessage = $"Error al crear usuario: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin user {Email}", request.Email);
            return new CreateAdminUserResult
            {
                Success = false,
                ErrorMessage = "Error de comunicación con el servicio de autenticación"
            };
        }
    }

    public async Task<AuthSecurityStatus> GetSecurityStatusAsync()
    {
        try
        {
            _logger.LogInformation("Getting security status from AuthService");

            var response = await _httpClient.GetAsync("/api/auth/admin/security-status");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthSecurityStatus>(JsonOptions);
                return result ?? new AuthSecurityStatus();
            }

            _logger.LogWarning("Failed to get security status: {StatusCode}", response.StatusCode);

            // Return default values if call fails
            return new AuthSecurityStatus
            {
                DefaultAdminExists = true, // Assume exists if we can't check
                RealSuperAdminCount = 0,
                TotalAdminCount = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security status from AuthService");
            return new AuthSecurityStatus
            {
                DefaultAdminExists = true,
                RealSuperAdminCount = 0,
                TotalAdminCount = 0
            };
        }
    }

    public async Task<DeleteAdminResult> DeleteDefaultAdminAsync(Guid requestedBy)
    {
        try
        {
            _logger.LogWarning("Requesting deletion of default admin account by {RequestedBy}", requestedBy);

            var response = await _httpClient.DeleteAsync($"/api/auth/admin/default-admin?requestedBy={requestedBy}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Default admin account deleted successfully");
                return new DeleteAdminResult { Success = true };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to delete default admin: {StatusCode} - {Error}",
                response.StatusCode, errorContent);

            return new DeleteAdminResult
            {
                Success = false,
                ErrorMessage = $"Error al eliminar admin: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting default admin");
            return new DeleteAdminResult
            {
                Success = false,
                ErrorMessage = "Error de comunicación con el servicio de autenticación"
            };
        }
    }

    private class CreateAdminUserResponse
    {
        public Guid UserId { get; set; }
        public string? AccessToken { get; set; }
    }
}
