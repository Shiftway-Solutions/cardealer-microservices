namespace BillingService.Domain.Entities;

public class AzulTransaction
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string AzulOrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal ITBIS { get; set; }
    public string AuthorizationCode { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
    public string IsoCode { get; set; } = string.Empty;
    public string ResponseMessage { get; set; } = string.Empty;
    public string ErrorDescription { get; set; } = string.Empty;
    public string RRN { get; set; } = string.Empty;
    public DateTime TransactionDateTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty; // Approved, Declined, Cancelled, Error

    // DataVault fields (tokenización)
    public string? DataVaultToken { get; set; }
    public string? DataVaultExpiration { get; set; }
    public string? DataVaultBrand { get; set; }

    // Relación con usuario (opcional)
    public Guid? UserId { get; set; }

    // Currency tracking (DOP/USD) — critical for Dominican card reconciliation
    public string Currency { get; set; } = "DOP";

    // Decline reason localized in Spanish for Dominican banks
    public string? DeclineReasonLocalized { get; set; }

    // Metadata adicional
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
