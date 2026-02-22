namespace KYCService.Domain.Entities;

/// <summary>
/// Borrador de perfil KYC — autosave del formulario del frontend.
/// Se persiste en cada paso del wizard y cada 30 segundos.
/// TTL de 30 días: si no se completa, se elimina automáticamente.
/// </summary>
public class KYCProfileDraft
{
    public Guid Id { get; set; }

    /// <summary>
    /// Usuario dueño del borrador (un usuario solo puede tener un borrador activo)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Paso actual del wizard (1-based)
    /// </summary>
    public int CurrentStep { get; set; } = 1;

    /// <summary>
    /// JSON serializado con todos los datos del formulario tal como los envía el frontend.
    /// Incluye datos parciales de todos los pasos visitados.
    /// </summary>
    public string FormData { get; set; } = "{}";

    /// <summary>
    /// Indica si el borrador ya fue convertido en un perfil KYC real
    /// </summary>
    public bool IsSubmitted { get; set; } = false;

    /// <summary>
    /// Fecha de creación del borrador
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Última actualización (se actualiza en cada autosave)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de expiración — 30 días después de la última actualización.
    /// El cleanup job elimina borradores expirados.
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);
}
