using System;

namespace UserService.Domain.Entities.Privacy;

/// <summary>
/// Tipo de solicitud ARCO
/// </summary>
public enum PrivacyRequestType
{
    Access,          // Ver mis datos
    Rectification,   // Corregir datos
    Cancellation,    // Eliminar cuenta
    Opposition,      // Oponerse a tratamiento
    Portability      // Exportar datos
}

/// <summary>
/// Estado de la solicitud
/// </summary>
public enum PrivacyRequestStatus
{
    Pending,         // Pendiente de procesamiento
    Processing,      // En proceso
    Completed,       // Completada
    Cancelled,       // Cancelada por usuario
    Rejected,        // Rechazada (ej: datos insuficientes)
    Expired          // Expirada (no se descargó a tiempo)
}

/// <summary>
/// Formato de exportación de datos
/// </summary>
public enum ExportFormat
{
    Json,
    Csv,
    Pdf
}

/// <summary>
/// Entidad para solicitudes de privacidad ARCO (Ley 172-13)
/// </summary>
public class PrivacyRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public PrivacyRequestType Type { get; set; }
    public PrivacyRequestStatus Status { get; set; } = PrivacyRequestStatus.Pending;

    // Para exportación de datos
    public ExportFormat? ExportFormat { get; set; }
    public string? DownloadToken { get; set; }
    public DateTime? DownloadTokenExpiresAt { get; set; }
    public string? FilePath { get; set; }
    public long? FileSizeBytes { get; set; }

    // Para eliminación de cuenta
    public string? DeletionReason { get; set; }
    public string? DeletionReasonOther { get; set; }
    public DateTime? GracePeriodEndsAt { get; set; } // 15 días de gracia
    public string? ConfirmationCode { get; set; }
    public bool IsConfirmed { get; set; } = false;

    // Metadatos
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Description { get; set; }
    public string? AdminNotes { get; set; }
    public Guid? ProcessedBy { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // Navegación
    public User? User { get; set; }
}
