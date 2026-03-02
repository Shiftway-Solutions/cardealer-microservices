namespace RecoAgent.Domain.Entities;

/// <summary>
/// Configurable behavior settings for the RecoAgent.
/// Managed via the admin panel and stored in the database.
/// Uses Claude Sonnet 4.5 for higher reasoning in recommendation generation.
/// </summary>
public class RecoAgentConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsEnabled { get; set; } = true;

    // LLM Settings — Sonnet 4.5 for higher reasoning
    public string Model { get; set; } = "claude-sonnet-4-5-20251022";
    public float Temperature { get; set; } = 0.5f;
    public int MaxTokens { get; set; } = 2048;

    // Business Rules
    public int MinRecommendations { get; set; } = 8;
    public int MaxRecommendations { get; set; } = 12;

    // Sponsored Settings (Regla #2)
    public float SponsoredAffinityThreshold { get; set; } = 0.50f;
    public float MaxSponsoredPercentage { get; set; } = 0.25f;
    public string SponsoredPositions { get; set; } = "2,6,11";
    public string SponsoredLabel { get; set; } = "Destacado";

    // Diversification Rules (Regla #3)
    public float MaxSameBrandPercent { get; set; } = 0.40f;
    public float MaxSamePriceRangePercent { get; set; } = 0.50f;
    public float MaxSameTypePercent { get; set; } = 0.60f;

    // Batch Processing
    public int BatchRefreshIntervalHours { get; set; } = 4;
    public int MaxCandidatesPerRequest { get; set; } = 50;

    // Caching
    public bool EnableCache { get; set; } = true;
    public int CacheTtlSeconds { get; set; } = 14400; // 4 hours for batch
    public int RealTimeCacheTtlSeconds { get; set; } = 900; // 15 min for real-time

    // Rate Limiting
    public int MaxRequestsPerMinutePerUser { get; set; } = 30;

    // System Prompt Override (allows admin customization)
    public string? SystemPromptOverride { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
