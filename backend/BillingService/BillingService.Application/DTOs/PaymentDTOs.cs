namespace BillingService.Application.DTOs;

public record PaymentDto(
    Guid Id,
    Guid? SubscriptionId,
    Guid? InvoiceId,
    decimal Amount,
    string Currency,
    string Status,
    string Method,
    string? StripePaymentIntentId,
    string? Description,
    string? ReceiptUrl,
    string? FailureReason,
    decimal RefundedAmount,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);

public record CreatePaymentRequest(
    decimal Amount,
    string Method,
    string? Description = null,
    Guid? SubscriptionId = null,
    Guid? InvoiceId = null
);

public record RefundPaymentRequest(
    decimal Amount,
    string Reason
);

// Payment Methods DTOs
public record PaymentMethodDto(
    string Id,
    string Type,
    bool IsDefault,
    CardInfoDto? Card,
    BankAccountInfoDto? BankAccount,
    string CreatedAt
);

public record CardInfoDto(
    string Brand,
    string Last4,
    int ExpMonth,
    int ExpYear
);

public record BankAccountInfoDto(
    string BankName,
    string Last4,
    string AccountType
);

public record AddPaymentMethodRequest
{
    public string Type { get; init; } = "card";
    public bool SetAsDefault { get; init; } = false;

    // Card details (for direct card entry, not recommended for production)
    public string? CardNumber { get; init; }
    public int? ExpMonth { get; init; }
    public int? ExpYear { get; init; }
    public string? Cvv { get; init; }

    // Token from payment processor (Azul/Stripe)
    public string? Token { get; init; }

    // Bank account details
    public string? AccountNumber { get; init; }
    public string? RoutingNumber { get; init; }
    public string? AccountType { get; init; }
}
