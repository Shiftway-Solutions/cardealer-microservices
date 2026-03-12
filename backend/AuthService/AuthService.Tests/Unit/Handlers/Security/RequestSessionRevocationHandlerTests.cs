using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using AuthService.Application.Features.Auth.Commands.RequestSessionRevocation;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Shared.Exceptions;
using System.Text;

namespace AuthService.Tests.Unit.Handlers.Security;

/// <summary>
/// Unit tests for RequestSessionRevocationCommandHandler
/// Tests AUTH-SEC-003-A: Request session revocation code
/// </summary>
public class RequestSessionRevocationHandlerTests
{
    private readonly Mock<IUserSessionRepository> _sessionRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<RequestSessionRevocationCommandHandler>> _loggerMock;
    private readonly RequestSessionRevocationCommandHandler _handler;

    public RequestSessionRevocationHandlerTests()
    {
        _sessionRepositoryMock = new Mock<IUserSessionRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _cacheMock = new Mock<IDistributedCache>();
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<RequestSessionRevocationCommandHandler>>();

        _handler = new RequestSessionRevocationCommandHandler(
            _sessionRepositoryMock.Object,
            _userRepositoryMock.Object,
            _cacheMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidSession_ShouldSendRevocationCode()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var session = CreateTestSession(sessionId, userId);

        var command = new RequestSessionRevocationCommand(
            UserId: userId,
            SessionId: sessionId.ToString(),
            CurrentSessionId: currentSessionId,
            IpAddress: "192.168.1.100");

        _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.CodeExpiresAt.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId.ToString());
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid();
        var command = new RequestSessionRevocationCommand(
            UserId: userId,
            SessionId: sessionId.ToString());

        _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSession?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Session not found*");
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToOtherUser_ShouldThrowNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, otherUserId);

        var command = new RequestSessionRevocationCommand(
            UserId: userId,
            SessionId: sessionId.ToString());

        _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Session not found*");
    }

    [Fact]
    public async Task Handle_WhenAttemptingToRevokeCurrentSession_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, userId);

        var command = new RequestSessionRevocationCommand(
            UserId: userId,
            SessionId: sessionId.ToString(),
            CurrentSessionId: sessionId.ToString());

        _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("current session");
    }

    [Fact]
    public async Task Handle_WhenRateLimitExceeded_ShouldThrowBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid();

        var command = new RequestSessionRevocationCommand(
            UserId: userId,
            SessionId: sessionId.ToString());

        var rateLimitValue = Encoding.UTF8.GetBytes("3");
        _cacheMock.Setup(x => x.GetAsync(It.Is<string>(s => s.Contains("session_revoke_rate")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rateLimitValue);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Too many requests*");
    }

    [Fact]
    public async Task Handle_WithInvalidSessionIdFormat_ShouldThrowBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new RequestSessionRevocationCommand(
            UserId: userId,
            SessionId: "invalid-guid-format");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Invalid session ID format*");
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
