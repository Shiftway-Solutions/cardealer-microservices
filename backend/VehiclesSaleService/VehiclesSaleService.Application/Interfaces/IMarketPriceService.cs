namespace VehiclesSaleService.Application.Interfaces;

/// <summary>
/// Market price comparison provider (MarketCheck API integration).
/// Current implementation returns simulated DR-market data for testing.
/// Replace with real API client by swapping DI registration.
/// </summary>
public interface IMarketPriceService
{
    /// <summary>
    /// Gets market price comparison for a vehicle.
    /// </summary>
    Task<MarketPriceAnalysis?> GetMarketPriceAsync(
        string make, string model, int year,
        string? trim = null, decimal? mileage = null,
        string? condition = null, string? province = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets comparable listings in the market for a given vehicle.
    /// </summary>
    Task<List<MarketListing>> GetComparableListingsAsync(
        string make, string model, int year,
        string? trim = null, decimal? maxMileage = null,
        int limit = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Gets market trends (average price over time) for a make/model.
    /// </summary>
    Task<MarketTrend?> GetMarketTrendAsync(
        string make, string model, int? year = null,
        int monthsBack = 12,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a price recommendation for listing a vehicle.
    /// </summary>
    Task<PriceRecommendation?> GetPriceRecommendationAsync(
        string make, string model, int year,
        decimal mileageKm, string condition,
        string? province = null,
        CancellationToken ct = default);
}

// ── DTOs ──────────────────────────────────────────────────────────

public record MarketPriceAnalysis(
    string Make,
    string Model,
    int Year,
    string? Trim,
    decimal AveragePrice,
    decimal MedianPrice,
    decimal MinPrice,
    decimal MaxPrice,
    string Currency,             // "DOP" | "USD"
    int SampleSize,
    decimal? PriceAboveMarket,   // positive = above avg, negative = below
    string MarketPosition,       // "Below Market" | "At Market" | "Above Market"
    decimal? DepreciationRate,   // annual % depreciation
    DateTime AnalyzedAt,
    string Provider              // "MarketCheck" | "Mock"
);

public record MarketListing(
    string ListingId,
    string Source,               // "OKLA" | "Facebook" | "AutoTrader" | "CoroMotors" | "Mock"
    string Make,
    string Model,
    int Year,
    string? Trim,
    decimal Price,
    string Currency,
    decimal? MileageKm,
    string? Condition,
    string? Province,
    string? DealerName,
    DateTime ListedDate,
    string? ListingUrl
);

public record MarketTrend(
    string Make,
    string Model,
    int? Year,
    List<PriceDataPoint> PriceHistory,
    decimal TrendDirection,      // +1.5 = 1.5% monthly increase, -2.3 = 2.3% monthly decrease
    string TrendLabel,           // "Rising" | "Stable" | "Declining"
    int TotalListingsAnalyzed,
    string Provider
);

public record PriceDataPoint(
    DateTime Month,
    decimal AveragePrice,
    decimal MedianPrice,
    int ListingCount
);

public record PriceRecommendation(
    decimal RecommendedPrice,
    decimal QuickSalePrice,      // 5-10% below recommended
    decimal PremiumPrice,        // 5-10% above recommended
    string Currency,
    string Explanation,
    int DaysToSellEstimate,
    decimal ConfidenceScore,     // 0-1
    string Provider
);
