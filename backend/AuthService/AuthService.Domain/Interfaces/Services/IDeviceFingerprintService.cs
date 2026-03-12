using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces.Services;

/// <summary>
/// US-18.4: Service for managing trusted devices and device fingerprinting.
/// </summary>
public interface IDeviceFingerprintService
{
    /// <summary>
    /// Check if a device fingerprint is trusted for a user.
    /// </summary>
    Task<bool> IsDeviceTrustedAsync(string userId, string fingerprintHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or create a trusted device record.
    /// Returns true if this is a new device (should trigger notification).
    /// </summary>
    Task<(TrustedDevice device, bool isNew)> GetOrCreateDeviceAsync(
        string userId,
        string fingerprintHash,
        string deviceName,
        string? userAgent = null,
        string? ipAddress = null,
        string? location = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a login on a device (update last used, increment count).
    /// </summary>
    Task RecordLoginAsync(string userId, string fingerprintHash, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a specific device.
    /// </summary>
    Task RevokeDeviceAsync(Guid deviceId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all devices for a user (e.g., on password change).
    /// </summary>
    Task RevokeAllDevicesAsync(string userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all devices for a user.
    /// </summary>
    Task<IReadOnlyList<TrustedDevice>> GetUserDevicesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a hash from fingerprint data.
    /// </summary>
    string HashFingerprint(string fingerprintData);
}
