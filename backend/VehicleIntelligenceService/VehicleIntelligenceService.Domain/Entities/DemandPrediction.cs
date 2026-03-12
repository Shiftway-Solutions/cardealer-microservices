namespace VehicleIntelligenceService.Domain.Entities;

public class DemandPrediction
{
    public Guid Id { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }

    // Demanda actual
    public DemandLevel CurrentDemand { get; set; }
    public decimal DemandScore { get; set; }          // 0-100

    // Tendencia
    public TrendDirection Trend { get; set; }
    public decimal TrendStrength { get; set; }        // 0-1

    // Predicción futura
    public DemandLevel PredictedDemand30Days { get; set; }
    public DemandLevel PredictedDemand90Days { get; set; }

    // Estadísticas de mercado
    public int SearchesPerDay { get; set; }
    public int AvailableInventory { get; set; }
    public decimal AvgDaysToSale { get; set; }

    // Recomendación de compra para dealers
    public BuyRecommendation BuyRecommendation { get; set; }
    public string BuyRecommendationReason { get; set; } = string.Empty;

    // Insights como JSON
    public string Insights { get; set; } = "[]";      // JSON array

    public DateTime PredictionDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum DemandLevel
{
    VeryHigh,   // Se venden en menos de 15 días
    High,       // Se venden en 15-30 días
    Medium,     // Se venden en 30-60 días
    Low,        // Se venden en 60-90 días
    VeryLow     // Difícil de vender, >90 días
}

public enum TrendDirection
{
    Rising,
    Stable,
    Falling
}

public enum BuyRecommendation
{
    StrongBuy,       // Excelente oportunidad
    Buy,             // Buena oportunidad
    Hold,            // Esperar mejores condiciones
    Avoid            // No recomendado
}
