using Microsoft.Extensions.Logging;
using VehiclesSaleService.Application.Interfaces;

namespace VehiclesSaleService.Infrastructure.External;

/// <summary>
/// Mock implementation of IMarketPriceService (MarketCheck API).
/// Returns realistic DR-market pricing data based on known vehicle values.
/// 
/// TO SWAP FOR REAL API:
/// 1. Create MarketCheckPriceService in this folder
/// 2. Implement IMarketPriceService using the real MarketCheck API
/// 3. Change DI registration in Program.cs:
///    builder.Services.AddHttpClient&lt;IMarketPriceService, MarketCheckPriceService&gt;(...)
/// 4. Add API key to appsettings/secrets:
///    "MarketPrice": { "Provider": "MarketCheck", "ApiKey": "xxx", "BaseUrl": "https://api.marketcheck.com" }
/// </summary>
public class MockMarketPriceService : IMarketPriceService
{
    private readonly ILogger<MockMarketPriceService> _logger;
    private readonly IExchangeRateService _exchangeRateService;
    private static readonly Random _rng = new();

    // Base USD prices for popular DR vehicles (2024 model year baseline)
    private static readonly Dictionary<string, decimal> BasePricesUsd = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Toyota_Corolla"] = 22_000m,
        ["Toyota_RAV4"] = 30_000m,
        ["Toyota_Camry"] = 27_000m,
        ["Toyota_Hilux"] = 35_000m,
        ["Toyota_Yaris"] = 18_000m,
        ["Toyota_Prado"] = 55_000m,
        ["Honda_Civic"] = 24_000m,
        ["Honda_CR-V"] = 32_000m,
        ["Honda_Accord"] = 28_000m,
        ["Honda_HR-V"] = 25_000m,
        ["Hyundai_Tucson"] = 29_000m,
        ["Hyundai_Elantra"] = 22_000m,
        ["Hyundai_Santa Fe"] = 35_000m,
        ["Kia_Sportage"] = 30_000m,
        ["Kia_Forte"] = 20_000m,
        ["Kia_Seltos"] = 24_000m,
        ["Nissan_Kicks"] = 22_000m,
        ["Nissan_Sentra"] = 20_000m,
        ["Nissan_X-Trail"] = 30_000m,
        ["Mitsubishi_Outlander"] = 29_000m,
        ["Jeep_Wrangler"] = 38_000m,
        ["Jeep_Grand Cherokee"] = 42_000m,
        ["BMW_X3"] = 47_000m,
        ["BMW_X5"] = 63_000m,
        ["Mercedes_GLC"] = 48_000m,
        ["Ford_Explorer"] = 36_000m,
    };

    public MockMarketPriceService(ILogger<MockMarketPriceService> logger, IExchangeRateService exchangeRateService)
    {
        _logger = logger;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<MarketPriceAnalysis?> GetMarketPriceAsync(
        string make, string model, int year,
        string? trim = null, decimal? mileage = null,
        string? condition = null, string? province = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] MarketPrice: GetMarketPrice({Make} {Model} {Year})", make, model, year);
        await Task.Delay(200 + _rng.Next(300), ct);

        var basePrice = GetBasePrice(make, model);
        var adjustedPrice = AdjustForYear(basePrice, year);
        adjustedPrice = AdjustForMileage(adjustedPrice, mileage);
        adjustedPrice = AdjustForCondition(adjustedPrice, condition);
        adjustedPrice = AdjustForProvince(adjustedPrice, province);

        var variance = adjustedPrice * 0.12m; // ±12% market variance
        var minPrice = adjustedPrice - variance;
        var maxPrice = adjustedPrice + variance;

        // Convert to DOP using live BCRD rate
        var rateResult = await _exchangeRateService.GetDopUsdRateAsync(ct);
        var dopUsdRate = rateResult.Rate;
        var avgDop = adjustedPrice * dopUsdRate;
        var minDop = minPrice * dopUsdRate;
        var maxDop = maxPrice * dopUsdRate;
        var medianDop = avgDop * (1 + (_rng.Next(-5, 5) * 0.01m));

        return new MarketPriceAnalysis(
            Make: make,
            Model: model,
            Year: year,
            Trim: trim,
            AveragePrice: Math.Round(avgDop, 0),
            MedianPrice: Math.Round(medianDop, 0),
            MinPrice: Math.Round(minDop, 0),
            MaxPrice: Math.Round(maxDop, 0),
            Currency: "DOP",
            SampleSize: _rng.Next(15, 85),
            PriceAboveMarket: null,
            MarketPosition: "At Market",
            DepreciationRate: GetDepreciationRate(year),
            AnalyzedAt: DateTime.UtcNow,
            Provider: "Mock"
        );
    }

    public async Task<List<MarketListing>> GetComparableListingsAsync(
        string make, string model, int year,
        string? trim = null, decimal? maxMileage = null,
        int limit = 10, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] MarketPrice: GetComparableListings({Make} {Model} {Year})", make, model, year);
        await Task.Delay(250 + _rng.Next(250), ct);

        var basePrice = GetBasePrice(make, model);
        var listings = new List<MarketListing>();

        var sources = new[] { "OKLA", "Facebook", "CoroMotors", "SuperCarros", "AutoPlaza RD" };
        var provinces = new[] { "Santo Domingo", "Santiago", "La Vega", "San Cristóbal", "Puerto Plata", "La Romana" };
        var conditions = new[] { "Excelente", "Bueno", "Regular" };

        var rateResult = await _exchangeRateService.GetDopUsdRateAsync(ct);
        var dopUsdRate = rateResult.Rate;

        for (int i = 0; i < Math.Min(limit, 15); i++)
        {
            var listYear = year + _rng.Next(-1, 2);
            var adjusted = AdjustForYear(basePrice, listYear);
            var mileage = 5_000m + _rng.Next(0, 150_000);
            adjusted = AdjustForMileage(adjusted, mileage);
            var priceDop = adjusted * dopUsdRate * (1 + (_rng.Next(-15, 15) * 0.01m));

            listings.Add(new MarketListing(
                ListingId: $"mock-{Guid.NewGuid():N}",
                Source: sources[_rng.Next(sources.Length)],
                Make: make,
                Model: model,
                Year: listYear,
                Trim: trim,
                Price: Math.Round(priceDop, 0),
                Currency: "DOP",
                MileageKm: mileage,
                Condition: conditions[_rng.Next(conditions.Length)],
                Province: provinces[_rng.Next(provinces.Length)],
                DealerName: _rng.Next(2) == 0 ? DealerNames[_rng.Next(DealerNames.Length)] : null,
                ListedDate: DateTime.UtcNow.AddDays(-_rng.Next(1, 90)),
                ListingUrl: null
            ));
        }

        return listings.OrderByDescending(l => l.ListedDate).ToList();
    }

    public async Task<MarketTrend?> GetMarketTrendAsync(
        string make, string model, int? year = null,
        int monthsBack = 12, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] MarketPrice: GetMarketTrend({Make} {Model})", make, model);
        await Task.Delay(200 + _rng.Next(200), ct);

        var rateResult = await _exchangeRateService.GetDopUsdRateAsync(ct);
        var dopUsdRate = rateResult.Rate;
        var basePrice = GetBasePrice(make, model) * dopUsdRate;
        var dataPoints = new List<PriceDataPoint>();

        // Simulate slight depreciation over time with seasonal variation
        for (int i = monthsBack; i >= 0; i--)
        {
            var month = DateTime.UtcNow.AddMonths(-i);
            var monthlyDepreciation = 0.005m; // 0.5% monthly depreciation
            var seasonal = i % 12 switch
            {
                0 or 1 => 1.02m,  // Jan-Feb slight premium (tax refund season)
                6 or 7 => 0.97m,  // Jul-Aug slight discount (slow season)
                _ => 1.0m,
            };
            var price = basePrice * (1 - monthlyDepreciation * i) * seasonal;
            var variance = price * 0.05m;

            dataPoints.Add(new PriceDataPoint(
                Month: new DateTime(month.Year, month.Month, 1),
                AveragePrice: Math.Round(price, 0),
                MedianPrice: Math.Round(price * (1 + (_rng.Next(-3, 3) * 0.01m)), 0),
                ListingCount: _rng.Next(10, 60)
            ));
        }

        // Calculate overall trend direction
        var firstPrice = dataPoints.First().AveragePrice;
        var lastPrice = dataPoints.Last().AveragePrice;
        var trendPct = firstPrice != 0 && monthsBack != 0
            ? ((lastPrice - firstPrice) / firstPrice) * 100 / monthsBack
            : 0m;

        return new MarketTrend(
            Make: make,
            Model: model,
            Year: year,
            PriceHistory: dataPoints,
            TrendDirection: Math.Round(trendPct, 2),
            TrendLabel: trendPct > 0.5m ? "Rising" : trendPct < -0.5m ? "Declining" : "Stable",
            TotalListingsAnalyzed: _rng.Next(200, 1500),
            Provider: "Mock"
        );
    }

    public async Task<PriceRecommendation?> GetPriceRecommendationAsync(
        string make, string model, int year,
        decimal mileageKm, string condition,
        string? province = null, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] MarketPrice: GetPriceRecommendation({Make} {Model} {Year})", make, model, year);
        await Task.Delay(300 + _rng.Next(200), ct);

        var basePrice = GetBasePrice(make, model);
        var adjusted = AdjustForYear(basePrice, year);
        adjusted = AdjustForMileage(adjusted, mileageKm);
        adjusted = AdjustForCondition(adjusted, condition);
        adjusted = AdjustForProvince(adjusted, province);

        var rateResult = await _exchangeRateService.GetDopUsdRateAsync(ct);
        var dopUsdRate = rateResult.Rate;
        var recommendedDop = Math.Round(adjusted * dopUsdRate, 0);
        var quickSale = Math.Round(recommendedDop * 0.92m, 0);   // 8% below
        var premium = Math.Round(recommendedDop * 1.07m, 0);     // 7% above

        var daysEstimate = condition?.ToLower() switch
        {
            "excelente" or "excellent" => _rng.Next(7, 21),
            "bueno" or "good" => _rng.Next(14, 35),
            "regular" or "fair" => _rng.Next(25, 55),
            _ => _rng.Next(15, 40),
        };

        return new PriceRecommendation(
            RecommendedPrice: recommendedDop,
            QuickSalePrice: quickSale,
            PremiumPrice: premium,
            Currency: "DOP",
            Explanation: $"Basado en {_rng.Next(20, 80)} vehículos similares en el mercado dominicano. " +
                         $"El {make} {model} {year} con {mileageKm:N0} km en condición {condition ?? "promedio"} " +
                         $"tiene una demanda {(daysEstimate < 20 ? "alta" : daysEstimate < 40 ? "media" : "baja")} en {province ?? "RD"}.",
            DaysToSellEstimate: daysEstimate,
            ConfidenceScore: Math.Round(0.65m + (_rng.Next(0, 25) * 0.01m), 2),
            Provider: "Mock"
        );
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static decimal GetBasePrice(string make, string model)
    {
        var key = $"{make}_{model}";
        return BasePricesUsd.TryGetValue(key, out var price) ? price : 25_000m;
    }

    private static decimal AdjustForYear(decimal basePrice, int year)
    {
        var age = DateTime.UtcNow.Year - year;
        if (age <= 0) return basePrice;
        // Depreciation: ~15% year 1, ~10% years 2-3, ~7% years 4-5, ~5% after
        decimal totalDepreciation = 0;
        for (int y = 1; y <= age; y++)
        {
            totalDepreciation += y switch
            {
                1 => 0.15m,
                2 or 3 => 0.10m,
                4 or 5 => 0.07m,
                _ => 0.05m,
            };
        }
        return basePrice * Math.Max(0.15m, 1 - totalDepreciation);
    }

    private static decimal AdjustForMileage(decimal price, decimal? mileageKm)
    {
        if (mileageKm is null or <= 0) return price;
        // Deduct ~$0.03 per km above 15,000 km/year baseline
        var excessKm = Math.Max(0, mileageKm.Value - 15_000);
        return price - (excessKm * 0.03m);
    }

    private static decimal AdjustForCondition(decimal price, string? condition)
    {
        return condition?.ToLower() switch
        {
            "excelente" or "excellent" => price * 1.05m,
            "bueno" or "good" => price,
            "regular" or "fair" => price * 0.90m,
            "malo" or "poor" => price * 0.75m,
            _ => price,
        };
    }

    private static decimal AdjustForProvince(decimal price, string? province)
    {
        // Santo Domingo and Santiago have slightly higher prices
        return province?.ToLower() switch
        {
            "santo domingo" or "distrito nacional" => price * 1.03m,
            "santiago" => price * 1.02m,
            "punta cana" or "la altagracia" => price * 1.05m,
            _ => price,
        };
    }

    private static decimal GetDepreciationRate(int year)
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

    private static readonly string[] DealerNames =
    {
        "AutoMax RD", "Toyota RD Autorizado", "Honda Place RD",
        "Hyundai Gallery", "RD Imports", "CoroMotors Santiago",
        "Auto Elite SD", "Premium Cars RD", "Zona de Autos",
    };
}
