using MediatR;
using Microsoft.Extensions.Logging;
using ReviewService.Application.DTOs;
using ReviewService.Domain.Base;
using ReviewService.Domain.Interfaces;

namespace ReviewService.Application.Features.Reviews.Queries;

/// <summary>
/// Sprint 15 - Obtener estadísticas de votos de una review
/// </summary>
public record GetReviewVoteStatsQuery : IRequest<Result<ReviewVoteStatsDto>>
{
    public Guid ReviewId { get; init; }
    public Guid? CurrentUserId { get; init; }
}

/// <summary>
/// Handler para obtener estadísticas de votos
/// </summary>
public class GetReviewVoteStatsQueryHandler : IRequestHandler<GetReviewVoteStatsQuery, Result<ReviewVoteStatsDto>>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IReviewHelpfulVoteRepository _voteRepository;
    private readonly ILogger<GetReviewVoteStatsQueryHandler> _logger;

    public GetReviewVoteStatsQueryHandler(
        IReviewRepository reviewRepository,
        IReviewHelpfulVoteRepository voteRepository,
        ILogger<GetReviewVoteStatsQueryHandler> logger)
    {
        _reviewRepository = reviewRepository;
        _voteRepository = voteRepository;
        _logger = logger;
    }

    public async Task<Result<ReviewVoteStatsDto>> Handle(GetReviewVoteStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var review = await _reviewRepository.GetByIdAsync(request.ReviewId);

            if (review == null)
            {
                return Result<ReviewVoteStatsDto>.Failure("Review no encontrada");
            }

            // Verificar si el usuario actual votó
            bool? currentUserVotedHelpful = null;
            if (request.CurrentUserId.HasValue)
            {
                var userVote = await _voteRepository.GetByReviewAndUserAsync(
                    request.ReviewId, request.CurrentUserId.Value, cancellationToken);

                currentUserVotedHelpful = userVote?.IsHelpful;
            }

            var percentage = review.TotalVotes > 0
                ? Math.Round((decimal)review.HelpfulVotes / review.TotalVotes * 100, 1)
                : 0;

            return Result<ReviewVoteStatsDto>.Success(new ReviewVoteStatsDto
            {
                ReviewId = review.Id,
                HelpfulVotes = review.HelpfulVotes,
                TotalVotes = review.TotalVotes,
                HelpfulPercentage = percentage,
                CurrentUserVotedHelpful = currentUserVotedHelpful
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vote stats for review {ReviewId}", request.ReviewId);
            return Result<ReviewVoteStatsDto>.Failure($"Error al obtener estadísticas: {ex.Message}");
        }
    }
}
