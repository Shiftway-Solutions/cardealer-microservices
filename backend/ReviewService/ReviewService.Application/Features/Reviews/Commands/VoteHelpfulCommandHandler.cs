using MediatR;
using Microsoft.Extensions.Logging;
using ReviewService.Domain.Base;
using ReviewService.Domain.Entities;
using ReviewService.Domain.Interfaces;

namespace ReviewService.Application.Features.Reviews.Commands;

/// <summary>
/// Handler para votar si una review es útil
/// </summary>
public class VoteHelpfulCommandHandler : IRequestHandler<VoteHelpfulCommand, Result<VoteResultDto>>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IReviewHelpfulVoteRepository _voteRepository;
    private readonly ILogger<VoteHelpfulCommandHandler> _logger;

    public VoteHelpfulCommandHandler(
        IReviewRepository reviewRepository,
        IReviewHelpfulVoteRepository voteRepository,
        ILogger<VoteHelpfulCommandHandler> logger)
    {
        _reviewRepository = reviewRepository;
        _voteRepository = voteRepository;
        _logger = logger;
    }

    public async Task<Result<VoteResultDto>> Handle(VoteHelpfulCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verificar que la review existe
            var review = await _reviewRepository.GetByIdAsync(request.ReviewId);

            if (review == null)
            {
                return Result<VoteResultDto>.Failure("Review no encontrada");
            }

            // Verificar si el usuario ya votó
            var existingVote = await _voteRepository.GetByReviewAndUserAsync(request.ReviewId, request.UserId, cancellationToken);

            if (existingVote != null)
            {
                // Actualizar voto existente si cambió
                if (existingVote.IsHelpful != request.IsHelpful)
                {
                    // Actualizar contadores
                    if (request.IsHelpful)
                    {
                        review.HelpfulVotes++;
                    }
                    else
                    {
                        review.HelpfulVotes = Math.Max(0, review.HelpfulVotes - 1);
                    }

                    existingVote.IsHelpful = request.IsHelpful;
                    existingVote.UpdatedAt = DateTime.UtcNow;
                    await _voteRepository.UpdateAsync(existingVote);

                    _logger.LogInformation("User {UserId} changed vote on review {ReviewId} to {IsHelpful}",
                        request.UserId, request.ReviewId, request.IsHelpful);
                }
                else
                {
                    // Remover voto si vota lo mismo (toggle)
                    await _voteRepository.DeleteAsync(existingVote.Id);
                    review.TotalVotes = Math.Max(0, review.TotalVotes - 1);
                    if (existingVote.IsHelpful)
                    {
                        review.HelpfulVotes = Math.Max(0, review.HelpfulVotes - 1);
                    }

                    _logger.LogInformation("User {UserId} removed vote from review {ReviewId}",
                        request.UserId, request.ReviewId);
                }
            }
            else
            {
                // Crear nuevo voto
                var newVote = new ReviewHelpfulVote
                {
                    Id = Guid.NewGuid(),
                    ReviewId = request.ReviewId,
                    UserId = request.UserId,
                    IsHelpful = request.IsHelpful,
                    UserIpAddress = request.UserIpAddress,
                    UserAgent = request.UserAgent,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _voteRepository.AddAsync(newVote);

                review.TotalVotes++;
                if (request.IsHelpful)
                {
                    review.HelpfulVotes++;
                }

                _logger.LogInformation("User {UserId} voted {IsHelpful} on review {ReviewId}",
                    request.UserId, request.IsHelpful, request.ReviewId);
            }

            review.UpdatedAt = DateTime.UtcNow;
            await _reviewRepository.UpdateAsync(review);

            // Calcular porcentaje
            var percentage = review.TotalVotes > 0
                ? Math.Round((decimal)review.HelpfulVotes / review.TotalVotes * 100, 1)
                : 0;

            return Result<VoteResultDto>.Success(new VoteResultDto
            {
                Success = true,
                Message = "Voto registrado exitosamente",
                TotalHelpfulVotes = review.HelpfulVotes,
                TotalVotes = review.TotalVotes,
                HelpfulPercentage = percentage,
                UserVotedHelpful = request.IsHelpful
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voting on review {ReviewId}", request.ReviewId);
            return Result<VoteResultDto>.Failure($"Error al votar: {ex.Message}");
        }
    }
}
