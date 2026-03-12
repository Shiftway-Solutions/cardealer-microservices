namespace DealerAnalyticsService.Domain.Entities;

/// <summary>
/// Benchmark de un dealer contra el mercado
/// Permite comparar performance y generar rankings
/// </summary>
public class DealerBenchmark
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public DateTime Date { get; set; }
    public string Period { get; set; } = "Monthly"; // Daily, Weekly, Monthly

    // Dealer's Metrics
    public double AvgDaysOnMarket { get; set; }
    public double ConversionRate { get; set; }
    public double AvgResponseTimeMinutes { get; set; }
    public double CustomerSatisfaction { get; set; } // 0-5 rating
    public double ListingQualityScore { get; set; } // 0-100
    public decimal AvgVehiclePrice { get; set; }
    public int ActiveListings { get; set; }
    public int MonthlySales { get; set; }
    public double ViewsPerListing { get; set; }
    public double ContactsPerListing { get; set; }

    // Market Averages (calculated from all dealers)
    public double MarketAvgDaysOnMarket { get; set; }
    public double MarketAvgConversionRate { get; set; }
    public double MarketAvgResponseTime { get; set; }
    public double MarketAvgSatisfaction { get; set; }
    public double MarketAvgListingQuality { get; set; }
    public decimal MarketAvgPrice { get; set; }
    public double MarketAvgViewsPerListing { get; set; }
    public double MarketAvgContactsPerListing { get; set; }

    // Percentile Rankings (0-100, higher is better)
    public int DaysOnMarketPercentile { get; set; }
    public int ConversionRatePercentile { get; set; }
    public int ResponseTimePercentile { get; set; }
    public int SatisfactionPercentile { get; set; }
    public int ListingQualityPercentile { get; set; }
    public int SalesPercentile { get; set; }
    public int EngagementPercentile { get; set; }

    // Overall Rankings
    public int OverallRank { get; set; }
    public int TotalDealers { get; set; }
    public int CategoryRank { get; set; } // Rank within same category/size
    public int TotalInCategory { get; set; }

    // Comparison Indicators
    public bool IsBetterThanMarketDaysOnMarket => AvgDaysOnMarket < MarketAvgDaysOnMarket;
    public bool IsBetterThanMarketConversion => ConversionRate > MarketAvgConversionRate;
    public bool IsBetterThanMarketResponseTime => AvgResponseTimeMinutes < MarketAvgResponseTime;
    public bool IsBetterThanMarketSatisfaction => CustomerSatisfaction > MarketAvgSatisfaction;

    // Performance Tier
    public DealerTier Tier { get; set; }
    public string TierName => Tier.ToString();

    // Improvement Opportunities
    public List<string> ImprovementAreas { get; set; } = new();
    public List<string> Strengths { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Constructor
    public DealerBenchmark()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calcula el tier basado en percentiles
    /// </summary>
    public void CalculateTier()
    {
        var avgPercentile = (
            ConversionRatePercentile +
            ResponseTimePercentile +
            SatisfactionPercentile +
            ListingQualityPercentile +
            EngagementPercentile
        ) / 5.0;

        Tier = avgPercentile switch
        {
            >= 90 => DealerTier.Diamond,
            >= 75 => DealerTier.Platinum,
            >= 50 => DealerTier.Gold,
            >= 25 => DealerTier.Silver,
            _ => DealerTier.Bronze
        };
    }

    /// <summary>
    /// Identifica áreas de mejora y fortalezas
    /// </summary>
    public void AnalyzePerformance()
    {
        ImprovementAreas.Clear();
        Strengths.Clear();

        // Response Time
        if (ResponseTimePercentile < 25)
            ImprovementAreas.Add("Tiempo de respuesta muy lento. Objetivo: responder en menos de 30 minutos");
        else if (ResponseTimePercentile >= 75)
            Strengths.Add("Excelente tiempo de respuesta");

        // Conversion
        if (ConversionRatePercentile < 25)
            ImprovementAreas.Add("Tasa de conversión baja. Revise calidad de leads y seguimiento");
        else if (ConversionRatePercentile >= 75)
            Strengths.Add("Alta tasa de conversión");

        // Days on Market
        if (DaysOnMarketPercentile < 25)
            ImprovementAreas.Add("Vehículos permanecen mucho tiempo en inventario. Considere ajustar precios");
        else if (DaysOnMarketPercentile >= 75)
            Strengths.Add("Rápida rotación de inventario");

        // Satisfaction
        if (SatisfactionPercentile < 25)
            ImprovementAreas.Add("Satisfacción del cliente baja. Mejore la experiencia de compra");
        else if (SatisfactionPercentile >= 75)
            Strengths.Add("Clientes muy satisfechos");

        // Listing Quality
        if (ListingQualityPercentile < 25)
            ImprovementAreas.Add("Calidad de listings baja. Agregue más fotos y descripciones detalladas");
        else if (ListingQualityPercentile >= 75)
            Strengths.Add("Listings de alta calidad");
    }
}

/// <summary>
/// Tiers de dealer basados en performance
/// </summary>
public enum DealerTier
{
    Bronze = 1,
    Silver = 2,
    Gold = 3,
    Platinum = 4,
    Diamond = 5
}
