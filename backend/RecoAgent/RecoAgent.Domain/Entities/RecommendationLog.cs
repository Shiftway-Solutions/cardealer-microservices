namespace RecoAgent.Domain.Entities;

/// <summary>
/// Represents a recommendation request processed by the RecoAgent.
/// Tracks the user profile snapshot, AI response, and results metadata.
/// </summary>
public class RecommendationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public string? IpAddress { get; set; }

    // Mode
    public string Mode { get; set; } = "batch"; // batch | real-time | hybrid

    // Profile snapshot
    public string? ProfileJson { get; set; }
    public int ColdStartLevel { get; set; }
    public string? DetectedStage { get; set; }

    // AI Response
    public string? RecommendationsJson { get; set; }
    public float ConfidenceScore { get; set; }
    public int RecommendationCount { get; set; }
    public int SponsoredCount { get; set; }

    // Diversification applied
    public string? DiversificationJson { get; set; }

    // Performance
    public int LatencyMs { get; set; }
    public bool WasCached { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
