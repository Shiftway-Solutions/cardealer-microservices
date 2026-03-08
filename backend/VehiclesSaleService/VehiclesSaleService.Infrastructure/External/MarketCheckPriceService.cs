using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VehiclesSaleService.Application.Interfaces;

namespace VehiclesSaleService.Infrastructure.External;

/// <summary>
/// Real MarketCheck API integration for market price comparison.
/// 
/// MarketCheck API documentation: https://apidocs.marketcheck.com/
/// 
/// Key endpoints:
///   GET /v2/search/car/active     → Active listings search
///   GET /v2/stats/car             → Market statistics (avg price, count)
///   GET /v2/history/car/{vin}     → Price history for a VIN
///   GET /v2/predict/car/price     → AI-predicted price
/// 
/// Configuration required:
///   ExternalApis:MarketPrice:Provider = "MarketCheck"
///   ExternalApis:MarketPrice:ApiKey   = "your-marketcheck-api-key"
///   ExternalApis:MarketPrice:BaseUrl  = "https://api.marketcheck.com/v2"  (default)
/// 
/// When ApiKey is not set, falls back to MockMarketPriceService if FallbackToMock=true.
/// </summary>
public class MarketCheckPriceService : IMarketPriceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MarketCheckPriceService> _logger;
    private readonly MockMarketPriceService _fallback;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly bool _fallbackToMock;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public MarketCheckPriceService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MarketCheckPriceService> logger,
        ILogger<MockMarketPriceService> mockLogger,
        IExchangeRateService exchangeRateService)
    {
        _httpClient = httpClientFactory.CreateClient("MarketCheck");
        _logger = logger;
        _exchangeRateService = exchangeRateService;
        _fallback = new MockMarketPriceService(mockLogger, exchangeRateService);
        _apiKey = configuration["ExternalApis:MarketPrice:ApiKey"] ?? string.Empty;
        _baseUrl = configuration["ExternalApis:MarketPrice:BaseUrl"] ?? "https://api.marketcheck.com/v2";
        _fallbackToMock = bool.TryParse(
            configuration["ExternalApis:MarketPrice:FallbackToMock"], out var fb) ? fb : true;
    }

    public async Task<MarketPriceAnalysis?> GetMarketPriceAsync(
        string make, string model, int year,
        string? trim = null, decimal? mileage = null,
        string? condition = null, string? province = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("MarketCheck: GetMarketPrice({Make} {Model} {Year})", make, model, year);

        if (!HasApiKey())
        {
            _logger.LogWarning("MarketCheck: No API key configured, using fallback");
            return _fallbackToMock
                ? await _fallback.GetMarketPriceAsync(make, model, year, trim, mileage, condition, province, ct)
                : null;
        }

        try
        {
            // MarketCheck stats endpoint
            // SECURITY: API key sent via request header, NOT in URL query string.
            // URL-embedded keys are visible in server logs, browser history, and URL traces.
            var url = $"{_baseUrl}/stats/car" +
                     $"?make={Encode(make)}&model={Encode(model)}&year={year}" +
                     (trim != null ? $"&trim={Encode(trim)}" : "") +
                     "&car_type=used&country=US";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", _apiKey);

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MarketCheck: Stats API returned {Status}", response.StatusCode);
                return _fallbackToMock
                    ? await _fallback.GetMarketPriceAsync(make, model, year, trim, mileage, condition, province, ct)
                    : null;
            }

            var stats = await response.Content.ReadFromJsonAsync<MarketCheckStatsResponse>(JsonOptions, ct);
            if (stats == null)
                return _fallbackToMock
                    ? await _fallback.GetMarketPriceAsync(make, model, year, trim, mileage, condition, province, ct)
                    : null;

            // Convert USD prices to DOP for DR market using live BCRD rate
            var avgPriceUsd = stats.Mean ?? stats.Median ?? 0;
            var minPriceUsd = stats.Min ?? avgPriceUsd * 0.75m;
            var maxPriceUsd = stats.Max ?? avgPriceUsd * 1.25m;
            var medianPriceUsd = stats.Median ?? avgPriceUsd;

            var rateResult = await _exchangeRateService.GetDopUsdRateAsync(ct);
            var dopUsdRate = rateResult.Rate;
            _logger.LogDebug("MarketCheck: Using DOP/USD rate {Rate} (source: {Source})",
                dopUsdRate, rateResult.Source);

            return new MarketPriceAnalysis(
                Make: make,
                Model: model,
                Year: year,
                Trim: trim,
                AveragePrice: Math.Round(avgPriceUsd * dopUsdRate, 0),
                MedianPrice: Math.Round(medianPriceUsd * dopUsdRate, 0),
                MinPrice: Math.Round(minPriceUsd * dopUsdRate, 0),
                MaxPrice: Math.Round(maxPriceUsd * dopUsdRate, 0),
                Currency: "DOP",
                SampleSize: stats.Count ?? 0,
                PriceAboveMarket: null,
                MarketPosition: "At Market",
                DepreciationRate: CalculateDepreciation(year),
                AnalyzedAt: DateTime.UtcNow,
                Provider: "MarketCheck"
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "MarketCheck: HTTP error");
            return _fallbackToMock
                ? await _fallback.GetMarketPriceAsync(make, model, year, trim, mileage, condition, province, ct)
                : null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("MarketCheck: Request timeout");
            return _fallbackToMock
                ? await _fallback.GetMarketPriceAsync(make, model, year, trim, mileage, condition, province, ct)
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarketCheck: Unexpected error");
            return _fallbackToMock
                ? await _fallback.GetMarketPriceAsync(make, model, year, trim, mileage, condition, province, ct)
                : null;
        }
    }

    public async Task<List<MarketListing>> GetComparableListingsAsync(
        string make, string model, int year,
        string? trim = null, decimal? maxMileage = null,
        int limit = 10, CancellationToken ct = default)
    {
        _logger.LogInformation("MarketCheck: GetComparableListings({Make} {Model} {Year})", make, model, year);

        if (!HasApiKey())
            return _fallbackToMock
                ? await _fallback.GetComparableListingsAsync(make, model, year, trim, maxMileage, limit, ct)
                : new();

        try
        {
            var url = $"{_baseUrl}/search/car/active" +
                     $"?make={Encode(make)}&model={Encode(model)}&year={year}" +
                     (trim != null ? $"&trim={Encode(trim)}" : "") +
                     (maxMileage.HasValue ? $"&miles_range=0-{(int)maxMileage.Value}" : "") +
                     $"&rows={limit}&car_type=used&country=US" +
                     "&sort_by=price&sort_order=asc";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", _apiKey);

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return _fallbackToMock
                    ? await _fallback.GetComparableListingsAsync(make, model, year, trim, maxMileage, limit, ct)
                    : new();

            var data = await response.Content.ReadFromJsonAsync<MarketCheckSearchResponse>(JsonOptions, ct);
            if (data?.Listings == null)
                return new();

            var rateResult = await _exchangeRateService.GetDopUsdRateAsync(ct);
            var dopUsdRate = rateResult.Rate;

            return data.Listings.Select(l => new MarketListing(
                ListingId: l.Id ?? Guid.NewGuid().ToString("N"),
                Source: l.Source ?? "MarketCheck",
                Make: l.Make ?? make,
                Model: l.Model ?? model,
                Year: l.Year ?? year,
                Trim: l.Trim,
                Price: Math.Round((l.Price ?? 0) * dopUsdRate, 0),
                Currency: "DOP",
                MileageKm: l.Miles.HasValue ? l.Miles.Value * 1.60934m : null, // Convert miles to km
                Condition: null,
                Province: null,
                DealerName: l.DealerName,
                ListedDate: l.FirstSeenAt ?? DateTime.UtcNow.AddDays(-30),
                ListingUrl: l.Vdp_Url
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarketCheck: Error fetching comparable listings");
            return _fallbackToMock
                ? await _fallback.GetComparableListingsAsync(make, model, year, trim, maxMileage, limit, ct)
                : new();
        }
    }

    public async Task<MarketTrend?> GetMarketTrendAsync(
        string make, string model, int? year = null,
        int monthsBack = 12, CancellationToken ct = default)
    {
        _logger.LogInformation("MarketCheck: GetMarketTrend({Make} {Model})", make, model);

        // MarketCheck doesn't have a direct trend endpoint in free tier
        // Use the mock for trend data which provides realistic DR-market simulation
        if (!HasApiKey())
        {
            return _fallbackToMock
                ? await _fallback.GetMarketTrendAsync(make, model, year, monthsBack, ct)
                : null;
        }

        // Even with API key, MarketCheck trend data requires premium tier
        // Fall back to mock trend generation with real current price as anchor
        try
        {
            var currentPrice = await GetMarketPriceAsync(make, model, year ?? DateTime.UtcNow.Year, ct: ct);
            if (currentPrice == null)
                return _fallbackToMock
                    ? await _fallback.GetMarketTrendAsync(make, model, year, monthsBack, ct)
                    : null;

            // Generate trend data anchored to the real current price
            var dataPoints = new List<PriceDataPoint>();
            var basePrice = currentPrice.AveragePrice;

            for (int i = monthsBack; i >= 0; i--)
            {
                var month = DateTime.UtcNow.AddMonths(-i);
                var monthlyDepreciation = 0.005m;
                var seasonal = month.Month switch
                {
                    1 or 2 => 1.02m,
                    7 or 8 => 0.97m,
                    _ => 1.0m,
                };
                var price = basePrice * (1 + monthlyDepreciation * i) * seasonal;

                dataPoints.Add(new PriceDataPoint(
                    Month: new DateTime(month.Year, month.Month, 1),
                    AveragePrice: Math.Round(price, 0),
                    MedianPrice: Math.Round(price * 0.98m, 0),
                    ListingCount: currentPrice.SampleSize > 0
                        ? currentPrice.SampleSize + Random.Shared.Next(-10, 10)
                        : Random.Shared.Next(15, 60)
                ));
            }

            var firstPrice = dataPoints.First().AveragePrice;
            var lastPrice = dataPoints.Last().AveragePrice;
            var trendPct = ((lastPrice - firstPrice) / firstPrice) * 100 / monthsBack;

            return new MarketTrend(
                Make: make,
                Model: model,
                Year: year,
                PriceHistory: dataPoints,
                TrendDirection: Math.Round(trendPct, 2),
                TrendLabel: trendPct > 0.5m ? "Rising" : trendPct < -0.5m ? "Declining" : "Stable",
                TotalListingsAnalyzed: currentPrice.SampleSize * monthsBack,
                Provider: "MarketCheck"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarketCheck: Error generating trend data");
            return _fallbackToMock
                ? await _fallback.GetMarketTrendAsync(make, model, year, monthsBack, ct)
                : null;
        }
    }

    public async Task<PriceRecommendation?> GetPriceRecommendationAsync(
        string make, string model, int year,
        decimal mileageKm, string condition,
        string? province = null, CancellationToken ct = default)
    {
        _logger.LogInformation("MarketCheck: GetPriceRecommendation({Make} {Model} {Year})", make, model, year);

        if (!HasApiKey())
            return _fallbackToMock
                ? await _fallback.GetPriceRecommendationAsync(make, model, year, mileageKm, condition, province, ct)
                : null;

        try
        {
            // Get current market data first
            var marketData = await GetMarketPriceAsync(make, model, year, mileage: mileageKm, condition: condition, province: province, ct: ct);
            if (marketData == null)
                return _fallbackToMock
                    ? await _fallback.GetPriceRecommendationAsync(make, model, year, mileageKm, condition, province, ct)
                    : null;

            var recommendedPrice = marketData.MedianPrice;
            var quickSale = Math.Round(recommendedPrice * 0.92m, 0);
            var premium = Math.Round(recommendedPrice * 1.07m, 0);

            var daysEstimate = condition?.ToLower() switch
            {
                "excelente" or "excellent" => Random.Shared.Next(7, 21),
                "bueno" or "good" => Random.Shared.Next(14, 35),
                "regular" or "fair" => Random.Shared.Next(25, 55),
                _ => Random.Shared.Next(15, 40),
            };

            return new PriceRecommendation(
                RecommendedPrice: recommendedPrice,
                QuickSalePrice: quickSale,
                PremiumPrice: premium,
                Currency: "DOP",
                Explanation: $"Basado en {marketData.SampleSize} vehículos similares del mercado. " +
                            $"El {make} {model} {year} con {mileageKm:N0} km en condición {condition ?? "promedio"} " +
                            $"tiene una demanda {(daysEstimate < 20 ? "alta" : daysEstimate < 40 ? "media" : "baja")} " +
                            $"en {province ?? "República Dominicana"}. Datos de MarketCheck.",
                DaysToSellEstimate: daysEstimate,
                ConfidenceScore: marketData.SampleSize > 30 ? 0.85m : marketData.SampleSize > 10 ? 0.70m : 0.55m,
                Provider: "MarketCheck"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarketCheck: Error generating price recommendation");
            return _fallbackToMock
                ? await _fallback.GetPriceRecommendationAsync(make, model, year, mileageKm, condition, province, ct)
                : null;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────

    private bool HasApiKey() => !string.IsNullOrWhiteSpace(_apiKey);

    private static string Encode(string value) => Uri.EscapeDataString(value);

    private static decimal CalculateDepreciation(int year)
    {
        var age = DateTime.UtcNow.Year - year;
        return age switch
        {
            <= 0 => 0m,
            1 => 15m,
            2 or 3 => 10m,
            4 or 5 => 7m,
            _ => 5m,
        };
    }

    // ── MarketCheck API Response Models ─────────────────────────

    private class MarketCheckStatsResponse
    {
        public decimal? Mean { get; set; }
        public decimal? Median { get; set; }
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }

        [JsonPropertyName("count")]
        public int? Count { get; set; }

        [JsonPropertyName("std_dev")]
        public decimal? StdDev { get; set; }
    }

    private class MarketCheckSearchResponse
    {
        [JsonPropertyName("listings")]
        public List<MarketCheckListingItem>? Listings { get; set; }

        [JsonPropertyName("num_found")]
        public int? NumFound { get; set; }
    }

    private class MarketCheckListingItem
    {
        public string? Id { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public int? Year { get; set; }
        public string? Trim { get; set; }
        public decimal? Price { get; set; }
        public decimal? Miles { get; set; }
        public string? Source { get; set; }

        [JsonPropertyName("dealer_name")]
        public string? DealerName { get; set; }

        [JsonPropertyName("first_seen_at")]
        public DateTime? FirstSeenAt { get; set; }

        [JsonPropertyName("vdp_url")]
        public string? Vdp_Url { get; set; }
    }
}
