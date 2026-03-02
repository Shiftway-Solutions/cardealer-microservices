namespace SearchAgent.Application.DTOs;

/// <summary>
/// DTO for updating SearchAgent configuration from admin panel.
/// </summary>
public class UpdateSearchAgentConfigRequest
{
    public bool? IsEnabled { get; set; }
    public string? Model { get; set; }
    public float? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public int? MinResultsPerPage { get; set; }
    public int? MaxResultsPerPage { get; set; }
    public float? SponsoredAffinityThreshold { get; set; }
    public float? MaxSponsoredPercentage { get; set; }
    public string? SponsoredPositions { get; set; }
    public string? SponsoredLabel { get; set; }
    public int? PriceRelaxPercent { get; set; }
    public int? YearRelaxRange { get; set; }
    public int? MaxRelaxationLevel { get; set; }
    public bool? EnableCache { get; set; }
    public int? CacheTtlSeconds { get; set; }
    public float? SemanticCacheThreshold { get; set; }
    public int? MaxQueriesPerMinutePerIp { get; set; }
    public int? AiSearchTrafficPercent { get; set; }
    public string? SystemPromptOverride { get; set; }
}
