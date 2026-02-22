using KYCService.Domain.Entities;

namespace KYCService.Domain.Interfaces;

/// <summary>
/// Repositorio para perfiles KYC
/// </summary>
public interface IKYCProfileRepository
{
    Task<KYCProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KYCProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<KYCProfile?> GetByDocumentNumberAsync(string documentNumber, CancellationToken cancellationToken = default);
    Task<List<KYCProfile>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<KYCProfile>> GetByStatusAsync(KYCStatus status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<KYCProfile>> GetByRiskLevelAsync(RiskLevel riskLevel, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<KYCProfile>> GetPendingReviewAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<KYCProfile>> GetExpiringAsync(int daysUntilExpiry, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<KYCProfile>> GetPEPProfilesAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountByStatusAsync(KYCStatus status, CancellationToken cancellationToken = default);
    Task<KYCProfile> CreateAsync(KYCProfile profile, CancellationToken cancellationToken = default);
    Task<KYCProfile> UpdateAsync(KYCProfile profile, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KYCStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Estadísticas de KYC
/// </summary>
public class KYCStatistics
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
}

/// <summary>
/// Repositorio para documentos KYC
/// </summary>
public interface IKYCDocumentRepository
{
    Task<KYCDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<KYCDocument>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<List<KYCDocument>> GetByStatusAsync(KYCDocumentStatus status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<KYCDocument> CreateAsync(KYCDocument document, CancellationToken cancellationToken = default);
    Task<KYCDocument> UpdateAsync(KYCDocument document, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repositorio para verificaciones KYC
/// </summary>
public interface IKYCVerificationRepository
{
    Task<KYCVerification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<KYCVerification>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<KYCVerification?> GetLatestByTypeAsync(Guid profileId, string verificationType, CancellationToken cancellationToken = default);
    Task<KYCVerification> CreateAsync(KYCVerification verification, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repositorio para evaluaciones de riesgo
/// </summary>
public interface IKYCRiskAssessmentRepository
{
    Task<KYCRiskAssessment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<KYCRiskAssessment>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<KYCRiskAssessment?> GetLatestByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<KYCRiskAssessment> CreateAsync(KYCRiskAssessment assessment, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repositorio para reportes de transacciones sospechosas
/// </summary>
public interface ISuspiciousTransactionReportRepository
{
    Task<SuspiciousTransactionReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SuspiciousTransactionReport?> GetByReportNumberAsync(string reportNumber, CancellationToken cancellationToken = default);
    Task<List<SuspiciousTransactionReport>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<SuspiciousTransactionReport>> GetByStatusAsync(STRStatus status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<SuspiciousTransactionReport>> GetPendingApprovalAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<SuspiciousTransactionReport>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<SuspiciousTransactionReport> CreateAsync(SuspiciousTransactionReport report, CancellationToken cancellationToken = default);
    Task<SuspiciousTransactionReport> UpdateAsync(SuspiciousTransactionReport report, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string> GenerateReportNumberAsync(CancellationToken cancellationToken = default);
    Task<STRStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Estadísticas de reportes de transacciones sospechosas
/// </summary>
public class STRStatistics
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
/// Repositorio para listas de control (watchlists)
/// </summary>
public interface IWatchlistRepository
{
    Task<WatchlistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<WatchlistEntry>> GetByTypeAsync(WatchlistType type, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<WatchlistEntry>> SearchAsync(string searchTerm, WatchlistType? type, CancellationToken cancellationToken = default);
    Task<WatchlistEntry?> FindMatchAsync(string fullName, string? documentNumber, DateTime? dateOfBirth, CancellationToken cancellationToken = default);
    Task<List<WatchlistMatchResult>> ScreenAsync(string fullName, string? documentNumber, DateTime? dateOfBirth, CancellationToken cancellationToken = default);
    Task<WatchlistEntry> CreateAsync(WatchlistEntry entry, CancellationToken cancellationToken = default);
    Task<WatchlistEntry> UpdateAsync(WatchlistEntry entry, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetCountByTypeAsync(WatchlistType type, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de búsqueda en listas de control
/// </summary>
public class WatchlistMatchResult
{
    public WatchlistEntry Entry { get; set; } = null!;
    public int MatchScore { get; set; } // 0-100
    public List<string> MatchedFields { get; set; } = new();
    public bool IsExactMatch { get; set; }
}

/// <summary>
/// Repositorio para borradores de perfil KYC (autosave del wizard)
/// </summary>
public interface IKYCProfileDraftRepository
{
    Task<KYCProfileDraft?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<KYCProfileDraft> UpsertAsync(KYCProfileDraft draft, CancellationToken cancellationToken = default);
    Task<bool> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);
}
