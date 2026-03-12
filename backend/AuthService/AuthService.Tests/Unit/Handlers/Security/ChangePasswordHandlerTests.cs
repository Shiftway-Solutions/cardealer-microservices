using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using AuthService.Application.Features.Auth.Commands.ChangePassword;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Shared.Exceptions;

namespace AuthService.Tests.Unit.Handlers.Security;

/// <summary>
/// Unit tests for ChangePasswordCommandHandler
/// Tests AUTH-SEC-001: Change Password functionality
/// </summary>
public class ChangePasswordHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IUserSessionRepository> _sessionRepositoryMock;
    private readonly Mock<IAuthNotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<ChangePasswordCommandHandler>> _loggerMock;
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _sessionRepositoryMock = new Mock<IUserSessionRepository>();
        _notificationServiceMock = new Mock<IAuthNotificationService>();
        _loggerMock = new Mock<ILogger<ChangePasswordCommandHandler>>();

        _handler = new ChangePasswordCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _refreshTokenRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldChangePasswordSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var command = new ChangePasswordCommand(
            UserId: userId,
            CurrentPassword: "OldPassword123!",
            NewPassword: "NewPassword456!",
            ConfirmPassword: "NewPassword456!",
            IpAddress: "192.168.1.100",
            UserAgent: "TestBrowser/1.0");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.VerifyPasswordAsync(user, "OldPassword123!"))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(x => x.VerifyPasswordAsync(user, "NewPassword456!"))
            .ReturnsAsync(false); // New password is different

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successfully");
    }

    [Fact]
    public async Task Handle_WithIncorrectCurrentPassword_ShouldThrowBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var command = new ChangePasswordCommand(
            UserId: userId,
            CurrentPassword: "WrongPassword!",
            NewPassword: "NewPassword456!",
            ConfirmPassword: "NewPassword456!");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.VerifyPasswordAsync(user, "WrongPassword!"))
            .ReturnsAsync(false);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Current password is incorrect*");
    }

    [Fact]
    public async Task Handle_WithSamePassword_ShouldThrowBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var command = new ChangePasswordCommand(
            UserId: userId,
            CurrentPassword: "SamePassword123!",
            NewPassword: "SamePassword123!",
            ConfirmPassword: "SamePassword123!");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.VerifyPasswordAsync(user, It.IsAny<string>()))
            .ReturnsAsync(true); // Both passwords match (same password)

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*different*");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new ChangePasswordCommand(
            UserId: userId,
            CurrentPassword: "OldPassword123!",
            NewPassword: "NewPassword456!",
            ConfirmPassword: "NewPassword456!");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*User not found*");
    }

    [Fact]
    public async Task Handle_WhenAccountLocked_ShouldThrowBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateLockedUser(userId);
        var command = new ChangePasswordCommand(
            UserId: userId,
            CurrentPassword: "OldPassword123!",
            NewPassword: "NewPassword456!",
            ConfirmPassword: "NewPassword456!");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*locked*");
    }

    [Fact]
    public async Task Handle_ShouldRevokeAllSessionsAfterPasswordChange()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var command = new ChangePasswordCommand(
            UserId: userId,
            CurrentPassword: "OldPassword123!",
            NewPassword: "NewPassword456!",
            ConfirmPassword: "NewPassword456!");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.VerifyPasswordAsync(user, "OldPassword123!"))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(x => x.VerifyPasswordAsync(user, "NewPassword456!"))
            .ReturnsAsync(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Use Guid? instead of string for exceptSessionId
        _sessionRepositoryMock.Verify(
            x => x.RevokeAllUserSessionsAsync(
                userId,
                It.IsAny<Guid?>(),
                "password_changed",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRevokeAllRefreshTokensAfterPasswordChange()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var command = new ChangePasswordCommand(
            UserId: userId,
            CurrentPassword: "OldPassword123!",
            NewPassword: "NewPassword456!",
            ConfirmPassword: "NewPassword456!");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.VerifyPasswordAsync(user, "OldPassword123!"))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(x => x.VerifyPasswordAsync(user, "NewPassword456!"))
            .ReturnsAsync(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenRepositoryMock.Verify(
            x => x.RevokeAllForUserAsync(
                userId,
                "password_changed",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSendEmailNotificationAfterPasswordChange()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var command = new ChangePasswordCommand(
            UserId: userId,
            CurrentPassword: "OldPassword123!",
            NewPassword: "NewPassword456!",
            ConfirmPassword: "NewPassword456!");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.VerifyPasswordAsync(user, "OldPassword123!"))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(x => x.VerifyPasswordAsync(user, "NewPassword456!"))
            .ReturnsAsync(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.SendPasswordChangedConfirmationAsync(user.Email!),
            Times.Once);
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

    private static ApplicationUser CreateLockedUser(string userId)
    {
        var user = CreateTestUser(userId);
        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30);
        user.LockoutEnabled = true;
        return user;
    }

    #endregion
}
