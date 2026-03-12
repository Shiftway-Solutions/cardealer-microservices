using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces;
using BillingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the report purchase repository.
/// </summary>
public class ReportPurchaseRepository : IReportPurchaseRepository
{
    private readonly BillingDbContext _context;

    public ReportPurchaseRepository(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<ReportPurchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ReportPurchases
            .FirstOrDefaultAsync(rp => rp.Id == id, cancellationToken);
    }

    public async Task<ReportPurchase?> GetCompletedPurchaseAsync(string vehicleId, string buyerEmail, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = buyerEmail.ToLowerInvariant();
        return await _context.ReportPurchases
            .FirstOrDefaultAsync(
                rp => rp.VehicleId == vehicleId
                    && rp.BuyerEmail == normalizedEmail
                    && rp.Status == ReportPurchaseStatus.Completed,
                cancellationToken);
    }

    public async Task<ReportPurchase?> GetCompletedPurchaseByUserAsync(string vehicleId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ReportPurchases
            .FirstOrDefaultAsync(
                rp => rp.VehicleId == vehicleId
                    && rp.UserId == userId
                    && rp.Status == ReportPurchaseStatus.Completed,
                cancellationToken);
    }

    public async Task<IEnumerable<ReportPurchase>> GetByEmailAsync(string buyerEmail, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = buyerEmail.ToLowerInvariant();
        return await _context.ReportPurchases
            .Where(rp => rp.BuyerEmail == normalizedEmail && rp.Status == ReportPurchaseStatus.Completed)
            .OrderByDescending(rp => rp.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ReportPurchase>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ReportPurchases
            .Where(rp => rp.UserId == userId && rp.Status == ReportPurchaseStatus.Completed)
            .OrderByDescending(rp => rp.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReportPurchase?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        return await _context.ReportPurchases
            .FirstOrDefaultAsync(rp => rp.StripePaymentIntentId == paymentIntentId, cancellationToken);
    }

    public async Task<ReportPurchase> AddAsync(ReportPurchase purchase, CancellationToken cancellationToken = default)
    {
        await _context.ReportPurchases.AddAsync(purchase, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return purchase;
    }

    public async Task UpdateAsync(ReportPurchase purchase, CancellationToken cancellationToken = default)
    {
        _context.ReportPurchases.Update(purchase);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
