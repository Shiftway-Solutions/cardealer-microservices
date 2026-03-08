using ComparisonService.Domain.Interfaces;
using MediatR;

namespace ComparisonService.Application.Features.Comparisons.Commands;

/// <summary>
/// Command to delete a comparison.
/// </summary>
public record DeleteComparisonCommand : IRequest<Unit>
{
    public Guid ComparisonId { get; init; }
    public Guid UserId { get; init; }
}

public class DeleteComparisonCommandHandler : IRequestHandler<DeleteComparisonCommand, Unit>
{
    private readonly IComparisonRepository _repository;

    public DeleteComparisonCommandHandler(IComparisonRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(DeleteComparisonCommand request, CancellationToken cancellationToken)
    {
        var comparison = await _repository.GetByIdAsync(request.ComparisonId, cancellationToken)
            ?? throw new KeyNotFoundException($"Comparison {request.ComparisonId} not found");

        if (comparison.UserId != request.UserId)
            throw new UnauthorizedAccessException("Not authorized to delete this comparison");

        await _repository.DeleteAsync(request.ComparisonId, cancellationToken);

        return Unit.Value;
    }
}
