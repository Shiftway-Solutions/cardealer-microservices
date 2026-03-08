using CarDealer.Contracts.Events.Recommendation;
using MediatR;
using Microsoft.Extensions.Logging;
using RecoAgent.Domain.Interfaces;

namespace RecoAgent.Application.Features.Feedback.Commands;

public class RecordFeedbackCommandHandler : IRequestHandler<RecordFeedbackCommand, bool>
{
    private readonly IRecoCacheService _cacheService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RecordFeedbackCommandHandler> _logger;

    public RecordFeedbackCommandHandler(
        IRecoCacheService cacheService,
        IEventPublisher eventPublisher,
        ILogger<RecordFeedbackCommandHandler> logger)
    {
        _cacheService = cacheService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<bool> Handle(RecordFeedbackCommand request, CancellationToken ct)
    {
        var feedback = request.Feedback;
        var isPositive = feedback.FeedbackType is "thumbs_up" or "click";

        _logger.LogInformation(
            "Recording feedback: User={UserId}, Vehicle={VehicleId}, Type={Type}, Position={Position}, IsPositive={IsPositive}",
            feedback.UserId, feedback.VehiculoId, feedback.FeedbackType, feedback.Position, isPositive);

        // Invalidate cached recommendations for this user so next request gets fresh ones
        if (feedback.FeedbackType is "thumbs_down" or "dismiss")
        {
            await _cacheService.InvalidateUserCacheAsync(feedback.UserId, ct);
            _logger.LogInformation("Cache invalidated for user {UserId} due to negative feedback", feedback.UserId);
        }

        // Publish feedback event to RabbitMQ for cross-service analytics
        // Consumed by: DealerAnalyticsService (engagement metrics), RecoAgent learning pipeline
        try
        {
            var feedbackEvent = new RecommendationFeedbackRecordedEvent
            {
                UserId = feedback.UserId,
                VehicleId = feedback.VehiculoId,
                FeedbackType = feedback.FeedbackType,
                SessionId = feedback.SessionId,
                Position = feedback.Position,
                IsPositive = isPositive,
                RecordedAt = DateTime.UtcNow
            };

            await _eventPublisher.PublishAsync(feedbackEvent, ct);

            _logger.LogInformation(
                "Feedback event published: EventId={EventId}, Type={FeedbackType}, User={UserId}",
                feedbackEvent.EventId, feedback.FeedbackType, feedback.UserId);
        }
        catch (Exception ex)
        {
            // Log but don't fail the request — feedback recording is best-effort for analytics
            _logger.LogError(ex,
                "Failed to publish feedback event for User={UserId}, Vehicle={VehicleId}. Event will be retried via DLQ.",
                feedback.UserId, feedback.VehiculoId);
        }

        return true;
    }
}
