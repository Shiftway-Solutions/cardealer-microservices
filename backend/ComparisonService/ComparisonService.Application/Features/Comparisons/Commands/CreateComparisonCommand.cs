using ComparisonService.Application.DTOs;
using ComparisonService.Domain.Entities;
using ComparisonService.Domain.Interfaces;
using MediatR;

namespace ComparisonService.Application.Features.Comparisons.Commands;

/// <summary>
/// Command to create a new vehicle comparison.
/// </summary>
public record CreateComparisonCommand : IRequest<ComparisonDto>
{
    public Guid UserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<Guid> VehicleIds { get; init; } = new();
    public bool IsPublic { get; init; } = false;
}

/// <summary>
/// Handler for CreateComparisonCommand.
/// Validates via ValidationBehavior, creates the domain entity, and persists.
/// </summary>
public class CreateComparisonCommandHandler : IRequestHandler<CreateComparisonCommand, ComparisonDto>
{
    private readonly IComparisonRepository _repository;

    public CreateComparisonCommandHandler(IComparisonRepository repository)
    {
        _repository = repository;
    }

    public async Task<ComparisonDto> Handle(CreateComparisonCommand request, CancellationToken cancellationToken)
    {
        var comparison = new VehicleComparison(
            request.UserId,
            request.Name,
            request.VehicleIds,
            request.IsPublic);

        await _repository.CreateAsync(comparison, cancellationToken);

        return new ComparisonDto
        {
            Id = comparison.Id,
            Name = comparison.Name,
            VehicleIds = comparison.VehicleIds,
            VehicleCount = comparison.VehicleIds.Count,
            CreatedAt = comparison.CreatedAt,
            UpdatedAt = comparison.UpdatedAt ?? comparison.CreatedAt,
            IsPublic = comparison.IsPublic,
            HasShareLink = comparison.ShareToken != null
        };
    }
}
