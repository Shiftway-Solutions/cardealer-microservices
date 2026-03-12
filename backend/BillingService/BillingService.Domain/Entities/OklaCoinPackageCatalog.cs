namespace BillingService.Domain.Entities;

/// <summary>
/// Catálogo de paquetes de OKLA Coins (modelo prepago).
/// Los bonus por volumen incentivan la compra anticipada.
/// 1 OKLA Coin = $0.01 USD.
/// </summary>
public static class OklaCoinPackageCatalog
{
    // ═══════════════════════════════════════════════════════════════
    // Deterministic IDs
    // ═══════════════════════════════════════════════════════════════
    public static readonly Guid BasicId = new("cc000000-0000-0000-0000-000000000001");
    public static readonly Guid IntermediateId = new("cc000000-0000-0000-0000-000000000002");
    public static readonly Guid ProfessionalId = new("cc000000-0000-0000-0000-000000000003");
    public static readonly Guid DealerId = new("cc000000-0000-0000-0000-000000000004");

    /// <summary>
    /// Returns all available OKLA Coins packages
    /// </summary>
    public static List<OklaCoinPackage> GetAll() => new()
    {
        new OklaCoinPackage
        {
            Id = BasicId,
            Slug = "pack-basico",
            Name = "Pack Básico",
            BaseCredits = 2500,
            BonusCredits = 0,
            TotalCredits = 2500,
            BonusPercentage = 0,
            PriceUsd = 25.00m,
            CostPerCoin = 0.01m,    // $25 / 2500 = $0.01
            DisplayOrder = 1,
            IsActive = true
        },
        new OklaCoinPackage
        {
            Id = IntermediateId,
            Slug = "pack-intermedio",
            Name = "Pack Intermedio",
            BaseCredits = 5000,
            BonusCredits = 500,
            TotalCredits = 5500,
            BonusPercentage = 10,
            PriceUsd = 50.00m,
            CostPerCoin = 0.00909m, // $50 / 5500 ≈ $0.009
            DisplayOrder = 2,
            IsActive = true,
            BadgeText = "+10% bonus"
        },
        new OklaCoinPackage
        {
            Id = ProfessionalId,
            Slug = "pack-profesional",
            Name = "Pack Profesional",
            BaseCredits = 10000,
            BonusCredits = 2000,
            TotalCredits = 12000,
            BonusPercentage = 20,
            PriceUsd = 100.00m,
            CostPerCoin = 0.00833m, // $100 / 12000 ≈ $0.0083
            DisplayOrder = 3,
            IsActive = true,
            BadgeText = "+20% bonus"
        },
        new OklaCoinPackage
        {
            Id = DealerId,
            Slug = "pack-dealer",
            Name = "Pack Dealer",
            BaseCredits = 25000,
            BonusCredits = 7500,
            TotalCredits = 32500,
            BonusPercentage = 30,
            PriceUsd = 250.00m,
            CostPerCoin = 0.00769m, // $250 / 32500 ≈ $0.0077
            DisplayOrder = 4,
            IsActive = true,
            BadgeText = "+30% bonus"
        }
    };

    /// <summary>
    /// Get a package by slug
    /// </summary>
    public static OklaCoinPackage? GetBySlug(string slug)
        => GetAll().FirstOrDefault(p => p.Slug == slug);

    /// <summary>
    /// Get a package by ID
    /// </summary>
    public static OklaCoinPackage? GetById(Guid id)
        => GetAll().FirstOrDefault(p => p.Id == id);
}

/// <summary>
/// Paquete de OKLA Coins
/// </summary>
public class OklaCoinPackage
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Créditos base (sin bonus)</summary>
    public int BaseCredits { get; set; }

    /// <summary>Créditos bonus por volumen</summary>
    public int BonusCredits { get; set; }

    /// <summary>Total = Base + Bonus</summary>
    public int TotalCredits { get; set; }

    /// <summary>Porcentaje de bonus (0, 10, 20, 30)</summary>
    public int BonusPercentage { get; set; }

    /// <summary>Precio en USD</summary>
    public decimal PriceUsd { get; set; }

    /// <summary>Costo efectivo por coin</summary>
    public decimal CostPerCoin { get; set; }

    /// <summary>Texto del badge (e.g., "+10% bonus")</summary>
    public string? BadgeText { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
