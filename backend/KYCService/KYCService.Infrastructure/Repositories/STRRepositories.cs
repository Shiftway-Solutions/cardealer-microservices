using Microsoft.EntityFrameworkCore;
using KYCService.Domain.Entities;
using KYCService.Domain.Interfaces;
using KYCService.Infrastructure.Persistence;

namespace KYCService.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de reportes de transacciones sospechosas
/// </summary>
public class SuspiciousTransactionReportRepository : ISuspiciousTransactionReportRepository
{
    private readonly KYCDbContext _context;

    public SuspiciousTransactionReportRepository(KYCDbContext context)
    {
        _context = context;
    }

    public async Task<SuspiciousTransactionReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SuspiciousTransactionReports.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<SuspiciousTransactionReport?> GetByReportNumberAsync(string reportNumber, CancellationToken cancellationToken = default)
    {
        return await _context.SuspiciousTransactionReports
            .FirstOrDefaultAsync(r => r.ReportNumber == reportNumber, cancellationToken);
    }

    public async Task<List<SuspiciousTransactionReport>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.SuspiciousTransactionReports
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SuspiciousTransactionReport>> GetByStatusAsync(STRStatus status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.SuspiciousTransactionReports
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SuspiciousTransactionReport>> GetPendingApprovalAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.SuspiciousTransactionReports
            .Where(r => r.Status == STRStatus.PendingReview)
            .OrderBy(r => r.ReportingDeadline)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SuspiciousTransactionReport>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.SuspiciousTransactionReports
            .Where(r => r.Status != STRStatus.SentToUAF && r.ReportingDeadline < now)
            .OrderBy(r => r.ReportingDeadline)
            .ToListAsync(cancellationToken);
    }

    public async Task<SuspiciousTransactionReport> CreateAsync(SuspiciousTransactionReport report, CancellationToken cancellationToken = default)
    {
        _context.SuspiciousTransactionReports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task<SuspiciousTransactionReport> UpdateAsync(SuspiciousTransactionReport report, CancellationToken cancellationToken = default)
    {
        _context.SuspiciousTransactionReports.Update(report);
        await _context.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await _context.SuspiciousTransactionReports.FindAsync(new object[] { id }, cancellationToken);
        if (report == null) return false;

        _context.SuspiciousTransactionReports.Remove(report);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string> GenerateReportNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.SuspiciousTransactionReports
            .CountAsync(r => r.CreatedAt.Year == year, cancellationToken);

        return $"STR-{year}-{(count + 1):D5}";
    }

    public async Task<STRStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reports = await _context.SuspiciousTransactionReports.ToListAsync(cancellationToken);

        return new STRStatistics
        {
            TotalReports = reports.Count,
            DraftReports = reports.Count(r => r.Status == STRStatus.Draft),
            PendingReviewReports = reports.Count(r => r.Status == STRStatus.PendingReview),
            ApprovedReports = reports.Count(r => r.Status == STRStatus.Approved),
            SentToUAFReports = reports.Count(r => r.Status == STRStatus.SentToUAF),
            OverdueReports = reports.Count(r => r.Status != STRStatus.SentToUAF && r.ReportingDeadline < now),
            TotalAmountReported = reports.Where(r => r.Amount.HasValue).Sum(r => r.Amount!.Value)
        };
    }
}

/// <summary>
/// Implementación del repositorio de listas de control (watchlists)
/// </summary>
public class WatchlistRepository : IWatchlistRepository
{
    private readonly KYCDbContext _context;

    public WatchlistRepository(KYCDbContext context)
    {
        _context = context;
    }

    public async Task<WatchlistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WatchlistEntries.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<WatchlistEntry>> GetByTypeAsync(WatchlistType type, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.WatchlistEntries
            .Where(e => e.ListType == type && e.IsActive)
            .OrderBy(e => e.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WatchlistEntry>> SearchAsync(string searchTerm, WatchlistType? type, CancellationToken cancellationToken = default)
    {
        var query = _context.WatchlistEntries.Where(e => e.IsActive);

        if (type.HasValue)
        {
            query = query.Where(e => e.ListType == type.Value);
        }

        var searchLower = searchTerm.ToLower();
        query = query.Where(e =>
            e.FullName.ToLower().Contains(searchLower) ||
            (e.DocumentNumber != null && e.DocumentNumber.Contains(searchTerm)) ||
            e.Aliases.Any(a => a.ToLower().Contains(searchLower))
        );

        return await query.Take(100).ToListAsync(cancellationToken);
    }

    public async Task<WatchlistEntry?> FindMatchAsync(string fullName, string? documentNumber, DateTime? dateOfBirth, CancellationToken cancellationToken = default)
    {
        var nameLower = fullName.ToLower();

        // Buscar coincidencia exacta por documento
        if (!string.IsNullOrEmpty(documentNumber))
        {
            var byDocument = await _context.WatchlistEntries
                .FirstOrDefaultAsync(e => e.IsActive && e.DocumentNumber == documentNumber, cancellationToken);
            if (byDocument != null) return byDocument;
        }

        // Buscar coincidencia exacta por nombre
        return await _context.WatchlistEntries
            .FirstOrDefaultAsync(e => e.IsActive && e.FullName.ToLower() == nameLower, cancellationToken);
    }

    public async Task<List<WatchlistMatchResult>> ScreenAsync(string fullName, string? documentNumber, DateTime? dateOfBirth, CancellationToken cancellationToken = default)
    {
        var results = new List<WatchlistMatchResult>();
        var nameLower = fullName.ToLower();
        var nameParts = nameLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var entries = await _context.WatchlistEntries
            .Where(e => e.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var entry in entries)
        {
            var matchedFields = new List<string>();
            var score = 0;
            var isExact = false;

            // Coincidencia por documento (peso alto)
            if (!string.IsNullOrEmpty(documentNumber) && entry.DocumentNumber == documentNumber)
            {
                matchedFields.Add("DocumentNumber");
                score += 50;
                isExact = true;
            }

            // Coincidencia por nombre exacto
            if (entry.FullName.ToLower() == nameLower)
            {
                matchedFields.Add("FullName");
                score += 40;
                isExact = true;
            }
            else
            {
                // Coincidencia parcial por nombre
                var entryNameParts = entry.FullName.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var matchingParts = nameParts.Intersect(entryNameParts).Count();
                if (matchingParts > 0)
                {
                    var partialScore = (matchingParts * 10) / Math.Max(nameParts.Length, entryNameParts.Length);
                    if (partialScore >= 5)
                    {
                        matchedFields.Add("PartialName");
                        score += partialScore;
                    }
                }
            }

            // Coincidencia por alias
            foreach (var alias in entry.Aliases)
            {
                if (alias.ToLower() == nameLower)
                {
                    matchedFields.Add("Alias");
                    score += 30;
                    break;
                }
            }

            // Coincidencia por fecha de nacimiento
            if (dateOfBirth.HasValue && entry.DateOfBirth.HasValue &&
                entry.DateOfBirth.Value.Date == dateOfBirth.Value.Date)
            {
                matchedFields.Add("DateOfBirth");
                score += 10;
            }

            if (score >= 20) // Umbral mínimo de coincidencia
            {
                results.Add(new WatchlistMatchResult
                {
                    Entry = entry,
                    MatchScore = Math.Min(score, 100),
                    MatchedFields = matchedFields,
                    IsExactMatch = isExact
                });
            }
        }

        return results.OrderByDescending(r => r.MatchScore).ToList();
    }

    public async Task<WatchlistEntry> CreateAsync(WatchlistEntry entry, CancellationToken cancellationToken = default)
    {
        _context.WatchlistEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<WatchlistEntry> UpdateAsync(WatchlistEntry entry, CancellationToken cancellationToken = default)
    {
        entry.LastUpdated = DateTime.UtcNow;
        _context.WatchlistEntries.Update(entry);
        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _context.WatchlistEntries.FindAsync(new object[] { id }, cancellationToken);
        if (entry == null) return false;

        _context.WatchlistEntries.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> GetCountByTypeAsync(WatchlistType type, CancellationToken cancellationToken = default)
    {
        return await _context.WatchlistEntries
            .CountAsync(e => e.ListType == type && e.IsActive, cancellationToken);
    }
}
