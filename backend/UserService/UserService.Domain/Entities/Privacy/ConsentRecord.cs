using System;

namespace UserService.Domain.Entities.Privacy;

/// <summary>
/// Registro de auditoría de consentimiento (Ley 172-13 Art. 27).
/// Cada cambio de preferencia de comunicación genera un registro inmutable.
/// </summary>
public class ConsentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    /// <summary>
    /// Tipo de consentimiento que cambió (e.g., "EmailPromotions", "WhatsAppMarketing", "SmsPromotions")
    /// </summary>
    public string ConsentType { get; set; } = string.Empty;

    /// <summary>
    /// true = consentimiento otorgado (opt-in), false = consentimiento revocado (opt-out)
    /// </summary>
    public bool Granted { get; set; }

    /// <summary>
    /// Fuente del cambio: "web_settings", "unsubscribe_link", "api", "registration", "admin"
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Dirección IP desde la cual se realizó el cambio
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User-Agent del navegador/cliente
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Fecha y hora exacta del cambio
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navegación
    public User? User { get; set; }
}
