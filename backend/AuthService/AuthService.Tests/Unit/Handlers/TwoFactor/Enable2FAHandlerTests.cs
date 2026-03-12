using FluentAssertions;
using Moq;
using Xunit;
using AuthService.Application.Features.TwoFactor.Commands.Enable2FA;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Shared.Exceptions;

namespace AuthService.Tests.Unit.Handlers.TwoFactor;

/// <summary>
/// Unit tests for Enable2FACommandHandler
/// Tests AUTH-2FA-001: Enable 2FA functionality
/// </summary>
public class Enable2FAHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITwoFactorService> _twoFactorServiceMock;
    private readonly Mock<IAuthNotificationService> _notificationServiceMock;
    private readonly Enable2FACommandHandler _handler;

    public Enable2FAHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _twoFactorServiceMock = new Mock<ITwoFactorService>();
        _notificationServiceMock = new Mock<IAuthNotificationService>();

        _handler = new Enable2FACommandHandler(
            _userRepositoryMock.Object,
            _twoFactorServiceMock.Object,
            _notificationServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithAuthenticator_ShouldGenerateQrCode()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var command = new Enable2FACommand(userId, TwoFactorAuthType.Authenticator);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _twoFactorServiceMock.Setup(x => x.GenerateAuthenticatorKeyAsync(user.Id, user.Email!))
            .ReturnsAsync(("SECRETKEY123", "otpauth://totp/OKLA:test@test.com?secret=SECRETKEY123"));
        _twoFactorServiceMock.Setup(x => x.GenerateRecoveryCodesAsync(user.Id))
            .ReturnsAsync(new List<string> { "CODE1", "CODE2", "CODE3" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Secret.Should().Be("SECRETKEY123");
        result.QrCodeUri.Should().Contain("otpauth://totp");
        result.RecoveryCodes.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WhenAlreadyEnabled_ShouldThrowBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUserWith2FAEnabled(userId);
        var command = new Enable2FACommand(userId, TwoFactorAuthType.Authenticator);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*already enabled*");
    }

    [Fact]
    public async Task Handle_WithSMS_RequiresVerifiedPhone()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId, phoneNumber: null);
        var command = new Enable2FACommand(userId, TwoFactorAuthType.SMS);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Phone number is required*");
    }

    [Fact]
    public async Task Handle_WithSMS_RequiresConfirmedPhone()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId, phoneNumber: "+18095551234", phoneConfirmed: false);
        var command = new Enable2FACommand(userId, TwoFactorAuthType.SMS);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*must be verified*");
    }

    [Fact]
    public async Task Handle_WithEmail_RequiresConfirmedEmail()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId, emailConfirmed: false);
        var command = new Enable2FACommand(userId, TwoFactorAuthType.Email);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Email must be verified*");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new Enable2FACommand(userId, TwoFactorAuthType.Authenticator);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*User not found*");
    }

    [Fact]
    public async Task Handle_ShouldReturnRecoveryCodesInResponse_NotSendEmail()
    {
        // Arrange
        // Recovery codes are returned in the response so the user can save them,
        // but they are NOT emailed yet — that only happens after Verify2FA confirms setup.
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var command = new Enable2FACommand(userId, TwoFactorAuthType.Authenticator);
        var recoveryCodes = new List<string> { "ABC123", "DEF456" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _twoFactorServiceMock.Setup(x => x.GenerateAuthenticatorKeyAsync(user.Id, user.Email!))
            .ReturnsAsync(("SECRET", "otpauth://..."));
        _twoFactorServiceMock.Setup(x => x.GenerateRecoveryCodesAsync(user.Id))
            .ReturnsAsync(recoveryCodes);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — codes are in the response but email is deferred to Verify2FA step
        result.RecoveryCodes.Should().BeEquivalentTo(recoveryCodes);
        _notificationServiceMock.Verify(
            x => x.SendTwoFactorBackupCodesAsync(It.IsAny<string>(), It.IsAny<List<string>>()),
            Times.Never);
    }

    #region Helper Methods

    /// <summary>
    /// Creates a test ApplicationUser without 2FA enabled
    /// </summary>
    private static ApplicationUser CreateTestUser(
        string userId,
        string? phoneNumber = null,
        bool phoneConfirmed = false,
        bool emailConfirmed = true)
    {
        var user = new ApplicationUser("testuser", "test@test.com", "hashedpassword123")
        {
            Id = userId,
            PhoneNumber = phoneNumber,
            PhoneNumberConfirmed = phoneConfirmed,
            EmailConfirmed = emailConfirmed
        };
        return user;
    }

    /// <summary>
    /// Creates a test ApplicationUser with 2FA already enabled
    /// IsTwoFactorEnabled is computed from TwoFactorAuth status
    /// </summary>
    private static ApplicationUser CreateTestUserWith2FAEnabled(string userId)
    {
        var user = CreateTestUser(userId);
        // Use reflection to set the private TwoFactorAuth property since IsTwoFactorEnabled is computed
        var twoFactorAuth = new TwoFactorAuth(userId, TwoFactorAuthType.Authenticator);
        twoFactorAuth.Enable("SECRET123", new List<string> { "CODE1", "CODE2" });

        var property = typeof(ApplicationUser).GetProperty("TwoFactorAuth");
        property?.SetValue(user, twoFactorAuth);

        return user;
    }

    #endregion
}
