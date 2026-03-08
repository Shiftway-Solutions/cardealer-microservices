using ComparisonService.Application.DTOs;
using ComparisonService.Domain.Interfaces;
using MediatR;

namespace ComparisonService.Application.Features.Comparisons.Queries;

/// <summary>
/// Query to get a shared (public) comparison by its share token.
/// Does not require authentication.
/// </summary>
public record GetSharedComparisonQuery : IRequest<ComparisonDetailDto?>
{
    public string ShareToken { get; init; } = string.Empty;
}

public class GetSharedComparisonQueryHandler : IRequestHandler<GetSharedComparisonQuery, ComparisonDetailDto?>
{
    private readonly IComparisonRepository _repository;

    public GetSharedComparisonQueryHandler(IComparisonRepository repository)
    {
        _repository = repository;
    }

    public async Task<ComparisonDetailDto?> Handle(GetSharedComparisonQuery request, CancellationToken cancellationToken)
    {
        var comparison = await _repository.GetByShareTokenAsync(request.ShareToken, cancellationToken);

        if (comparison == null)
            return null;

        return new ComparisonDetailDto
        {
            Id = comparison.Id,
            Name = comparison.Name,
            VehicleIds = comparison.VehicleIds,
            Vehicles = new(), // Vehicles fetched at controller level via HTTP call
            CreatedAt = comparison.CreatedAt,
            UpdatedAt = comparison.UpdatedAt,
            IsPublic = comparison.IsPublic,
            ShareToken = comparison.ShareToken
        };
    }
}
