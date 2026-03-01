using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AdminService.Infrastructure.External
{
    public class VehicleServiceClient : IVehicleServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<VehicleServiceClient> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Vehicle status mapping (matches VehiclesSaleService enum)
        private static readonly Dictionary<int, string> StatusMap = new()
        {
            { 0, "draft" },
            { 1, "pending" },
            { 2, "active" },
            { 3, "reserved" },
            { 4, "sold" },
            { 5, "archived" },
            { 6, "rejected" }
        };

        // Reverse map: VehiclesSaleService string status → int code
        private static readonly Dictionary<string, int> StatusNameToCode = new(StringComparer.OrdinalIgnoreCase)
        {
            { "draft",        0 },
            { "pendingreview", 1 },
            { "pending",      1 },
            { "active",       2 },
            { "reserved",     3 },
            { "sold",         4 },
            { "archived",     5 },
            { "rejected",     6 }
        };

        // String status names used by VehiclesSaleService in query params
        private static readonly Dictionary<int, string> StatusCodeToApiName = new()
        {
            { 0, "Draft" },
            { 1, "PendingReview" },
            { 2, "Active" },
            { 3, "Reserved" },
            { 4, "Sold" },
            { 5, "Archived" },
            { 6, "Rejected" }
        };

        public VehicleServiceClient(HttpClient httpClient, ILogger<VehicleServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<VehicleSearchResponse?> SearchVehiclesAsync(VehicleSearchFilters filters, CancellationToken ct = default)
        {
            try
            {
                // Build query string - VehiclesSaleService uses different param names
                var queryParams = new List<string>
                {
                    $"Page={filters.Page}",
                    $"PageSize={filters.PageSize}"
                };

                if (!string.IsNullOrWhiteSpace(filters.Search))
                    queryParams.Add($"Search={Uri.EscapeDataString(filters.Search)}");

                var url = $"/api/vehicles?{string.Join("&", queryParams)}";

                _logger.LogDebug("Searching vehicles: {Url}", url);

                var response = await _httpClient.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("VehiclesSaleService search returned {StatusCode}", response.StatusCode);
                    return new VehicleSearchResponse
                    {
                        Vehicles = new List<VehicleDto>(),
                        TotalCount = 0,
                        Page = filters.Page,
                        PageSize = filters.PageSize,
                        TotalPages = 0
                    };
                }

                var content = await response.Content.ReadAsStringAsync(ct);
                var rawResult = JsonSerializer.Deserialize<RawVehicleSearchResult>(content, JsonOptions);

                if (rawResult == null)
                {
                    return new VehicleSearchResponse
                    {
                        Vehicles = new List<VehicleDto>(),
                        TotalCount = 0,
                        Page = filters.Page,
                        PageSize = filters.PageSize,
                        TotalPages = 0
                    };
                }

                // Map raw vehicles to our DTOs and apply admin-specific filters
                var vehicles = (rawResult.Vehicles ?? new List<RawVehicle>())
                    .Select(MapToVehicleDto)
                    .ToList();

                // Apply admin-specific filters (status, sellerType, featured) client-side
                // since VehiclesSaleService search only filters Active vehicles by default
                if (!string.IsNullOrWhiteSpace(filters.Status))
                {
                    vehicles = vehicles.Where(v =>
                        string.Equals(v.StatusName, filters.Status, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(filters.SellerType))
                {
                    vehicles = vehicles.Where(v =>
                        string.Equals(v.SellerType, filters.SellerType, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (filters.Featured.HasValue)
                {
                    vehicles = vehicles.Where(v => v.IsFeatured == filters.Featured.Value).ToList();
                }

                return new VehicleSearchResponse
                {
                    Vehicles = vehicles,
                    TotalCount = rawResult.TotalCount,
                    Page = rawResult.Page,
                    PageSize = rawResult.PageSize,
                    TotalPages = rawResult.TotalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching vehicles from VehiclesSaleService");
                return new VehicleSearchResponse
                {
                    Vehicles = new List<VehicleDto>(),
                    TotalCount = 0,
                    Page = filters.Page,
                    PageSize = filters.PageSize,
                    TotalPages = 0
                };
            }
        }

        public async Task<VehicleDetailResponse?> GetVehicleByIdAsync(Guid vehicleId, CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/vehicles/{vehicleId}", ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("VehiclesSaleService GetById returned {StatusCode} for {VehicleId}",
                        response.StatusCode, vehicleId);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync(ct);
                var rawVehicle = JsonSerializer.Deserialize<RawVehicle>(content, JsonOptions);

                if (rawVehicle == null) return null;

                var dto = MapToVehicleDto(rawVehicle);
                return new VehicleDetailResponse
                {
                    Id = dto.Id,
                    Title = dto.Title,
                    Make = dto.Make,
                    Model = dto.Model,
                    Year = dto.Year,
                    Price = dto.Price,
                    Currency = dto.Currency,
                    Status = dto.Status,
                    StatusName = dto.StatusName,
                    SellerId = dto.SellerId,
                    SellerName = dto.SellerName,
                    SellerType = dto.SellerType,
                    ViewCount = dto.ViewCount,
                    LeadCount = dto.LeadCount,
                    IsFeatured = dto.IsFeatured,
                    CreatedAt = dto.CreatedAt,
                    PublishedAt = dto.PublishedAt,
                    RejectionReason = dto.RejectionReason,
                    Images = dto.Images
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicle {VehicleId} from VehiclesSaleService", vehicleId);
                return null;
            }
        }

        public async Task<VehicleStatsResponse> GetVehicleStatsAsync(CancellationToken ct = default)
        {
            try
            {
                // Fetch all vehicles in a single call (large page) to compute stats client-side
                // VehiclesSaleService has no admin stats endpoint, so we aggregate here
                var stats = new VehicleStatsResponse();

                // Use a large page size to get all vehicles in one call
                const int batchSize = 500;
                var response = await _httpClient.GetAsync(
                    $"/api/vehicles?Page=1&PageSize={batchSize}", ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("VehiclesSaleService stats fetch returned {StatusCode}",
                        response.StatusCode);
                    return stats;
                }

                var content = await response.Content.ReadAsStringAsync(ct);
                var rawResult = JsonSerializer.Deserialize<RawVehicleSearchResult>(content, JsonOptions);

                if (rawResult == null) return stats;

                stats.Total = rawResult.TotalCount;

                // Count by status from the returned vehicles
                var vehicles = rawResult.Vehicles ?? new List<RawVehicle>();
                stats.Active   = vehicles.Count(v => string.Equals(v.Status, "Active",       StringComparison.OrdinalIgnoreCase));
                stats.Pending  = vehicles.Count(v => string.Equals(v.Status, "PendingReview", StringComparison.OrdinalIgnoreCase));
                stats.Rejected = vehicles.Count(v => string.Equals(v.Status, "Rejected",      StringComparison.OrdinalIgnoreCase));
                stats.Featured = vehicles.Count(v => v.IsFeatured);
                stats.WithReports = 0;  // Not available from VehiclesSaleService

                // If total exceeds batch, fetch remaining pages to get full counts
                if (rawResult.TotalCount > batchSize)
                {
                    var totalPages = (int)Math.Ceiling((double)rawResult.TotalCount / batchSize);
                    for (var page = 2; page <= Math.Min(totalPages, 10); page++)
                    {
                        var pageResponse = await _httpClient.GetAsync(
                            $"/api/vehicles?Page={page}&PageSize={batchSize}", ct);
                        if (!pageResponse.IsSuccessStatusCode) break;

                        var pageContent = await pageResponse.Content.ReadAsStringAsync(ct);
                        var pageResult = JsonSerializer.Deserialize<RawVehicleSearchResult>(pageContent, JsonOptions);
                        if (pageResult?.Vehicles == null) break;

                        stats.Active   += pageResult.Vehicles.Count(v => string.Equals(v.Status, "Active",       StringComparison.OrdinalIgnoreCase));
                        stats.Pending  += pageResult.Vehicles.Count(v => string.Equals(v.Status, "PendingReview", StringComparison.OrdinalIgnoreCase));
                        stats.Rejected += pageResult.Vehicles.Count(v => string.Equals(v.Status, "Rejected",      StringComparison.OrdinalIgnoreCase));
                        stats.Featured += pageResult.Vehicles.Count(v => v.IsFeatured);
                    }
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicle stats from VehiclesSaleService");
                return new VehicleStatsResponse();
            }
        }

        public async Task<bool> FeatureVehicleAsync(Guid vehicleId, bool featured, CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"/api/vehicles/{vehicleId}/feature",
                    new { featured },
                    ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to feature vehicle {VehicleId}: {StatusCode}",
                        vehicleId, response.StatusCode);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error featuring vehicle {VehicleId}", vehicleId);
                return false;
            }
        }

        public async Task<bool> DeleteVehicleAsync(Guid vehicleId, CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/vehicles/{vehicleId}", ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to delete vehicle {VehicleId}: {StatusCode}",
                        vehicleId, response.StatusCode);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vehicle {VehicleId}", vehicleId);
                return false;
            }
        }

        public async Task<bool> PublishVehicleAsync(Guid vehicleId, CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/api/vehicles/{vehicleId}/publish", null, ct);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing vehicle {VehicleId}", vehicleId);
                return false;
            }
        }

        public async Task<bool> UnpublishVehicleAsync(Guid vehicleId, CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/api/vehicles/{vehicleId}/unpublish", null, ct);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unpublishing vehicle {VehicleId}", vehicleId);
                return false;
            }
        }

        // ── Private helpers ─────────────────────────────────────────

        private async Task<int> FetchVehiclesCount(int? status, CancellationToken ct)
        {
            try
            {
                var url = "/api/vehicles?Page=1&PageSize=1";
                var response = await _httpClient.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode) return 0;

                var content = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<RawVehicleSearchResult>(content, JsonOptions);
                return result?.TotalCount ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<int> FetchVehiclesByStatus(string statusName, CancellationToken ct)
        {
            try
            {
                // The general search endpoint doesn't support status filtering,
                // so we fetch all vehicles and count client-side
                // This is called with small page sizes just to get TotalCount per status
                // NOTE: This won't give per-status counts from the public API.
                // We return 0 as a safe fallback; stats are computed in GetVehicleStatsAsync
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching vehicle count for status {Status}", statusName);
                return 0;
            }
        }

        private async Task<int> FetchFeaturedCount(CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/vehicles/featured", ct);
                if (!response.IsSuccessStatusCode) return 0;

                var content = await response.Content.ReadAsStringAsync(ct);
                var vehicles = JsonSerializer.Deserialize<List<RawVehicle>>(content, JsonOptions);
                return vehicles?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private static VehicleDto MapToVehicleDto(RawVehicle raw)
        {
            var statusInt = StatusNameToCode.GetValueOrDefault(raw.Status ?? string.Empty, 0);
            var statusName = StatusMap.GetValueOrDefault(statusInt, (raw.Status ?? "unknown").ToLower());

            // Determine primary image
            var primaryImage = raw.Images?
                .FirstOrDefault(i => i.IsPrimary)?.Url
                ?? raw.Images?.FirstOrDefault()?.Url
                ?? string.Empty;

            return new VehicleDto
            {
                Id = raw.Id,
                Title = raw.Title ?? $"{raw.Year} {raw.Make} {raw.Model}",
                Make = raw.Make ?? string.Empty,
                Model = raw.Model ?? string.Empty,
                Year = raw.Year,
                Price = raw.Price,
                Currency = raw.Currency ?? "DOP",
                Status = statusInt,
                StatusName = statusName,
                SellerId = raw.SellerId,
                SellerName = raw.SellerName ?? raw.SellerEmail ?? "Unknown",
                SellerType = MapSellerType(raw.SellerType),
                ViewCount = raw.ViewCount,
                LeadCount = raw.LeadCount,
                IsFeatured = raw.IsFeatured,
                CreatedAt = raw.CreatedAt,
                PublishedAt = raw.PublishedAt,
                RejectionReason = raw.RejectionReason,
                Images = raw.Images?.Select(i => new VehicleImageDto
                {
                    Url = i.Url ?? string.Empty,
                    IsPrimary = i.IsPrimary
                }).ToList()
            };
        }

        private static string MapSellerType(string? sellerType)
        {
            return sellerType?.ToLowerInvariant() switch
            {
                "individual" => "individual",
                "seller"     => "individual",
                "dealer"     => "dealer",
                "franchise"  => "franchise",
                "wholesale"  => "wholesale",
                _            => sellerType?.ToLowerInvariant() ?? "individual"
            };
        }

        // ── Raw DTOs for VehiclesSaleService deserialization ─────────

        private class RawVehicleSearchResult
        {
            public List<RawVehicle> Vehicles { get; set; } = new();
            public int TotalCount { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
        }

        private class RawVehicle
        {
            public Guid Id { get; set; }
            public string? Title { get; set; }
            public string? Make { get; set; }
            public string? Model { get; set; }
            public int Year { get; set; }
            public decimal Price { get; set; }
            public string? Currency { get; set; }
            public string? Status { get; set; }   // VehiclesSaleService returns string enum, e.g. "Active", "PendingReview"
            public Guid? SellerId { get; set; }
            public string? SellerName { get; set; }
            public string? SellerEmail { get; set; }
            public string? SellerType { get; set; }  // VehiclesSaleService returns string enum, e.g. "Seller", "Dealer"
            public int ViewCount { get; set; }
            public int LeadCount { get; set; }
            public bool IsFeatured { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? PublishedAt { get; set; }
            public string? RejectionReason { get; set; }
            public List<RawVehicleImage>? Images { get; set; }
        }

        private class RawVehicleImage
        {
            public string? Url { get; set; }
            public bool IsPrimary { get; set; }
        }
    }
}
