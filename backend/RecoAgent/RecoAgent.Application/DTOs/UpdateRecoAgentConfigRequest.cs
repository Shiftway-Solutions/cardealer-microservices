namespace RecoAgent.Application.DTOs;

/// <summary>
/// Request to update the RecoAgent configuration.
/// </summary>
public class UpdateRecoAgentConfigRequest
{
    public string? Model { get; set; }
    public float? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public int? MinRecommendations { get; set; }
    public int? MaxRecommendations { get; set; }
    public float? SponsoredAffinityThreshold { get; set; }
    public string? SponsoredPositions { get; set; }
    public string? SponsoredLabel { get; set; }
    public float? MaxSameBrandPercent { get; set; }
    public int? BatchRefreshIntervalHours { get; set; }
    public int? CacheTtlSeconds { get; set; }
    public int? RealTimeCacheTtlSeconds { get; set; }
    public bool? IsEnabled { get; set; }
}
