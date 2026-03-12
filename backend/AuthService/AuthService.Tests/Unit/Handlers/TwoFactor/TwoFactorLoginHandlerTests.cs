using FluentAssertions;
using Moq;
using Xunit;
using AuthService.Application.Features.TwoFactor.Commands.TwoFactorLogin;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Shared.Exceptions;

namespace AuthService.Tests.Unit.Handlers.TwoFactor;

/// <summary>
/// Unit tests for TwoFactorLoginCommandHandler
/// Tests AUTH-2FA-005: 2FA Login verification functionality
/// </summary>
public class TwoFactorLoginHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IJwtGenerator> _jwtGeneratorMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<ITwoFactorService> _twoFactorServiceMock;
    private readonly TwoFactorLoginCommandHandler _handler;

    public TwoFactorLoginHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _jwtGeneratorMock = new Mock<IJwtGenerator>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _twoFactorServiceMock = new Mock<ITwoFactorService>();

        _handler = new TwoFactorLoginCommandHandler(
            _userRepositoryMock.Object,
            _jwtGeneratorMock.Object,
            _refreshTokenRepositoryMock.Object,
            _twoFactorServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCode_ShouldReturnTokens()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateUserWithTwoFactor(userId);
        var twoFactorAuth = CreateTwoFactorAuth(userId);
        var command = new TwoFactorLoginCommand("valid_temp_token", "123456");

        _jwtGeneratorMock.Setup(x => x.ValidateTempToken("valid_temp_token"))
            .Returns((userId, "test@test.com"));
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetTwoFactorAuthAsync(userId))
            .ReturnsAsync(twoFactorAuth);
        _twoFactorServiceMock.Setup(x => x.VerifyAuthenticatorCode(twoFactorAuth.Secret, "123456"))
            .Returns(true);
        _jwtGeneratorMock.Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
            .Returns("access_token_123");
        _jwtGeneratorMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token_123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token_123");
        result.RefreshToken.Should().Be("refresh_token_123");
    }

    [Fact]
    public async Task Handle_WithInvalidTempToken_ShouldThrowUnauthorized()
    {
        // Arrange
        var command = new TwoFactorLoginCommand("invalid_token", "123456");

        _jwtGeneratorMock.Setup(x => x.ValidateTempToken("invalid_token"))
            .Returns((ValueTuple<string, string>?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid or expired*");
    }

    [Fact]
    public async Task Handle_WithInvalidCode_ShouldThrowUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateUserWithTwoFactor(userId);
        var twoFactorAuth = CreateTwoFactorAuth(userId);
        var command = new TwoFactorLoginCommand("valid_temp_token", "000000");

        _jwtGeneratorMock.Setup(x => x.ValidateTempToken("valid_temp_token"))
            .Returns((userId, "test@test.com"));
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetTwoFactorAuthAsync(userId))
            .ReturnsAsync(twoFactorAuth);
        _twoFactorServiceMock.Setup(x => x.VerifyAuthenticatorCode(twoFactorAuth.Secret, "000000"))
            .Returns(false);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid*code*");
    }

    [Fact]
    public async Task Handle_When2FANotEnabled_ShouldThrowBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId); // No 2FA
        var command = new TwoFactorLoginCommand("valid_temp_token", "123456");

        _jwtGeneratorMock.Setup(x => x.ValidateTempToken("valid_temp_token"))
            .Returns((userId, "test@test.com"));
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

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
        var command = new TwoFactorLoginCommand("valid_temp_token", "123456");

        _jwtGeneratorMock.Setup(x => x.ValidateTempToken("valid_temp_token"))
            .Returns((userId, "test@test.com"));
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldSaveRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateUserWithTwoFactor(userId);
        var twoFactorAuth = CreateTwoFactorAuth(userId);
        var command = new TwoFactorLoginCommand("valid_temp_token", "123456");

        _jwtGeneratorMock.Setup(x => x.ValidateTempToken("valid_temp_token"))
            .Returns((userId, "test@test.com"));
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetTwoFactorAuthAsync(userId))
            .ReturnsAsync(twoFactorAuth);
        _twoFactorServiceMock.Setup(x => x.VerifyAuthenticatorCode(twoFactorAuth.Secret, "123456"))
            .Returns(true);
        _jwtGeneratorMock.Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
            .Returns("access_token");
        _jwtGeneratorMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
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

    private static ApplicationUser CreateUserWithTwoFactor(string userId)
    {
        var user = CreateTestUser(userId);

        // Create and enable TwoFactorAuth
        var twoFactorAuth = CreateTwoFactorAuth(userId);

        // Use reflection to set the private TwoFactorAuth property
        var property = typeof(ApplicationUser).GetProperty("TwoFactorAuth");
        property?.SetValue(user, twoFactorAuth);

        return user;
    }

    private static TwoFactorAuth CreateTwoFactorAuth(string userId)
    {
        var twoFactorAuth = new TwoFactorAuth(userId, TwoFactorAuthType.Authenticator);
        twoFactorAuth.Enable("TESTSECRET123", new List<string> { "RECOVERY1" });
        return twoFactorAuth;
    }

    #endregion
}
