using CarDealer.Contracts.Abstractions;

namespace CarDealer.Contracts.Events.Billing;

/// <summary>
/// Published when a buyer completes the purchase of an OKLA Score report.
/// Consumed by NotificationService to send the receipt email.
/// </summary>
public class ReportPurchaseCompletedEvent : EventBase
{
    public override string EventType => "billing.report.purchase.completed";

    public Guid PurchaseId { get; set; }
    public string VehicleId { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string ProductId { get; set; } = "okla-score-report";
    public long AmountCents { get; set; }
    public string Currency { get; set; } = "usd";
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}
