using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecommendationService.Application.DTOs;
using RecommendationService.Domain.Interfaces;

namespace RecommendationService.Application.Features.Recommendations.Queries;

public record GetUserPreferencesQuery(Guid UserId) : IRequest<UserPreferenceDto?>;

public class GetUserPreferencesQueryHandler : IRequestHandler<GetUserPreferencesQuery, UserPreferenceDto?>
{
    private readonly IUserPreferenceRepository _repository;

    public GetUserPreferencesQueryHandler(IUserPreferenceRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserPreferenceDto?> Handle(GetUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        var preferences = await _repository.GetByUserIdAsync(request.UserId);

        if (preferences == null)
            return null;

        return new UserPreferenceDto(
            preferences.Id,
            preferences.UserId,
            preferences.PreferredMakes,
            preferences.PreferredModels,
            preferences.PreferredBodyTypes,
            preferences.PreferredFuelTypes,
            preferences.PreferredTransmissions,
            preferences.MinYear,
            preferences.MaxYear,
            preferences.MinPrice,
            preferences.MaxPrice,
            preferences.MinMileage,
            preferences.MaxMileage,
            preferences.PreferredColors,
            preferences.PreferredFeatures,
            preferences.Confidence,
            preferences.TotalVehiclesViewed,
            preferences.TotalSearches,
            preferences.TotalFavorites,
            preferences.TotalContacts,
            preferences.CreatedAt,
            preferences.UpdatedAt
        );
    }
}
