namespace RecoAgent.Application.DTOs;

/// <summary>
/// Feedback request for a recommendation result.
/// </summary>
public class RecommendationFeedbackRequest
{
    /// <summary>
    /// User ID who provided the feedback.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle ID that was recommended.
    /// </summary>
    public string VehiculoId { get; set; } = string.Empty;

    /// <summary>
    /// Feedback type: thumbs_up, thumbs_down, dismiss, click.
    /// </summary>
    public string FeedbackType { get; set; } = "neutral";

    /// <summary>
    /// Optional session ID for tracking.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Position where the recommendation was shown.
    /// </summary>
    public int? Position { get; set; }
}
