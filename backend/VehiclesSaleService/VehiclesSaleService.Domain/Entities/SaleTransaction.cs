namespace VehiclesSaleService.Domain.Entities;

/// <summary>
/// Represents a completed sale transaction between a seller and buyer.
/// Tracks the full lifecycle of a vehicle sale for analytics, fraud detection,
/// and OKLA Score integration.
/// </summary>
public class SaleTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Vehicle that was sold</summary>
    public Guid VehicleId { get; set; }

    /// <summary>Seller who listed the vehicle</summary>
    public Guid SellerId { get; set; }

    /// <summary>Seller type: Individual, Dealer</summary>
    public string SellerType { get; set; } = string.Empty;

    /// <summary>Buyer's email (optional, captured at sale close)</summary>
    public string? BuyerEmail { get; set; }

    /// <summary>Buyer's user ID if registered on OKLA</summary>
    public Guid? BuyerId { get; set; }

    /// <summary>Original listed price when published</summary>
    public decimal ListedPrice { get; set; }

    /// <summary>Final sale price</summary>
    public decimal SalePrice { get; set; }

    /// <summary>Price difference (ListedPrice - SalePrice)</summary>
    public decimal PriceDifference => ListedPrice - SalePrice;

    /// <summary>Percentage discount from listed price</summary>
    public decimal DiscountPercentage => ListedPrice > 0
        ? Math.Round((ListedPrice - SalePrice) / ListedPrice * 100, 2)
        : 0;

    /// <summary>Currency code (DOP or USD)</summary>
    public string Currency { get; set; } = "DOP";

    /// <summary>Vehicle title for display</summary>
    public string VehicleTitle { get; set; } = string.Empty;

    /// <summary>Vehicle VIN number</summary>
    public string? Vin { get; set; }

    /// <summary>Vehicle make</summary>
    public string? Make { get; set; }

    /// <summary>Vehicle model</summary>
    public string? Model { get; set; }

    /// <summary>Vehicle year</summary>
    public int? Year { get; set; }

    /// <summary>When the vehicle was first listed</summary>
    public DateTime ListedAt { get; set; }

    /// <summary>When the sale was recorded</summary>
    public DateTime SoldAt { get; set; }

    /// <summary>Days from listing to sale</summary>
    public int DaysToSell => (int)(SoldAt - ListedAt).TotalDays;

    /// <summary>
    /// Confidence level of the sale transaction.
    /// High = buyer confirmed, Medium = seller confirmed only, Low = unconfirmed
    /// </summary>
    public SaleConfidenceLevel ConfidenceLevel { get; set; } = SaleConfidenceLevel.Medium;

    /// <summary>
    /// Fraud score (0-100). 100 = no risk, 0 = high risk.
    /// Decremented by risk signals.
    /// </summary>
    public int FraudScore { get; set; } = 100;

    /// <summary>Whether the buyer has confirmed this purchase</summary>
    public bool BuyerConfirmed { get; set; }

    /// <summary>When the buyer confirmed the purchase</summary>
    public DateTime? BuyerConfirmedAt { get; set; }

    /// <summary>Optional notes about the sale</summary>
    public string? Notes { get; set; }

    /// <summary>IP address of the request that created this transaction</summary>
    public string? IpAddress { get; set; }

    /// <summary>User agent of the request</summary>
    public string? UserAgent { get; set; }

    /// <summary>Tenant ID for multi-tenant support</summary>
    public string? TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Confidence level of a sale transaction
/// </summary>
public enum SaleConfidenceLevel
{
    /// <summary>Rejected — fraud detected or disputed</summary>
    Rejected = 0,

    /// <summary>Low confidence — minimal verification</summary>
    Low = 1,

    /// <summary>Medium — seller confirmed only</summary>
    Medium = 2,

    /// <summary>High — both buyer and seller confirmed</summary>
    High = 3
}
