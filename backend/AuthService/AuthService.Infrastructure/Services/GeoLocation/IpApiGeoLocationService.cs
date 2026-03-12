using AuthService.Domain.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AuthService.Infrastructure.Services.GeoLocation;

/// <summary>
/// IP Geolocation service using ip-api.com (free tier: 45 requests/minute)
/// Includes caching to reduce API calls and improve performance.
/// </summary>
public class IpApiGeoLocationService : IGeoLocationService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IpApiGeoLocationService> _logger;

    // Cache duration for IP lookups (24 hours - IPs don't change location often)
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    // ip-api.com free endpoint (no API key required)
    private const string ApiBaseUrl = "http://ip-api.com/json/";

    public IpApiGeoLocationService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<IpApiGeoLocationService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<GeoLocationResult?> GetLocationFromIpAsync(
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(ipAddress) || ipAddress == "Unknown")
        {
            return null;
        }

        // Skip private/local IPs
        if (IsPrivateOrLocalIp(ipAddress))
        {
            _logger.LogDebug("Skipping geolocation for private/local IP: {IpAddress}", ipAddress);
            return new GeoLocationResult(
                Country: "Red Local",
                CountryCode: "LO",
                City: "",
                Region: "",
                Timezone: "",
                Latitude: null,
                Longitude: null,
                Isp: "Red Local"
            );
        }

        // Check cache first
        var cacheKey = $"geolocation:{ipAddress}";
        if (_cache.TryGetValue(cacheKey, out GeoLocationResult? cachedResult))
        {
            _logger.LogDebug("Geolocation cache hit for IP: {IpAddress}", ipAddress);
            return cachedResult;
        }

        try
        {
            // Call ip-api.com
            var response = await _httpClient.GetFromJsonAsync<IpApiResponse>(
                $"{ApiBaseUrl}{ipAddress}?fields=status,message,country,countryCode,region,regionName,city,zip,lat,lon,timezone,isp,query",
                cancellationToken);

            if (response == null || response.Status != "success")
            {
                _logger.LogWarning("Geolocation lookup failed for IP: {IpAddress}. Message: {Message}",
                    ipAddress, response?.Message ?? "No response");
                return null;
            }

            var result = new GeoLocationResult(
                Country: response.Country ?? "Unknown",
                CountryCode: response.CountryCode ?? "XX",
                City: response.City ?? "Unknown",
                Region: response.RegionName ?? "",
                Timezone: response.Timezone ?? "",
                Latitude: response.Lat,
                Longitude: response.Lon,
                Isp: response.Isp ?? "Unknown"
            );

            // Cache the result
            _cache.Set(cacheKey, result, CacheDuration);

            _logger.LogInformation("Geolocation resolved for IP {IpAddress}: {City}, {Country}",
                ipAddress, result.City, result.Country);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during geolocation lookup for IP: {IpAddress}", ipAddress);
            return null;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _logger.LogDebug("Geolocation lookup cancelled for IP: {IpAddress}", ipAddress);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during geolocation lookup for IP: {IpAddress}", ipAddress);
            return null;
        }
    }

    /// <summary>
    /// Checks if an IP address is private or local (not routable on the internet)
    /// </summary>
    private static bool IsPrivateOrLocalIp(string ipAddress)
    {
        if (ipAddress == "::1" || ipAddress == "127.0.0.1" || ipAddress == "localhost")
            return true;

        // Check IPv4 private ranges
        if (ipAddress.StartsWith("10.") ||
            ipAddress.StartsWith("192.168.") ||
            ipAddress.StartsWith("172.16.") ||
            ipAddress.StartsWith("172.17.") ||
            ipAddress.StartsWith("172.18.") ||
            ipAddress.StartsWith("172.19.") ||
            ipAddress.StartsWith("172.20.") ||
            ipAddress.StartsWith("172.21.") ||
            ipAddress.StartsWith("172.22.") ||
            ipAddress.StartsWith("172.23.") ||
            ipAddress.StartsWith("172.24.") ||
            ipAddress.StartsWith("172.25.") ||
            ipAddress.StartsWith("172.26.") ||
            ipAddress.StartsWith("172.27.") ||
            ipAddress.StartsWith("172.28.") ||
            ipAddress.StartsWith("172.29.") ||
            ipAddress.StartsWith("172.30.") ||
            ipAddress.StartsWith("172.31."))
        {
            return true;
        }

        // Check for IPv6 link-local or private
        if (ipAddress.StartsWith("fe80:") ||
            ipAddress.StartsWith("fc00:") ||
            ipAddress.StartsWith("fd00:"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Response model from ip-api.com
    /// </summary>
    private class IpApiResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("regionName")]
        public string? RegionName { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("zip")]
        public string? Zip { get; set; }

        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonPropertyName("lon")]
        public double? Lon { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("isp")]
        public string? Isp { get; set; }

        [JsonPropertyName("query")]
        public string? Query { get; set; }
    }
}
