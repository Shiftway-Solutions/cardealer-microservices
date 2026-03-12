using Microsoft.EntityFrameworkCore;
using ReviewService.Domain.Entities;
using ReviewService.Domain.Interfaces;
using ReviewService.Infrastructure.Persistence;

namespace ReviewService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación del repositorio de solicitudes de review
/// </summary>
public class ReviewRequestRepository : Repository<ReviewRequest, Guid>, IReviewRequestRepository
{
    public ReviewRequestRepository(ReviewDbContext context) : base(context)
    {
    }

    public async Task<ReviewRequest?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewRequests
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
    }

    public async Task<ReviewRequest?> GetPendingBySellerAndBuyerAsync(Guid sellerId, Guid buyerId, Guid? vehicleId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ReviewRequests
            .Where(x => x.SellerId == sellerId && x.BuyerId == buyerId && x.Status != ReviewRequestStatus.Completed);

        if (vehicleId.HasValue)
        {
            query = query.Where(x => x.VehicleId == vehicleId);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<ReviewRequest>> GetPendingRequestsAsync(int daysAfterPurchase = 7, int limit = 100, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysAfterPurchase);

        return await _context.ReviewRequests
            .Where(x => x.Status == ReviewRequestStatus.Sent &&
                       x.PurchaseDate <= cutoffDate &&
                       x.ExpiresAt > DateTime.UtcNow)
            .OrderBy(x => x.PurchaseDate)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReviewRequest>> GetReminderRequestsAsync(int daysAfterRequest = 3, int maxReminders = 2, int limit = 100, CancellationToken cancellationToken = default)
    {
        var reminderCutoff = DateTime.UtcNow.AddDays(-daysAfterRequest);

        return await _context.ReviewRequests
            .Where(x => x.Status == ReviewRequestStatus.Sent &&
                       x.ExpiresAt > DateTime.UtcNow &&
                       x.RemindersSent < maxReminders &&
                       x.RequestSentAt <= reminderCutoff &&
                       (x.LastReminderAt == null || x.LastReminderAt <= reminderCutoff))
            .OrderBy(x => x.RequestSentAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReviewRequest>> GetByBuyerIdAsync(Guid buyerId, ReviewRequestStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ReviewRequests.Where(x => x.BuyerId == buyerId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderByDescending(x => x.RequestSentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReviewRequest>> GetBySellerIdAsync(Guid sellerId, ReviewRequestStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ReviewRequests.Where(x => x.SellerId == sellerId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderByDescending(x => x.RequestSentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkAsCompletedAsync(Guid requestId, Guid reviewId, CancellationToken cancellationToken = default)
    {
        var request = await _context.ReviewRequests.FindAsync(new object[] { requestId }, cancellationToken);
        if (request != null)
        {
            request.Status = ReviewRequestStatus.Completed;
            request.ReviewId = reviewId;
            request.ReviewCreatedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        return false;
    }

    public async Task<int> ExpireOldRequestsAsync(CancellationToken cancellationToken = default)
    {
        var expiredRequests = await _context.ReviewRequests
            .Where(x => x.Status == ReviewRequestStatus.Sent && x.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var request in expiredRequests)
        {
            request.Status = ReviewRequestStatus.Expired;
            request.UpdatedAt = DateTime.UtcNow;
        }

        if (expiredRequests.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return expiredRequests.Count;
    }

    public async Task<Dictionary<ReviewRequestStatus, int>> GetStatsAsync(Guid? sellerId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ReviewRequests.AsQueryable();

        if (sellerId.HasValue)
            query = query.Where(x => x.SellerId == sellerId.Value);

        if (fromDate.HasValue)
            query = query.Where(x => x.RequestSentAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.RequestSentAt <= toDate.Value);

        var stats = await query
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<ReviewRequestStatus, int>();
        foreach (var stat in stats)
        {
            result[stat.Status] = stat.Count;
        }

        // Ensure all statuses are represented
        foreach (var status in Enum.GetValues<ReviewRequestStatus>())
        {
            if (!result.ContainsKey(status))
                result[status] = 0;
        }

        return result;
    }

    public async Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewRequests
            .AnyAsync(x => x.OrderId == orderId, cancellationToken);
    }

    public async Task IncrementReminderCountAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await _context.ReviewRequests.FindAsync(new object[] { requestId }, cancellationToken);
        if (request != null)
        {
            request.RemindersSent++;
            request.LastReminderAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}