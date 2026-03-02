namespace SearchAgent.Domain.Entities;

/// <summary>
/// Configurable behavior settings for the SearchAgent.
/// Managed via the admin panel and stored in the database.
/// </summary>
public class SearchAgentConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsEnabled { get; set; } = true;

    // LLM Settings
    public string Model { get; set; } = "claude-haiku-4-5-20251001";
    public float Temperature { get; set; } = 0.2f;
    public int MaxTokens { get; set; } = 1024;

    // Business Rules
    public int MinResultsPerPage { get; set; } = 8;
    public int MaxResultsPerPage { get; set; } = 40;
    public float SponsoredAffinityThreshold { get; set; } = 0.45f;
    public float MaxSponsoredPercentage { get; set; } = 0.25f;
    public string SponsoredPositions { get; set; } = "1,5,10";
    public string SponsoredLabel { get; set; } = "Patrocinado";

    // Relaxation Settings
    public int PriceRelaxPercent { get; set; } = 25;
    public int YearRelaxRange { get; set; } = 2;
    public int MaxRelaxationLevel { get; set; } = 5;

    // Caching
    public bool EnableCache { get; set; } = true;
    public int CacheTtlSeconds { get; set; } = 3600;
    public float SemanticCacheThreshold { get; set; } = 0.95f;

    // Rate Limiting
    public int MaxQueriesPerMinutePerIp { get; set; } = 60;

    // A/B Testing
    public int AiSearchTrafficPercent { get; set; } = 100;

    // System Prompt Override (allows admin customization)
    public string? SystemPromptOverride { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
