using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecommendationService.Domain.Interfaces;

namespace RecommendationService.Application.Features.Recommendations.Commands;

public record MarkRecommendationViewedCommand(Guid RecommendationId) : IRequest<bool>;

public class MarkRecommendationViewedCommandHandler : IRequestHandler<MarkRecommendationViewedCommand, bool>
{
    private readonly IRecommendationRepository _repository;

    public MarkRecommendationViewedCommandHandler(IRecommendationRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(MarkRecommendationViewedCommand request, CancellationToken cancellationToken)
    {
        var recommendation = await _repository.GetByIdAsync(request.RecommendationId);

        if (recommendation == null)
            return false;

        recommendation.MarkViewed();
        await _repository.UpdateAsync(recommendation);

        return true;
    }
}

public record MarkRecommendationClickedCommand(Guid RecommendationId) : IRequest<bool>;

public class MarkRecommendationClickedCommandHandler : IRequestHandler<MarkRecommendationClickedCommand, bool>
{
    private readonly IRecommendationRepository _repository;

    public MarkRecommendationClickedCommandHandler(IRecommendationRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(MarkRecommendationClickedCommand request, CancellationToken cancellationToken)
    {
        var recommendation = await _repository.GetByIdAsync(request.RecommendationId);

        if (recommendation == null)
            return false;

        recommendation.MarkClicked();
        await _repository.UpdateAsync(recommendation);

        return true;
    }
}
