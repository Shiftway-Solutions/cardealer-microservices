using AuthService.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace AuthService.Tests.Unit.Entities;

/// <summary>
/// US-18.4: Unit tests for TrustedDevice entity.
/// </summary>
public class TrustedDeviceTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var userId = "user-123";
        var fingerprintHash = "hash-abc";
        var deviceName = "Chrome on Mac";
        var userAgent = "Mozilla/5.0";
        var ipAddress = "192.168.1.1";
        var location = "Santo Domingo, DO";

        // Act
        var device = new TrustedDevice(userId, fingerprintHash, deviceName, userAgent, ipAddress, location);

        // Assert
        device.Id.Should().NotBeEmpty();
        device.UserId.Should().Be(userId);
        device.FingerprintHash.Should().Be(fingerprintHash);
        device.DeviceName.Should().Be(deviceName);
        device.UserAgent.Should().Be(userAgent);
        device.IpAddress.Should().Be(ipAddress);
        device.Location.Should().Be(location);
        device.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        device.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        device.LoginCount.Should().Be(1); // Initial login
        device.IsTrusted.Should().BeTrue();
        device.RevokedAt.Should().BeNull();
        device.RevokeReason.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithOptionalParametersNull_Works()
    {
        // Arrange & Act
        var device = new TrustedDevice("user-123", "hash-abc", "Safari on iPhone");

        // Assert
        device.UserAgent.Should().BeNull();
        device.IpAddress.Should().BeNull();
        device.Location.Should().BeNull();
        device.IsTrusted.Should().BeTrue();
    }

    [Fact]
    public void RecordLogin_IncrementsCountAndUpdatesTime()
    {
        // Arrange
        var device = new TrustedDevice("user-123", "hash-abc", "Chrome on Mac");
        var initialCount = device.LoginCount;
        var initialTime = device.LastUsedAt;

        // Wait a bit to ensure time difference
        System.Threading.Thread.Sleep(10);

        // Act
        device.RecordLogin();

        // Assert
        device.LoginCount.Should().Be(initialCount + 1);
        device.LastUsedAt.Should().BeAfter(initialTime);
    }

    [Fact]
    public void RecordLogin_WithNewIpAddress_UpdatesIpAddress()
    {
        // Arrange
        var device = new TrustedDevice("user-123", "hash-abc", "Chrome on Mac", ipAddress: "192.168.1.1");
        var newIpAddress = "10.0.0.1";

        // Act
        device.RecordLogin(newIpAddress);

        // Assert
        device.IpAddress.Should().Be(newIpAddress);
        device.LoginCount.Should().Be(2); // Initial + 1
    }

    [Fact]
    public void RecordLogin_WithNullIpAddress_KeepsExistingIp()
    {
        // Arrange
        var originalIp = "192.168.1.1";
        var device = new TrustedDevice("user-123", "hash-abc", "Chrome on Mac", ipAddress: originalIp);

        // Act
        device.RecordLogin(null);

        // Assert
        device.IpAddress.Should().Be(originalIp);
    }

    [Fact]
    public void Revoke_SetsRevokedProperties()
    {
        // Arrange
        var device = new TrustedDevice("user-123", "hash-abc", "Chrome on Mac");
        var reason = "Suspicious activity detected";

        // Act
        device.Revoke(reason);

        // Assert
        device.IsTrusted.Should().BeFalse();
        device.RevokedAt.Should().NotBeNull();
        device.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        device.RevokeReason.Should().Be(reason);
    }

    [Fact]
    public void Trust_ResetsRevokedProperties()
    {
        // Arrange
        var device = new TrustedDevice("user-123", "hash-abc", "Chrome on Mac");
        device.Revoke("Test revoke");
        device.IsTrusted.Should().BeFalse(); // Verify it was revoked

        // Act
        device.Trust();

        // Assert
        device.IsTrusted.Should().BeTrue();
        device.RevokedAt.Should().BeNull();
        device.RevokeReason.Should().BeNull();
    }

    [Fact]
    public void MultipleRecordLogins_AccumulateCorrectly()
    {
        // Arrange
        var device = new TrustedDevice("user-123", "hash-abc", "Chrome on Mac");

        // Act
        device.RecordLogin();
        device.RecordLogin();
        device.RecordLogin();

        // Assert
        device.LoginCount.Should().Be(4); // Initial (1) + 3 logins
    }
}
