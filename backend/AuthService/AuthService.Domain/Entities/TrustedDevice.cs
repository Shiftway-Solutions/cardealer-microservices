namespace AuthService.Domain.Entities;

/// <summary>
/// US-18.4: Trusted Device entity for device fingerprinting.
/// Stores fingerprint data to identify and trust devices.
/// Uses FingerprintJS or similar client-side library.
/// </summary>
public class TrustedDevice
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The user who owns this device
    /// </summary>
    public string UserId { get; private set; } = null!;

    /// <summary>
    /// Unique device fingerprint (hash from FingerprintJS)
    /// </summary>
    public string FingerprintHash { get; private set; } = null!;

    /// <summary>
    /// User-friendly device name (e.g., "Chrome on Windows")
    /// </summary>
    public string DeviceName { get; private set; } = null!;

    /// <summary>
    /// Browser user agent string
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// IP address when device was first trusted
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// Geographic location (city, country) when first trusted
    /// </summary>
    public string? Location { get; private set; }

    /// <summary>
    /// When this device was first trusted
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last time this device was used for login
    /// </summary>
    public DateTime LastUsedAt { get; private set; }

    /// <summary>
    /// Number of times this device has been used for login
    /// </summary>
    public int LoginCount { get; private set; }

    /// <summary>
    /// Whether this device is currently trusted (can be revoked)
    /// </summary>
    public bool IsTrusted { get; private set; }

    /// <summary>
    /// When the device was revoked (if applicable)
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Reason for revoking (manual, suspicious activity, password change)
    /// </summary>
    public string? RevokeReason { get; private set; }

    // Private constructor for EF Core
    private TrustedDevice() { }

    public TrustedDevice(
        string userId,
        string fingerprintHash,
        string deviceName,
        string? userAgent = null,
        string? ipAddress = null,
        string? location = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        FingerprintHash = fingerprintHash;
        DeviceName = deviceName;
        UserAgent = userAgent;
        IpAddress = ipAddress;
        Location = location;
        CreatedAt = DateTime.UtcNow;
        LastUsedAt = DateTime.UtcNow;
        LoginCount = 1;
        IsTrusted = true;
    }

    /// <summary>
    /// Update last used timestamp and increment login count
    /// </summary>
    public void RecordLogin(string? newIpAddress = null)
    {
        LastUsedAt = DateTime.UtcNow;
        LoginCount++;
        if (!string.IsNullOrEmpty(newIpAddress))
        {
            IpAddress = newIpAddress;
        }
    }

    /// <summary>
    /// Revoke trust for this device
    /// </summary>
    public void Revoke(string reason)
    {
        IsTrusted = false;
        RevokedAt = DateTime.UtcNow;
        RevokeReason = reason;
    }

    /// <summary>
    /// Re-trust a previously revoked device
    /// </summary>
    public void Trust()
    {
        IsTrusted = true;
        RevokedAt = null;
        RevokeReason = null;
    }
}
