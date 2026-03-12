using KYCService.Domain.Entities;

namespace KYCService.Application.DTOs;

/// <summary>
/// DTO para perfil KYC completo
/// </summary>
public record KYCProfileDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public EntityType EntityType { get; init; }
    public KYCStatus Status { get; init; }
    public string StatusName => Status.ToString();
    public RiskLevel RiskLevel { get; init; }
    public string RiskLevelName => RiskLevel.ToString();
    public int RiskScore { get; init; }
    public List<string> RiskFactors { get; init; } = new();

    // Información Personal
    public string FullName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string? LastName { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? PlaceOfBirth { get; init; }
    public string? Nationality { get; init; }
    public string? Gender { get; init; }
    public string? Occupation { get; init; }

    // Documentos
    public DocumentType PrimaryDocumentType { get; init; }
    /// <summary>
    /// Masked document number (shows only last 4 characters).
    /// Full number is NOT returned in API responses per Ley 172-13 Art. 31.
    /// Admin endpoints should use a separate DTO with the full number if needed.
    /// </summary>
    public string? PrimaryDocumentNumber { get; init; }
    public DateTime? PrimaryDocumentExpiry { get; init; }

    // Contacto
    public string? Email { get; init; }
    public string? Phone { get; init; }

    // Dirección
    public string? Address { get; init; }
    public string? Sector { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }

    // PEP
    public bool IsPEP { get; init; }
    public string? PEPPosition { get; init; }

    // Empresa
    public string? BusinessName { get; init; }
    public string? RNC { get; init; }

    // Verificaciones
    public bool IsIdentityVerified => IdentityVerifiedAt.HasValue;
    public bool IsAddressVerified => AddressVerifiedAt.HasValue;
    public DateTime? IdentityVerifiedAt { get; init; }
    public DateTime? AddressVerifiedAt { get; init; }

    // Fechas
    public DateTime CreatedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? NextReviewAt { get; init; }

    // Rechazo
    public string? RejectionReason { get; init; }
    public DateTime? RejectedAt { get; init; }
    public Guid? RejectedBy { get; init; }

    // Documentos y verificaciones
    public List<KYCDocumentDto> Documents { get; init; } = new();
    public List<KYCVerificationDto> Verifications { get; init; } = new();
}

/// <summary>
/// DTO resumido para listados
/// </summary>
public record KYCProfileSummaryDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public EntityType EntityType { get; init; }
    public KYCStatus Status { get; init; }
    public string StatusName => Status.ToString();
    public RiskLevel RiskLevel { get; init; }
    public string RiskLevelName => RiskLevel.ToString();
    public bool IsPEP { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public int DocumentsCount { get; init; }
    public bool HasPendingDocuments { get; init; }
}

/// <summary>
/// DTO para documento KYC
/// </summary>
public record KYCDocumentDto
{
    public Guid Id { get; init; }
    public Guid KYCProfileId { get; init; }
    public DocumentType Type { get; init; }
    public string TypeName => Type.ToString();
    public string DocumentName { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string? StorageKey { get; init; }
    public string FileUrl { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string? Side { get; init; }
    public KYCDocumentStatus Status { get; init; }
    public string StatusName => Status.ToString();
    public string? RejectionReason { get; init; }
    public string? ExtractedNumber { get; init; }
    public DateTime? ExtractedExpiry { get; init; }
    public string? ExtractedName { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime? VerifiedAt { get; init; }
}

/// <summary>
/// DTO para verificación KYC
/// </summary>
public record KYCVerificationDto
{
    public Guid Id { get; init; }
    public Guid KYCProfileId { get; init; }
    public string VerificationType { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public string? FailureReason { get; init; }
    public int ConfidenceScore { get; init; }
    public DateTime VerifiedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// DTO para evaluación de riesgo
/// </summary>
public record KYCRiskAssessmentDto
{
    public Guid Id { get; init; }
    public Guid KYCProfileId { get; init; }
    public RiskLevel PreviousLevel { get; init; }
    public RiskLevel NewLevel { get; init; }
    public int PreviousScore { get; init; }
    public int NewScore { get; init; }
    public string Reason { get; init; } = string.Empty;
    public List<string> Factors { get; init; } = new();
    public List<string> RecommendedActions { get; init; } = new();
    public string AssessedByName { get; init; } = string.Empty;
    public DateTime AssessedAt { get; init; }
}

/// <summary>
/// DTO para reporte de transacción sospechosa
/// </summary>
public record SuspiciousTransactionReportDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid? KYCProfileId { get; init; }
    public string ReportNumber { get; init; } = string.Empty;
    public string SuspiciousActivityType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal? Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public List<string> RedFlags { get; init; } = new();
    public STRStatus Status { get; init; }
    public string StatusName => Status.ToString();
    public DateTime DetectedAt { get; init; }
    public DateTime ReportingDeadline { get; init; }
    public bool IsOverdue => DateTime.UtcNow > ReportingDeadline && Status != STRStatus.SentToUAF;
    public string? UAFReportNumber { get; init; }
    public DateTime? SentToUAFAt { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO para entrada en lista de control
/// </summary>
public record WatchlistEntryDto
{
    public Guid Id { get; init; }
    public WatchlistType ListType { get; init; }
    public string ListTypeName => ListType.ToString();
    public string Source { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public List<string> Aliases { get; init; } = new();
    public string? DocumentNumber { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? Nationality { get; init; }
    public string? Details { get; init; }
    public DateTime ListedDate { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// DTO para resultado de screening
/// </summary>
public record ScreeningResultDto
{
    public bool HasMatches { get; init; }
    public int TotalMatches { get; init; }
    public List<WatchlistMatchDto> Matches { get; init; } = new();
    public DateTime ScreenedAt { get; init; } = DateTime.UtcNow;
}

public record WatchlistMatchDto
{
    public WatchlistEntryDto Entry { get; init; } = null!;
    public int MatchScore { get; init; }
    public List<string> MatchedFields { get; init; } = new();
    public bool IsExactMatch { get; init; }
}

/// <summary>
/// DTO de estadísticas KYC
/// </summary>
public class KYCStatisticsDto
{
    public int TotalProfiles { get; set; }
    public int PendingProfiles { get; set; }
    public int InProgressProfiles { get; set; }
    public int ApprovedProfiles { get; set; }
    public int RejectedProfiles { get; set; }
    public int ExpiredProfiles { get; set; }
    public int HighRiskProfiles { get; set; }
    public int PEPProfiles { get; set; }
    public int ExpiringIn30Days { get; set; }
    public double ApprovalRate { get; set; }
    public double HighRiskPercentage { get; set; }
}

/// <summary>
/// DTO de estadísticas de STR
/// </summary>
public class STRStatisticsDto
{
    public int TotalReports { get; set; }
    public int DraftReports { get; set; }
    public int PendingReviewReports { get; set; }
    public int ApprovedReports { get; set; }
    public int SentToUAFReports { get; set; }
    public int OverdueReports { get; set; }
    public decimal TotalAmountReported { get; set; }
}

/// <summary>
/// Resultado paginado
/// </summary>
public record PaginatedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
