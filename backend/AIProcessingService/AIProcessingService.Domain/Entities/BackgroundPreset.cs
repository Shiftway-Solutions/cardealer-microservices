namespace AIProcessingService.Domain.Entities;

/// <summary>
/// Preset de fondo disponible para reemplazo
/// </summary>
public class BackgroundPreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Imágenes del fondo
    public string BackgroundImageUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;

    // Configuración
    public BackgroundType Type { get; set; }
    public string FloorColor { get; set; } = "#FFFFFF";
    public float ShadowIntensity { get; set; } = 0.4f;

    // Permisos
    public bool IsPublic { get; set; } = true;
    public bool RequiresDealerMembership { get; set; } = false;

    // Ordenamiento
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum BackgroundType
{
    SolidColor,
    Studio,
    Showroom,
    Outdoor,
    Custom
}

/// <summary>
/// Fondos predefinidos del sistema
/// </summary>
public static class SystemBackgrounds
{
    public static readonly BackgroundPreset WhiteStudio = new()
    {
        Code = "white_studio",
        Name = "Blanco Infinito",
        Description = "Fondo blanco profesional estilo catálogo",
        Type = BackgroundType.Studio,
        FloorColor = "#FFFFFF",
        ShadowIntensity = 0.3f,
        IsPublic = true,
        RequiresDealerMembership = false,
        IsDefault = true,
        SortOrder = 1
    };

    public static readonly BackgroundPreset GrayShowroom = new()
    {
        Code = "gray_showroom",
        Name = "Showroom Gris",
        Description = "Fondo de showroom profesional en gris",
        Type = BackgroundType.Showroom,
        FloorColor = "#E5E5E5",
        ShadowIntensity = 0.4f,
        IsPublic = true,
        RequiresDealerMembership = true,
        IsDefault = false,
        SortOrder = 2
    };

    public static readonly BackgroundPreset DarkStudio = new()
    {
        Code = "dark_studio",
        Name = "Estudio Oscuro",
        Description = "Fondo oscuro dramático para vehículos de lujo",
        Type = BackgroundType.Studio,
        FloorColor = "#1A1A1A",
        ShadowIntensity = 0.2f,
        IsPublic = true,
        RequiresDealerMembership = true,
        IsDefault = false,
        SortOrder = 3
    };

    public static readonly BackgroundPreset OutdoorDay = new()
    {
        Code = "outdoor_day",
        Name = "Exterior Día",
        Description = "Escena exterior con cielo azul",
        Type = BackgroundType.Outdoor,
        FloorColor = "#CCCCCC",
        ShadowIntensity = 0.5f,
        IsPublic = true,
        RequiresDealerMembership = true,
        IsDefault = false,
        SortOrder = 4
    };

    public static List<BackgroundPreset> All => new()
    {
        WhiteStudio,
        GrayShowroom,
        DarkStudio,
        OutdoorDay
    };

    public static List<BackgroundPreset> FreeBackgrounds => new()
    {
        WhiteStudio
    };

    public static List<BackgroundPreset> PremiumBackgrounds => new()
    {
        GrayShowroom,
        DarkStudio,
        OutdoorDay
    };
}
