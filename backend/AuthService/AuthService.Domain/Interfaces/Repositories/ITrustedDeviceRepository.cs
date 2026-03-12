using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces.Repositories;

/// <summary>
/// US-18.4: Repository for managing trusted devices.
/// </summary>
public interface ITrustedDeviceRepository
{
    /// <summary>
    /// Get a trusted device by its fingerprint hash and user ID.
    /// </summary>
    Task<TrustedDevice?> GetByFingerprintAsync(string userId, string fingerprintHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all trusted devices for a user.
    /// </summary>
    Task<IReadOnlyList<TrustedDevice>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get only active (trusted) devices for a user.
    /// </summary>
    Task<IReadOnlyList<TrustedDevice>> GetTrustedByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new trusted device.
    /// </summary>
    Task<TrustedDevice> AddAsync(TrustedDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing device.
    /// </summary>
    Task UpdateAsync(TrustedDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all devices for a user (e.g., on password change).
    /// </summary>
    Task RevokeAllForUserAsync(string userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get device by ID.
    /// </summary>
    Task<TrustedDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a specific device.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count trusted devices for a user.
    /// </summary>
    Task<int> CountTrustedDevicesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ley 172-13: Hard-deletes ALL trusted devices for a user (cascade deletion).
    /// </summary>
    Task<int> DeleteAllByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
