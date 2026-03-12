using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecommendationService.Application.DTOs;
using RecommendationService.Domain.Entities;
using RecommendationService.Domain.Interfaces;

namespace RecommendationService.Application.Features.Recommendations.Queries;

public record GetSimilarVehiclesQuery(Guid VehicleId, int Limit = 10) : IRequest<List<RecommendationDto>>;

public class GetSimilarVehiclesQueryHandler : IRequestHandler<GetSimilarVehiclesQuery, List<RecommendationDto>>
{
    private readonly IRecommendationRepository _repository;

    public GetSimilarVehiclesQueryHandler(IRecommendationRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<RecommendationDto>> Handle(GetSimilarVehiclesQuery request, CancellationToken cancellationToken)
    {
        var recommendations = await _repository.GetSimilarVehiclesAsync(request.VehicleId, request.Limit);

        return recommendations.Select(MapToDto).ToList();
    }

    private RecommendationDto MapToDto(Recommendation rec)
    {
        return new RecommendationDto(
            rec.Id,
            rec.UserId,
            rec.VehicleId,
            rec.Type.ToString(),
            rec.Score,
            rec.Reason,
            rec.Metadata,
            rec.CreatedAt,
            rec.ViewedAt,
            rec.ClickedAt,
            rec.IsRelevant
        );
    }
}
