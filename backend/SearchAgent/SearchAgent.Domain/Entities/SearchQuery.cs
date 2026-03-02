namespace SearchAgent.Domain.Entities;

/// <summary>
/// Represents a user search query processed by the SearchAgent.
/// Tracks the original query, AI interpretation, and results metadata.
/// </summary>
public class SearchQuery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OriginalQuery { get; set; } = string.Empty;
    public string? ReformulatedQuery { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public string? IpAddress { get; set; }

    // AI Response
    public string? FiltersJson { get; set; }
    public float Confidence { get; set; }
    public int FilterLevel { get; set; } = 1;

    // Results metadata
    public int OrganicResultCount { get; set; }
    public int SponsoredResultCount { get; set; }
    public int TotalResultCount { get; set; }

    // Performance
    public int LatencyMs { get; set; }
    public bool WasCached { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
