using System.Security.Cryptography;
using System.Text;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// US-18.4: Device fingerprinting service implementation.
/// Manages trusted devices and sends alerts for new device logins.
/// </summary>
public class DeviceFingerprintService : IDeviceFingerprintService
{
    private readonly ITrustedDeviceRepository _deviceRepository;
    private readonly IAuthNotificationService _notificationService;
    private readonly ILogger<DeviceFingerprintService> _logger;

    private const int MAX_DEVICES_PER_USER = 10;

    public DeviceFingerprintService(
        ITrustedDeviceRepository deviceRepository,
        IAuthNotificationService notificationService,
        ILogger<DeviceFingerprintService> logger)
    {
        _deviceRepository = deviceRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<bool> IsDeviceTrustedAsync(string userId, string fingerprintHash, CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByFingerprintAsync(userId, fingerprintHash, cancellationToken);
        return device?.IsTrusted ?? false;
    }

    public async Task<(TrustedDevice device, bool isNew)> GetOrCreateDeviceAsync(
        string userId,
        string fingerprintHash,
        string deviceName,
        string? userAgent = null,
        string? ipAddress = null,
        string? location = null,
        CancellationToken cancellationToken = default)
    {
        var existingDevice = await _deviceRepository.GetByFingerprintAsync(userId, fingerprintHash, cancellationToken);

        if (existingDevice != null)
        {
            // Existing device - update last used
            existingDevice.RecordLogin(ipAddress);
            await _deviceRepository.UpdateAsync(existingDevice, cancellationToken);

            _logger.LogInformation("Known device {DeviceId} used for login by user {UserId}",
                existingDevice.Id, userId);

            return (existingDevice, isNew: false);
        }

        // New device - create and potentially limit devices
        await EnforcMaxDevicesLimitAsync(userId, cancellationToken);

        var newDevice = new TrustedDevice(
            userId,
            fingerprintHash,
            deviceName,
            userAgent,
            ipAddress,
            location);

        await _deviceRepository.AddAsync(newDevice, cancellationToken);

        _logger.LogInformation("New device {DeviceId} trusted for user {UserId}: {DeviceName}",
            newDevice.Id, userId, deviceName);

        return (newDevice, isNew: true);
    }

    public async Task RecordLoginAsync(string userId, string fingerprintHash, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByFingerprintAsync(userId, fingerprintHash, cancellationToken);

        if (device != null)
        {
            device.RecordLogin(ipAddress);
            await _deviceRepository.UpdateAsync(device, cancellationToken);
        }
    }

    public async Task RevokeDeviceAsync(Guid deviceId, string reason, CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken);

        if (device != null)
        {
            device.Revoke(reason);
            await _deviceRepository.UpdateAsync(device, cancellationToken);

            _logger.LogInformation("Device {DeviceId} revoked for user {UserId}. Reason: {Reason}",
                deviceId, device.UserId, reason);
        }
    }

    public async Task RevokeAllDevicesAsync(string userId, string reason, CancellationToken cancellationToken = default)
    {
        await _deviceRepository.RevokeAllForUserAsync(userId, reason, cancellationToken);

        _logger.LogInformation("All devices revoked for user {UserId}. Reason: {Reason}", userId, reason);
    }

    public async Task<IReadOnlyList<TrustedDevice>> GetUserDevicesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _deviceRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public string HashFingerprint(string fingerprintData)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(fingerprintData);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Enforce maximum devices limit per user by removing oldest devices.
    /// </summary>
    private async Task EnforcMaxDevicesLimitAsync(string userId, CancellationToken cancellationToken)
    {
        var deviceCount = await _deviceRepository.CountTrustedDevicesAsync(userId, cancellationToken);

        if (deviceCount >= MAX_DEVICES_PER_USER)
        {
            // Get all devices and remove the oldest
            var devices = await _deviceRepository.GetTrustedByUserIdAsync(userId, cancellationToken);
            var devicesToRemove = devices
                .OrderBy(d => d.LastUsedAt)
                .Take(deviceCount - MAX_DEVICES_PER_USER + 1);

            foreach (var device in devicesToRemove)
            {
                await _deviceRepository.DeleteAsync(device.Id, cancellationToken);
                _logger.LogInformation("Removed oldest device {DeviceId} for user {UserId} due to device limit",
                    device.Id, userId);
            }
        }
    }
}
