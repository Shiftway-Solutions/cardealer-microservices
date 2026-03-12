namespace DealerAnalyticsService.Domain.Entities;

public class MarketBenchmark
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string VehicleCategory { get; set; } = string.Empty; // SUV, Sedan, etc.
    public string PriceRange { get; set; } = string.Empty; // 0-1M, 1-2M, etc.

    // Métricas de mercado (promedio anónimo)
    public decimal MarketAveragePrice { get; set; }
    public decimal MarketAverageDaysOnMarket { get; set; }
    public decimal MarketAverageViews { get; set; }
    public decimal MarketConversionRate { get; set; }

    // Percentiles
    public decimal PricePercentile25 { get; set; }
    public decimal PricePercentile50 { get; set; }
    public decimal PricePercentile75 { get; set; }

    public int TotalDealersInSample { get; set; }
    public int TotalVehiclesInSample { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
