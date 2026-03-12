using BillingService.Domain.Entities;

namespace BillingService.Domain.Interfaces;

/// <summary>
/// Repository for OKLA Score™ report purchases.
/// Supports querying by email (guest) and by userId (authenticated).
/// </summary>
public interface IReportPurchaseRepository
{
    Task<ReportPurchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Check if a completed purchase exists for a vehicle + email combination.</summary>
    Task<ReportPurchase?> GetCompletedPurchaseAsync(string vehicleId, string buyerEmail, CancellationToken cancellationToken = default);

    /// <summary>Check if a completed purchase exists for a vehicle + userId combination.</summary>
    Task<ReportPurchase?> GetCompletedPurchaseByUserAsync(string vehicleId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Get all purchases for a given email (for guest → registered linking).</summary>
    Task<IEnumerable<ReportPurchase>> GetByEmailAsync(string buyerEmail, CancellationToken cancellationToken = default);

    /// <summary>Get all purchases for a given user.</summary>
    Task<IEnumerable<ReportPurchase>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Find a pending purchase by PaymentIntent ID (for confirmation).</summary>
    Task<ReportPurchase?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default);

    Task<ReportPurchase> AddAsync(ReportPurchase purchase, CancellationToken cancellationToken = default);
    Task UpdateAsync(ReportPurchase purchase, CancellationToken cancellationToken = default);
}
