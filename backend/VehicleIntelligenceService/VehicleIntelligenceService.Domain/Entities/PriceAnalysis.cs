namespace VehicleIntelligenceService.Domain.Entities;

public class PriceAnalysis
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public decimal CurrentPrice { get; set; }

    // Precio sugerido
    public decimal SuggestedPrice { get; set; }
    public decimal SuggestedPriceMin { get; set; }
    public decimal SuggestedPriceMax { get; set; }

    // Comparación con mercado
    public decimal MarketAvgPrice { get; set; }
    public decimal PriceVsMarket { get; set; }        // 1.05 = 5% arriba

    /// <summary>
    /// Posición de precio en el mercado (5 niveles):
    /// "Great Deal" (≤-15%), "Good Deal" (-15% a -5%), "Fair" (±5%),
    /// "High Price" (+5% a +15%), "Overpriced" (>+15%)
    /// </summary>
    public string PricePosition { get; set; } = "Fair";

    /// <summary>
    /// Indica si el vehículo está sobrevaluado más de 20% respecto al mercado.
    /// Activa alertas y recomendaciones especiales para el vendedor.
    /// </summary>
    public bool IsOvervalued { get; set; }

    /// <summary>
    /// Porcentaje exacto por encima del mercado (ej: 25.5 = 25.5% arriba).
    /// Valor negativo indica que está por debajo del mercado.
    /// </summary>
    public decimal OvervaluationPercentage { get; set; }

    // Predicción
    public int PredictedDaysToSaleAtCurrentPrice { get; set; }
    public int PredictedDaysToSaleAtSuggestedPrice { get; set; }

    // Metadata
    public DateTime AnalysisDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Confianza del modelo
    public decimal ConfidenceScore { get; set; }      // 0-100

    // Factores que influyen en el precio
    public string InfluencingFactors { get; set; } = "{}"; // JSON
}
