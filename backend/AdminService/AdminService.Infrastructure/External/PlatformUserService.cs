using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AdminService.Application.Interfaces;
using AdminService.Application.UseCases.PlatformUsers;

namespace AdminService.Infrastructure.External;

/// <summary>
/// Client for interacting with UserService to manage platform users
/// </summary>
public class PlatformUserService : IPlatformUserService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PlatformUserService> _logger;
    private readonly string _baseUrl;

    public PlatformUserService(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PlatformUserService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        // Use configuration, then env var, then correct K8s service default
        _baseUrl = configuration["ServiceUrls:UserService"]
            ?? Environment.GetEnvironmentVariable("USERSERVICE_URL")
            ?? "http://userservice:8080";
    }

    /// <summary>
    /// Forwards the Authorization header from the incoming request to internal service calls
    /// </summary>
    private void ForwardAuthorizationHeader()
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                AuthenticationHeaderValue.Parse(authHeader);
        }
    }

    public async Task<PaginatedUserResult> GetUsersAsync(
        string? search = null,
        string? type = null,
        string? status = null,
        bool? verified = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(search)) queryParams["search"] = search;
            if (!string.IsNullOrEmpty(type)) queryParams["type"] = type;
            if (!string.IsNullOrEmpty(status)) queryParams["status"] = status;
            if (verified.HasValue) queryParams["verified"] = verified.Value.ToString().ToLower();
            queryParams["page"] = page.ToString();
            queryParams["pageSize"] = pageSize.ToString();

            var url = $"{_baseUrl}/api/users/admin/list?{queryParams}";
            _logger.LogInformation("Fetching users from UserService: {Url}", url);

            ForwardAuthorizationHeader();
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UserServiceResponse>(cancellationToken: cancellationToken);
                if (result != null)
                {
                    return new PaginatedUserResult
                    {
                        Items = result.Data?.Select(MapToDto).ToList() ?? new List<PlatformUserDto>(),
                        Total = result.TotalCount,
                        Page = result.Page,
                        PageSize = result.PageSize
                    };
                }
            }
            else
            {
                _logger.LogWarning("UserService returned {StatusCode} for get users", response.StatusCode);
            }

            // Return mock data for development/fallback
            return GetMockUsers(search, type, status, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users from UserService");
            // Return mock data for development/fallback
            return GetMockUsers(search, type, status, page, pageSize);
        }
    }

    public async Task<PlatformUserStatsDto> GetUserStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/api/users/admin/stats";
            _logger.LogInformation("Fetching user stats from UserService: {Url}", url);

            ForwardAuthorizationHeader();
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PlatformUserStatsDto>(cancellationToken: cancellationToken);
                if (result != null)
                {
                    return result;
                }
            }
            else
            {
                _logger.LogWarning("UserService returned {StatusCode} for user stats", response.StatusCode);
            }

            // Return mock stats for development/fallback
            return GetMockStats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user stats from UserService");
            return GetMockStats();
        }
    }

    public async Task<PlatformUserDetailDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/api/users/{userId}";
            _logger.LogInformation("Fetching user {UserId} from UserService", userId);

            ForwardAuthorizationHeader();
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UserServiceDetailDto>(cancellationToken: cancellationToken);
                if (result != null)
                {
                    return MapDetailDtoFromUserService(result);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                _logger.LogWarning("UserService returned {StatusCode} for user {UserId}", response.StatusCode, userId);
            }

            // Return mock user for development/fallback
            return GetMockUserDetail(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId} from UserService", userId);
            return GetMockUserDetail(userId);
        }
    }

    public async Task UpdateUserStatusAsync(Guid userId, string status, string? reason = null, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/users/admin/{userId}/status";
        _logger.LogInformation("Updating user {UserId} status to {Status}", userId, status);

        ForwardAuthorizationHeader();
        var payload = new { Status = status, Reason = reason };
        var response = await _httpClient.PatchAsJsonAsync(url, payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("UserService returned {StatusCode} when updating user {UserId} status", response.StatusCode, userId);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("Successfully updated user {UserId} status to {Status}", userId, status);
    }

    public async Task VerifyUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/users/admin/{userId}/verify";
        _logger.LogInformation("Verifying user {UserId}", userId);

        ForwardAuthorizationHeader();
        var response = await _httpClient.PostAsync(url, null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("UserService returned {StatusCode} when verifying user {UserId}", response.StatusCode, userId);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("Successfully verified user {UserId}", userId);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/users/admin/{userId}";
        _logger.LogInformation("Deleting user {UserId}", userId);

        ForwardAuthorizationHeader();
        var response = await _httpClient.DeleteAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("UserService returned {StatusCode} when deleting user {UserId}", response.StatusCode, userId);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("Successfully deleted user {UserId}", userId);
    }

    // =========================================================================
    // PRIVATE MAPPING METHODS
    // =========================================================================

    private static PlatformUserDto MapToDto(UserServiceUserDto user)
    {
        return new PlatformUserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Avatar = user.Avatar,
            Type = user.Type,             // Already mapped by UserService (buyer/seller/dealer)
            UserIntent = user.UserIntent, // browse, buy, sell, buy_and_sell
            Status = user.Status,         // Already mapped by UserService (active/pending/suspended/banned)
            Verified = user.Verified,
            EmailVerified = user.EmailVerified,
            CreatedAt = user.CreatedAt.ToString("O"),
            LastActiveAt = user.LastActiveAt?.ToString("O"),
            VehiclesCount = user.VehiclesCount,
            FavoritesCount = user.FavoritesCount,
            DealsCount = user.DealsCount
        };
    }

    private static PlatformUserDetailDto MapDetailDtoFromUserService(UserServiceDetailDto user)
    {
        // Map accountType from UserService format (e.g. "buyer", "seller", "dealer") to frontend format (buyer/seller/dealer)
        var type = user.AccountType?.ToLowerInvariant() switch
        {
            "seller" => "seller",
            "dealer" => "dealer",
            "dealeremployee" => "dealer",
            "admin" => "admin",
            "platformemployee" => "admin",
            _ => "buyer"
        };

        var status = user.IsActive ? "active" : "suspended";

        return new PlatformUserDetailDto
        {
            Id = user.Id.ToString(),
            Name = !string.IsNullOrEmpty(user.FullName) ? user.FullName : $"{user.FirstName} {user.LastName}".Trim(),
            Email = user.Email,
            Phone = user.Phone ?? user.PhoneNumber,
            Avatar = user.AvatarUrl,
            Type = type,
            Status = status,
            Verified = user.IsVerified || user.IsEmailVerified,
            EmailVerified = user.EmailConfirmed,
            CreatedAt = user.CreatedAt.ToString("O"),
            LastActiveAt = (user.LastActive ?? user.LastLoginAt)?.ToString("O"),
            VehiclesCount = user.VehicleCount,
            FavoritesCount = 0,
            DealsCount = 0,
            Address = user.Location,
            City = user.City,
            Province = user.Province,
            Country = "República Dominicana",
            TwoFactorEnabled = false,
            RecentActivity = new List<UserActivityItemDto>(),
            Vehicles = new List<UserVehicleDto>(),
            Reports = new List<UserReportDto>()
        };
    }

    // Note: Type and Status mapping is now done by UserService's AdminUsersController.
    // UserService returns pre-mapped values like "buyer"/"seller"/"dealer" for Type
    // and "active"/"pending"/"suspended"/"banned" for Status.

    // =========================================================================
    // MOCK DATA FOR DEVELOPMENT
    // =========================================================================

    private static PaginatedUserResult GetMockUsers(string? search, string? type, string? status, int page, int pageSize)
    {
        var allUsers = new List<PlatformUserDto>
        {
            new() { Id = Guid.NewGuid().ToString(), Name = "Juan Pérez", Email = "juan@example.com", Phone = "809-555-0001", Type = "buyer", Status = "active", Verified = true, EmailVerified = true, CreatedAt = DateTime.UtcNow.AddDays(-30).ToString("O"), VehiclesCount = 0, FavoritesCount = 5, DealsCount = 0 },
            new() { Id = Guid.NewGuid().ToString(), Name = "María García", Email = "maria@example.com", Phone = "809-555-0002", Type = "seller", Status = "active", Verified = true, EmailVerified = true, CreatedAt = DateTime.UtcNow.AddDays(-60).ToString("O"), VehiclesCount = 3, FavoritesCount = 2, DealsCount = 1 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Carlos Rodríguez", Email = "carlos@dealer.com", Phone = "809-555-0003", Type = "dealer", Status = "active", Verified = true, EmailVerified = true, CreatedAt = DateTime.UtcNow.AddDays(-90).ToString("O"), VehiclesCount = 25, FavoritesCount = 0, DealsCount = 15 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Ana Martínez", Email = "ana@example.com", Phone = "809-555-0004", Type = "buyer", Status = "suspended", Verified = false, EmailVerified = true, CreatedAt = DateTime.UtcNow.AddDays(-15).ToString("O"), VehiclesCount = 0, FavoritesCount = 8, DealsCount = 0 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Pedro López", Email = "pedro@example.com", Phone = "809-555-0005", Type = "seller", Status = "pending", Verified = false, EmailVerified = false, CreatedAt = DateTime.UtcNow.AddDays(-5).ToString("O"), VehiclesCount = 1, FavoritesCount = 0, DealsCount = 0 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Laura Sánchez", Email = "laura@example.com", Phone = "809-555-0006", Type = "buyer", Status = "active", Verified = true, EmailVerified = true, CreatedAt = DateTime.UtcNow.AddDays(-45).ToString("O"), VehiclesCount = 0, FavoritesCount = 12, DealsCount = 2 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Roberto Díaz", Email = "roberto@dealer.com", Phone = "809-555-0007", Type = "dealer", Status = "active", Verified = true, EmailVerified = true, CreatedAt = DateTime.UtcNow.AddDays(-120).ToString("O"), VehiclesCount = 45, FavoritesCount = 0, DealsCount = 30 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Carmen Torres", Email = "carmen@example.com", Phone = "809-555-0008", Type = "buyer", Status = "banned", Verified = false, EmailVerified = true, CreatedAt = DateTime.UtcNow.AddDays(-200).ToString("O"), VehiclesCount = 0, FavoritesCount = 0, DealsCount = 0 },
        };

        // Apply filters
        var filtered = allUsers.AsEnumerable();

        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(u =>
                u.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(type))
        {
            filtered = filtered.Where(u => u.Type == type);
        }

        if (!string.IsNullOrEmpty(status))
        {
            filtered = filtered.Where(u => u.Status == status);
        }

        var total = filtered.Count();
        var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PaginatedUserResult
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static PlatformUserStatsDto GetMockStats()
    {
        return new PlatformUserStatsDto
        {
            Total = 1250,
            Active = 1100,
            Suspended = 45,
            Pending = 85,
            NewThisMonth = 120
        };
    }

    private static PlatformUserDetailDto GetMockUserDetail(Guid userId)
    {
        return new PlatformUserDetailDto
        {
            Id = userId.ToString(),
            Name = "Usuario de Prueba",
            Email = "usuario@example.com",
            Phone = "809-555-0000",
            Type = "buyer",
            Status = "active",
            Verified = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30).ToString("O"),
            LastActiveAt = DateTime.UtcNow.AddHours(-2).ToString("O"),
            VehiclesCount = 0,
            FavoritesCount = 5,
            DealsCount = 1,
            Address = "Calle Principal #123",
            City = "Santo Domingo",
            Province = "Distrito Nacional",
            Country = "República Dominicana",
            TwoFactorEnabled = false,
            RecentActivity = new List<UserActivityItemDto>
            {
                new() { Id = "1", Action = "Login", Timestamp = DateTime.UtcNow.AddHours(-2) },
                new() { Id = "2", Action = "View Vehicle", Target = "Toyota RAV4 2023", Timestamp = DateTime.UtcNow.AddHours(-3) },
            }
        };
    }
}

// =========================================================================
// DTOs FOR USERSERVICE RESPONSE
// =========================================================================

internal class UserServiceResponse
{
    public List<UserServiceUserDto>? Data { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// DTO matching the AdminUserDto returned by UserService's AdminUsersController.
/// Fields must match the JSON property names from UserService.
/// </summary>
internal class UserServiceUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public string Type { get; set; } = "buyer"; // buyer, seller, dealer (already mapped by UserService)
    public string UserIntent { get; set; } = "browse"; // browse, buy, sell, buy_and_sell
    public string Status { get; set; } = "active"; // active, pending, suspended, banned (already mapped)
    public bool Verified { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public int VehiclesCount { get; set; }
    public int FavoritesCount { get; set; }
    public int DealsCount { get; set; }
}

/// <summary>
/// DTO matching the UserDto returned by UserService's GET /api/users/{id} endpoint.
/// Field names match the UserService.Application.DTOs.UserDto JSON serialization.
/// </summary>
internal class UserServiceDetailDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string AccountType { get; set; } = "buyer";
    public bool IsVerified { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public int VehicleCount { get; set; }
    public int ReviewCount { get; set; }
    public double AverageRating { get; set; }
    public double ResponseRate { get; set; }
    public DateTime MemberSince { get; set; }
    public DateTime? LastActive { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
