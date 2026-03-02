namespace SearchAgent.Application.DTOs;

/// <summary>
/// DTO for the complete search result returned to the frontend.
/// Includes AI filters, vehicle results, sponsored items, and metadata.
/// </summary>
public class SearchAgentResultDto
{
    /// <summary>
    /// AI-generated filter interpretation
    /// </summary>
    public SearchAgentResponse AiFilters { get; set; } = new();

    /// <summary>
    /// Whether the response was served from cache
    /// </summary>
    public bool WasCached { get; set; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public int LatencyMs { get; set; }

    /// <summary>
    /// Whether the AI search feature is enabled
    /// </summary>
    public bool IsAiSearchEnabled { get; set; } = true;
}
