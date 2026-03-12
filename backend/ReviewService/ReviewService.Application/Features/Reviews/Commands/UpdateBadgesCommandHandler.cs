using MediatR;
using Microsoft.Extensions.Logging;
using ReviewService.Application.DTOs;
using ReviewService.Domain.Base;
using ReviewService.Domain.Entities;
using ReviewService.Domain.Interfaces;

namespace ReviewService.Application.Features.Reviews.Commands;

/// <summary>
/// Handler para actualizar badges de vendedor
/// Evalúa criterios y otorga/revoca badges automáticamente
/// </summary>
public class UpdateBadgesCommandHandler : IRequestHandler<UpdateBadgesCommand, Result<BadgeUpdateResultDto>>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly ISellerBadgeRepository _badgeRepository;
    private readonly IReviewSummaryRepository _summaryRepository;
    private readonly ILogger<UpdateBadgesCommandHandler> _logger;

    public UpdateBadgesCommandHandler(
        IReviewRepository reviewRepository,
        ISellerBadgeRepository badgeRepository,
        IReviewSummaryRepository summaryRepository,
        ILogger<UpdateBadgesCommandHandler> logger)
    {
        _reviewRepository = reviewRepository;
        _badgeRepository = badgeRepository;
        _summaryRepository = summaryRepository;
        _logger = logger;
    }

    public async Task<Result<BadgeUpdateResultDto>> Handle(UpdateBadgesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var sellerId = request.SellerId;
            var newBadgesEarned = new List<string>();
            var badgesRevoked = new List<string>();

            // Obtener estadísticas actuales del vendedor
            var (reviews, _) = await _reviewRepository.GetBySellerIdAsync(sellerId, 1, 1000, true);
            var reviewsList = reviews.ToList();

            var totalReviews = reviewsList.Count;
            var averageRating = totalReviews > 0 ? reviewsList.Average(r => r.Rating) : 0;
            var fiveStarCount = reviewsList.Count(r => r.Rating == 5);
            var positiveCount = reviewsList.Count(r => r.Rating >= 4);
            var verifiedCount = reviewsList.Count(r => r.IsVerifiedPurchase);

            // Obtener badges actuales
            var currentBadges = await _badgeRepository.GetActiveBySellerIdAsync(sellerId, cancellationToken);

            // Evaluar cada tipo de badge
            await EvaluateBadge(sellerId, BadgeType.TopRated,
                averageRating >= 4.8 && totalReviews >= 10,
                "Top Rated", "Calificación promedio de 4.8+ con 10+ reviews",
                "⭐", "#FFD700",
                currentBadges, newBadgesEarned, badgesRevoked, cancellationToken);

            await EvaluateBadge(sellerId, BadgeType.TrustedDealer,
                totalReviews >= 25 && averageRating >= 4.5,
                "Dealer de Confianza", "25+ reviews con promedio de 4.5+",
                "🛡️", "#10B981",
                currentBadges, newBadgesEarned, badgesRevoked, cancellationToken);

            await EvaluateBadge(sellerId, BadgeType.FiveStarSeller,
                fiveStarCount >= 20,
                "Vendedor 5 Estrellas", "20+ reviews de 5 estrellas",
                "🌟", "#8B5CF6",
                currentBadges, newBadgesEarned, badgesRevoked, cancellationToken);

            await EvaluateBadge(sellerId, BadgeType.VerifiedProfessional,
                verifiedCount >= 15 && averageRating >= 4.5,
                "Profesional Verificado", "15+ compras verificadas con excelentes reviews",
                "✅", "#3B82F6",
                currentBadges, newBadgesEarned, badgesRevoked, cancellationToken);

            await EvaluateBadge(sellerId, BadgeType.CustomerChoice,
                positiveCount >= 30,
                "Elección del Cliente", "30+ reviews positivas (4-5 estrellas)",
                "❤️", "#EF4444",
                currentBadges, newBadgesEarned, badgesRevoked, cancellationToken);

            await EvaluateBadge(sellerId, BadgeType.RisingStar,
                totalReviews >= 5 && totalReviews < 15 && averageRating >= 4.5,
                "Estrella en Ascenso", "Vendedor nuevo con excelentes reviews",
                "🚀", "#F59E0B",
                currentBadges, newBadgesEarned, badgesRevoked, cancellationToken);

            // Obtener badges actualizados
            var updatedBadges = await _badgeRepository.GetActiveBySellerIdAsync(sellerId, cancellationToken);

            _logger.LogInformation(
                "Badges updated for seller {SellerId}. Earned: {Earned}, Revoked: {Revoked}",
                sellerId, string.Join(", ", newBadgesEarned), string.Join(", ", badgesRevoked));

            return Result<BadgeUpdateResultDto>.Success(new BadgeUpdateResultDto
            {
                SellerId = sellerId,
                CurrentBadges = updatedBadges.Select(b => new SellerBadgeDto
                {
                    Id = b.Id,
                    BadgeType = b.BadgeType.ToString(),
                    Title = b.Title ?? "",
                    Description = b.Description ?? "",
                    Icon = b.Icon ?? "",
                    Color = b.Color ?? "",
                    IsActive = b.IsActive,
                    EarnedAt = b.EarnedAt,
                    ExpiresAt = b.ExpiresAt
                }).ToList(),
                NewBadgesEarned = newBadgesEarned,
                BadgesRevoked = badgesRevoked
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating badges for seller {SellerId}", request.SellerId);
            return Result<BadgeUpdateResultDto>.Failure($"Error al actualizar badges: {ex.Message}");
        }
    }

    private async Task EvaluateBadge(
        Guid sellerId,
        BadgeType badgeType,
        bool meetsRequirements,
        string title,
        string description,
        string icon,
        string color,
        List<SellerBadge> currentBadges,
        List<string> newBadgesEarned,
        List<string> badgesRevoked,
        CancellationToken cancellationToken)
    {
        var existingBadge = currentBadges.FirstOrDefault(b => b.BadgeType == badgeType);

        if (meetsRequirements && existingBadge == null)
        {
            // Otorgar nuevo badge
            await _badgeRepository.GrantBadgeAsync(sellerId, badgeType, description, null, cancellationToken);
            newBadgesEarned.Add(title);

            _logger.LogInformation("Badge '{Title}' earned by seller {SellerId}", title, sellerId);
        }
        else if (!meetsRequirements && existingBadge != null && existingBadge.IsActive)
        {
            // Revocar badge
            await _badgeRepository.RevokeBadgeAsync(sellerId, badgeType, cancellationToken);
            badgesRevoked.Add(title);

            _logger.LogInformation("Badge '{Title}' revoked from seller {SellerId}", title, sellerId);
        }
    }
}
