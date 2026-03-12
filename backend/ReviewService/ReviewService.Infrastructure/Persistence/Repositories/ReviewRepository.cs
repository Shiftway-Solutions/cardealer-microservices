using Microsoft.EntityFrameworkCore;
using ReviewService.Domain.Entities;
using ReviewService.Domain.Interfaces;
using ReviewService.Domain.Base;
using ReviewService.Infrastructure.Persistence;

namespace ReviewService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación del repositorio para Reviews
/// </summary>
public class ReviewRepository : Repository<Review, Guid>, IReviewRepository
{
    private new readonly ReviewDbContext _context;

    public ReviewRepository(ReviewDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtener reviews de un vendedor con paginación
    /// </summary>
    public async Task<(IEnumerable<Review> Reviews, int TotalCount)> GetBySellerIdAsync(
        Guid sellerId,
        int page = 1,
        int pageSize = 20,
        bool onlyApproved = true)
    {
        var query = _context.Reviews
            .Include(r => r.Response)
            .Where(r => r.SellerId == sellerId);

        if (onlyApproved)
        {
            query = query.Where(r => r.IsApproved);
        }

        var totalCount = await query.CountAsync();

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (reviews, totalCount);
    }

    /// <summary>
    /// Obtener reviews de un comprador
    /// </summary>
    public async Task<IEnumerable<Review>> GetByBuyerIdAsync(Guid buyerId)
    {
        return await _context.Reviews
            .Include(r => r.Response)
            .Where(r => r.BuyerId == buyerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Verificar si un comprador ya dejó review para un vendedor específico
    /// </summary>
    public async Task<bool> HasBuyerReviewedSellerAsync(Guid buyerId, Guid sellerId, Guid? vehicleId = null)
    {
        var query = _context.Reviews
            .Where(r => r.BuyerId == buyerId && r.SellerId == sellerId);

        if (vehicleId.HasValue)
        {
            query = query.Where(r => r.VehicleId == vehicleId.Value);
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// Obtener review por OrderId para validar compra verificada
    /// </summary>
    public async Task<Review?> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.Reviews
            .Include(r => r.Response)
            .FirstOrDefaultAsync(r => r.OrderId == orderId);
    }

    /// <summary>
    /// Obtener reviews recientes para moderación
    /// </summary>
    public async Task<IEnumerable<Review>> GetPendingModerationAsync(int limit = 50)
    {
        return await _context.Reviews
            .Where(r => !r.IsApproved && r.ModeratedById == null)
            .OrderBy(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Buscar reviews por contenido (para moderar)
    /// </summary>
    public async Task<IEnumerable<Review>> SearchByContentAsync(string searchTerm)
    {
        // Security (CWE-943): Escape LIKE wildcard characters to prevent wildcard injection
        var escapedTerm = searchTerm
            .Replace("\\", "\\\\")  // Escape backslash first
            .Replace("%", "\\%")    // Escape % wildcard
            .Replace("_", "\\_");   // Escape _ wildcard

        return await _context.Reviews
            .Where(r => EF.Functions.ILike(r.Title, $"%{escapedTerm}%") ||
                        EF.Functions.ILike(r.Content, $"%{escapedTerm}%"))
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    /// <summary>
    /// Obtener top reviews (más útiles) para un vendedor
    /// </summary>
    public async Task<IEnumerable<Review>> GetTopReviewsAsync(Guid sellerId, int limit = 5)
    {
        return await _context.Reviews
            .Where(r => r.SellerId == sellerId && r.IsApproved)
            .OrderByDescending(r => r.HelpfulVotes)
            .ThenByDescending(r => r.Rating)
            .ThenByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Override GetByIdAsync para incluir Response
    /// </summary>
    public override async Task<Review?> GetByIdAsync(Guid id)
    {
        return await _context.Reviews
            .Include(r => r.Response)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <summary>
    /// Sprint 15 - Obtener estadísticas completas de un vendedor
    /// </summary>
    public async Task<SellerReviewStats> GetSellerStatsAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        var reviews = await _context.Reviews
            .Where(r => r.SellerId == sellerId && r.IsApproved)
            .ToListAsync(cancellationToken);

        if (!reviews.Any())
        {
            return new SellerReviewStats
            {
                AverageRating = 0,
                TotalReviews = 0,
                ResponseRate = 0,
                FirstReviewDate = DateTime.UtcNow,
                LastReviewDate = DateTime.UtcNow
            };
        }

        var totalReviews = reviews.Count;
        var averageRating = reviews.Average(r => r.Rating);
        var reviewsWithResponse = reviews.Count(r => r.Response != null);
        var responseRate = totalReviews > 0 ? (decimal)reviewsWithResponse / totalReviews * 100 : 0;

        // Contar total de votos útiles recibidos
        var totalHelpfulVotes = await _context.ReviewHelpfulVotes
            .Where(v => reviews.Select(r => r.Id).Contains(v.ReviewId) && v.IsHelpful)
            .CountAsync(cancellationToken);

        return new SellerReviewStats
        {
            AverageRating = (decimal)averageRating,
            TotalReviews = totalReviews,
            FiveStarCount = reviews.Count(r => r.Rating == 5),
            FourStarCount = reviews.Count(r => r.Rating == 4),
            ThreeStarCount = reviews.Count(r => r.Rating == 3),
            TwoStarCount = reviews.Count(r => r.Rating == 2),
            OneStarCount = reviews.Count(r => r.Rating == 1),
            ResponseRate = responseRate,
            TotalHelpfulVotes = totalHelpfulVotes,
            FirstReviewDate = reviews.Min(r => r.CreatedAt),
            LastReviewDate = reviews.Max(r => r.CreatedAt)
        };
    }

    /// <summary>
    /// Sprint 15 - Obtener lista de vendedores que tienen reviews
    /// </summary>
    public async Task<List<Guid>> GetSellersWithReviewsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .Where(r => r.IsApproved)
            .Select(r => r.SellerId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    // =========================================================
    // Admin methods
    // =========================================================

    /// <summary>
    /// Obtener todas las reviews con paginación y filtros (admin)
    /// </summary>
    public async Task<(IEnumerable<Review> Reviews, int TotalCount)> GetAdminReviewsAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? statusFilter = null)
    {
        var query = _context.Reviews.Include(r => r.Response).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(r =>
                r.BuyerName.ToLower().Contains(term) ||
                r.Title.ToLower().Contains(term) ||
                r.Content.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = statusFilter switch
            {
                "approved" => query.Where(r => r.IsApproved && !r.IsFlagged),
                "pending" => query.Where(r => !r.IsApproved && r.ModeratedById == null),
                "rejected" => query.Where(r => !r.IsApproved && r.ModeratedById != null),
                "reported" => query.Where(r => r.IsFlagged),
                _ => query
            };
        }

        var totalCount = await query.CountAsync();
        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (reviews, totalCount);
    }

    /// <summary>
    /// Obtener reviews reportadas/flagged (admin)
    /// </summary>
    public async Task<IEnumerable<Review>> GetFlaggedReviewsAsync(int limit = 100)
    {
        return await _context.Reviews
            .Include(r => r.Response)
            .Where(r => r.IsFlagged)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Obtener estadísticas globales de reviews (admin)
    /// </summary>
    public async Task<AdminReviewStats> GetAdminStatsAsync()
    {
        var total = await _context.Reviews.CountAsync();
        var approved = await _context.Reviews.CountAsync(r => r.IsApproved && !r.IsFlagged);
        var pending = await _context.Reviews.CountAsync(r => !r.IsApproved && r.ModeratedById == null);
        var rejected = await _context.Reviews.CountAsync(r => !r.IsApproved && r.ModeratedById != null);
        var flagged = await _context.Reviews.CountAsync(r => r.IsFlagged);
        var avgRating = total > 0
            ? (decimal)await _context.Reviews.AverageAsync(r => (double)r.Rating)
            : 0m;

        return new AdminReviewStats
        {
            TotalReviews = total,
            ApprovedReviews = approved,
            PendingReviews = pending,
            RejectedReviews = rejected,
            FlaggedReviews = flagged,
            AverageRating = Math.Round(avgRating, 2)
        };
    }

    /// <summary>
    /// Eliminar una review sin validación de propiedad (solo admin)
    /// </summary>
    public async Task AdminDeleteAsync(Guid reviewId)
    {
        var review = await _context.Reviews.FindAsync(reviewId);
        if (review != null)
        {
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
        }
    }
}