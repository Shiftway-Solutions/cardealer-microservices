using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecommendationService.Application.DTOs;
using RecommendationService.Domain.Entities;
using RecommendationService.Domain.Interfaces;

namespace RecommendationService.Application.Features.Recommendations.Commands;

public record GenerateRecommendationsCommand(Guid UserId, int Limit = 10) : IRequest<List<RecommendationDto>>;

public class GenerateRecommendationsCommandHandler : IRequestHandler<GenerateRecommendationsCommand, List<RecommendationDto>>
{
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly IUserPreferenceRepository _preferenceRepository;
    private readonly IVehicleInteractionRepository _interactionRepository;

    public GenerateRecommendationsCommandHandler(
        IRecommendationRepository recommendationRepository,
        IUserPreferenceRepository preferenceRepository,
        IVehicleInteractionRepository interactionRepository)
    {
        _recommendationRepository = recommendationRepository;
        _preferenceRepository = preferenceRepository;
        _interactionRepository = interactionRepository;
    }

    public async Task<List<RecommendationDto>> Handle(GenerateRecommendationsCommand request, CancellationToken cancellationToken)
    {
        // Obtener preferencias del usuario
        var preferences = await _preferenceRepository.GetByUserIdAsync(request.UserId);

        // Obtener interacciones recientes
        var recentInteractions = await _interactionRepository.GetRecentByUserIdAsync(request.UserId, days: 30);

        // Aquí iría el algoritmo de recomendación real
        // Por ahora generamos recomendaciones mock
        var recommendations = new List<Recommendation>();

        // En producción, esto haría:
        // 1. Query a VehiclesSaleService para obtener vehículos matching preferencias
        // 2. Aplicar collaborative filtering basado en usuarios similares
        // 3. Ranking por score (combinación de match con preferencias + popularidad + recency)

        var result = new List<RecommendationDto>();
        foreach (var rec in recommendations)
        {
            result.Add(MapToDto(rec));
        }

        return result;
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
