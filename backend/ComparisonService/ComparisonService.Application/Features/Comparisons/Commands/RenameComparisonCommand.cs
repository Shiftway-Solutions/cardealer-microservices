using ComparisonService.Domain.Interfaces;
using MediatR;

namespace ComparisonService.Application.Features.Comparisons.Commands;

/// <summary>
/// Command to rename an existing comparison.
/// </summary>
public record RenameComparisonCommand : IRequest<Unit>
{
    public Guid ComparisonId { get; init; }
    public Guid UserId { get; init; }
    public string Name { get; init; } = string.Empty;
}

public class RenameComparisonCommandHandler : IRequestHandler<RenameComparisonCommand, Unit>
{
    private readonly IComparisonRepository _repository;

    public RenameComparisonCommandHandler(IComparisonRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(RenameComparisonCommand request, CancellationToken cancellationToken)
    {
        var comparison = await _repository.GetByIdAsync(request.ComparisonId, cancellationToken)
            ?? throw new KeyNotFoundException($"Comparison {request.ComparisonId} not found");

        if (comparison.UserId != request.UserId)
            throw new UnauthorizedAccessException("Not authorized to modify this comparison");

        comparison.Rename(request.Name);
        await _repository.UpdateAsync(comparison, cancellationToken);

        return Unit.Value;
    }
}
