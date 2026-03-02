using MediatR;
using RecoAgent.Application.DTOs;

namespace RecoAgent.Application.Features.Feedback.Commands;

/// <summary>
/// Command to record feedback on a recommendation.
/// </summary>
public record RecordFeedbackCommand(
    RecommendationFeedbackRequest Feedback
) : IRequest<bool>;
