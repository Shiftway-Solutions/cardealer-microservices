using FluentAssertions;
using Moq;
using Xunit;
using AuthService.Application.Features.TwoFactor.Commands.Verify2FA;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Shared.Exceptions;

namespace AuthService.Tests.Unit.Handlers.TwoFactor;

/// <summary>
/// Unit tests for Verify2FACommandHandler
/// Tests AUTH-2FA-002: Verify 2FA code functionality
/// </summary>
public class Verify2FAHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITwoFactorService> _twoFactorServiceMock;
    private readonly Mock<IAuthNotificationService> _notificationServiceMock;
    private readonly Verify2FACommandHandler _handler;

    public Verify2FAHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _twoFactorServiceMock = new Mock<ITwoFactorService>();
        _notificationServiceMock = new Mock<IAuthNotificationService>();
        
        _handler = new Verify2FACommandHandler(
            _userRepositoryMock.Object,
            _twoFactorServiceMock.Object,
            _notificationServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidAuthenticatorCode_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateUserWithTwoFactor(userId, TwoFactorAuthType.Authenticator);
        var command = new Verify2FACommand(userId, "123456", TwoFactorAuthType.Authenticator);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _twoFactorServiceMock.Setup(x => x.VerifyAuthenticatorCode(It.IsAny<string>(), "123456"))
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("verified").And.Contain("successfully");
    }

    [Fact]
    public async Task Handle_WithInvalidCode_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateUserWithTwoFactor(userId, TwoFactorAuthType.Authenticator);
        var command = new Verify2FACommand(userId, "000000", TwoFactorAuthType.Authenticator);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _twoFactorServiceMock.Setup(x => x.VerifyAuthenticatorCode(It.IsAny<string>(), "000000"))
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid");
    }

    [Fact]
    public async Task Handle_WithoutTwoFactorSetup_ShouldThrowBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId); // No 2FA
        var command = new Verify2FACommand(userId, "123456", TwoFactorAuthType.Authenticator);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*not set up*");
    }

    [Fact]
    public async Task Handle_WithSMSCode_ShouldCallVerifyCodeAsync()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateUserWithTwoFactor(userId, TwoFactorAuthType.SMS);
        var command = new Verify2FACommand(userId, "123456", TwoFactorAuthType.SMS);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _twoFactorServiceMock.Setup(x => x.VerifyCodeAsync(userId, "123456", TwoFactorAuthType.SMS))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _twoFactorServiceMock.Verify(
            x => x.VerifyCodeAsync(userId, "123456", TwoFactorAuthType.SMS),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmailCode_ShouldCallVerifyCodeAsync()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateUserWithTwoFactor(userId, TwoFactorAuthType.Email);
        var command = new Verify2FACommand(userId, "123456", TwoFactorAuthType.Email);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _twoFactorServiceMock.Setup(x => x.VerifyCodeAsync(userId, "123456", TwoFactorAuthType.Email))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _twoFactorServiceMock.Verify(
            x => x.VerifyCodeAsync(userId, "123456", TwoFactorAuthType.Email),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new Verify2FACommand(userId, "123456", TwoFactorAuthType.Authenticator);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
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

    private static ApplicationUser CreateUserWithTwoFactor(string userId, TwoFactorAuthType type)
    {
        var user = CreateTestUser(userId);
        
        // Create and enable TwoFactorAuth
        var twoFactorAuth = new TwoFactorAuth(userId, type);
        twoFactorAuth.Enable("TESTSECRET123", new List<string> { "RECOVERY1", "RECOVERY2" });
        
        // Use reflection to set the private TwoFactorAuth property
        var property = typeof(ApplicationUser).GetProperty("TwoFactorAuth");
        property?.SetValue(user, twoFactorAuth);
        
        return user;
    }

    #endregion
}
