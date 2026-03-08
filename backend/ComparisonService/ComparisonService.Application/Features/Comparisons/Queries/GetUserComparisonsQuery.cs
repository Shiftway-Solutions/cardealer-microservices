using ComparisonService.Application.DTOs;
using ComparisonService.Domain.Interfaces;
using MediatR;

namespace ComparisonService.Application.Features.Comparisons.Queries;

/// <summary>
/// Query to get all comparisons for a user.
/// </summary>
public record GetUserComparisonsQuery : IRequest<IEnumerable<ComparisonDto>>
{
    public Guid UserId { get; init; }
}

public class GetUserComparisonsQueryHandler : IRequestHandler<GetUserComparisonsQuery, IEnumerable<ComparisonDto>>
{
    private readonly IComparisonRepository _repository;

    public GetUserComparisonsQueryHandler(IComparisonRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ComparisonDto>> Handle(GetUserComparisonsQuery request, CancellationToken cancellationToken)
    {
        var comparisons = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);

        return comparisons.Select(c => new ComparisonDto
        {
            Id = c.Id,
            Name = c.Name,
            VehicleIds = c.VehicleIds,
            VehicleCount = c.VehicleIds.Count,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt ?? c.CreatedAt,
            IsPublic = c.IsPublic,
            HasShareLink = c.ShareToken != null
        });
    }
}
