namespace Video360Service.Application.Interfaces;

/// <summary>
/// Servicio para eliminar el fondo de las imágenes extraídas.
/// Implementaciones disponibles: remove.bg API, SkipBgRemoval (sin eliminación).
/// </summary>
public interface IBackgroundRemovalService
{
    /// <summary>
    /// Indica si el servicio está configurado y disponible.
    /// Si no está disponible, las imágenes se devuelven sin procesar.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Elimina el fondo de una imagen.
    /// </summary>
    /// <param name="imageBytes">Bytes de la imagen original (JPEG, PNG, WebP)</param>
    /// <param name="contentType">Content type de la imagen (image/jpeg, etc.)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Bytes de la imagen con fondo eliminado (PNG con transparencia)</returns>
    Task<BackgroundRemovalResult> RemoveBackgroundAsync(
        byte[] imageBytes,
        string contentType,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de la eliminación de fondo
/// </summary>
public record BackgroundRemovalResult
{
    /// <summary>Si la operación fue exitosa</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Bytes de la imagen resultante</summary>
    public byte[] ImageBytes { get; init; } = [];

    /// <summary>Content type resultante (image/png para fondo transparente)</summary>
    public string ContentType { get; init; } = "image/png";

    /// <summary>Mensaje de error si falló</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Si el background removal fue omitido (servicio no configurado)</summary>
    public bool WasSkipped { get; init; }
}
