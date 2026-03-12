using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using AuthService.Application.Features.Auth.Commands.RevokeSession;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Application.Services;
using AuthService.Shared.Exceptions;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;

namespace AuthService.Tests.Unit.Handlers.Security;

/// <summary>
/// Unit tests for RevokeSessionCommandHandler
/// Tests AUTH-SEC-003: Revoke session with verification code
/// </summary>
public class RevokeSessionHandlerTests
{
    private readonly Mock<IUserSessionRepository> _sessionRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IRevokedDeviceService> _revokedDeviceServiceMock;
    private readonly Mock<ILogger<RevokeSessionCommandHandler>> _loggerMock;
    private readonly RevokeSessionCommandHandler _handler;

    public RevokeSessionHandlerTests()
    {
        _sessionRepositoryMock = new Mock<IUserSessionRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _cacheMock = new Mock<IDistributedCache>();
        _notificationServiceMock = new Mock<INotificationService>();
        _revokedDeviceServiceMock = new Mock<IRevokedDeviceService>();
        _loggerMock = new Mock<ILogger<RevokeSessionCommandHandler>>();

        _handler = new RevokeSessionCommandHandler(
            _sessionRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _userRepositoryMock.Object,
            _cacheMock.Object,
            _notificationServiceMock.Object,
            _revokedDeviceServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCode_ShouldRevokeSessionSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid().ToString();
        var session = CreateTestSession(sessionId, userId);
        var user = CreateTestUser(userId);
        var verificationCode = "123456";
        var codeHash = HashCode(verificationCode);

        var cacheData = new
        {
            CodeHash = codeHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            RemainingAttempts = 3,
            SessionId = sessionId.ToString()
        };

        var command = new RevokeSessionCommand(
            UserId: userId,
            SessionId: sessionId.ToString(),
            VerificationCode: verificationCode,
            CurrentSessionId: currentSessionId);

        _cacheMock.Setup(x => x.GetAsync(It.Is<string>(s => s.Contains("session_revoke_lockout")), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _cacheMock.Setup(x => x.GetAsync(It.Is<string>(s => s.Contains("session_revoke_code")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cacheData)));
        _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenAttemptingToRevokeCurrentSession_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid();

        var command = new RevokeSessionCommand(
            UserId: userId,
            SessionId: sessionId.ToString(),
            VerificationCode: "123456",
            CurrentSessionId: sessionId.ToString());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("current session");
    }

    [Fact]
    public async Task Handle_WithExpiredCode_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid().ToString();
        var verificationCode = "123456";
        var codeHash = HashCode(verificationCode);

        var cacheData = new
        {
            CodeHash = codeHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            RemainingAttempts = 3,
            SessionId = sessionId.ToString()
        };

        var command = new RevokeSessionCommand(
            UserId: userId,
            SessionId: sessionId.ToString(),
            VerificationCode: verificationCode,
            CurrentSessionId: currentSessionId);

        _cacheMock.Setup(x => x.GetAsync(It.Is<string>(s => s.Contains("session_revoke_lockout")), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _cacheMock.Setup(x => x.GetAsync(It.Is<string>(s => s.Contains("session_revoke_code")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cacheData)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task Handle_WithInvalidCode_ShouldDecrementAttempts()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid().ToString();
        var correctCodeHash = HashCode("123456");

        var cacheData = new
        {
            CodeHash = correctCodeHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            RemainingAttempts = 3,
            SessionId = sessionId.ToString()
        };

        var command = new RevokeSessionCommand(
            UserId: userId,
            SessionId: sessionId.ToString(),
            VerificationCode: "999999",
            CurrentSessionId: currentSessionId);

        _cacheMock.Setup(x => x.GetAsync(It.Is<string>(s => s.Contains("session_revoke_lockout")), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _cacheMock.Setup(x => x.GetAsync(It.Is<string>(s => s.Contains("session_revoke_code")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cacheData)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.RemainingAttempts.Should().Be(2);
    }

    [Fact]
    public async Task Handle_InvalidSessionIdFormat_ShouldThrowBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        var command = new RevokeSessionCommand(
            UserId: userId,
            SessionId: "not-a-valid-guid-format",
            VerificationCode: "123456",
            CurrentSessionId: Guid.NewGuid().ToString());

        // No lockout setup needed - invalid format is checked before code verification
        _cacheMock.Setup(x => x.GetAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Invalid session ID format*");
    }

    [Fact]
    public async Task Handle_WhenNoCodeRequested_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid();

        var command = new RevokeSessionCommand(
            UserId: userId,
            SessionId: sessionId.ToString(),
            VerificationCode: "123456",
            CurrentSessionId: Guid.NewGuid().ToString());

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expired or not requested");
    }

    [Fact]
    public async Task Handle_WithInvalidSessionIdFormat_ShouldThrowBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new RevokeSessionCommand(
            UserId: userId,
            SessionId: "invalid-guid",
            VerificationCode: "123456",
            CurrentSessionId: Guid.NewGuid().ToString());

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Invalid session ID format*");
    }

    #region Helper Methods

    private static string HashCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static ApplicationUser CreateTestUser(string userId)
    {
        return new ApplicationUser("testuser", "test@test.com", "hashedpassword123")
        {
            Id = userId,
            EmailConfirmed = true
        };
    }

    private static UserSession CreateTestSession(Guid sessionId, string userId)
    {
        return new UserSession(
            userId: userId,
            refreshTokenId: Guid.NewGuid().ToString(),
            deviceInfo: "Chrome on Windows",
            browser: "Chrome",
            operatingSystem: "Windows",
            ipAddress: "192.168.1.100",
            expiresAt: DateTime.UtcNow.AddDays(7));
    }

    #endregion
}
