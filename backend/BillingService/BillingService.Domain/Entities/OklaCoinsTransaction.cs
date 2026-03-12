namespace BillingService.Domain.Entities;

/// <summary>
/// Registro de cada transacción de OKLA Coins.
/// Proporciona trazabilidad completa del historial de coins.
/// </summary>
public class OklaCoinsTransaction
{
    public Guid Id { get; set; }

    /// <summary>
    /// FK a la billetera del dealer
    /// </summary>
    public Guid WalletId { get; set; }

    /// <summary>
    /// ID del dealer
    /// </summary>
    public Guid DealerId { get; set; }

    /// <summary>
    /// Tipo de transacción
    /// </summary>
    public CoinTransactionType Type { get; set; }

    /// <summary>
    /// Cantidad de coins (positivo para créditos, negativo para débitos)
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Balance después de la transacción
    /// </summary>
    public int BalanceAfter { get; set; }

    /// <summary>
    /// Descripción de la transacción
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// ID del paquete comprado (si es tipo Purchase)
    /// </summary>
    public string? PackageSlug { get; set; }

    /// <summary>
    /// ID del producto publicitario consumido (si es tipo Spend)
    /// </summary>
    public Guid? AdvertisingProductId { get; set; }

    /// <summary>
    /// ID de la campaña generada (si es tipo Spend)
    /// </summary>
    public Guid? CampaignId { get; set; }

    /// <summary>
    /// Monto en USD (para compras)
    /// </summary>
    public decimal? AmountUsd { get; set; }

    /// <summary>
    /// ID del pago asociado (si es tipo Purchase)
    /// </summary>
    public Guid? PaymentId { get; set; }

    /// <summary>
    /// ID del admin que hizo el ajuste (si es tipo Adjustment)
    /// </summary>
    public Guid? AdminUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
