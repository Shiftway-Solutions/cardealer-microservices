using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using RecommendationService.Application.DTOs;
using RecommendationService.Domain.Entities;
using RecommendationService.Domain.Interfaces;

namespace RecommendationService.Application.Features.Recommendations.Commands;

public record TrackInteractionCommand(
    Guid UserId,
    Guid VehicleId,
    string Type,
    int DurationSeconds = 0,
    string? Source = null) : IRequest<VehicleInteractionDto>;

public class TrackInteractionCommandHandler : IRequestHandler<TrackInteractionCommand, VehicleInteractionDto>
{
    private readonly IVehicleInteractionRepository _interactionRepository;
    private readonly IUserPreferenceRepository _preferenceRepository;
    private readonly ILogger<TrackInteractionCommandHandler> _logger;

    public TrackInteractionCommandHandler(
        IVehicleInteractionRepository interactionRepository,
        IUserPreferenceRepository preferenceRepository,
        ILogger<TrackInteractionCommandHandler> logger)
    {
        _interactionRepository = interactionRepository;
        _preferenceRepository = preferenceRepository;
        _logger = logger;
    }

    public async Task<VehicleInteractionDto> Handle(TrackInteractionCommand request, CancellationToken cancellationToken)
    {
        // Parsear tipo de interacción
        if (!Enum.TryParse<InteractionType>(request.Type, true, out var interactionType))
        {
            throw new ArgumentException($"Invalid interaction type: {request.Type}");
        }

        // Crear interacción
        var interaction = new VehicleInteraction(
            request.UserId,
            request.VehicleId,
            interactionType,
            request.Source
        );
        interaction.DurationSeconds = request.DurationSeconds;

        var created = await _interactionRepository.CreateAsync(interaction);

        // Actualizar preferencias del usuario (async, no bloqueante)
        _ = UpdateUserPreferencesAsync(request.UserId);

        return new VehicleInteractionDto(
            created.Id,
            created.UserId,
            created.VehicleId,
            created.Type.ToString(),
            created.CreatedAt,
            created.DurationSeconds,
            created.Source
        );
    }

    private async Task UpdateUserPreferencesAsync(Guid userId)
    {
        try
        {
            var preferences = await _preferenceRepository.GetByUserIdAsync(userId);
            
            if (preferences == null)
            {
                preferences = new UserPreference(userId);
                await _preferenceRepository.CreateAsync(preferences);
                return;
            }

            // Incrementar contadores
            preferences.TotalVehiclesViewed++;
            preferences.CalculateConfidence();
            preferences.UpdateTimestamp();

            await _preferenceRepository.UpdateAsync(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update user preferences for user {UserId}", userId);
        }
    }
}
