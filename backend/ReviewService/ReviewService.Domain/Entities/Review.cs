using ReviewService.Domain.Base;

namespace ReviewService.Domain.Entities;

/// <summary>
/// Review de un comprador sobre un vendedor/dealer
/// Sistema estilo Amazon con rating 1-5 estrellas + texto
/// </summary>
public class Review : BaseEntity<Guid>
{
    /// <summary>
    /// ID del comprador que deja la review
    /// </summary>
    public Guid BuyerId { get; set; }

    /// <summary>
    /// ID del vendedor que recibe la review
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// ID del vehículo sobre el que es la review (opcional)
    /// </summary>
    public Guid? VehicleId { get; set; }

    /// <summary>
    /// ID de la transacción/orden que validó la compra
    /// Para mostrar badge "Compra verificada"
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Rating de 1 a 5 estrellas
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Título de la review (ej: "Excelente servicio")
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Contenido detallado de la review
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Si la review está aprobada por moderación
    /// </summary>
    public bool IsApproved { get; set; } = true;

    /// <summary>
    /// Si la review es de una compra verificada
    /// </summary>
    public bool IsVerifiedPurchase { get; set; } = false;

    /// <summary>
    /// Razón de rechazo si IsApproved = false
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// ID del admin que moderó
    /// </summary>
    public Guid? ModeratedById { get; set; }

    /// <summary>
    /// Fecha de moderación
    /// </summary>
    public DateTime? ModeratedAt { get; set; }

    /// <summary>
    /// Nombre del comprador (cache para performance)
    /// </summary>
    public string BuyerName { get; set; } = string.Empty;

    /// <summary>
    /// Foto del comprador (cache)
    /// </summary>
    public string? BuyerPhotoUrl { get; set; }

    /// <summary>
    /// Si la review fue útil para otros usuarios (upvotes)
    /// </summary>
    public int HelpfulVotes { get; set; } = 0;

    /// <summary>
    /// Total de votos recibidos
    /// </summary>
    public int TotalVotes { get; set; } = 0;

    /// <summary>
    /// Score de confianza contra fraude (0-100)
    /// </summary>
    public int TrustScore { get; set; } = 100;

    /// <summary>
    /// Si fue detectada como potencial fraude/spam
    /// </summary>
    public bool IsFlagged { get; set; } = false;

    /// <summary>
    /// Razón del flag (si aplica)
    /// </summary>
    public string? FlagReason { get; set; }

    /// <summary>
    /// Si fue solicitada automáticamente por el sistema
    /// </summary>
    public bool WasAutoRequested { get; set; } = false;

    /// <summary>
    /// Fecha cuando se solicitó automáticamente
    /// </summary>
    public DateTime? AutoRequestedAt { get; set; }

    /// <summary>
    /// IP del usuario que creó la review (para detección de fraude)
    /// </summary>
    public string? UserIpAddress { get; set; }

    /// <summary>
    /// User Agent del navegador (para detección de fraude)
    /// </summary>
    public string? UserAgent { get; set; }

    // Navigation properties

    /// <summary>
    /// Respuesta del vendedor a esta review
    /// </summary>
    public ReviewResponse? Response { get; set; }

    /// <summary>
    /// Votos de usuarios sobre esta review
    /// </summary>
    public List<ReviewHelpfulVote> HelpfulVotesList { get; set; } = new();
}