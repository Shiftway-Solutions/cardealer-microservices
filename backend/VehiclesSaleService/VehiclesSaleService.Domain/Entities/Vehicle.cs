using CarDealer.Shared.MultiTenancy;

namespace VehiclesSaleService.Domain.Entities;

/// <summary>
/// Vehículo para venta en el marketplace
/// </summary>
public class Vehicle : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; } // Multi-tenant

    // ========================================
    // INFORMACIÓN BÁSICA
    // ========================================
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public VehicleStatus Status { get; set; } = VehicleStatus.Draft;

    // ========================================
    // INFORMACIÓN DEL VENDEDOR
    // ========================================
    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public SellerType SellerType { get; set; } = SellerType.Seller;
    public string? SellerPhone { get; set; }
    public string? SellerEmail { get; set; }
    public string? SellerWhatsApp { get; set; }
    public bool SellerVerified { get; set; } = false;
    public decimal? SellerRating { get; set; }
    public int? SellerReviewCount { get; set; }
    public string? SellerCity { get; set; }
    public string? SellerState { get; set; }
    public string? SellerLogoUrl { get; set; } // Para dealers

    // ========================================
    // IDENTIFICACIÓN DEL VEHÍCULO
    // ========================================
    public string? VIN { get; set; } // Vehicle Identification Number
    public string? StockNumber { get; set; } // Número de inventario del dealer

    // ========================================
    // MARCA / MODELO / AÑO
    // ========================================
    public Guid? MakeId { get; set; }
    public string Make { get; set; } = string.Empty; // Toyota, Honda, Ford
    public Guid? ModelId { get; set; }
    public string Model { get; set; } = string.Empty; // Camry, Civic, F-150
    public string? Trim { get; set; } // LE, SE, XLE, Sport
    public int Year { get; set; }
    public string? Generation { get; set; } // XV70, 11th Gen

    // ========================================
    // TIPO Y CARROCERÍA
    // ========================================
    public VehicleType VehicleType { get; set; } = VehicleType.Car;
    public BodyStyle BodyStyle { get; set; } = BodyStyle.Sedan;
    public int Doors { get; set; } = 4;
    public int Seats { get; set; } = 5;

    // ========================================
    // MOTOR Y TRANSMISIÓN
    // ========================================
    public FuelType FuelType { get; set; } = FuelType.Gasoline;
    public string? EngineSize { get; set; } // 2.5L, 3.0L
    public int? Horsepower { get; set; }
    public int? Torque { get; set; }
    public TransmissionType Transmission { get; set; } = TransmissionType.Automatic;
    public DriveType DriveType { get; set; } = DriveType.FWD;
    public int? Cylinders { get; set; }

    // ========================================
    // KILOMETRAJE Y CONDICIÓN
    // ========================================
    public int Mileage { get; set; }
    public MileageUnit MileageUnit { get; set; } = MileageUnit.Miles;
    public VehicleCondition Condition { get; set; } = VehicleCondition.Used;
    public int? PreviousOwners { get; set; }
    public bool AccidentHistory { get; set; } = false;
    public bool HasCleanTitle { get; set; } = true;

    // ========================================
    // APARIENCIA
    // ========================================
    public string? ExteriorColor { get; set; }
    public string? InteriorColor { get; set; }
    public string? InteriorMaterial { get; set; } // Leather, Cloth, Vinyl

    // ========================================
    // ECONOMÍA DE COMBUSTIBLE
    // ========================================
    public int? MpgCity { get; set; }
    public int? MpgHighway { get; set; }
    public int? MpgCombined { get; set; }

    // ========================================
    // UBICACIÓN
    // ========================================
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; } = "USA";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // ========================================
    // HISTORIAL Y CERTIFICACIONES
    // ========================================
    public bool IsCertified { get; set; } = false;
    public string? CertificationProgram { get; set; } // CPO, Carfax Certified
    public string? CarfaxReportUrl { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public string? ServiceHistoryNotes { get; set; }
    public string? WarrantyInfo { get; set; }

    // ========================================
    // CARACTERÍSTICAS Y EQUIPAMIENTO
    // ========================================
    /// <summary>
    /// Lista de features como JSON array
    /// Ejemplo: ["Sunroof", "Navigation", "Leather Seats", "Backup Camera"]
    /// </summary>
    public string FeaturesJson { get; set; } = "[]";

    /// <summary>
    /// Paquetes de equipamiento
    /// Ejemplo: ["Technology Package", "Premium Package"]
    /// </summary>
    public string PackagesJson { get; set; } = "[]";

    // ========================================
    // MÉTRICAS DE ENGAGEMENT
    // ========================================
    public int ViewCount { get; set; } = 0;
    public int FavoriteCount { get; set; } = 0;
    public int InquiryCount { get; set; } = 0;

    // ========================================
    // SECCIONES DEL HOMEPAGE
    // ========================================
    /// <summary>
    /// Secciones del homepage donde se mostrará este vehículo.
    /// Es un campo de flags que permite múltiples secciones.
    /// </summary>
    public HomepageSection HomepageSections { get; set; } = HomepageSection.None;

    // ========================================
    // METADATOS
    // ========================================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public DateTime? SoldAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public bool IsFeatured { get; set; } = false;

    // ── Propiedades de Campaña Publicitaria (AdvertisingService sync) ─────────
    /// <summary>Vehículo tiene una campaña PremiumSpot activa.</summary>
    public bool IsPremium { get; set; } = false;

    /// <summary>Referencia a la campaña activa que origina la promoción.</summary>
    public Guid? LinkedCampaignId { get; set; }

    /// <summary>Fecha de expiración de la promoción (null = sin fecha fija).</summary>
    public DateTime? FeaturedUntil { get; set; }

    /// <summary>Prioridad de aparición: 100 = Premium, 50 = Featured, 0 = Normal.</summary>
    public int FeaturedPriority { get; set; } = 0;

    // ── Métodos de dominio ────────────────────────────────────────────────────
    /// <summary>Marca el vehículo como Premium (campaña PremiumSpot activa).</summary>
    public void MarkAsPremium(Guid campaignId, DateTime? until = null, int priority = 100)
    {
        IsPremium = true;
        IsFeatured = false; // Premium excluye Featured simple
        LinkedCampaignId = campaignId;
        FeaturedUntil = until;
        FeaturedPriority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Marca el vehículo como Destacado (FeaturedSpot o asignación manual del admin).</summary>
    public void MarkAsFeaturedByAdmin(int priority = 50)
    {
        IsFeatured = true;
        FeaturedPriority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Limpia todos los flags de promoción cuando la campaña termina.</summary>
    public void ClearPromotion()
    {
        IsFeatured = false;
        IsPremium = false;
        LinkedCampaignId = null;
        FeaturedUntil = null;
        FeaturedPriority = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    // ========================================
    // CONCURRENCY CONTROL
    // ========================================
    /// <summary>
    /// Optimistic concurrency token. Updated automatically on every save.
    /// </summary>
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    // ========================================
    // NAVEGACIÓN
    // ========================================
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public ICollection<VehicleImage> Images { get; set; } = new List<VehicleImage>();

    /// <summary>
    /// Secciones del homepage donde aparece este vehículo
    /// </summary>
    public ICollection<VehicleHomepageSection> HomepageSectionAssignments { get; set; } = new List<VehicleHomepageSection>();
}

/// <summary>
/// Estados del vehículo
/// </summary>
public enum VehicleStatus
{
    Draft = 0,
    PendingReview = 1,
    Active = 2,
    Reserved = 3,
    Sold = 4,
    Archived = 5,
    Rejected = 6
}

/// <summary>
/// Tipo de vehículo
/// </summary>
public enum VehicleType
{
    Car = 0,
    Truck = 1,
    SUV = 2,
    Van = 3,
    Motorcycle = 4,
    RV = 5,
    Boat = 6,
    ATV = 7,
    Commercial = 8,
    Other = 99
}

/// <summary>
/// Estilo de carrocería
/// </summary>
public enum BodyStyle
{
    Sedan = 0,
    Coupe = 1,
    Hatchback = 2,
    Wagon = 3,
    SUV = 4,
    Crossover = 5,
    Pickup = 6,
    Van = 7,
    Minivan = 8,
    Convertible = 9,
    SportsCar = 10,
    Other = 99
}

/// <summary>
/// Tipo de combustible
/// </summary>
public enum FuelType
{
    Gasoline = 0,
    Diesel = 1,
    Electric = 2,
    Hybrid = 3,
    PlugInHybrid = 4,
    Hydrogen = 5,
    FlexFuel = 6,
    NaturalGas = 7,
    Other = 99
}

/// <summary>
/// Tipo de transmisión
/// </summary>
public enum TransmissionType
{
    Automatic = 0,
    Manual = 1,
    CVT = 2,
    Automated = 3, // Automated Manual
    DualClutch = 4,
    Other = 99
}

/// <summary>
/// Tipo de tracción
/// </summary>
public enum DriveType
{
    FWD = 0, // Front-Wheel Drive
    RWD = 1, // Rear-Wheel Drive
    AWD = 2, // All-Wheel Drive
    FourWD = 3, // 4x4
    Other = 99
}

/// <summary>
/// Unidad de kilometraje
/// </summary>
public enum MileageUnit
{
    Miles = 0,
    Kilometers = 1
}

/// <summary>
/// Condición del vehículo
/// </summary>
public enum VehicleCondition
{
    New = 0,
    CertifiedPreOwned = 1,
    Used = 2,
    Salvage = 3,
    Rebuilt = 4
}

/// <summary>
/// Tipo de vendedor
/// </summary>
public enum SellerType
{
    Seller = 0,         // Vendedor particular
    Dealer = 1,         // Concesionario/Lote
    Franchise = 2,      // Concesionario oficial de marca
    Wholesale = 3       // Mayorista
}

/// <summary>
/// Tipo de imagen
/// </summary>
public enum ImageType
{
    Exterior = 0,
    Interior = 1,
    Engine = 2,
    Damage = 3,
    Documents = 4,
    Other = 99
}

/// <summary>
/// Secciones del homepage donde se puede mostrar un vehículo.
/// Usa Flags para permitir múltiples secciones simultáneamente.
/// </summary>
[Flags]
public enum HomepageSection
{
    /// <summary>No aparece en ninguna sección del homepage</summary>
    None = 0,

    /// <summary>Aparece en el carousel principal (Hero)</summary>
    Carousel = 1,

    /// <summary>Aparece en la sección de Sedanes</summary>
    Sedanes = 2,

    /// <summary>Aparece en la sección de SUVs</summary>
    SUVs = 4,

    /// <summary>Aparece en la sección de Camionetas</summary>
    Camionetas = 8,

    /// <summary>Aparece en la sección de Deportivos</summary>
    Deportivos = 16,

    /// <summary>Aparece en la sección de Destacados</summary>
    Destacados = 32,

    /// <summary>Aparece en la sección de Lujo</summary>
    Lujo = 64,

    /// <summary>Aparece en todas las secciones aplicables</summary>
    All = Carousel | Sedanes | SUVs | Camionetas | Deportivos | Destacados | Lujo
}
