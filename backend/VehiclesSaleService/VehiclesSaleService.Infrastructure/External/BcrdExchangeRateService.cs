using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VehiclesSaleService.Application.Interfaces;

namespace VehiclesSaleService.Infrastructure.External;

/// <summary>
/// Live exchange-rate provider that fetches DOP/USD from the Banco Central
/// de la República Dominicana (BCRD) public API.
///
/// Fallback chain (never throws):
///   1. In-memory cache (TTL configurable, default 4 hours)
///   2. Last successfully-fetched rate (persisted across cache evictions)
///   3. Hardcoded constant as absolute last resort
///
/// Configuration keys:
///   ExternalApis:ExchangeRate:BaseUrl          (default: https://api.bancentral.gov.do)
///   ExternalApis:ExchangeRate:CacheDurationMin (default: 240 = 4 hours)
///
/// BCRD public endpoint returns JSON with DOP buy/sell rates.
/// We use the "venta" (sell) rate because that's what consumers pay to convert USD → DOP.
/// </summary>
public class BcrdExchangeRateService : IExchangeRateService
{
    private const string CacheKey = "bcrd:dop_usd_rate";

    /// <summary>
    /// Hardcoded fallback rate. Updated manually when BCRD API is unavailable
    /// for extended periods. Last verified: 2026-03-07.
    /// </summary>
    private const decimal FallbackRate = 60.50m;

    private readonly HttpClient _httpClient;
    private readonly ILogger<BcrdExchangeRateService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _baseUrl;
    private readonly TimeSpan _cacheDuration;

    /// <summary>
    /// Stores the last successfully-fetched rate so it survives cache eviction.
    /// Protected by <see cref="_lastRateLock"/>.
    /// </summary>
    private static ExchangeRateResult? _lastSuccessfulRate;
    private static readonly object _lastRateLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public BcrdExchangeRateService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<BcrdExchangeRateService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClientFactory.CreateClient("BCRD");
        _logger = logger;
        _cache = cache;
        _baseUrl = configuration["ExternalApis:ExchangeRate:BaseUrl"]
                   ?? "https://api.bancentral.gov.do";
        var minutes = int.TryParse(
            configuration["ExternalApis:ExchangeRate:CacheDurationMin"],
            out var m) ? m : 240;
        _cacheDuration = TimeSpan.FromMinutes(minutes);
    }

    /// <inheritdoc />
    public async Task<ExchangeRateResult> GetDopUsdRateAsync(CancellationToken ct = default)
    {
        // 1️⃣ Try in-memory cache
        if (_cache.TryGetValue(CacheKey, out ExchangeRateResult? cached) && cached is not null)
        {
            _logger.LogDebug("BCRD: Returning cached DOP/USD rate {Rate} (fetched {FetchedAt})",
                cached.Rate, cached.FetchedAt);
            return cached;
        }

        // 2️⃣ Try live fetch from BCRD
        try
        {
            var rate = await FetchLiveRateAsync(ct);
            if (rate is not null)
            {
                var result = new ExchangeRateResult(rate.Value, DateTimeOffset.UtcNow, ExchangeRateSource.Live);

                _cache.Set(CacheKey, result, _cacheDuration);
                lock (_lastRateLock) { _lastSuccessfulRate = result; }

                _logger.LogInformation("BCRD: Fetched live DOP/USD rate {Rate}", rate.Value);
                return result;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "BCRD: Failed to fetch live exchange rate");
        }

        // 3️⃣ Fall back to last successful rate
        ExchangeRateResult? lastGood;
        lock (_lastRateLock) { lastGood = _lastSuccessfulRate; }
        if (lastGood is not null)
        {
            var cachedResult = lastGood with { Source = ExchangeRateSource.Cached };
            _cache.Set(CacheKey, cachedResult, TimeSpan.FromMinutes(30)); // shorter TTL for stale
            _logger.LogWarning("BCRD: Using last-known rate {Rate} (fetched {FetchedAt})",
                lastGood.Rate, lastGood.FetchedAt);
            return cachedResult;
        }

        // 4️⃣ Absolute fallback — hardcoded constant
        _logger.LogError("BCRD: No cached rate available, using hardcoded fallback {Rate}", FallbackRate);
        var fallback = new ExchangeRateResult(FallbackRate, DateTimeOffset.MinValue, ExchangeRateSource.Fallback);
        _cache.Set(CacheKey, fallback, TimeSpan.FromMinutes(15)); // retry sooner
        return fallback;
    }

    // ── Private helpers ─────────────────────────────────────────

    /// <summary>
    /// Fetches the live DOP/USD sell rate from the BCRD public API.
    /// The BCRD provides a JSON endpoint with buy/sell rates for major currencies.
    /// Returns null if the fetch fails or the response is unparseable.
    /// </summary>
    private async Task<decimal?> FetchLiveRateAsync(CancellationToken ct)
    {
        // BCRD public API endpoint for exchange rates
        // Returns JSON array with currency rates including DOP/USD
        var url = $"{_baseUrl}/TCSV/TCUSD";

        _logger.LogDebug("BCRD: Fetching live rate from {Url}", url);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/json");

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("BCRD: API returned {StatusCode}", response.StatusCode);

            // Try alternative endpoint (some BCRD implementations use different paths)
            return await TryAlternativeEndpointAsync(ct);
        }

        var content = await response.Content.ReadAsStringAsync(ct);

        // Try to parse the BCRD response — format varies by endpoint
        return ParseBcrdResponse(content);
    }

    /// <summary>
    /// Alternative endpoint in case the primary one is down or changed.
    /// BCRD occasionally updates their API paths.
    /// </summary>
    private async Task<decimal?> TryAlternativeEndpointAsync(CancellationToken ct)
    {
        try
        {
            // Alternative: BCRD statistics page endpoint
            var altUrl = $"{_baseUrl}/estadisticas/mercado-cambiario/tasas-de-cambio";

            using var request = new HttpRequestMessage(HttpMethod.Get, altUrl);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("BCRD: Alternative endpoint also returned {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return ParseBcrdResponse(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BCRD: Alternative endpoint failed");
            return null;
        }
    }

    /// <summary>
    /// Attempts to extract the USD sell rate (venta) from various BCRD JSON response formats.
    /// BCRD API responses are not always consistent, so we try multiple parsing strategies.
    /// </summary>
    private decimal? ParseBcrdResponse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Strategy 1: Direct object with "venta" / "sell" / "rate" property
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetDecimalProperty(root, "venta", out var venta))
                    return venta;
                if (TryGetDecimalProperty(root, "sell", out var sell))
                    return sell;
                if (TryGetDecimalProperty(root, "rate", out var rate))
                    return rate;
                if (TryGetDecimalProperty(root, "valor", out var valor))
                    return valor;

                // Check nested "data" object
                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                {
                    if (TryGetDecimalProperty(data, "venta", out var dataVenta))
                        return dataVenta;
                }
            }

            // Strategy 2: Array of currency objects — find USD
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    var currency = GetStringProperty(item, "moneda")
                                ?? GetStringProperty(item, "currency")
                                ?? GetStringProperty(item, "code");

                    if (currency?.Contains("USD", StringComparison.OrdinalIgnoreCase) == true
                        || currency?.Contains("dolar", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (TryGetDecimalProperty(item, "venta", out var arrVenta))
                            return arrVenta;
                        if (TryGetDecimalProperty(item, "sell", out var arrSell))
                            return arrSell;
                        if (TryGetDecimalProperty(item, "valor", out var arrValor))
                            return arrValor;
                    }
                }
            }

            _logger.LogWarning("BCRD: Could not parse exchange rate from response: {ContentPreview}",
                content.Length > 300 ? content[..300] + "..." : content);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "BCRD: JSON parse error");
            return null;
        }
    }

    private static bool TryGetDecimalProperty(JsonElement element, string propertyName, out decimal value)
    {
        value = 0;
        if (!element.TryGetProperty(propertyName, out var prop))
            return false;

        if (prop.ValueKind == JsonValueKind.Number)
            return prop.TryGetDecimal(out value);

        if (prop.ValueKind == JsonValueKind.String)
            return decimal.TryParse(prop.GetString(), out value);

        return false;
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }
}
