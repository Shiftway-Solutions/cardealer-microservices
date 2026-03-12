using MediatR;
using KYCService.Domain.Entities;
using KYCService.Application.DTOs;

namespace KYCService.Application.Commands;

/// <summary>
/// Crear perfil KYC
/// </summary>
public record CreateKYCProfileCommand : IRequest<KYCProfileDto>
{
    public Guid UserId { get; init; }
    public EntityType EntityType { get; init; } = EntityType.Individual;

    // Información Personal
    public string FullName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string? LastName { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? PlaceOfBirth { get; init; }
    public string? Nationality { get; init; }
    public string? Gender { get; init; }

    // Documentos
    public DocumentType PrimaryDocumentType { get; init; }
    public string? PrimaryDocumentNumber { get; init; }
    public DateTime? PrimaryDocumentExpiry { get; init; }
    public string? PrimaryDocumentCountry { get; init; }

    // Contacto
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? MobilePhone { get; init; }

    // Dirección
    public string? Address { get; init; }
    public string? Sector { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; } = "DO";

    // Información Económica
    public string? Occupation { get; init; }
    public string? EmployerName { get; init; }
    public string? SourceOfFunds { get; init; }
    public string? ExpectedTransactionVolume { get; init; }
    public decimal? EstimatedAnnualIncome { get; init; }

    // PEP
    public bool IsPEP { get; init; }
    public string? PEPPosition { get; init; }
    public string? PEPRelationship { get; init; }

    // Empresa (si EntityType = Business)
    public string? BusinessName { get; init; }
    public string? RNC { get; init; }
    public string? BusinessType { get; init; }
    public DateTime? IncorporationDate { get; init; }
    public string? LegalRepresentative { get; init; }

    // Consentimiento (Ley 172-13 Art. 5)
    /// <summary>
    /// El usuario debe aceptar explícitamente el procesamiento de datos personales.
    /// Obligatorio por Ley 172-13 Art. 5. Sin consentimiento, no se puede crear el perfil.
    /// </summary>
    public bool DataProcessingConsent { get; init; }

    /// <summary>
    /// Consentimiento separado para procesamiento de datos biométricos
    /// (foto de cédula, selfie, face matching).
    /// </summary>
    public bool BiometricProcessingConsent { get; init; }

    /// <summary>
    /// Versión del aviso de privacidad aceptado (e.g., "v2.0-2026-01")
    /// </summary>
    public string? ConsentVersion { get; init; }
}

/// <summary>
/// Actualizar perfil KYC
/// </summary>
public record UpdateKYCProfileCommand : IRequest<KYCProfileDto>
{
    public Guid Id { get; init; }

    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? MobilePhone { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? Occupation { get; init; }
    public string? EmployerName { get; init; }
    public string? SourceOfFunds { get; init; }
    public decimal? EstimatedAnnualIncome { get; init; }
}

/// <summary>
/// Aprobar perfil KYC
/// </summary>
public record ApproveKYCProfileCommand : IRequest<KYCProfileDto>
{
    public Guid Id { get; init; }
    public Guid ApprovedBy { get; init; }
    public string ApprovedByName { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public int ValidityDays { get; init; } = 365; // Por defecto 1 año
}

/// <summary>
/// Rechazar perfil KYC
/// </summary>
public record RejectKYCProfileCommand : IRequest<KYCProfileDto>
{
    public Guid Id { get; init; }
    public Guid RejectedBy { get; init; }
    public string RejectedByName { get; init; } = string.Empty;
    public string RejectionReason { get; init; } = string.Empty;
}

/// <summary>
/// Enviar perfil KYC para revisión
/// Cambia el estado de Pending/InProgress a UnderReview
/// </summary>
public record SubmitKYCForReviewCommand : IRequest<KYCProfileDto>
{
    public Guid Id { get; init; }
}

/// <summary>
/// Subir documento KYC
/// </summary>
public record UploadKYCDocumentCommand : IRequest<KYCDocumentDto>
{
    public Guid KYCProfileId { get; init; }
    public DocumentType Type { get; init; }
    public string DocumentName { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    /// <summary>
    /// Clave de almacenamiento en S3 (permanente, puede ser null para documentos legacy)
    /// </summary>
    public string? StorageKey { get; init; }
    /// <summary>
    /// URL pre-firmada (puede expirar, usar StorageKey para regenerar)
    /// </summary>
    public string FileUrl { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string? FileHash { get; init; }
    public Guid UploadedBy { get; init; }
    public string? Side { get; init; } // "Front" or "Back" for cedula
}

/// <summary>
/// Verificar documento KYC
/// </summary>
public record VerifyKYCDocumentCommand : IRequest<KYCDocumentDto>
{
    public Guid Id { get; init; }
    public Guid VerifiedBy { get; init; }
    public bool Approved { get; init; }
    public string? RejectionReason { get; init; }
    public string? ExtractedNumber { get; init; }
    public DateTime? ExtractedExpiry { get; init; }
    public string? ExtractedName { get; init; }
}

/// <summary>
/// Registrar verificación KYC
/// </summary>
public record CreateKYCVerificationCommand : IRequest<KYCVerificationDto>
{
    public Guid KYCProfileId { get; init; }
    public string VerificationType { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public string? FailureReason { get; init; }
    public string? RawResponse { get; init; }
    public int ConfidenceScore { get; init; }
    public Guid? VerifiedBy { get; init; }
    public int? ValidityDays { get; init; }
}

/// <summary>
/// Evaluar riesgo de perfil KYC
/// </summary>
public record AssessKYCRiskCommand : IRequest<KYCRiskAssessmentDto>
{
    public Guid KYCProfileId { get; init; }
    public RiskLevel NewLevel { get; init; }
    public int NewScore { get; init; }
    public string Reason { get; init; } = string.Empty;
    public List<string> Factors { get; init; } = new();
    public List<string> RecommendedActions { get; init; } = new();
    public Guid AssessedBy { get; init; }
    public string AssessedByName { get; init; } = string.Empty;
}

/// <summary>
/// Crear reporte de transacción sospechosa
/// </summary>
public record CreateSuspiciousTransactionReportCommand : IRequest<SuspiciousTransactionReportDto>
{
    public Guid UserId { get; init; }
    public Guid? KYCProfileId { get; init; }
    public Guid? TransactionId { get; init; }
    public string SuspiciousActivityType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal? Amount { get; init; }
    public string Currency { get; init; } = "DOP";
    public List<string> RedFlags { get; init; } = new();
    public DateTime DetectedAt { get; init; }
    public Guid CreatedBy { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
}

/// <summary>
/// Aprobar reporte de transacción sospechosa
/// </summary>
public record ApproveSTRCommand : IRequest<SuspiciousTransactionReportDto>
{
    public Guid Id { get; init; }
    public Guid ApprovedBy { get; init; }
}

/// <summary>
/// Enviar reporte a UAF
/// </summary>
public record SendSTRToUAFCommand : IRequest<SuspiciousTransactionReportDto>
{
    public Guid Id { get; init; }
    public Guid SentBy { get; init; }
    public string UAFReportNumber { get; init; } = string.Empty;
}

/// <summary>
/// Agregar entrada a lista de control
/// </summary>
public record AddWatchlistEntryCommand : IRequest<WatchlistEntryDto>
{
    public WatchlistType ListType { get; init; }
    public string Source { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public List<string>? Aliases { get; init; }
    public string? DocumentNumber { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? Nationality { get; init; }
    public string? Details { get; init; }
}

/// <summary>
/// Realizar screening de watchlist
/// </summary>
public record ScreenWatchlistCommand : IRequest<ScreeningResultDto>
{
    public string FullName { get; init; } = string.Empty;
    public string? DocumentNumber { get; init; }
    public DateTime? DateOfBirth { get; init; }
}
