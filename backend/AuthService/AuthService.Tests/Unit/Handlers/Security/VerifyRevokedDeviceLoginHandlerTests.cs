using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using AuthService.Application.Features.Auth.Commands.VerifyRevokedDeviceLogin;
using AuthService.Domain.Interfaces.Services;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;

namespace AuthService.Tests.Unit.Handlers.Security;

/// <summary>
/// Unit tests for VerifyRevokedDeviceLoginCommandHandler
/// Tests AUTH-SEC-005: Verify revoked device login attempt
/// Handler uses: IDistributedCache, INotificationService, ILogger
/// </summary>
public class VerifyRevokedDeviceLoginHandlerTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<VerifyRevokedDeviceLoginCommandHandler>> _loggerMock;
    private readonly VerifyRevokedDeviceLoginCommandHandler _handler;

    public VerifyRevokedDeviceLoginHandlerTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<VerifyRevokedDeviceLoginCommandHandler>>();

        _handler = new VerifyRevokedDeviceLoginCommandHandler(
            _cacheMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCode_ShouldVerifySuccessfully()
    {
        // Arrange
        var verificationToken = Guid.NewGuid().ToString();
        var verificationCode = "123456";
        var codeHash = HashCode(verificationCode);
        var userId = Guid.NewGuid().ToString();
        var deviceFingerprint = "device-fingerprint-123";

        var cacheData = new
        {
            CodeHash = codeHash,
            UserId = userId,
            Email = "test@test.com",
            DeviceFingerprint = deviceFingerprint,
            IpAddress = "192.168.1.100",
            UserAgent = "TestBrowser/1.0",
            Browser = "Chrome",
            OperatingSystem = "Windows",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            RemainingAttempts = 3
        };

        var command = new VerifyRevokedDeviceLoginCommand(
            VerificationToken: verificationToken,
            Code: verificationCode,
            IpAddress: "192.168.1.100");

        var cacheKey = $"revoked_device_login:{verificationToken}";
        _cacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cacheData)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.DeviceCleared.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidCode_ShouldReturnErrorWithRemainingAttempts()
    {
        // Arrange
        var verificationToken = Guid.NewGuid().ToString();
        var correctCodeHash = HashCode("123456");
        var userId = Guid.NewGuid().ToString();
        var deviceFingerprint = "device-fingerprint-123";

        var cacheData = new
        {
            CodeHash = correctCodeHash,
            UserId = userId,
            Email = "test@test.com",
            DeviceFingerprint = deviceFingerprint,
            IpAddress = "192.168.1.100",
            UserAgent = "TestBrowser/1.0",
            Browser = "Chrome",
            OperatingSystem = "Windows",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            RemainingAttempts = 3
        };

        var command = new VerifyRevokedDeviceLoginCommand(
            VerificationToken: verificationToken,
            Code: "999999",
            IpAddress: "192.168.1.100");

        var cacheKey = $"revoked_device_login:{verificationToken}";
        _cacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cacheData)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.RemainingAttempts.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenNoVerificationPending_ShouldReturnError()
    {
        // Arrange
        var verificationToken = Guid.NewGuid().ToString();

        var command = new VerifyRevokedDeviceLoginCommand(
            VerificationToken: verificationToken,
            Code: "123456",
            IpAddress: "192.168.1.100");

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithExpiredCode_ShouldReturnError()
    {
        // Arrange
        var verificationToken = Guid.NewGuid().ToString();
        var verificationCode = "123456";
        var codeHash = HashCode(verificationCode);
        var userId = Guid.NewGuid().ToString();

        var cacheData = new
        {
            CodeHash = codeHash,
            UserId = userId,
            Email = "test@test.com",
            DeviceFingerprint = "device-fingerprint-123",
            IpAddress = "192.168.1.100",
            UserAgent = "TestBrowser/1.0",
            Browser = "Chrome",
            OperatingSystem = "Windows",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
            RemainingAttempts = 3
        };

        var command = new VerifyRevokedDeviceLoginCommand(
            VerificationToken: verificationToken,
            Code: verificationCode,
            IpAddress: "192.168.1.100");

        var cacheKey = $"revoked_device_login:{verificationToken}";
        _cacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cacheData)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenMaxAttemptsReached_ShouldSetLockout()
    {
        // Arrange
        var verificationToken = Guid.NewGuid().ToString();
        var correctCodeHash = HashCode("123456");
        var userId = Guid.NewGuid().ToString();

        var cacheData = new
        {
            CodeHash = correctCodeHash,
            UserId = userId,
            Email = "test@test.com",
            DeviceFingerprint = "device-fingerprint-123",
            IpAddress = "192.168.1.100",
            UserAgent = "TestBrowser/1.0",
            Browser = "Chrome",
            OperatingSystem = "Windows",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            RemainingAttempts = 1 // Last attempt
        };

        var command = new VerifyRevokedDeviceLoginCommand(
            VerificationToken: verificationToken,
            Code: "999999", // Wrong code
            IpAddress: "192.168.1.100");

        var cacheKey = $"revoked_device_login:{verificationToken}";
        _cacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cacheData)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.RemainingAttempts.Should().Be(0);
    }

    [Fact]
    public async Task Handle_SuccessfulVerification_ShouldClearCacheEntry()
    {
        // Arrange
        var verificationToken = Guid.NewGuid().ToString();
        var verificationCode = "123456";
        var codeHash = HashCode(verificationCode);
        var userId = Guid.NewGuid().ToString();

        var cacheData = new
        {
            CodeHash = codeHash,
            UserId = userId,
            Email = "test@test.com",
            DeviceFingerprint = "device-fingerprint-123",
            IpAddress = "192.168.1.100",
            UserAgent = "TestBrowser/1.0",
            Browser = "Chrome",
            OperatingSystem = "Windows",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            RemainingAttempts = 3
        };

        var command = new VerifyRevokedDeviceLoginCommand(
            VerificationToken: verificationToken,
            Code: verificationCode,
            IpAddress: "192.168.1.100");

        var cacheKey = $"revoked_device_login:{verificationToken}";
        _cacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cacheData)));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _cacheMock.Verify(
            x => x.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #region Helper Methods

    private static string HashCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    #endregion
}
