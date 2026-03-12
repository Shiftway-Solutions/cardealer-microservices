using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using AuthService.Application.Features.Auth.Commands.RevokeAllSessions;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Shared.Exceptions;
using AuthService.Application.DTOs;

namespace AuthService.Tests.Unit.Handlers.Security;

/// <summary>
/// Unit tests for RevokeAllSessionsCommandHandler
/// Tests AUTH-SEC-004: Revoke all sessions (logout everywhere)
/// </summary>
public class RevokeAllSessionsHandlerTests
{
    private readonly Mock<IUserSessionRepository> _sessionRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAuthNotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<RevokeAllSessionsCommandHandler>> _loggerMock;
    private readonly RevokeAllSessionsCommandHandler _handler;

    public RevokeAllSessionsHandlerTests()
    {
        _sessionRepositoryMock = new Mock<IUserSessionRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _notificationServiceMock = new Mock<IAuthNotificationService>();
        _loggerMock = new Mock<ILogger<RevokeAllSessionsCommandHandler>>();

        _handler = new RevokeAllSessionsCommandHandler(
            _sessionRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _userRepositoryMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldRevokeAllSessionsSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var sessions = CreateTestSessions(userId, 5);
        var command = new RevokeAllSessionsCommand(
            UserId: userId,
            KeepCurrentSession: false,
            IpAddress: "192.168.1.100");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _sessionRepositoryMock.Setup(x => x.GetActiveSessionsByUserIdAsync(
                userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SessionsRevoked.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithKeepCurrentSession_ShouldExcludeCurrentSession()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var currentSessionId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        var sessions = CreateTestSessionsWithId(userId, currentSessionId, 5);
        var command = new RevokeAllSessionsCommand(
            UserId: userId,
            CurrentSessionId: currentSessionId.ToString(),
            KeepCurrentSession: true,
            IpAddress: "192.168.1.100");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _sessionRepositoryMock.Setup(x => x.GetActiveSessionsByUserIdAsync(
                userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.SessionsRevoked.Should().Be(4); // 5 - 1 (current)
        result.CurrentSessionKept.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRevokeRefreshTokensWhenEnabled()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var sessions = CreateTestSessionsWithRefreshTokens(userId, 3);
        var command = new RevokeAllSessionsCommand(
            UserId: userId,
            KeepCurrentSession: false,
            RevokeRefreshTokens: true,
            IpAddress: "192.168.1.100");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _sessionRepositoryMock.Setup(x => x.GetActiveSessionsByUserIdAsync(
                userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);
        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRefreshToken(userId));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        // Verify refresh tokens were fetched for revocation
        _refreshTokenRepositoryMock.Verify(
            x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldStillSucceedButSkipEmail()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new RevokeAllSessionsCommand(
            UserId: userId,
            KeepCurrentSession: false);

        // No user found - but handler doesn't validate user, only uses for email at the end
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        _sessionRepositoryMock.Setup(x => x.GetActiveSessionsByUserIdAsync(
                userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSession>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Handler succeeds but skips email notification
        result.Success.Should().BeTrue();
        _notificationServiceMock.Verify(
            x => x.SendSecurityAlertAsync(It.IsAny<string>(), It.IsAny<SecurityAlertDto>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSendSecurityAlertNotification()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var sessions = CreateTestSessions(userId, 3);
        var command = new RevokeAllSessionsCommand(
            UserId: userId,
            KeepCurrentSession: false,
            IpAddress: "192.168.1.100");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _sessionRepositoryMock.Setup(x => x.GetActiveSessionsByUserIdAsync(
                userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.SendSecurityAlertAsync(
                user.Email!,
                It.Is<SecurityAlertDto>(a => a.AlertType == "all_sessions_revoked")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoSessionsToRevoke_ShouldReturnZeroCount()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var command = new RevokeAllSessionsCommand(
            UserId: userId,
            KeepCurrentSession: false,
            IpAddress: "192.168.1.100");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _sessionRepositoryMock.Setup(x => x.GetActiveSessionsByUserIdAsync(
                userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSession>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.SessionsRevoked.Should().Be(0);
    }

    #region Helper Methods

    private static ApplicationUser CreateTestUser(string userId)
    {
        return new ApplicationUser("testuser", "test@test.com", "hashedpassword123")
        {
            Id = userId,
            EmailConfirmed = true
        };
    }

    private static List<UserSession> CreateTestSessions(string userId, int count)
    {
        var sessions = new List<UserSession>();
        for (int i = 0; i < count; i++)
        {
            sessions.Add(new UserSession(
                userId: userId,
                refreshTokenId: Guid.NewGuid().ToString(),
                deviceInfo: $"Chrome on Windows {i}",
                browser: "Chrome",
                operatingSystem: "Windows",
                ipAddress: $"192.168.1.{100 + i}",
                expiresAt: DateTime.UtcNow.AddDays(7)));
        }
        return sessions;
    }

    private static List<UserSession> CreateTestSessionsWithId(string userId, Guid currentSessionId, int count)
    {
        var sessions = new List<UserSession>();
        // First session is the current one
        var currentSession = new UserSession(
            userId: userId,
            refreshTokenId: Guid.NewGuid().ToString(),
            deviceInfo: "Current Session Chrome",
            browser: "Chrome",
            operatingSystem: "Windows",
            ipAddress: "192.168.1.1",
            expiresAt: DateTime.UtcNow.AddDays(7));
        // Use reflection to set Id since it's read-only
        typeof(UserSession).GetProperty("Id")!.SetValue(currentSession, currentSessionId);
        sessions.Add(currentSession);

        // Add remaining sessions
        for (int i = 1; i < count; i++)
        {
            sessions.Add(new UserSession(
                userId: userId,
                refreshTokenId: Guid.NewGuid().ToString(),
                deviceInfo: $"Chrome on Windows {i}",
                browser: "Chrome",
                operatingSystem: "Windows",
                ipAddress: $"192.168.1.{100 + i}",
                expiresAt: DateTime.UtcNow.AddDays(7)));
        }
        return sessions;
    }

    private static List<UserSession> CreateTestSessionsWithRefreshTokens(string userId, int count)
    {
        var sessions = new List<UserSession>();
        for (int i = 0; i < count; i++)
        {
            sessions.Add(new UserSession(
                userId: userId,
                refreshTokenId: $"refresh-token-{i}",
                deviceInfo: $"Chrome on Windows {i}",
                browser: "Chrome",
                operatingSystem: "Windows",
                ipAddress: $"192.168.1.{100 + i}",
                expiresAt: DateTime.UtcNow.AddDays(7)));
        }
        return sessions;
    }

    private static RefreshToken CreateTestRefreshToken(string userId)
    {
        return new RefreshToken(
            userId: userId,
            token: Guid.NewGuid().ToString(),
            expiresAt: DateTime.UtcNow.AddDays(30),
            createdByIp: "192.168.1.100");
    }

    #endregion
}
