using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthService.Tests.Unit.Services;

/// <summary>
/// US-18.4: Unit tests for DeviceFingerprintService.
/// </summary>
public class DeviceFingerprintServiceTests
{
    private readonly Mock<ITrustedDeviceRepository> _deviceRepositoryMock;
    private readonly Mock<IAuthNotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<DeviceFingerprintService>> _loggerMock;
    private readonly DeviceFingerprintService _service;

    public DeviceFingerprintServiceTests()
    {
        _deviceRepositoryMock = new Mock<ITrustedDeviceRepository>();
        _notificationServiceMock = new Mock<IAuthNotificationService>();
        _loggerMock = new Mock<ILogger<DeviceFingerprintService>>();

        _service = new DeviceFingerprintService(
            _deviceRepositoryMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void HashFingerprint_SameInput_ProducesSameHash()
    {
        // Arrange
        var fingerprintData = "device-fingerprint-data";

        // Act
        var hash1 = _service.HashFingerprint(fingerprintData);
        var hash2 = _service.HashFingerprint(fingerprintData);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashFingerprint_DifferentInputs_ProduceDifferentHashes()
    {
        // Arrange
        var data1 = "device-1-data";
        var data2 = "device-2-data";

        // Act
        var hash1 = _service.HashFingerprint(data1);
        var hash2 = _service.HashFingerprint(data2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task IsDeviceTrustedAsync_WhenDeviceExists_ReturnsTrue()
    {
        // Arrange
        var userId = "user-123";
        var fingerprintHash = "hash-abc";
        var device = new TrustedDevice(userId, fingerprintHash, "Chrome on Mac");

        _deviceRepositoryMock
            .Setup(r => r.GetByFingerprintAsync(userId, fingerprintHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        // Act
        var result = await _service.IsDeviceTrustedAsync(userId, fingerprintHash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsDeviceTrustedAsync_WhenDeviceNotExists_ReturnsFalse()
    {
        // Arrange
        var userId = "user-123";
        var fingerprintHash = "hash-abc";

        _deviceRepositoryMock
            .Setup(r => r.GetByFingerprintAsync(userId, fingerprintHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrustedDevice?)null);

        // Act
        var result = await _service.IsDeviceTrustedAsync(userId, fingerprintHash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsDeviceTrustedAsync_WhenDeviceRevoked_ReturnsFalse()
    {
        // Arrange
        var userId = "user-123";
        var fingerprintHash = "hash-abc";
        var device = new TrustedDevice(userId, fingerprintHash, "Old Device");
        device.Revoke("Security concern");

        _deviceRepositoryMock
            .Setup(r => r.GetByFingerprintAsync(userId, fingerprintHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        // Act
        var result = await _service.IsDeviceTrustedAsync(userId, fingerprintHash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrCreateDeviceAsync_WhenNewDevice_CreatesAndReturnsNew()
    {
        // Arrange
        var userId = "user-123";
        var fingerprintHash = "new-hash";
        var deviceName = "Safari on iPhone";

        _deviceRepositoryMock
            .Setup(r => r.GetByFingerprintAsync(userId, fingerprintHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrustedDevice?)null);

        _deviceRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustedDevice>());

        _deviceRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<TrustedDevice>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrustedDevice device, CancellationToken _) => device);

        // Act
        var (device, isNew) = await _service.GetOrCreateDeviceAsync(
            userId, fingerprintHash, deviceName, "Mozilla/5.0", "192.168.1.1", "Santo Domingo");

        // Assert
        isNew.Should().BeTrue();
        device.UserId.Should().Be(userId);
        device.FingerprintHash.Should().Be(fingerprintHash);
        device.DeviceName.Should().Be(deviceName);

        _deviceRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TrustedDevice>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateDeviceAsync_WhenExistingDevice_UpdatesAndReturnsExisting()
    {
        // Arrange
        var userId = "user-123";
        var fingerprintHash = "existing-hash";
        var existingDevice = new TrustedDevice(userId, fingerprintHash, "Chrome on Mac");

        _deviceRepositoryMock
            .Setup(r => r.GetByFingerprintAsync(userId, fingerprintHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDevice);

        _deviceRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<TrustedDevice>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var (device, isNew) = await _service.GetOrCreateDeviceAsync(
            userId, fingerprintHash, "Chrome on Mac", "Mozilla/5.0", "192.168.1.1");

        // Assert
        isNew.Should().BeFalse();
        device.Should().Be(existingDevice);

        _deviceRepositoryMock.Verify(r => r.UpdateAsync(existingDevice, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordLoginAsync_UpdatesDeviceLoginInfo()
    {
        // Arrange
        var userId = "user-123";
        var fingerprintHash = "hash-abc";
        var device = new TrustedDevice(userId, fingerprintHash, "Chrome on Mac");
        var initialCount = device.LoginCount;

        _deviceRepositoryMock
            .Setup(r => r.GetByFingerprintAsync(userId, fingerprintHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        _deviceRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<TrustedDevice>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RecordLoginAsync(userId, fingerprintHash, "10.0.0.1");

        // Assert
        device.LoginCount.Should().Be(initialCount + 1);
        device.IpAddress.Should().Be("10.0.0.1");
        _deviceRepositoryMock.Verify(r => r.UpdateAsync(device, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeDeviceAsync_RevokesDevice()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var device = new TrustedDevice("user-123", "hash-abc", "Old Device");

        _deviceRepositoryMock
            .Setup(r => r.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        _deviceRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<TrustedDevice>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RevokeDeviceAsync(deviceId, "User requested");

        // Assert
        device.IsTrusted.Should().BeFalse();
        device.RevokeReason.Should().Be("User requested");
        device.RevokedAt.Should().NotBeNull();
        _deviceRepositoryMock.Verify(r => r.UpdateAsync(device, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserDevicesAsync_ReturnsAllUserDevices()
    {
        // Arrange
        var userId = "user-123";
        var devices = new List<TrustedDevice>
        {
            new TrustedDevice(userId, "hash-1", "Device 1"),
            new TrustedDevice(userId, "hash-2", "Device 2"),
            new TrustedDevice(userId, "hash-3", "Device 3")
        };

        _deviceRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);

        // Act
        var result = await _service.GetUserDevicesAsync(userId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(devices);
    }
}
