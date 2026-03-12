namespace DealerAnalyticsService.Domain.Entities;

public enum InsightType
{
    PricingRecommendation,
    InventoryOptimization,
    MarketingStrategy,
    SeasonalTrend,
    CompetitorAnalysis,
    // Additional types used in seed data
    Visibility,
    Conversion,
    Response,
    Pricing,
    Performance,
    Engagement
}

public enum InsightPriority
{
    Low,
    Medium,
    High,
    Critical
}

public class DealerInsight
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public InsightType Type { get; set; }
    public InsightPriority Priority { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionRecommendation { get; set; } = string.Empty;

    // Datos de soporte
    public string SupportingData { get; set; } = string.Empty; // JSON
    public decimal PotentialImpact { get; set; } // % de mejora estimada
    public decimal Confidence { get; set; } // 0-100%

    // Estado
    public bool IsRead { get; set; }
    public bool IsActedUpon { get; set; }
    public DateTime? ActionDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
