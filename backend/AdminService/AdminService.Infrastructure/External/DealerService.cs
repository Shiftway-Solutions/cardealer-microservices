using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using AdminService.Application.Interfaces;
using AdminService.Application.UseCases.Dealers;

namespace AdminService.Infrastructure.External;

/// <summary>
/// Client for managing dealers via UserService.
/// UserService is the authoritative source of dealer data.
/// Returns empty results when UserService is unavailable.
/// </summary>
public class DealerService : IDealerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DealerService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public DealerService(HttpClient httpClient, ILogger<DealerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaginatedDealerResult> GetDealersAsync(
        string? search = null,
        string? status = null,
        string? plan = null,
        bool? verified = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["page"] = page.ToString();
            queryParams["pageSize"] = pageSize.ToString();
            if (!string.IsNullOrEmpty(search)) queryParams["searchTerm"] = search;
            if (verified.HasValue)
            {
                queryParams["verificationStatus"] = verified.Value ? "Verified" : "Pending";
            }

            // UserService admin endpoint: GET /api/admin/dealers
            var url = $"api/admin/dealers?{queryParams}";
            _logger.LogInformation("Fetching dealers from UserService: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UserServiceDealerListResponse>(JsonOptions, cancellationToken);
                if (result?.Dealers != null)
                {
                    var dealers = result.Dealers.Select(MapUserServiceDealerToAdmin).ToList();

                    // Apply status filter client-side (map admin status to UserService status)
                    if (!string.IsNullOrEmpty(status))
                    {
                        dealers = dealers.Where(d =>
                            string.Equals(d.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    // Apply plan filter client-side
                    if (!string.IsNullOrEmpty(plan))
                    {
                        dealers = dealers.Where(d =>
                            string.Equals(d.Plan, plan, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    return new PaginatedDealerResult
                    {
                        Items = dealers,
                        Total = result.TotalCount,
                        Page = result.Page,
                        PageSize = result.PageSize
                    };
                }
            }
            else
            {
                _logger.LogWarning("UserService returned {StatusCode} for dealer list", response.StatusCode);
            }

            return new PaginatedDealerResult { Items = new(), Total = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dealers from UserService");
            return new PaginatedDealerResult { Items = new(), Total = 0, Page = page, PageSize = pageSize };
        }
    }

    public async Task<DealerStatsDto> GetDealerStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // UserService has no dedicated statistics endpoint; compute from dealer list
            return await ComputeStatsFromListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing dealer stats");
            return new DealerStatsDto();
        }
    }

    public async Task<AdminDealerDto?> GetDealerByIdAsync(Guid dealerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"api/admin/dealers/{dealerId}";
            _logger.LogInformation("Fetching dealer {DealerId} from UserService", dealerId);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var dealer = await response.Content.ReadFromJsonAsync<UserServiceDealerDto>(JsonOptions, cancellationToken);
                if (dealer != null)
                {
                    return MapUserServiceDealerToAdmin(dealer);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Dealer {DealerId} not found in UserService", dealerId);
                return null;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dealer {DealerId}", dealerId);
            return null;
        }
    }

    public async Task VerifyDealerAsync(Guid dealerId, CancellationToken cancellationToken = default)
    {
        try
        {
            // UserService: POST /api/admin/dealers/{id}/approve
            var url = $"api/admin/dealers/{dealerId}/approve";
            _logger.LogInformation("Verifying dealer {DealerId} via UserService", dealerId);

            var payload = new { Notes = "Approved by admin via AdminService" };
            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to verify dealer {DealerId}: {StatusCode} - {Body}", dealerId, response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying dealer {DealerId}", dealerId);
        }
    }

    public async Task SuspendDealerAsync(Guid dealerId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            // UserService: PUT /api/dealers/{id} with IsActive=false
            var url = $"api/dealers/{dealerId}";
            _logger.LogInformation("Suspending dealer {DealerId}: {Reason}", dealerId, reason);

            var payload = new { IsActive = false };
            var response = await _httpClient.PutAsJsonAsync(url, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to suspend dealer {DealerId}: {StatusCode} - {Body}", dealerId, response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending dealer {DealerId}", dealerId);
        }
    }

    public async Task ReactivateDealerAsync(Guid dealerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"api/dealers/{dealerId}";
            _logger.LogInformation("Reactivating dealer {DealerId}", dealerId);

            var payload = new { IsActive = true };
            var response = await _httpClient.PutAsJsonAsync(url, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to reactivate dealer {DealerId}: {StatusCode} - {Body}", dealerId, response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating dealer {DealerId}", dealerId);
        }
    }

    public async Task DeleteDealerAsync(Guid dealerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"api/dealers/{dealerId}";
            _logger.LogInformation("Deleting dealer {DealerId}", dealerId);

            var response = await _httpClient.DeleteAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to delete dealer {DealerId}: {StatusCode} - {Body}", dealerId, response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dealer {DealerId}", dealerId);
        }
    }

    public async Task<AdminDealerDto?> CreateDealerProfileForUserAsync(
        Guid userId,
        string businessName,
        string email,
        string phone,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if dealer profile already exists for this user
            var checkUrl = $"api/dealers/owner/{userId}";
            var checkResponse = await _httpClient.GetAsync(checkUrl, cancellationToken);
            if (checkResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Dealer profile already exists for user {UserId}", userId);
                var existingDto = await checkResponse.Content.ReadFromJsonAsync<UserServiceDealerDto>(JsonOptions, cancellationToken);
                return existingDto != null ? MapUserServiceDealerToAdmin(existingDto) : null;
            }

            var payload = new
            {
                OwnerUserId = userId,
                BusinessName = businessName,
                Email = email,
                Phone = phone,
                Address = "Pendiente de completar",
                City = "Pendiente",
                State = "Pendiente",
                DealerType = 0 // Independent
            };

            var url = "api/dealers";
            _logger.LogInformation(
                "Creating dealer profile for user {UserId} (BusinessName={BusinessName})", userId, businessName);

            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<UserServiceDealerDto>(JsonOptions, cancellationToken);
                if (created != null)
                {
                    _logger.LogInformation(
                        "Created dealer profile {DealerId} for user {UserId}", created.Id, userId);
                    return MapUserServiceDealerToAdmin(created);
                }
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Failed to create dealer profile for user {UserId}: {StatusCode} - {Body}",
                    userId, response.StatusCode, body);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dealer profile for user {UserId}", userId);
            return null;
        }
    }

    // =========================================================================
    // COMPUTE STATS FROM LIST (fallback when /statistics returns partial data)
    // =========================================================================

    private async Task<DealerStatsDto> ComputeStatsFromListAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Fetch a large page to compute stats
            var result = await GetDealersAsync(page: 1, pageSize: 500, cancellationToken: cancellationToken);
            var dealers = result.Items;

            // Use v2 display names from PlanConfiguration for consistent breakdown
            return new DealerStatsDto
            {
                Total = result.Total,
                Active = dealers.Count(d => string.Equals(d.Status, "active", StringComparison.OrdinalIgnoreCase)),
                Pending = dealers.Count(d => string.Equals(d.Status, "pending", StringComparison.OrdinalIgnoreCase)),
                Suspended = dealers.Count(d => string.Equals(d.Status, "suspended", StringComparison.OrdinalIgnoreCase)),
                TotalMrr = dealers.Sum(d => d.Mrr),
                ByPlan = new DealerPlanBreakdown
                {
                    Libre = dealers.Count(d => string.Equals(d.Plan, "libre", StringComparison.OrdinalIgnoreCase)),
                    Visible = dealers.Count(d => string.Equals(d.Plan, "visible", StringComparison.OrdinalIgnoreCase)),
                    Pro = dealers.Count(d => string.Equals(d.Plan, "pro", StringComparison.OrdinalIgnoreCase)),
                    Elite = dealers.Count(d => string.Equals(d.Plan, "elite", StringComparison.OrdinalIgnoreCase))
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing stats from dealer list");
            return new DealerStatsDto();
        }
    }

    // =========================================================================
    // MAPPING: UserService DealerDto → AdminDealerDto
    // =========================================================================

    private static AdminDealerDto MapUserServiceDealerToAdmin(UserServiceDealerDto dealer)
    {
        // Derive status from IsActive + VerificationStatus
        var status = dealer.IsActive
            ? (dealer.VerificationStatus == "Verified" ? "active" : "pending")
            : (dealer.VerificationStatus == "Rejected" ? "rejected" : "suspended");

        var verified = string.Equals(dealer.VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase);

        // Default to "libre" plan (UserService doesn't track subscription plan directly)
        var plan = "libre";
        var mrr = 0m;

        return new AdminDealerDto
        {
            Id = dealer.Id.ToString(),
            Name = dealer.BusinessName ?? "Sin nombre",
            Email = dealer.Email ?? "",
            Phone = dealer.Phone ?? "",
            Status = status,
            Verified = verified,
            Plan = plan,
            VehiclesCount = dealer.ActiveListings,
            SalesCount = dealer.TotalSales,
            Rating = (double)dealer.AverageRating,
            ReviewsCount = dealer.TotalReviews,
            Location = !string.IsNullOrEmpty(dealer.City) ? $"{dealer.City}, {dealer.State}" : "N/A",
            CreatedAt = dealer.CreatedAt.ToString("O"),
            Mrr = mrr,
            DocumentsCount = 0,
            PendingDocuments = 0
        };
    }

}

// =========================================================================
// DTOs FOR USERSERVICE RESPONSES
// =========================================================================

/// <summary>
/// Matches UserService AdminDealersController paginated response.
/// </summary>
internal class UserServiceDealerListResponse
{
    public List<UserServiceDealerDto> Dealers { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Matches UserService DealerDto field names (camelCase JSON).
/// </summary>
internal class UserServiceDealerDto
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? DealerType { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? WhatsApp { get; set; }
    public string? Website { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? VerificationStatus { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? RejectionReason { get; set; }
    public int ActiveListings { get; set; }
    public int MaxListings { get; set; }
    public int TotalListings { get; set; }
    public int TotalSales { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
