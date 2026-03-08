using ComparisonService.Domain.Interfaces;
using MediatR;

namespace ComparisonService.Application.Features.Comparisons.Commands;

/// <summary>
/// Command to update the vehicles in an existing comparison.
/// </summary>
public record UpdateComparisonVehiclesCommand : IRequest<Unit>
{
    public Guid ComparisonId { get; init; }
    public Guid UserId { get; init; }
    public List<Guid> VehicleIds { get; init; } = new();
}

public class UpdateComparisonVehiclesCommandHandler : IRequestHandler<UpdateComparisonVehiclesCommand, Unit>
{
    private readonly IComparisonRepository _repository;

    public UpdateComparisonVehiclesCommandHandler(IComparisonRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(UpdateComparisonVehiclesCommand request, CancellationToken cancellationToken)
    {
        var comparison = await _repository.GetByIdAsync(request.ComparisonId, cancellationToken)
            ?? throw new KeyNotFoundException($"Comparison {request.ComparisonId} not found");

        if (comparison.UserId != request.UserId)
            throw new UnauthorizedAccessException("Not authorized to modify this comparison");

        comparison.UpdateVehicles(request.VehicleIds);
        await _repository.UpdateAsync(comparison, cancellationToken);

        return Unit.Value;
    }
}
