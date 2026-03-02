using MediatR;
using Microsoft.Extensions.Logging;
using RecoAgent.Domain.Interfaces;

namespace RecoAgent.Application.Features.Feedback.Commands;

public class RecordFeedbackCommandHandler : IRequestHandler<RecordFeedbackCommand, bool>
{
    private readonly IRecoCacheService _cacheService;
    private readonly ILogger<RecordFeedbackCommandHandler> _logger;

    public RecordFeedbackCommandHandler(
        IRecoCacheService cacheService,
        ILogger<RecordFeedbackCommandHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<bool> Handle(RecordFeedbackCommand request, CancellationToken ct)
    {
        var feedback = request.Feedback;

        _logger.LogInformation(
            "Recording feedback: User={UserId}, Vehicle={VehicleId}, Type={Type}, Position={Position}",
            feedback.UserId, feedback.VehiculoId, feedback.FeedbackType, feedback.Position);

        // Invalidate cached recommendations for this user so next request gets fresh ones
        if (feedback.FeedbackType is "thumbs_down" or "dismiss")
        {
            await _cacheService.InvalidateUserCacheAsync(feedback.UserId, ct);
            _logger.LogInformation("Cache invalidated for user {UserId} due to negative feedback", feedback.UserId);
        }

        // In a full implementation, feedback would be published to RabbitMQ
        // for the profile service to update user preferences.
        // For v1.0, we just log and invalidate cache.

        return true;
    }
}
