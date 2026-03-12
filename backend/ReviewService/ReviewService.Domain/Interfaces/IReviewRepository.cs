using ReviewService.Domain.Base;
using ReviewService.Domain.Entities;

namespace ReviewService.Domain.Interfaces;

/// <summary>
/// Repository para gestión de reviews
/// </summary>
public interface IReviewRepository : IRepository<Review, Guid>
{
    /// <summary>
    /// Obtener reviews de un vendedor con paginación
    /// </summary>
    Task<(IEnumerable<Review> Reviews, int TotalCount)> GetBySellerIdAsync(
        Guid sellerId,
        int page = 1,
        int pageSize = 20,
        bool onlyApproved = true);

    /// <summary>
    /// Obtener reviews de un comprador
    /// </summary>
    Task<IEnumerable<Review>> GetByBuyerIdAsync(Guid buyerId);

    /// <summary>
    /// Verificar si un comprador ya dejó review para un vendedor específico
    /// </summary>
    Task<bool> HasBuyerReviewedSellerAsync(Guid buyerId, Guid sellerId, Guid? vehicleId = null);

    /// <summary>
    /// Obtener review por OrderId para validar compra verificada
    /// </summary>
    Task<Review?> GetByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Obtener reviews recientes para moderación
    /// </summary>
    Task<IEnumerable<Review>> GetPendingModerationAsync(int limit = 50);

    /// <summary>
    /// Buscar reviews por contenido (para moderar)
    /// </summary>
    Task<IEnumerable<Review>> SearchByContentAsync(string searchTerm);

    /// <summary>
    /// Obtener top reviews (más útiles) para un vendedor
    /// </summary>
    Task<IEnumerable<Review>> GetTopReviewsAsync(Guid sellerId, int limit = 5);

    /// <summary>
    /// Obtener estadísticas completas de un vendedor
    /// </summary>
    Task<SellerReviewStats> GetSellerStatsAsync(Guid sellerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtener lista de vendedores que tienen reviews
    /// </summary>
    Task<List<Guid>> GetSellersWithReviewsAsync(CancellationToken cancellationToken = default);

    // =========================================================
    // Admin methods
    // =========================================================

    /// <summary>
    /// Obtener todas las reviews con paginación y filtros (admin)
    /// </summary>
    Task<(IEnumerable<Review> Reviews, int TotalCount)> GetAdminReviewsAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? statusFilter = null);

    /// <summary>
    /// Obtener reviews reportadas/flagged (admin)
    /// </summary>
    Task<IEnumerable<Review>> GetFlaggedReviewsAsync(int limit = 100);

    /// <summary>
    /// Obtener estadísticas globales de reviews (admin)
    /// </summary>
    Task<AdminReviewStats> GetAdminStatsAsync();

    /// <summary>
    /// Eliminar una review sin validación de propiedad (solo admin)
    /// </summary>
    Task AdminDeleteAsync(Guid reviewId);
}

/// <summary>
/// Estadísticas globales de reviews para el panel de admin
/// </summary>
public class AdminReviewStats
{
    public int TotalReviews { get; set; }
    public int ApprovedReviews { get; set; }
    public int PendingReviews { get; set; }
    public int RejectedReviews { get; set; }
    public int FlaggedReviews { get; set; }
    public decimal AverageRating { get; set; }
}

/// <summary>
/// Estadísticas de reviews para un vendedor
/// </summary>
public class SellerReviewStats
{
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }
    public decimal ResponseRate { get; set; }
    public int TotalHelpfulVotes { get; set; }
    public DateTime FirstReviewDate { get; set; }
    public DateTime LastReviewDate { get; set; }
}