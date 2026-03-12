using KYCService.Domain.Entities;

namespace KYCService.Domain.Interfaces;

/// <summary>
/// Repository for idempotency key management
/// </summary>
public interface IIdempotencyKeyRepository
{
    /// <summary>
    /// Get an existing idempotency key
    /// </summary>
    Task<IdempotencyKey?> GetByKeyAsync(string key, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new idempotency key (marks as processing)
    /// </summary>
    Task<IdempotencyKey> CreateAsync(IdempotencyKey entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update idempotency key with response
    /// </summary>
    Task UpdateAsync(IdempotencyKey entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete expired idempotency keys
    /// </summary>
    Task CleanupExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for KYC audit logging
/// </summary>
public interface IKYCAuditLogRepository
{
    /// <summary>
    /// Log an audit entry
    /// </summary>
    Task<KYCAuditLog> LogAsync(KYCAuditLog entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a profile
    /// </summary>
    Task<List<KYCAuditLog>> GetByProfileIdAsync(Guid profileId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a user
    /// </summary>
    Task<List<KYCAuditLog>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by action type
    /// </summary>
    Task<List<KYCAuditLog>> GetByActionAsync(KYCAuditAction action, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get security-related audit logs (suspicious activity, rate limits, etc.)
    /// </summary>
    Task<List<KYCAuditLog>> GetSecurityEventsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for rate limiting
/// </summary>
public interface IRateLimitRepository
{
    /// <summary>
    /// Increment request count for a key/endpoint
    /// </summary>
    Task<RateLimitEntry> IncrementAsync(string key, string endpoint, TimeSpan windowDuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current rate limit entry
    /// </summary>
    Task<RateLimitEntry?> GetAsync(string key, string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset rate limit for a key
    /// </summary>
    Task ResetAsync(string key, string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleanup expired entries
    /// </summary>
    Task CleanupExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for KYC saga state management
/// </summary>
public interface IKYCSagaRepository
{
    /// <summary>
    /// Create a new saga
    /// </summary>
    Task<KYCSagaState> CreateAsync(KYCSagaState saga, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get saga by correlation ID
    /// </summary>
    Task<KYCSagaState?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update saga state
    /// </summary>
    Task UpdateAsync(KYCSagaState saga, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get incomplete sagas for a user
    /// </summary>
    Task<List<KYCSagaState>> GetIncompleteSagasAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get sagas that need rollback (failed but not rolled back)
    /// </summary>
    Task<List<KYCSagaState>> GetSagasNeedingRollbackAsync(CancellationToken cancellationToken = default);
}
