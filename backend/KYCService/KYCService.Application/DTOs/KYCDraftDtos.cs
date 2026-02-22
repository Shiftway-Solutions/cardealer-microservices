namespace KYCService.Application.DTOs;

/// <summary>
/// DTO para respuesta de borrador KYC
/// </summary>
public record KYCProfileDraftDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public int CurrentStep { get; init; }

    /// <summary>
    /// JSON opaco — el frontend lo deserializa según su propio schema de wizard.
    /// </summary>
    public string FormData { get; init; } = "{}";

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Request para crear/actualizar borrador KYC
/// </summary>
public record UpsertKYCProfileDraftRequest
{
    /// <summary>
    /// Paso actual del wizard (1-based)
    /// </summary>
    public int CurrentStep { get; init; } = 1;

    /// <summary>
    /// JSON del formulario completo (todos los pasos visitados).
    /// El backend lo persiste tal cual sin validar el contenido —
    /// la validación real ocurre al hacer submit del perfil KYC.
    /// </summary>
    public string FormData { get; init; } = "{}";
}
