namespace KYCService.Domain.Entities;

/// <summary>
/// Clasificación de riesgo según Ley 155-17 (PLD/FT)
/// </summary>
public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4,
    Prohibited = 5
}

/// <summary>
/// Estado de verificación KYC
/// </summary>
public enum KYCStatus
{
    Pending = 1,
    InProgress = 2,
    DocumentsRequired = 3,
    UnderReview = 4,
    Approved = 5,
    Rejected = 6,
    Expired = 7,
    Suspended = 8
}

/// <summary>
/// Tipo de documento de identidad
/// </summary>
public enum DocumentType
{
    Cedula = 1,
    Passport = 2,
    DriverLicense = 3,
    ResidencyCard = 4,
    RNC = 5,  // Para empresas
    Other = 99
}

/// <summary>
/// Tipo de entidad
/// </summary>
public enum EntityType
{
    Individual = 1,
    Business = 2,
    NonProfit = 3,
    Government = 4
}

/// <summary>
/// Perfil KYC de un cliente
/// Según Ley 155-17 de Prevención de Lavado de Activos
/// </summary>
public class KYCProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>
    /// Tipo de entidad (persona natural o jurídica)
    /// </summary>
    public EntityType EntityType { get; set; } = EntityType.Individual;

    /// <summary>
    /// Estado actual del KYC
    /// </summary>
    public KYCStatus Status { get; set; } = KYCStatus.Pending;

    /// <summary>
    /// Nivel de riesgo asignado
    /// </summary>
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

    /// <summary>
    /// Puntuación de riesgo (0-100)
    /// </summary>
    public int RiskScore { get; set; } = 0;

    /// <summary>
    /// Factores de riesgo identificados
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();

    // Información Personal
    public string FullName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? Gender { get; set; }

    // Documentos de Identidad
    public DocumentType PrimaryDocumentType { get; set; }
    public string? PrimaryDocumentNumber { get; set; }
    public DateTime? PrimaryDocumentExpiry { get; set; }
    public string? PrimaryDocumentCountry { get; set; }

    // Información de Contacto
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }

    // Dirección
    public string? Address { get; set; }
    public string? Sector { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; } = "DO";

    // Información Económica
    public string? Occupation { get; set; }
    public string? EmployerName { get; set; }
    public string? SourceOfFunds { get; set; }
    public string? ExpectedTransactionVolume { get; set; }
    public decimal? EstimatedAnnualIncome { get; set; }

    // PEP (Politically Exposed Person)
    public bool IsPEP { get; set; } = false;
    public string? PEPPosition { get; set; }
    public string? PEPRelationship { get; set; }

    // Información para Empresas (EntityType = Business)
    public string? BusinessName { get; set; }
    public string? RNC { get; set; }
    public string? BusinessType { get; set; }
    public DateTime? IncorporationDate { get; set; }
    public string? LegalRepresentative { get; set; }

    // Verificaciones
    public DateTime? IdentityVerifiedAt { get; set; }
    public DateTime? AddressVerifiedAt { get; set; }
    public DateTime? IncomeVerifiedAt { get; set; }
    public DateTime? PEPCheckedAt { get; set; }
    public DateTime? SanctionsCheckedAt { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovalNotes { get; set; }
    public DateTime? RejectedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }

    // Expiración
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastReviewAt { get; set; }
    public DateTime? NextReviewAt { get; set; }

    // Consentimiento (Ley 172-13 Art. 5 — Protección de Datos Personales RD)
    /// <summary>
    /// Fecha en que el usuario otorgó consentimiento explícito para
    /// el procesamiento de sus datos personales y biométricos.
    /// Requerido por Ley 172-13 Art. 5 antes de recopilar PII.
    /// </summary>
    public DateTime? ConsentGivenAt { get; set; }

    /// <summary>
    /// Versión del aviso de privacidad/términos aceptados.
    /// Permite rastrear qué términos aceptó el usuario.
    /// </summary>
    public string? ConsentVersion { get; set; }

    /// <summary>
    /// Fecha en que el usuario aceptó el procesamiento de datos biométricos
    /// (selfie, face matching). Consentimiento separado según Ley 172-13.
    /// </summary>
    public DateTime? BiometricConsentGivenAt { get; set; }

    // Navegación
    public List<KYCDocument> Documents { get; set; } = new();
    public List<KYCVerification> Verifications { get; set; } = new();
    public List<KYCRiskAssessment> RiskAssessments { get; set; } = new();
}

/// <summary>
/// Documento subido para verificación KYC
/// </summary>
public class KYCDocument
{
    public Guid Id { get; set; }
    public Guid KYCProfileId { get; set; }

    public DocumentType Type { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Clave de almacenamiento en S3 (permanente, no expira)
    /// Formato: "kyc-documents/2026/02/07/{guid}.jpg"
    /// Puede ser null para documentos legacy sin storage key
    /// </summary>
    public string? StorageKey { get; set; }

    /// <summary>
    /// URL pre-firmada (legacy, puede estar expirada)
    /// Para nuevos documentos, usar StorageKey para generar URLs frescas
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? FileHash { get; set; }

    /// <summary>
    /// Lado del documento (Front/Back) - Para cédula se requieren ambos lados
    /// </summary>
    public string? Side { get; set; }

    /// <summary>
    /// Estado de verificación del documento
    /// </summary>
    public KYCDocumentStatus Status { get; set; } = KYCDocumentStatus.Pending;
    public string? RejectionReason { get; set; }

    // Datos extraídos
    public string? ExtractedNumber { get; set; }
    public DateTime? ExtractedExpiry { get; set; }
    public string? ExtractedName { get; set; }

    // Audit
    public Guid UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedBy { get; set; }

    // Navegación
    public KYCProfile? KYCProfile { get; set; }
}

public enum KYCDocumentStatus
{
    Pending = 1,
    Verified = 2,
    Rejected = 3,
    Expired = 4
}

/// <summary>
/// Registro de verificación realizada
/// </summary>
public class KYCVerification
{
    public Guid Id { get; set; }
    public Guid KYCProfileId { get; set; }

    public string VerificationType { get; set; } = string.Empty; // Identity, Address, Income, PEP, Sanctions
    public string Provider { get; set; } = string.Empty; // Internal, ExternalService
    public bool Passed { get; set; }
    public string? FailureReason { get; set; }
    public string? RawResponse { get; set; }
    public int ConfidenceScore { get; set; } // 0-100

    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
    public Guid? VerifiedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // Navegación
    public KYCProfile? KYCProfile { get; set; }
}

/// <summary>
/// Evaluación de riesgo KYC
/// </summary>
public class KYCRiskAssessment
{
    public Guid Id { get; set; }
    public Guid KYCProfileId { get; set; }

    public RiskLevel PreviousLevel { get; set; }
    public RiskLevel NewLevel { get; set; }
    public int PreviousScore { get; set; }
    public int NewScore { get; set; }

    /// <summary>
    /// Razón del cambio de nivel
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Factores considerados
    /// </summary>
    public List<string> Factors { get; set; } = new();

    /// <summary>
    /// Acciones recomendadas
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new();

    public Guid AssessedBy { get; set; }
    public string AssessedByName { get; set; } = string.Empty;
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;

    // Navegación
    public KYCProfile? KYCProfile { get; set; }
}

/// <summary>
/// Reporte de transacción sospechosa (ROS)
/// Según Ley 155-17
/// </summary>
public class SuspiciousTransactionReport
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? KYCProfileId { get; set; }
    public Guid? TransactionId { get; set; }

    /// <summary>
    /// Número único del reporte
    /// </summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de actividad sospechosa
    /// </summary>
    public string SuspiciousActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Descripción detallada
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Monto involucrado (si aplica)
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Moneda
    /// </summary>
    public string Currency { get; set; } = "DOP";

    /// <summary>
    /// Indicadores de sospecha
    /// </summary>
    public List<string> RedFlags { get; set; } = new();

    /// <summary>
    /// Estado del reporte
    /// </summary>
    public STRStatus Status { get; set; } = STRStatus.Draft;

    /// <summary>
    /// Fecha de detección de la actividad
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// Fecha límite para reportar a UAF
    /// </summary>
    public DateTime ReportingDeadline { get; set; }

    /// <summary>
    /// Número de reporte UAF (si ya fue enviado)
    /// </summary>
    public string? UAFReportNumber { get; set; }

    /// <summary>
    /// Fecha de envío a UAF
    /// </summary>
    public DateTime? SentToUAFAt { get; set; }

    // Audit
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? SentBy { get; set; }
}

public enum STRStatus
{
    Draft = 1,
    PendingReview = 2,
    Approved = 3,
    Rejected = 4,
    SentToUAF = 5,
    Archived = 6
}

/// <summary>
/// Lista de control (PEP, Sanciones, etc.)
/// </summary>
public class WatchlistEntry
{
    public Guid Id { get; set; }

    /// <summary>
    /// Tipo de lista
    /// </summary>
    public WatchlistType ListType { get; set; }

    /// <summary>
    /// Nombre de la fuente
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo de la persona/entidad
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Alias conocidos
    /// </summary>
    public List<string> Aliases { get; set; } = new();

    /// <summary>
    /// Número de documento (si se conoce)
    /// </summary>
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// Fecha de nacimiento (si se conoce)
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Nacionalidad
    /// </summary>
    public string? Nationality { get; set; }

    /// <summary>
    /// Detalles adicionales
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Fecha de adición a la lista
    /// </summary>
    public DateTime ListedDate { get; set; }

    /// <summary>
    /// Fecha de última actualización
    /// </summary>
    public DateTime? LastUpdated { get; set; }

    public bool IsActive { get; set; } = true;
}

public enum WatchlistType
{
    PEP = 1,
    Sanctions = 2,
    AdverseMedia = 3,
    InternalBlacklist = 4,
    OFAC = 5,
    UN = 6,
    EU = 7
}
