using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ServiceDiscovery.Application.Interfaces;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.External;

/// <summary>
/// Cliente HTTP para comunicarse con VehiclesSaleService
/// </summary>
public class VehiclesSaleServiceClient : IVehiclesSaleServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VehiclesSaleServiceClient> _logger;
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly IMemoryCache _cache;
    private const int CacheTtlMinutes = 2;

    public VehiclesSaleServiceClient(
        HttpClient httpClient,
        ILogger<VehiclesSaleServiceClient> logger,
        IServiceDiscovery serviceDiscovery,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _serviceDiscovery = serviceDiscovery;
        _cache = cache;
    }

    private async Task<string> GetServiceUrlAsync()
    {
        try
        {
            var instance = await _serviceDiscovery.FindServiceInstanceAsync("VehiclesSaleService");
            return instance != null ? $"http://{instance.Host}:{instance.Port}" : "http://vehiclessaleservice:80";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving VehiclesSaleService from Consul, using fallback");
            return "http://vehiclessaleservice:80";
        }
    }

    /// <summary>
    /// Obtiene los listados de vehículos de un vendedor
    /// </summary>
    public async Task<SellerListingsResult> GetSellerListingsAsync(
        Guid sellerId,
        int page = 1,
        int pageSize = 12,
        string? status = null)
    {
        try
        {
            var cacheKey = $"seller-listings:{sellerId}:{page}:{pageSize}:{status ?? "all"}";
            if (_cache.TryGetValue(cacheKey, out SellerListingsResult? cached))
            {
                _logger.LogDebug("Seller listings for {SellerId} retrieved from cache", sellerId);
                return cached!;
            }

            var baseUrl = await GetServiceUrlAsync();
            var url = $"{baseUrl}/api/vehicles/seller/{sellerId}?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(status))
            {
                url += $"&status={status}";
            }

            _logger.LogDebug("Fetching seller listings from {Url}", url);

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get seller listings. Status: {StatusCode}", response.StatusCode);
                return new SellerListingsResult { Page = page, PageSize = pageSize };
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<VehiclesApiResponse>();
            if (apiResponse?.Data == null)
            {
                return new SellerListingsResult { Page = page, PageSize = pageSize };
            }

            var result = new SellerListingsResult
            {
                Listings = apiResponse.Data.Select(v => new VehicleListingDto
                {
                    Id = v.Id,
                    Title = v.Title ?? $"{v.Year} {v.Make} {v.Model}",
                    Slug = v.Slug ?? v.Id.ToString(),
                    Price = v.Price,
                    Currency = v.Currency ?? "DOP",
                    // Normalize VehiclesSaleService enum names to lowercase frontend values
                    Status = NormalizeStatus(v.Status),
                    MainImageUrl = v.MainImageUrl,
                    Year = v.Year,
                    Make = v.Make ?? string.Empty,
                    Model = v.Model ?? string.Empty,
                    Mileage = v.Mileage,
                    Transmission = v.Transmission,
                    FuelType = v.FuelType,
                    Views = v.Views,
                    Favorites = v.Favorites,
                    CreatedAt = v.CreatedAt
                }).ToList(),
                Page = apiResponse.Page,
                PageSize = apiResponse.PageSize,
                TotalCount = apiResponse.TotalCount,
                TotalPages = apiResponse.TotalPages
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheTtlMinutes));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching seller listings for {SellerId}", sellerId);
            return new SellerListingsResult { Page = page, PageSize = pageSize };
        }
    }

    /// <summary>
    /// Maps VehiclesSaleService enum names ("PendingReview", "Active", etc.)
    /// to frontend-expected lowercase values ("pending", "active", etc.)
    /// </summary>
    private static string NormalizeStatus(string? raw) => raw?.ToLowerInvariant() switch
    {
        "pendingreview" => "pending",
        "pending" => "pending",
        "active" => "active",
        "rejected" => "rejected",
        "sold" => "sold",
        "archived" => "paused",
        "draft" => "draft",
        "reserved" => "sold",
        _ => "pending"
    };

    /// <summary>
    /// Obtiene las estadísticas de listados de un vendedor
    /// </summary>
    public async Task<SellerListingStatsResult?> GetSellerListingStatsAsync(Guid sellerId)
    {
        try
        {
            var cacheKey = $"seller-listing-stats:{sellerId}";
            if (_cache.TryGetValue(cacheKey, out SellerListingStatsResult? cached))
            {
                return cached;
            }

            var baseUrl = await GetServiceUrlAsync();
            var url = $"{baseUrl}/api/vehicles/seller/{sellerId}/stats";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get seller listing stats. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<SellerListingStatsResult>();
            if (result != null)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheTtlMinutes));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching seller listing stats for {SellerId}", sellerId);
            return null;
        }
    }

    // Internal DTOs for API response mapping
    private class VehiclesApiResponse
    {
        public List<VehicleDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    private class VehicleDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Slug { get; set; }
        public decimal Price { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
        public string? MainImageUrl { get; set; }
        public int Year { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public int Mileage { get; set; }
        public string? Transmission { get; set; }
        public string? FuelType { get; set; }
        public int Views { get; set; }
        public int Favorites { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
