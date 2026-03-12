namespace BillingService.Domain.Entities;

/// <summary>
/// Represents a one-time purchase of an OKLA Score™ report.
/// Supports both authenticated users (via UserId) and guest buyers (via BuyerEmail).
/// After a guest registers with the same email, the purchase is linked to their account.
/// </summary>
public enum ReportPurchaseStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

public class ReportPurchase
{
    public Guid Id { get; private set; }

    /// <summary>VIN or vehicle listing ID that was scored.</summary>
    public string VehicleId { get; private set; } = string.Empty;

    /// <summary>Product identifier, e.g. "okla-score-report".</summary>
    public string ProductId { get; private set; } = string.Empty;

    /// <summary>Email of the buyer (always present — used as key for guest purchases).</summary>
    public string BuyerEmail { get; private set; } = string.Empty;

    /// <summary>
    /// Optional: OKLA user ID. Null for guest purchases.
    /// Set when the buyer is authenticated at purchase time, or linked post-registration.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>Stripe PaymentIntent ID for this purchase.</summary>
    public string StripePaymentIntentId { get; private set; } = string.Empty;

    /// <summary>Amount charged in the smallest currency unit (e.g. cents for USD).</summary>
    public long AmountCents { get; private set; }

    /// <summary>ISO currency code (e.g. "usd").</summary>
    public string Currency { get; private set; } = "usd";

    public ReportPurchaseStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Concurrency token for optimistic concurrency.</summary>
    public string ConcurrencyStamp { get; private set; } = Guid.NewGuid().ToString();

    private ReportPurchase() { }

    public ReportPurchase(
        string vehicleId,
        string productId,
        string buyerEmail,
        string stripePaymentIntentId,
        long amountCents,
        string currency = "usd",
        Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(vehicleId))
            throw new ArgumentException("VehicleId is required", nameof(vehicleId));
        if (string.IsNullOrWhiteSpace(buyerEmail))
            throw new ArgumentException("BuyerEmail is required", nameof(buyerEmail));
        if (string.IsNullOrWhiteSpace(stripePaymentIntentId))
            throw new ArgumentException("StripePaymentIntentId is required", nameof(stripePaymentIntentId));

        Id = Guid.NewGuid();
        VehicleId = vehicleId;
        ProductId = productId;
        BuyerEmail = buyerEmail.ToLowerInvariant();
        StripePaymentIntentId = stripePaymentIntentId;
        AmountCents = amountCents;
        Currency = currency;
        UserId = userId;
        Status = ReportPurchaseStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = ReportPurchaseStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void MarkFailed()
    {
        Status = ReportPurchaseStatus.Failed;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Links this guest purchase to a registered user account.
    /// Called when a user registers with the same email used for the guest purchase.
    /// </summary>
    public void LinkToUser(Guid userId)
    {
        UserId = userId;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }
}
