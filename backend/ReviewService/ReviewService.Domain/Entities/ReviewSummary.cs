using ReviewService.Domain.Base;

namespace ReviewService.Domain.Entities;

/// <summary>
/// Estadísticas agregadas de reviews para un vendedor
/// Para performance y evitar cálculos en tiempo real
/// </summary>
public class ReviewSummary : BaseEntity<Guid>
{
    /// <summary>
    /// ID del vendedor
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Total de reviews recibidas
    /// </summary>
    public int TotalReviews { get; set; } = 0;

    /// <summary>
    /// Rating promedio (1.0 - 5.0)
    /// </summary>
    public decimal AverageRating { get; set; } = 0;

    /// <summary>
    /// Cantidad de reviews de 5 estrellas
    /// </summary>
    public int FiveStarReviews { get; set; } = 0;

    /// <summary>
    /// Cantidad de reviews de 4 estrellas
    /// </summary>
    public int FourStarReviews { get; set; } = 0;

    /// <summary>
    /// Cantidad de reviews de 3 estrellas
    /// </summary>
    public int ThreeStarReviews { get; set; } = 0;

    /// <summary>
    /// Cantidad de reviews de 2 estrellas
    /// </summary>
    public int TwoStarReviews { get; set; } = 0;

    /// <summary>
    /// Cantidad de reviews de 1 estrella
    /// </summary>
    public int OneStarReviews { get; set; } = 0;

    /// <summary>
    /// Fecha de la última review recibida
    /// </summary>
    public DateTime? LastReviewDate { get; set; }

    /// <summary>
    /// Porcentaje de reviews positivas (4-5 estrellas)
    /// </summary>
    public decimal PositivePercentage { get; set; } = 0;

    /// <summary>
    /// Total de compras verificadas que tienen review
    /// </summary>
    public int VerifiedPurchaseReviews { get; set; } = 0;

    /// <summary>
    /// Calcular distribución de ratings
    /// </summary>
    public Dictionary<int, int> GetRatingDistribution()
    {
        return new Dictionary<int, int>
        {
            { 5, FiveStarReviews },
            { 4, FourStarReviews },
            { 3, ThreeStarReviews },
            { 2, TwoStarReviews },
            { 1, OneStarReviews }
        };
    }

    /// <summary>
    /// Recalcular todas las métricas basadas en las reviews actuales
    /// </summary>
    public void RecalculateMetrics(IEnumerable<Review> reviews)
    {
        var reviewList = reviews.Where(r => r.IsApproved).ToList();

        TotalReviews = reviewList.Count;

        if (TotalReviews > 0)
        {
            AverageRating = (decimal)reviewList.Average(r => r.Rating);

            FiveStarReviews = reviewList.Count(r => r.Rating == 5);
            FourStarReviews = reviewList.Count(r => r.Rating == 4);
            ThreeStarReviews = reviewList.Count(r => r.Rating == 3);
            TwoStarReviews = reviewList.Count(r => r.Rating == 2);
            OneStarReviews = reviewList.Count(r => r.Rating == 1);

            PositivePercentage = (decimal)(FiveStarReviews + FourStarReviews) / TotalReviews * 100;
            VerifiedPurchaseReviews = reviewList.Count(r => r.IsVerifiedPurchase);
            LastReviewDate = reviewList.Max(r => r.CreatedAt);
        }
        else
        {
            // Reset valores
            AverageRating = 0;
            FiveStarReviews = FourStarReviews = ThreeStarReviews = TwoStarReviews = OneStarReviews = 0;
            PositivePercentage = 0;
            VerifiedPurchaseReviews = 0;
            LastReviewDate = null;
        }

        UpdatedAt = DateTime.UtcNow;
    }
}