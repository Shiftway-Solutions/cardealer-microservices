using CarDealer.Contracts.Abstractions;

namespace CarDealer.Contracts.Events.KYC;

/// <summary>
/// Evento publicado por KYCService cuando el estado de un perfil KYC cambia
/// (aprobado, rechazado, enviado a revisión).
///
/// Exchange: cardealer.events  (Topic, durable)
/// Routing key: kyc.profile.status_changed
///
/// Consumidores:
///   - NotificationService → envía email al usuario
///   - AuditService        → registra cambio de estado
/// </summary>
public class KYCProfileStatusChangedEvent : EventBase
{
    public override string EventType => "kyc.profile.status_changed";

    /// <summary>Identificador del perfil KYC afectado</summary>
    public Guid ProfileId { get; set; }

    /// <summary>Identificador del usuario dueño del perfil</summary>
    public Guid UserId { get; set; }

    /// <summary>Email del usuario (para enviar la notificación directamente)</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Nombre completo del usuario</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Estado anterior del perfil (e.g. "UnderReview")</summary>
    public string PreviousStatus { get; set; } = string.Empty;

    /// <summary>Nuevo estado del perfil (e.g. "Approved", "Rejected")</summary>
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>
    /// Motivo del cambio.
    /// En rechazos contiene la razón. En aprobaciones puede contener notas del revisor.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>ID del admin/compliance officer que realizó el cambio</summary>
    public Guid? ChangedBy { get; set; }

    /// <summary>Nombre del admin que realizó el cambio (para logs y emails)</summary>
    public string? ChangedByName { get; set; }

    /// <summary>Fecha y hora del cambio de estado</summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Días de validez de la aprobación (solo relevante cuando NewStatus == "Approved")
    /// </summary>
    public int? ValidityDays { get; set; }
}
