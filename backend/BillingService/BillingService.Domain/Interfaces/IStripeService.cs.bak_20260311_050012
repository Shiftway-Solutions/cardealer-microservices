using BillingService.Domain.Entities;

namespace BillingService.Domain.Interfaces;

/// <summary>
/// Interface para interactuar con la API de Stripe
/// </summary>
public interface IStripeService
{
    // ========================================
    // CUSTOMER OPERATIONS
    // ========================================

    /// <summary>
    /// Crea un nuevo customer en Stripe
    /// </summary>
    Task<StripeCustomerResult> CreateCustomerAsync(
        string email,
        string name,
        string? phone = null,
        string? dealerId = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un customer de Stripe por su ID
    /// </summary>
    Task<StripeCustomerResult?> GetCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un customer en Stripe
    /// </summary>
    Task<StripeCustomerResult> UpdateCustomerAsync(
        string customerId,
        string? email = null,
        string? name = null,
        string? phone = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un customer de Stripe
    /// </summary>
    Task<bool> DeleteCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    // ========================================
    // SUBSCRIPTION OPERATIONS
    // ========================================

    /// <summary>
    /// Crea una suscripción en Stripe
    /// </summary>
    Task<StripeSubscriptionResult> CreateSubscriptionAsync(
        string customerId,
        string priceId,
        int trialDays = 0,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una suscripción de Stripe
    /// </summary>
    Task<StripeSubscriptionResult?> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza una suscripción (upgrade/downgrade)
    /// </summary>
    Task<StripeSubscriptionResult> UpdateSubscriptionAsync(
        string subscriptionId,
        string newPriceId,
        bool prorationBehavior = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancela una suscripción
    /// </summary>
    Task<StripeSubscriptionResult> CancelSubscriptionAsync(
        string subscriptionId,
        bool cancelAtPeriodEnd = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactiva una suscripción cancelada (si aún está en período)
    /// </summary>
    Task<StripeSubscriptionResult> ReactivateSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);

    // ========================================
    // PAYMENT METHOD OPERATIONS
    // ========================================

    /// <summary>
    /// Adjunta un método de pago a un customer
    /// </summary>
    Task<StripePaymentMethodResult> AttachPaymentMethodAsync(
        string paymentMethodId,
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Establece el método de pago por defecto
    /// </summary>
    Task<bool> SetDefaultPaymentMethodAsync(
        string customerId,
        string paymentMethodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene los métodos de pago de un customer
    /// </summary>
    Task<IEnumerable<StripePaymentMethodResult>> GetPaymentMethodsAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un método de pago
    /// </summary>
    Task<bool> DetachPaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default);

    // ========================================
    // INVOICE OPERATIONS
    // ========================================

    /// <summary>
    /// Obtiene una factura de Stripe
    /// </summary>
    Task<StripeInvoiceResult?> GetInvoiceAsync(
        string invoiceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las facturas de un customer
    /// </summary>
    Task<IEnumerable<StripeInvoiceResult>> GetInvoicesAsync(
        string customerId,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Paga una factura pendiente
    /// </summary>
    Task<StripeInvoiceResult> PayInvoiceAsync(
        string invoiceId,
        CancellationToken cancellationToken = default);

    // ========================================
    // CHECKOUT & BILLING PORTAL
    // ========================================

    /// <summary>
    /// Crea una sesión de Checkout para suscripción
    /// </summary>
    Task<StripeCheckoutResult> CreateCheckoutSessionAsync(
        string customerId,
        string priceId,
        string successUrl,
        string cancelUrl,
        int trialDays = 0,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea una sesión del portal de facturación
    /// </summary>
    Task<StripeBillingPortalResult> CreateBillingPortalSessionAsync(
        string customerId,
        string returnUrl,
        CancellationToken cancellationToken = default);

    // ========================================
    // WEBHOOK HANDLING
    // ========================================

    /// <summary>
    /// Verifica y parsea un evento de webhook
    /// </summary>
    StripeWebhookEvent? ConstructWebhookEvent(
        string payload,
        string signature,
        string webhookSecret);
}

// ========================================
// RESULT DTOs
// ========================================

public record StripeCustomerResult(
    string Id,
    string Email,
    string Name,
    string? Phone,
    string? DefaultPaymentMethodId,
    long Balance,
    string Currency,
    bool IsDeleted,
    DateTime Created
);

public record StripeSubscriptionResult(
    string Id,
    string CustomerId,
    string Status,
    string PriceId,
    decimal Amount,
    string Currency,
    string Interval,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    DateTime? TrialStart,
    DateTime? TrialEnd,
    DateTime? CancelAt,
    bool CancelAtPeriodEnd,
    DateTime Created
);

public record StripePaymentMethodResult(
    string Id,
    string Type,
    string? CardBrand,
    string? CardLast4,
    int? CardExpMonth,
    int? CardExpYear,
    bool IsDefault
);

public record StripeInvoiceResult(
    string Id,
    string CustomerId,
    string? SubscriptionId,
    string Status,
    decimal AmountDue,
    decimal AmountPaid,
    string Currency,
    string? InvoicePdf,
    string? HostedInvoiceUrl,
    DateTime Created,
    DateTime? DueDate,
    DateTime? PaidAt
);

public record StripeCheckoutResult(
    string SessionId,
    string Url
);

public record StripeBillingPortalResult(
    string Url
);

public record StripeWebhookEvent(
    string Id,
    string Type,
    object Data,
    DateTime Created
);
