using FluentAssertions;
using Moq;
using Xunit;
using AuthService.Application.Features.TwoFactor.Commands.GenerateRecoveryCodes;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Shared.Exceptions;

namespace AuthService.Tests.Unit.Handlers.TwoFactor;

/// <summary>
/// Unit tests for GenerateRecoveryCodesCommandHandler
/// Tests AUTH-2FA-004: Generate new recovery codes functionality
/// </summary>
public class GenerateRecoveryCodesHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITwoFactorService> _twoFactorServiceMock;
    private readonly Mock<IAuthNotificationService> _notificationServiceMock;
    private readonly GenerateRecoveryCodesCommandHandler _handler;

    public GenerateRecoveryCodesHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _twoFactorServiceMock = new Mock<ITwoFactorService>();
        _notificationServiceMock = new Mock<IAuthNotificationService>();

        _handler = new GenerateRecoveryCodesCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _twoFactorServiceMock.Object,
            _notificationServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidPassword_ShouldGenerateNewCodes()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateUserWithTwoFactor(userId, "hashedPassword");
        var command = new GenerateRecoveryCodesCommand(userId, "correctPassword");
        var expectedCodes = new List<string> { "ABC123", "DEF456", "GHI789" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify("correctPassword", "hashedPassword"))
            .Returns(true);
        _twoFactorServiceMock.Setup(x => x.GenerateRecoveryCodesAsync(userId))
            .ReturnsAsync(expectedCodes);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RecoveryCodes.Should().BeEquivalentTo(expectedCodes);
    }

    [Fact]
    public async Task Handle_ShouldSendCodesByEmail()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateUserWithTwoFactor(userId, "hashedPassword");
        var command = new GenerateRecoveryCodesCommand(userId, "correctPassword");
        var expectedCodes = new List<string> { "ABC123", "DEF456" };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify("correctPassword", "hashedPassword"))
            .Returns(true);
        _twoFactorServiceMock.Setup(x => x.GenerateRecoveryCodesAsync(userId))
            .ReturnsAsync(expectedCodes);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            x => x.SendTwoFactorBackupCodesAsync(user.Email!, expectedCodes),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ShouldThrowUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateUserWithTwoFactor(userId, "hashedPassword");
        var command = new GenerateRecoveryCodesCommand(userId, "wrongPassword");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify("wrongPassword", "hashedPassword"))
            .Returns(false);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid password*");
    }

    [Fact]
    public async Task Handle_When2FANotEnabled_ShouldThrowBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId, "hashedPassword"); // No 2FA
        var command = new GenerateRecoveryCodesCommand(userId, "password");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify("password", "hashedPassword"))
            .Returns(true);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*not enabled*");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new GenerateRecoveryCodesCommand(userId, "password");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #region Helper Methods

    private static ApplicationUser CreateTestUser(string userId, string passwordHash)
    {
        return new ApplicationUser("testuser", "test@test.com", passwordHash)
        {
            Id = userId,
            EmailConfirmed = true
        };
    }

    private static ApplicationUser CreateUserWithTwoFactor(string userId, string passwordHash)
    {
        var user = CreateTestUser(userId, passwordHash);

        // Create and enable TwoFactorAuth
        var twoFactorAuth = new TwoFactorAuth(userId, TwoFactorAuthType.Authenticator);
        twoFactorAuth.Enable("SECRET", new List<string> { "OLD_RECOVERY" });

        // Use reflection to set the private TwoFactorAuth property
        var property = typeof(ApplicationUser).GetProperty("TwoFactorAuth");
        property?.SetValue(user, twoFactorAuth);

        return user;
    }

    #endregion
}
