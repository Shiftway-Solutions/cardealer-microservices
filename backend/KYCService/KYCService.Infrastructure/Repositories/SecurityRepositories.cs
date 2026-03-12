using Microsoft.EntityFrameworkCore;
using KYCService.Domain.Entities;
using KYCService.Domain.Interfaces;
using KYCService.Infrastructure.Persistence;

namespace KYCService.Infrastructure.Repositories;

/// <summary>
/// Repository for idempotency key management
/// </summary>
public class IdempotencyKeyRepository : IIdempotencyKeyRepository
{
    private readonly KYCDbContext _context;

    public IdempotencyKeyRepository(KYCDbContext context)
    {
        _context = context;
    }

    public async Task<IdempotencyKey?> GetByKeyAsync(string key, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.IdempotencyKeys
            .FirstOrDefaultAsync(k => k.Key == key && k.UserId == userId && k.ExpiresAt > DateTime.UtcNow, cancellationToken);
    }

    public async Task<IdempotencyKey> CreateAsync(IdempotencyKey entry, CancellationToken cancellationToken = default)
    {
        entry.Id = Guid.NewGuid();
        entry.IsProcessing = true;
        _context.IdempotencyKeys.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task UpdateAsync(IdempotencyKey entry, CancellationToken cancellationToken = default)
    {
        entry.IsProcessing = false;
        _context.IdempotencyKeys.Update(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expired = await _context.IdempotencyKeys
            .Where(k => k.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _context.IdempotencyKeys.RemoveRange(expired);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Repository for KYC audit logging
/// </summary>
public class KYCAuditLogRepository : IKYCAuditLogRepository
{
    private readonly KYCDbContext _context;

    public KYCAuditLogRepository(KYCDbContext context)
    {
        _context = context;
    }

    public async Task<KYCAuditLog> LogAsync(KYCAuditLog entry, CancellationToken cancellationToken = default)
    {
        entry.Id = Guid.NewGuid();
        entry.CreatedAt = DateTime.UtcNow;
        _context.KYCAuditLogs.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<List<KYCAuditLog>> GetByProfileIdAsync(Guid profileId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _context.KYCAuditLogs
            .Where(l => l.ProfileId == profileId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KYCAuditLog>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _context.KYCAuditLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KYCAuditLog>> GetByActionAsync(KYCAuditAction action, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _context.KYCAuditLogs
            .Where(l => l.Action == action && l.CreatedAt >= from && l.CreatedAt <= to)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KYCAuditLog>> GetSecurityEventsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var securityActions = new[]
        {
            KYCAuditAction.DuplicateProfileAttempt,
            KYCAuditAction.DuplicateDocumentAttempt,
            KYCAuditAction.RateLimitExceeded,
            KYCAuditAction.SuspiciousActivity,
            KYCAuditAction.UnauthorizedAccess
        };

        return await _context.KYCAuditLogs
            .Where(l => securityActions.Contains(l.Action) && l.CreatedAt >= from && l.CreatedAt <= to)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Repository for rate limiting
/// </summary>
public class RateLimitRepository : IRateLimitRepository
{
    private readonly KYCDbContext _context;

    public RateLimitRepository(KYCDbContext context)
    {
        _context = context;
    }

    public async Task<RateLimitEntry> IncrementAsync(string key, string endpoint, TimeSpan windowDuration, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entry = await _context.RateLimitEntries
            .FirstOrDefaultAsync(r => r.Key == key && r.Endpoint == endpoint && r.WindowEnd > now, cancellationToken);

        if (entry == null)
        {
            entry = new RateLimitEntry
            {
                Id = Guid.NewGuid(),
                Key = key,
                Endpoint = endpoint,
                RequestCount = 1,
                WindowStart = now,
                WindowEnd = now.Add(windowDuration)
            };
            _context.RateLimitEntries.Add(entry);
        }
        else
        {
            entry.RequestCount++;
            _context.RateLimitEntries.Update(entry);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<RateLimitEntry?> GetAsync(string key, string endpoint, CancellationToken cancellationToken = default)
    {
        return await _context.RateLimitEntries
            .FirstOrDefaultAsync(r => r.Key == key && r.Endpoint == endpoint && r.WindowEnd > DateTime.UtcNow, cancellationToken);
    }

    public async Task ResetAsync(string key, string endpoint, CancellationToken cancellationToken = default)
    {
        var entries = await _context.RateLimitEntries
            .Where(r => r.Key == key && r.Endpoint == endpoint)
            .ToListAsync(cancellationToken);

        _context.RateLimitEntries.RemoveRange(entries);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expired = await _context.RateLimitEntries
            .Where(r => r.WindowEnd < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _context.RateLimitEntries.RemoveRange(expired);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Repository for KYC saga state management
/// </summary>
public class KYCSagaRepository : IKYCSagaRepository
{
    private readonly KYCDbContext _context;

    public KYCSagaRepository(KYCDbContext context)
    {
        _context = context;
    }

    public async Task<KYCSagaState> CreateAsync(KYCSagaState saga, CancellationToken cancellationToken = default)
    {
        saga.Id = Guid.NewGuid();
        saga.StartedAt = DateTime.UtcNow;
        _context.KYCSagaStates.Add(saga);
        await _context.SaveChangesAsync(cancellationToken);
        return saga;
    }

    public async Task<KYCSagaState?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return await _context.KYCSagaStates
            .FirstOrDefaultAsync(s => s.CorrelationId == correlationId, cancellationToken);
    }

    public async Task UpdateAsync(KYCSagaState saga, CancellationToken cancellationToken = default)
    {
        _context.KYCSagaStates.Update(saga);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<KYCSagaState>> GetIncompleteSagasAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.KYCSagaStates
            .Where(s => s.UserId == userId &&
                       (s.Status == SagaStatus.Started || s.Status == SagaStatus.InProgress))
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KYCSagaState>> GetSagasNeedingRollbackAsync(CancellationToken cancellationToken = default)
    {
        return await _context.KYCSagaStates
            .Where(s => s.Status == SagaStatus.Failed && s.RolledBackAt == null)
            .OrderBy(s => s.StartedAt)
            .ToListAsync(cancellationToken);
    }
}
