using FluentAssertions;
using Moq;
using Xunit;
using AuthService.Application.Features.TwoFactor.Commands.Disable2FA;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Shared.Exceptions;

namespace AuthService.Tests.Unit.Handlers.TwoFactor;

/// <summary>
/// Unit tests for Disable2FACommandHandler
/// Tests AUTH-2FA-003: Disable 2FA functionality
/// </summary>
public class Disable2FAHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITwoFactorService> _twoFactorServiceMock;
    private readonly Disable2FACommandHandler _handler;

    public Disable2FAHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _twoFactorServiceMock = new Mock<ITwoFactorService>();

        _handler = new Disable2FACommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _twoFactorServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidPassword_ShouldDisable2FA()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateUserWithTwoFactor(userId, "hashedPassword");
        var command = new Disable2FACommand(userId, "correctPassword");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify("correctPassword", "hashedPassword"))
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("disabled successfully");
        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ShouldThrowUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateUserWithTwoFactor(userId, "hashedPassword");
        var command = new Disable2FACommand(userId, "wrongPassword");

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
        var command = new Disable2FACommand(userId, "password");

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
        var command = new Disable2FACommand(userId, "password");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithNullPasswordHash_ShouldThrowUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateUserWithTwoFactor(userId, null!); // OAuth user without password
        var command = new Disable2FACommand(userId, "password");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    #region Helper Methods

    private static ApplicationUser CreateTestUser(string userId, string? passwordHash)
    {
        var user = new ApplicationUser("testuser", "test@test.com", passwordHash ?? "dummyhash")
        {
            Id = userId,
            EmailConfirmed = true
        };

        // If passwordHash is null, use reflection to set it
        if (passwordHash == null)
        {
            typeof(ApplicationUser).GetProperty("PasswordHash")?.SetValue(user, null);
        }

        return user;
    }

    private static ApplicationUser CreateUserWithTwoFactor(string userId, string? passwordHash)
    {
        var user = CreateTestUser(userId, passwordHash);

        // Create and enable TwoFactorAuth
        var twoFactorAuth = new TwoFactorAuth(userId, TwoFactorAuthType.Authenticator);
        twoFactorAuth.Enable("SECRET", new List<string> { "RECOVERY1" });

        // Use reflection to set the private TwoFactorAuth property
        var property = typeof(ApplicationUser).GetProperty("TwoFactorAuth");
        property?.SetValue(user, twoFactorAuth);

        return user;
    }

    #endregion
}
