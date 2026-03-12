using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces.Repositories;

/// <summary>
/// Repositorio para gestión de sesiones de usuario
/// </summary>
public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserSession?> GetByRefreshTokenIdAsync(string refreshTokenId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSession>> GetActiveSessionsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSession>> GetAllSessionsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> GetActiveSessionCountAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default);
    Task RevokeSessionAsync(Guid sessionId, string reason = "User requested", CancellationToken cancellationToken = default);
    Task RevokeAllUserSessionsAsync(string userId, Guid? exceptSessionId = null, string reason = "User requested", CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an existing active session matching the device fingerprint for a user.
    /// The fingerprint is derived from Browser + OS + DeviceType (NOT IP address),
    /// so the same browser/OS combination from any IP reuses the same session
    /// instead of creating duplicates (handles dynamic IPs, VPNs, k8s pod rotation).
    /// </summary>
    Task<UserSession?> GetActiveSessionByDeviceAsync(
        string userId,
        string deviceFingerprint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ley 172-13: Hard-deletes ALL sessions for a user (cascade deletion).
    /// </summary>
    Task<int> DeleteAllByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
