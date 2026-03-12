using ReviewService.Domain.Base;

namespace ReviewService.Domain.Entities;

/// <summary>
/// Solicitud automática de review enviada al comprador
/// Se envía 7 días después de la compra
/// </summary>
public class ReviewRequest : BaseEntity<Guid>
{
    /// <summary>
    /// ID del comprador al que se solicita la review
    /// </summary>
    public Guid BuyerId { get; set; }

    /// <summary>
    /// ID del vendedor sobre quien debe escribir la review
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// ID del vehículo comprado
    /// </summary>
    public Guid VehicleId { get; set; }

    /// <summary>
    /// ID de la orden/transacción
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Email del comprador
    /// </summary>
    public string BuyerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del comprador
    /// </summary>
    public string BuyerName { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de la compra original
    /// </summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// Fecha cuando se envió la solicitud
    /// </summary>
    public DateTime RequestSentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha límite para escribir la review
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Estado de la solicitud
    /// </summary>
    public ReviewRequestStatus Status { get; set; } = ReviewRequestStatus.Sent;

    /// <summary>
    /// Token único para el link de la review
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Número de recordatorios enviados
    /// </summary>
    public int RemindersSent { get; set; } = 0;

    /// <summary>
    /// Fecha del último recordatorio
    /// </summary>
    public DateTime? LastReminderAt { get; set; }

    /// <summary>
    /// ID de la review creada (si ya se escribió)
    /// </summary>
    public Guid? ReviewId { get; set; }

    /// <summary>
    /// Fecha cuando se escribió la review
    /// </summary>
    public DateTime? ReviewCreatedAt { get; set; }

    /// <summary>
    /// Metadata adicional (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    // Navigation properties

    /// <summary>
    /// Review creada (si aplica)
    /// </summary>
    public Review? Review { get; set; }
}

/// <summary>
/// Estados de una solicitud de review
/// </summary>
public enum ReviewRequestStatus
{
    /// <summary>
    /// Solicitud enviada, esperando respuesta
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Comprador vio el email pero no escribió review
    /// </summary>
    Viewed = 2,

    /// <summary>
    /// Review fue escrita exitosamente
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Solicitud expiró sin respuesta
    /// </summary>
    Expired = 4,

    /// <summary>
    /// Comprador declinó escribir review
    /// </summary>
    Declined = 5,

    /// <summary>
    /// Solicitud fue cancelada
    /// </summary>
    Cancelled = 6
}