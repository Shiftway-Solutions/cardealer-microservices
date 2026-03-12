using ReviewService.Domain.Base;

namespace ReviewService.Domain.Entities;

/// <summary>
/// Voto de utilidad de un usuario sobre una review
/// Un usuario solo puede votar una vez por review
/// </summary>
public class ReviewHelpfulVote : BaseEntity<Guid>
{
    /// <summary>
    /// ID de la review que se está votando
    /// </summary>
    public Guid ReviewId { get; set; }

    /// <summary>
    /// ID del usuario que vota
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Si considera la review útil (true) o no útil (false)
    /// </summary>
    public bool IsHelpful { get; set; }

    /// <summary>
    /// Fecha y hora del voto
    /// </summary>
    public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha y hora del voto (alias for CreatedAt)
    /// </summary>
    public DateTime VotedAt
    {
        get => CreatedAt;
        set => CreatedAt = value;
    }

    /// <summary>
    /// IP del usuario (para detección de fraude)
    /// </summary>
    public string? UserIpAddress { get; set; }

    /// <summary>
    /// User Agent del navegador
    /// </summary>
    public string? UserAgent { get; set; }

    // Navigation properties

    /// <summary>
    /// Review asociada
    /// </summary>
    public Review Review { get; set; } = null!;
}