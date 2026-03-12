using ReviewService.Domain.Entities;
using ReviewService.Domain.Interfaces;

namespace ReviewService.Domain.Services;

/// <summary>
/// Servicio para calcular eligibilidad de badges basado en reviews
/// </summary>
public class BadgeCalculationService : IBadgeCalculationService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly ISellerBadgeRepository _sellerBadgeRepository;

    public BadgeCalculationService(
        IReviewRepository reviewRepository,
        ISellerBadgeRepository sellerBadgeRepository)
    {
        _reviewRepository = reviewRepository;
        _sellerBadgeRepository = sellerBadgeRepository;
    }

    public async Task<bool> IsEligibleForBadgeAsync(Guid sellerId, BadgeType badgeType, CancellationToken cancellationToken = default)
    {
        return badgeType switch
        {
            BadgeType.TopRated => await IsEligibleForTopRatedAsync(sellerId, cancellationToken),
            BadgeType.TrustedDealer => await IsEligibleForTrustedDealerAsync(sellerId, cancellationToken),
            BadgeType.FiveStarSeller => await IsEligibleForFiveStarSellerAsync(sellerId, cancellationToken),
            BadgeType.QuickResponder => await IsEligibleForQuickResponderAsync(sellerId, cancellationToken),
            BadgeType.VerifiedProfessional => await IsEligibleForVerifiedProfessionalAsync(sellerId, cancellationToken),
            BadgeType.CustomerChoice => await IsEligibleForCustomerChoiceAsync(sellerId, cancellationToken),
            BadgeType.RisingStar => await IsEligibleForRisingStarAsync(sellerId, cancellationToken),
            BadgeType.VolumeLeader => await IsEligibleForVolumeLeaderAsync(sellerId, cancellationToken),
            BadgeType.ConsistencyWinner => await IsEligibleForConsistencyWinnerAsync(sellerId, cancellationToken),
            BadgeType.CommunityFavorite => await IsEligibleForCommunityFavoriteAsync(sellerId, cancellationToken),
            _ => false
        };
    }

    public async Task<List<BadgeType>> CalculateAndGrantBadgesAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        var grantedBadges = new List<BadgeType>();

        // Check si ya tiene el badge activo para evitar duplicados
        var existingBadges = await _sellerBadgeRepository.GetActiveBySellerIdAsync(sellerId, cancellationToken);
        var existingBadgeTypes = existingBadges.Select(b => b.BadgeType).ToHashSet();

        // Revisar cada tipo de badge
        foreach (var badgeType in Enum.GetValues<BadgeType>())
        {
            if (existingBadgeTypes.Contains(badgeType))
                continue;

            if (await IsEligibleForBadgeAsync(sellerId, badgeType, cancellationToken))
            {
                var badge = CreateBadge(sellerId, badgeType);
                await _sellerBadgeRepository.AddAsync(badge);
                grantedBadges.Add(badgeType);
            }
        }

        return grantedBadges;
    }

    public async Task<Dictionary<Guid, List<BadgeType>>> CalculateBadgesForAllSellersAsync(CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, List<BadgeType>>();

        // Obtener todos los vendedores únicos que tienen reviews
        var sellerIds = await _reviewRepository.GetSellersWithReviewsAsync(cancellationToken);

        foreach (var sellerId in sellerIds)
        {
            var grantedBadges = await CalculateAndGrantBadgesAsync(sellerId, cancellationToken);
            if (grantedBadges.Any())
            {
                result[sellerId] = grantedBadges;
            }
        }

        return result;
    }

    /// <summary>
    /// Helper method para obtener reviews de un vendedor como lista
    /// </summary>
    private async Task<List<Review>> GetSellerReviewsAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var (reviews, _) = await _reviewRepository.GetBySellerIdAsync(sellerId, 1, 1000, onlyApproved: true);
        return reviews.ToList();
    }

    private async Task<bool> IsEligibleForTopRatedAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var reviews = await GetSellerReviewsAsync(sellerId, cancellationToken);

        if (reviews.Count < 10) return false;

        var averageRating = reviews.Average(r => r.Rating);
        return averageRating >= 4.8;
    }

    private async Task<bool> IsEligibleForTrustedDealerAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var reviews = await GetSellerReviewsAsync(sellerId, cancellationToken);

        if (reviews.Count < 5) return false;

        // 6+ meses en plataforma (basado en primera review)
        var firstReview = reviews.OrderBy(r => r.CreatedAt).First();
        var monthsActive = (DateTime.UtcNow - firstReview.CreatedAt).TotalDays / 30;

        if (monthsActive < 6) return false;

        // 95%+ reviews positivas (4+ estrellas)
        var positiveReviews = reviews.Count(r => r.Rating >= 4);
        var positivePercentage = (double)positiveReviews / reviews.Count * 100;

        return positivePercentage >= 95;
    }

    private async Task<bool> IsEligibleForFiveStarSellerAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var reviews = await GetSellerReviewsAsync(sellerId, cancellationToken);

        if (reviews.Count < 5) return false;

        // Todas las reviews deben ser 5 estrellas
        return reviews.All(r => r.Rating == 5);
    }

    private async Task<bool> IsEligibleForQuickResponderAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var reviews = await GetSellerReviewsAsync(sellerId, cancellationToken);
        var reviewsWithResponses = reviews.Where(r => r.Response != null).ToList();

        if (reviewsWithResponses.Count < 3) return false;

        // 80%+ de responses en menos de 24 horas
        var quickResponses = reviewsWithResponses.Count(r =>
            r.Response!.CreatedAt <= r.CreatedAt.AddHours(24));

        var quickResponsePercentage = (double)quickResponses / reviewsWithResponses.Count * 100;
        return quickResponsePercentage >= 80;
    }

    private async Task<bool> IsEligibleForVerifiedProfessionalAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var reviews = await GetSellerReviewsAsync(sellerId, cancellationToken);

        if (reviews.Count < 8) return false;

        var averageRating = reviews.Average(r => r.Rating);
        return averageRating >= 4.5;

        // TODO: Integrar con dealer verification service
    }

    private async Task<bool> IsEligibleForCustomerChoiceAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var reviews = await GetSellerReviewsAsync(sellerId, cancellationToken);

        if (reviews.Count < 5) return false;

        // 80%+ reviews mencionan "recomendado" o "recommend"
        var recommendedCount = reviews.Count(r =>
            !string.IsNullOrEmpty(r.Content) &&
            (r.Content.Contains("recomendado", StringComparison.OrdinalIgnoreCase) ||
             r.Content.Contains("recommend", StringComparison.OrdinalIgnoreCase)));

        var recommendedPercentage = (double)recommendedCount / reviews.Count * 100;
        return recommendedPercentage >= 80;
    }

    private async Task<bool> IsEligibleForRisingStarAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var allReviews = await GetSellerReviewsAsync(sellerId, cancellationToken);
        var last3Months = DateTime.UtcNow.AddMonths(-3);
        var recentReviews = allReviews.Where(r => r.CreatedAt >= last3Months).ToList();
        var olderReviews = allReviews.Where(r => r.CreatedAt < last3Months).ToList();

        if (recentReviews.Count < 3 || olderReviews.Count < 3) return false;

        var recentAverage = recentReviews.Average(r => r.Rating);
        var olderAverage = olderReviews.Average(r => r.Rating);

        // Mejora de al menos 0.5 puntos en rating
        return recentAverage - olderAverage >= 0.5;
    }

    private async Task<bool> IsEligibleForVolumeLeaderAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var reviews = await GetSellerReviewsAsync(sellerId, cancellationToken);
        return reviews.Count >= 50;
    }

    private async Task<bool> IsEligibleForConsistencyWinnerAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var reviews = await GetSellerReviewsAsync(sellerId, cancellationToken);
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var relevantReviews = reviews.Where(r => r.CreatedAt >= sixMonthsAgo).ToList();

        if (relevantReviews.Count < 10) return false;

        // Agrupar por mes y verificar que el rating promedio mensual no varíe más de 0.3
        var monthlyAverages = relevantReviews
            .GroupBy(r => new { Year = r.CreatedAt.Year, Month = r.CreatedAt.Month })
            .Where(g => g.Count() >= 2)
            .Select(g => g.Average(r => r.Rating))
            .ToList();

        if (monthlyAverages.Count < 3) return false;

        var maxVariation = monthlyAverages.Max() - monthlyAverages.Min();
        return maxVariation <= 0.3;
    }

    private async Task<bool> IsEligibleForCommunityFavoriteAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var reviews = await GetSellerReviewsAsync(sellerId, cancellationToken);

        if (reviews.Count < 5) return false;

        // Promedio de helpful votes por review >= 3
        var averageHelpfulVotes = reviews.Average(r => r.HelpfulVotes);
        return averageHelpfulVotes >= 3;
    }

    private static SellerBadge CreateBadge(Guid sellerId, BadgeType badgeType)
    {
        return badgeType switch
        {
            BadgeType.TopRated => new SellerBadge
            {
                SellerId = sellerId,
                BadgeType = badgeType,
                Title = "Top Rated",
                Description = "Mantiene 4.8+ estrellas con 10+ reviews",
                Icon = "⭐",
                Color = "#FFD700"
            },
            BadgeType.TrustedDealer => new SellerBadge
            {
                SellerId = sellerId,
                BadgeType = badgeType,
                Title = "Dealer Confiable",
                Description = "6+ meses activo con 95%+ reviews positivas",
                Icon = "🛡️",
                Color = "#10B981"
            },
            BadgeType.FiveStarSeller => new SellerBadge
            {
                SellerId = sellerId,
                BadgeType = badgeType,
                Title = "Vendedor 5 Estrellas",
                Description = "Solo reviews perfectas",
                Icon = "🌟",
                Color = "#F59E0B"
            },
            BadgeType.QuickResponder => new SellerBadge
            {
                SellerId = sellerId,
                BadgeType = badgeType,
                Title = "Respuesta Rápida",
                Description = "Responde en menos de 24 horas",
                Icon = "⚡",
                Color = "#3B82F6"
            },
            BadgeType.VerifiedProfessional => new SellerBadge
            {
                SellerId = sellerId,
                BadgeType = badgeType,
                Title = "Profesional Verificado",
                Description = "Documentación verificada + excelentes reviews",
                Icon = "✅",
                Color = "#059669"
            },
            BadgeType.CustomerChoice => new SellerBadge
            {
                SellerId = sellerId,
                BadgeType = badgeType,
                Title = "Elección del Cliente",
                Description = "Altamente recomendado por compradores",
                Icon = "👑",
                Color = "#7C3AED"
            },
            BadgeType.RisingStar => new SellerBadge
            {
                SellerId = sellerId,
                BadgeType = badgeType,
                Title = "Estrella Emergente",
                Description = "Mejorando constantemente",
                Icon = "🚀",
                Color = "#EC4899"
            },
            BadgeType.VolumeLeader => new SellerBadge
            {
                SellerId = sellerId,
                BadgeType = badgeType,
                Title = "Líder en Volumen",
                Description = "Más de 50 reviews",
                Icon = "📈",
                Color = "#6366F1"
            },
            BadgeType.ConsistencyWinner => new SellerBadge
            {
                SellerId = sellerId,
                BadgeType = badgeType,
                Title = "Ganador Consistente",
                Description = "Rating estable por 6+ meses",
                Icon = "🎯",
                Color = "#0891B2"
            },
            BadgeType.CommunityFavorite => new SellerBadge
            {
                SellerId = sellerId,
                BadgeType = badgeType,
                Title = "Favorito de la Comunidad",
                Description = "Reviews más útiles",
                Icon = "❤️",
                Color = "#DC2626"
            },
            _ => throw new ArgumentException($"Tipo de badge no soportado: {badgeType}")
        };
    }
}