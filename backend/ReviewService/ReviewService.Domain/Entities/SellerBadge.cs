using ReviewService.Domain.Base;

namespace ReviewService.Domain.Entities;

/// <summary>
/// Badge/insignia de un vendedor basado en sus reviews
/// Ej: "Top Rated", "Trusted Dealer", "5-Star Seller"
/// </summary>
public class SellerBadge : BaseEntity<Guid>
{
    /// <summary>
    /// ID del vendedor que tiene el badge
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Tipo de badge
    /// </summary>
    public BadgeType BadgeType { get; set; }

    /// <summary>
    /// Título del badge
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción del badge
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Icono/emoji del badge
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Color del badge (hex code)
    /// </summary>
    public string Color { get; set; } = "#3B82F6";

    /// <summary>
    /// Si el badge está activo
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Fecha cuando se obtuvo el badge
    /// </summary>
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha cuando se otorgó el badge (alias for EarnedAt)
    /// </summary>
    public DateTime GrantedAt
    {
        get => EarnedAt;
        set => EarnedAt = value;
    }

    /// <summary>
    /// Fecha cuando se revocó el badge (si aplica)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Fecha de expiración (null = permanente)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Criterios que se cumplieron para obtener el badge
    /// </summary>
    public string? Criteria { get; set; }

    /// <summary>
    /// Estadísticas que califican para el badge (JSON)
    /// </summary>
    public string? QualifyingStats { get; set; }

    /// <summary>
    /// Notas adicionales del badge
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Posición en el display (menor = más prominente)
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Tipos de badges disponibles
/// </summary>
public enum BadgeType
{
    /// <summary>
    /// Top Rated - 4.8+ estrellas promedio con 10+ reviews
    /// </summary>
    TopRated = 1,

    /// <summary>
    /// Trusted Dealer - 6+ meses en plataforma, 95%+ reviews positivas
    /// </summary>
    TrustedDealer = 2,

    /// <summary>
    /// 5-Star Seller - Solo reviews de 5 estrellas (min 5 reviews)
    /// </summary>
    FiveStarSeller = 3,

    /// <summary>
    /// Customer Choice - Más del 80% de reviews mencionan "recomendado"
    /// </summary>
    CustomerChoice = 4,

    /// <summary>
    /// Quick Responder - Responde a reviews en menos de 24h
    /// </summary>
    QuickResponder = 5,

    /// <summary>
    /// Verified Professional - Documentación verificada + reviews positivas
    /// </summary>
    VerifiedProfessional = 6,

    /// <summary>
    /// Rising Star - Tendencia positiva en últimos 3 meses
    /// </summary>
    RisingStar = 7,

    /// <summary>
    /// Volume Leader - Más de 50 reviews
    /// </summary>
    VolumeLeader = 8,

    /// <summary>
    /// Consistency Winner - Rating estable por 6+ meses
    /// </summary>
    ConsistencyWinner = 9,

    /// <summary>
    /// Community Favorite - Reviews más útiles del mes
    /// </summary>
    CommunityFavorite = 10
}