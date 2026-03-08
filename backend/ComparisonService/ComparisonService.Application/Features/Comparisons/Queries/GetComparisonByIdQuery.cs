using ComparisonService.Application.DTOs;
using ComparisonService.Domain.Interfaces;
using MediatR;

namespace ComparisonService.Application.Features.Comparisons.Queries;

/// <summary>
/// Query to get a comparison by its ID with ownership verification.
/// </summary>
public record GetComparisonByIdQuery : IRequest<ComparisonDetailDto?>
{
    public Guid ComparisonId { get; init; }
    public Guid UserId { get; init; }
}

public class GetComparisonByIdQueryHandler : IRequestHandler<GetComparisonByIdQuery, ComparisonDetailDto?>
{
    private readonly IComparisonRepository _repository;

    public GetComparisonByIdQueryHandler(IComparisonRepository repository)
    {
        _repository = repository;
    }

    public async Task<ComparisonDetailDto?> Handle(GetComparisonByIdQuery request, CancellationToken cancellationToken)
    {
        var comparison = await _repository.GetByIdAsync(request.ComparisonId, cancellationToken);

        if (comparison == null)
            return null;

        if (comparison.UserId != request.UserId)
            throw new UnauthorizedAccessException("Not authorized to view this comparison");

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
