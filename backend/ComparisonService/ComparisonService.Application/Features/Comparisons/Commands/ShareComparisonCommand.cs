using ComparisonService.Application.DTOs;
using ComparisonService.Domain.Interfaces;
using MediatR;

namespace ComparisonService.Application.Features.Comparisons.Commands;

/// <summary>
/// Command to share a comparison publicly by generating a share token.
/// </summary>
public record ShareComparisonCommand : IRequest<ShareResponseDto>
{
    public Guid ComparisonId { get; init; }
    public Guid UserId { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
}

public class ShareComparisonCommandHandler : IRequestHandler<ShareComparisonCommand, ShareResponseDto>
{
    private readonly IComparisonRepository _repository;

    public ShareComparisonCommandHandler(IComparisonRepository repository)
    {
        _repository = repository;
    }

    public async Task<ShareResponseDto> Handle(ShareComparisonCommand request, CancellationToken cancellationToken)
    {
        var comparison = await _repository.GetByIdAsync(request.ComparisonId, cancellationToken)
            ?? throw new KeyNotFoundException($"Comparison {request.ComparisonId} not found");

        if (comparison.UserId != request.UserId)
            throw new UnauthorizedAccessException("Not authorized to share this comparison");

        comparison.MakePublic();
        await _repository.UpdateAsync(comparison, cancellationToken);

        return new ShareResponseDto
        {
            ShareToken = comparison.ShareToken!,
            ShareUrl = $"{request.BaseUrl}/compare/{comparison.ShareToken}"
        };
    }
}
