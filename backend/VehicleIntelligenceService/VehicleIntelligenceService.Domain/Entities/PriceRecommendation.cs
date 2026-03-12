namespace VehicleIntelligenceService.Domain.Entities;

public class PriceRecommendation
{
    public Guid Id { get; set; }
    public Guid PriceAnalysisId { get; set; }
    public PriceAnalysis? PriceAnalysis { get; set; }

    public RecommendationType Type { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal? SuggestedValue { get; set; }
    public string ImpactDescription { get; set; } = string.Empty;
    public int Priority { get; set; }                 // 1-5 (1 = highest)

    public DateTime CreatedAt { get; set; }
}

public enum RecommendationType
{
    ReducePrice,
    MaintainPrice,
    HighlightFeature,
    AddPhotos,
    ImproveDescription,
    OfferFinancing
}
