namespace RecoAgent.Application.DTOs;

/// <summary>
/// Wrapper DTO for API response containing the recommendations plus metadata.
/// </summary>
public class RecoAgentResultDto
{
    /// <summary>
    /// The full recommendation response from the agent.
    /// </summary>
    public RecoAgentResponse Response { get; set; } = new();

    /// <summary>
    /// Whether the response was served from cache.
    /// </summary>
    public bool WasCached { get; set; }

    /// <summary>
    /// Latency in milliseconds for the recommendation generation.
    /// </summary>
    public long LatencyMs { get; set; }

    /// <summary>
    /// Mode used: batch, real-time, or hybrid.
    /// </summary>
    public string Mode { get; set; } = "real-time";

    /// <summary>
    /// Timestamp of when the recommendations were generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
