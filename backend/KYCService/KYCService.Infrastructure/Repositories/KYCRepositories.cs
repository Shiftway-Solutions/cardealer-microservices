using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using KYCService.Domain.Entities;
using KYCService.Domain.Interfaces;
using KYCService.Infrastructure.Persistence;

namespace KYCService.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de perfiles KYC
/// </summary>
public class KYCProfileRepository : IKYCProfileRepository
{
    private readonly KYCDbContext _context;

    public KYCProfileRepository(KYCDbContext context)
    {
        _context = context;
    }

    public async Task<KYCProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.KYCProfiles
            .Include(p => p.Documents)
            .Include(p => p.Verifications)
            .Include(p => p.RiskAssessments.OrderByDescending(r => r.AssessedAt).Take(5))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<KYCProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.KYCProfiles
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<KYCProfile?> GetByDocumentNumberAsync(string documentNumber, CancellationToken cancellationToken = default)
    {
        return await _context.KYCProfiles
            .FirstOrDefaultAsync(p => p.PrimaryDocumentNumber == documentNumber, cancellationToken);
    }

    public async Task<List<KYCProfile>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.KYCProfiles
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KYCProfile>> GetByStatusAsync(KYCStatus status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.KYCProfiles
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KYCProfile>> GetByRiskLevelAsync(RiskLevel riskLevel, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.KYCProfiles
            .Where(p => p.RiskLevel == riskLevel)
            .OrderByDescending(p => p.RiskScore)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KYCProfile>> GetPendingReviewAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.KYCProfiles
            .Where(p => p.Status == KYCStatus.InProgress || p.Status == KYCStatus.UnderReview)
            .OrderBy(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KYCProfile>> GetExpiringAsync(int daysUntilExpiry, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var expiryThreshold = DateTime.UtcNow.AddDays(daysUntilExpiry);
        return await _context.KYCProfiles
            .Where(p => p.Status == KYCStatus.Approved && p.ExpiresAt <= expiryThreshold && p.ExpiresAt > DateTime.UtcNow)
            .OrderBy(p => p.ExpiresAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KYCProfile>> GetPEPProfilesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.KYCProfiles
            .Where(p => p.IsPEP)
            .OrderByDescending(p => p.RiskScore)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByStatusAsync(KYCStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.KYCProfiles
            .CountAsync(p => p.Status == status, cancellationToken);
    }

    public async Task<KYCProfile> CreateAsync(KYCProfile profile, CancellationToken cancellationToken = default)
    {
        _context.KYCProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<KYCProfile> UpdateAsync(KYCProfile profile, CancellationToken cancellationToken = default)
    {
        _context.KYCProfiles.Update(profile);
        await _context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var profile = await _context.KYCProfiles.FindAsync(new object[] { id }, cancellationToken);
        if (profile == null) return false;

        _context.KYCProfiles.Remove(profile);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<KYCStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var in30Days = now.AddDays(30);

        var profiles = await _context.KYCProfiles.ToListAsync(cancellationToken);

        return new KYCStatistics
        {
            TotalProfiles = profiles.Count,
            PendingProfiles = profiles.Count(p => p.Status == KYCStatus.Pending),
            InProgressProfiles = profiles.Count(p => p.Status == KYCStatus.InProgress || p.Status == KYCStatus.UnderReview),
            ApprovedProfiles = profiles.Count(p => p.Status == KYCStatus.Approved),
            RejectedProfiles = profiles.Count(p => p.Status == KYCStatus.Rejected),
            ExpiredProfiles = profiles.Count(p => p.Status == KYCStatus.Expired || (p.ExpiresAt.HasValue && p.ExpiresAt < now)),
            HighRiskProfiles = profiles.Count(p => p.RiskLevel >= RiskLevel.High),
            PEPProfiles = profiles.Count(p => p.IsPEP),
            ExpiringIn30Days = profiles.Count(p => p.Status == KYCStatus.Approved && p.ExpiresAt.HasValue && p.ExpiresAt <= in30Days && p.ExpiresAt > now)
        };
    }
}

/// <summary>
/// Implementación del repositorio de documentos KYC
/// </summary>
public class KYCDocumentRepository : IKYCDocumentRepository
{
    private readonly KYCDbContext _context;
    private readonly ILogger<KYCDocumentRepository> _logger;

    public KYCDocumentRepository(KYCDbContext context, ILogger<KYCDocumentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<KYCDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.KYCDocuments.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<KYCDocument>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await _context.KYCDocuments
            .Where(d => d.KYCProfileId == profileId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KYCDocument>> GetByStatusAsync(KYCDocumentStatus status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.KYCDocuments
            .Where(d => d.Status == status)
            .OrderBy(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<KYCDocument> CreateAsync(KYCDocument document, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== REPOSITORY: CreateAsync START === Document ID: {DocumentId}, ProfileId: {ProfileId}, Type: {Type}", 
            document.Id, document.KYCProfileId, document.Type);
        
        _context.KYCDocuments.Add(document);
        
        _logger.LogInformation("Document added to context, calling SaveChangesAsync...");
        
        var changeCount = await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("=== REPOSITORY: CreateAsync SUCCESS === SaveChanges returned {ChangeCount} changes. Document persisted: {DocumentId}", 
            changeCount, document.Id);
        
        return document;
    }

    public async Task<KYCDocument> UpdateAsync(KYCDocument document, CancellationToken cancellationToken = default)
    {
        _context.KYCDocuments.Update(document);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _context.KYCDocuments.FindAsync(new object[] { id }, cancellationToken);
        if (document == null) return false;

        _context.KYCDocuments.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>
/// Implementación del repositorio de verificaciones KYC
/// </summary>
public class KYCVerificationRepository : IKYCVerificationRepository
{
    private readonly KYCDbContext _context;

    public KYCVerificationRepository(KYCDbContext context)
    {
        _context = context;
    }

    public async Task<KYCVerification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.KYCVerifications.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<KYCVerification>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await _context.KYCVerifications
            .Where(v => v.KYCProfileId == profileId)
            .OrderByDescending(v => v.VerifiedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<KYCVerification?> GetLatestByTypeAsync(Guid profileId, string verificationType, CancellationToken cancellationToken = default)
    {
        return await _context.KYCVerifications
            .Where(v => v.KYCProfileId == profileId && v.VerificationType == verificationType)
            .OrderByDescending(v => v.VerifiedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<KYCVerification> CreateAsync(KYCVerification verification, CancellationToken cancellationToken = default)
    {
        _context.KYCVerifications.Add(verification);
        await _context.SaveChangesAsync(cancellationToken);
        return verification;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var verification = await _context.KYCVerifications.FindAsync(new object[] { id }, cancellationToken);
        if (verification == null) return false;

        _context.KYCVerifications.Remove(verification);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>
/// Implementación del repositorio de evaluaciones de riesgo KYC
/// </summary>
public class KYCRiskAssessmentRepository : IKYCRiskAssessmentRepository
{
    private readonly KYCDbContext _context;

    public KYCRiskAssessmentRepository(KYCDbContext context)
    {
        _context = context;
    }

    public async Task<KYCRiskAssessment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.KYCRiskAssessments.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<KYCRiskAssessment>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await _context.KYCRiskAssessments
            .Where(r => r.KYCProfileId == profileId)
            .OrderByDescending(r => r.AssessedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<KYCRiskAssessment?> GetLatestByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await _context.KYCRiskAssessments
            .Where(r => r.KYCProfileId == profileId)
            .OrderByDescending(r => r.AssessedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<KYCRiskAssessment> CreateAsync(KYCRiskAssessment assessment, CancellationToken cancellationToken = default)
    {
        _context.KYCRiskAssessments.Add(assessment);
        await _context.SaveChangesAsync(cancellationToken);
        return assessment;
    }
}
