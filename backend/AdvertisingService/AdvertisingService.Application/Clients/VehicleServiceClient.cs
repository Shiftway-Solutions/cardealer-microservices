using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace AdvertisingService.Application.Clients;

public class VehicleServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VehicleServiceClient> _logger;

    // E-002: 2-second per-request timeout to avoid blocking homepage on slow downstream
    private static readonly TimeSpan _vehicleRequestTimeout = TimeSpan.FromSeconds(2);
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public VehicleServiceClient(HttpClient httpClient, ILogger<VehicleServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<VehicleBasicInfo?> GetVehicleBasicInfoAsync(Guid vehicleId, CancellationToken ct = default)
    {
        // E-002: each enrichment request has its own 2-second timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_vehicleRequestTimeout);

        try
        {
            var response = await _httpClient.GetAsync($"/api/vehicles/{vehicleId}", cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get vehicle {VehicleId}: {StatusCode}", vehicleId, response.StatusCode);
                return null;
            }

            // VehiclesSaleService returns the raw Vehicle entity (not wrapped in ApiResponse).
            // Map to VehicleBasicInfo, computing derived fields.
            var raw = await response.Content.ReadFromJsonAsync<VehicleRawDto>(_jsonOptions, cts.Token);
            if (raw == null) return null;

            return MapToBasicInfo(raw);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("VehicleService request timed out for vehicle {VehicleId} (2s limit)", vehicleId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling VehicleService for vehicle {VehicleId}", vehicleId);
            return null;
        }
    }

    private static VehicleBasicInfo MapToBasicInfo(VehicleRawDto raw)
    {
        // Primary image: prefer IsPrimary=true, then lowest SortOrder
        var primaryImage = raw.Images?
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .FirstOrDefault();

        // Location: "City, State" or whichever is available
        var location = string.Join(", ",
            new[] { raw.City, raw.State }.Where(s => !string.IsNullOrWhiteSpace(s)));

        // Slug follows VehiclesSaleService slug pattern: {year}-{make}-{model}-{shortId}
        var shortId = raw.Id.ToString("N")[..8];
        var slug = $"{raw.Year}-{(raw.Make ?? "").ToLowerInvariant().Replace(" ", "-")}" +
                   $"-{(raw.Model ?? "").ToLowerInvariant().Replace(" ", "-")}-{shortId}";

        return new VehicleBasicInfo
        {
            Id = raw.Id,
            Title = raw.Title,
            Make = raw.Make,
            Model = raw.Model,
            Year = raw.Year,
            Price = raw.Price,
            Currency = !string.IsNullOrWhiteSpace(raw.Currency) ? raw.Currency : "DOP",
            PrimaryImageUrl = primaryImage?.Url ?? primaryImage?.ThumbnailUrl,
            Slug = slug,
            Location = string.IsNullOrWhiteSpace(location) ? null : location,
            IsFeatured = raw.IsFeatured,
            IsPremium = raw.IsPremium,
            ImageCount = raw.Images?.Count ?? 0,
            HasDescription = !string.IsNullOrWhiteSpace(raw.Description),
            SellerId = raw.SellerId
        };
    }

    // ── Internal DTO: mirrors the Vehicle entity fields we need ──────────────
    private sealed class VehicleRawDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Make { get; set; }
        public string? Model { get; set; }
        public int Year { get; set; }
        public decimal Price { get; set; }
        public string? Currency { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Description { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPremium { get; set; }
        public Guid SellerId { get; set; }
        public List<VehicleImageRawDto>? Images { get; set; }
    }

    private sealed class VehicleImageRawDto
    {
        public string Url { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }
}

public class VehicleBasicInfo
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "DOP";
    public string? PrimaryImageUrl { get; set; }    // ← NEW: primary image URL
    public string? Slug { get; set; }               // ← NEW: URL-friendly slug
    public string? Location { get; set; }           // ← NEW: "City, State"
    public bool IsFeatured { get; set; }            // ← NEW
    public bool IsPremium { get; set; }             // ← NEW
    public int ImageCount { get; set; }
    public bool HasDescription { get; set; }
    public Guid SellerId { get; set; }
}

public class ApiResponseWrapper<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
}
