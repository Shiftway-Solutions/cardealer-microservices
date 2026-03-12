using ReviewService.Domain.Base;

namespace ReviewService.Domain.Entities;

/// <summary>
/// Log de detección de fraude para reviews
/// Registra todas las verificaciones anti-spam
/// </summary>
public class FraudDetectionLog : BaseEntity<Guid>
{
    /// <summary>
    /// ID de la review analizada
    /// </summary>
    public Guid ReviewId { get; set; }

    /// <summary>
    /// Tipo de verificación ejecutada
    /// </summary>
    public FraudCheckType CheckType { get; set; }

    /// <summary>
    /// Resultado de la verificación (PASS/FAIL/WARNING)
    /// </summary>
    public FraudCheckResult Result { get; set; }

    /// <summary>
    /// Score de confianza de esta verificación (0-100)
    /// </summary>
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// Detalles de la verificación
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Metadata adicional de la verificación (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Fecha y hora de la verificación
    /// </summary>
    public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha y hora cuando se verificó (alias for CreatedAt)
    /// </summary>
    public DateTime CheckedAt
    {
        get => CreatedAt;
        set => CreatedAt = value;
    }

    /// <summary>
    /// Versión del algoritmo de detección
    /// </summary>
    public string AlgorithmVersion { get; set; } = "1.0";

    // Navigation properties

    /// <summary>
    /// Review asociada
    /// </summary>
    public Review Review { get; set; } = null!;
}

/// <summary>
/// Tipos de verificaciones anti-fraude
/// </summary>
public enum FraudCheckType
{
    /// <summary>
    /// Verificación de IP duplicada
    /// </summary>
    DuplicateIp = 1,

    /// <summary>
    /// Verificación de dispositivo duplicado
    /// </summary>
    DuplicateDevice = 2,

    /// <summary>
    /// Análisis de contenido (spam patterns)
    /// </summary>
    ContentAnalysis = 3,

    /// <summary>
    /// Verificación de velocidad (muchas reviews muy rápido)
    /// </summary>
    SpeedCheck = 4,

    /// <summary>
    /// Verificación de usuario nuevo (cuenta recién creada)
    /// </summary>
    NewUserCheck = 5,

    /// <summary>
    /// Verificación de rating pattern (solo 5 estrellas siempre)
    /// </summary>
    RatingPattern = 6,

    /// <summary>
    /// Verificación de texto similar
    /// </summary>
    TextSimilarity = 7,

    /// <summary>
    /// Verificación de relación vendedor-comprador
    /// </summary>
    RelationshipCheck = 8,

    /// <summary>
    /// Verificación de geolocalización
    /// </summary>
    GeolocationCheck = 9,

    /// <summary>
    /// Verificación de compra real
    /// </summary>
    PurchaseVerification = 10
}

/// <summary>
/// Resultados de verificación anti-fraude
/// </summary>
public enum FraudCheckResult
{
    /// <summary>
    /// Verificación pasó, todo normal
    /// </summary>
    Pass = 1,

    /// <summary>
    /// Advertencia, revisar manualmente
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Falló, probable fraude
    /// </summary>
    Fail = 3,

    /// <summary>
    /// Fallo detectado (alias)
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Sospechoso, necesita revisión
    /// </summary>
    Suspicious = 4,

    /// <summary>
    /// Error en la verificación
    /// </summary>
    Error = 5
}