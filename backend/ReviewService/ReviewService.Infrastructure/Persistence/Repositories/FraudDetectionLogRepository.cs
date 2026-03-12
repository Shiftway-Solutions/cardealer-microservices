using Microsoft.EntityFrameworkCore;
using ReviewService.Domain.Entities;
using ReviewService.Domain.Interfaces;
using ReviewService.Infrastructure.Persistence;

namespace ReviewService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación del repositorio de logs de detección de fraude
/// </summary>
public class FraudDetectionLogRepository : Repository<FraudDetectionLog, Guid>, IFraudDetectionLogRepository
{
    public FraudDetectionLogRepository(ReviewDbContext context) : base(context)
    {
    }

    public async Task<FraudDetectionLog> LogCheckAsync(
        Guid reviewId,
        FraudCheckType checkType,
        FraudCheckResult result,
        int confidenceScore,
        string details,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var log = new FraudDetectionLog
        {
            Id = Guid.NewGuid(),
            ReviewId = reviewId,
            CheckType = checkType,
            Result = result,
            ConfidenceScore = confidenceScore,
            Details = details,
            CheckedAt = DateTime.UtcNow,
            Metadata = metadata
        };

        await _context.FraudDetectionLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return log;
    }

    public async Task<List<FraudDetectionLog>> GetByReviewIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        return await _context.FraudDetectionLogs
            .Where(x => x.ReviewId == reviewId)
            .OrderByDescending(x => x.CheckedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FraudDetectionLog>> GetByCheckTypeAsync(
        FraudCheckType checkType,
        FraudCheckResult? result = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FraudDetectionLogs.Where(x => x.CheckType == checkType);

        if (result.HasValue)
            query = query.Where(x => x.Result == result.Value);

        if (fromDate.HasValue)
            query = query.Where(x => x.CheckedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.CheckedAt <= toDate.Value);

        return await query
            .OrderByDescending(x => x.CheckedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetSuspiciousReviewsAsync(int minFailedChecks = 2, int timeWindow = 24, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-timeWindow);

        return await _context.FraudDetectionLogs
            .Where(x => x.CheckedAt >= cutoffTime &&
                       (x.Result == FraudCheckResult.Failed || x.Result == FraudCheckResult.Suspicious))
            .GroupBy(x => x.ReviewId)
            .Where(g => g.Count() >= minFailedChecks)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<FraudCheckType, Dictionary<FraudCheckResult, int>>> GetStatsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FraudDetectionLogs.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(x => x.CheckedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.CheckedAt <= toDate.Value);

        var stats = await query
            .GroupBy(x => new { x.CheckType, x.Result })
            .Select(g => new { g.Key.CheckType, g.Key.Result, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<FraudCheckType, Dictionary<FraudCheckResult, int>>();

        foreach (var stat in stats)
        {
            if (!result.ContainsKey(stat.CheckType))
            {
                result[stat.CheckType] = new Dictionary<FraudCheckResult, int>();
            }

            result[stat.CheckType][stat.Result] = stat.Count;
        }

        return result;
    }

    public async Task<List<FraudDetectionLog>> GetByIpPatternAsync(string ipAddress, int timeWindow = 24, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-timeWindow);

        // Buscar en metadata donde esté almacenada la IP (formato JSON)
        return await _context.FraudDetectionLogs
            .Where(x => x.CheckedAt >= cutoffTime &&
                       x.Metadata != null &&
                       x.Metadata.Contains(ipAddress))
            .OrderByDescending(x => x.CheckedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<double> GetAverageConfidenceScoreForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Buscar reviews del usuario y calcular promedio de confidence scores
        var userReviewIds = await _context.Reviews
            .Where(r => r.BuyerId == userId)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (!userReviewIds.Any())
            return 100.0; // Default high confidence for users without reviews

        var logs = await _context.FraudDetectionLogs
            .Where(x => userReviewIds.Contains(x.ReviewId))
            .ToListAsync(cancellationToken);

        if (!logs.Any())
            return 100.0; // Default high confidence if no fraud checks

        return logs.Average(x => x.ConfidenceScore);
    }
}