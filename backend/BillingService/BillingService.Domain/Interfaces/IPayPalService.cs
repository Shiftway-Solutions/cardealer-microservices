namespace BillingService.Domain.Interfaces;

/// <summary>
/// Interface para interactuar con la API REST de PayPal v2
/// </summary>
public interface IPayPalService
{
    /// <summary>
    /// Crea una orden de PayPal para un pago único
    /// </summary>
    /// <param name="intent">"CAPTURE" (cobro inmediato, default) o "AUTHORIZE" (autorizar sin cobrar)</param>
    /// <returns>PayPal Order ID</returns>
    Task<PayPalOrderResult> CreateOrderAsync(
        decimal amount,
        string currency,
        string description,
        string returnUrl,
        string cancelUrl,
        Dictionary<string, string>? metadata = null,
        string intent = "CAPTURE",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Captura (cobra) una orden aprobada por el comprador
    /// </summary>
    Task<PayPalCaptureResult> CaptureOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el estado de una orden
    /// </summary>
    Task<PayPalOrderResult?> GetOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reembolsa un pago capturado (total o parcial)
    /// </summary>
    Task<PayPalRefundResult> RefundCaptureAsync(
        string captureId,
        decimal? amount = null,
        string? currency = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica la firma de un webhook de PayPal
    /// </summary>
    Task<bool> VerifyWebhookSignatureAsync(
        string webhookId,
        IReadOnlyDictionary<string, string> headers,
        string body,
        CancellationToken cancellationToken = default);
}

// =============================================================================
// RESULT TYPES
// =============================================================================

public record PayPalOrderResult(
    string OrderId,
    string Status,
    string? ApprovalUrl,
    decimal Amount,
    string Currency,
    DateTime CreatedAt,
    string? PayerEmail = null,
    string? PayerId = null
);

public record PayPalCaptureResult(
    string OrderId,
    string CaptureId,
    string Status,
    decimal Amount,
    string Currency,
    string? ReceiptUrl,
    string? PayerEmail,
    string? PayerId
);

public record PayPalRefundResult(
    string RefundId,
    string Status,
    decimal Amount,
    string Currency
);
