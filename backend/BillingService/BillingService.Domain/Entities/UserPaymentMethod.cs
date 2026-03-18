namespace BillingService.Domain.Entities;

/// <summary>
/// Represents a saved payment method for a regular user (non-dealer).
/// Stores the PayPal account reference after a successful verification capture.
/// </summary>
public class UserPaymentMethod
{
    public Guid Id { get; set; }

    /// <summary>Auth service user ID (sub claim)</summary>
    public Guid UserId { get; set; }

    /// <summary>"PayPal", "Stripe", etc.</summary>
    public string Gateway { get; set; } = string.Empty;

    /// <summary>"paypal_account", "card"</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gateway-specific identifier used for future charges.
    /// PayPal: payer_id (e.g. "QYRL5XDVJNE4Q")
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable label shown in the UI.
    /// PayPal: buyer's email address.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    public string? NickName { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }

    public int UsageCount { get; set; }
}
