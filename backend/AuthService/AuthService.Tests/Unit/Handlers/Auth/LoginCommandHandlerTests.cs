using Xunit;
using Moq;
using FluentAssertions;
using AuthService.Application.Features.Auth.Commands.Login;
using AuthService.Application.DTOs.Auth;
using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Application.Services;
using AuthService.Shared.Exceptions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AuthService.Tests.Unit.Handlers.Auth;

/// <summary>
/// Sprint 5 – Unit tests for LoginCommandHandler.
/// Covers: successful login, invalid credentials, unverified email, locked account,
/// CAPTCHA flow, 2FA flow, revoked device detection, session management, and events.
/// </summary>
public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtGenerator> _jwtGeneratorMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IUserSessionRepository> _sessionRepositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<IRequestContext> _requestContextMock;
    private readonly Mock<ITwoFactorService> _twoFactorServiceMock;
    private readonly Mock<ICaptchaService> _captchaServiceMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<IAuthNotificationService> _notificationServiceMock;
    private readonly Mock<IRevokedDeviceService> _revokedDeviceServiceMock;
    private readonly Mock<IGeoLocationService> _geoLocationServiceMock;
    private readonly Mock<ISecurityConfigProvider> _securityConfigMock;
    private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock;

    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtGeneratorMock = new Mock<IJwtGenerator>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _sessionRepositoryMock = new Mock<IUserSessionRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _requestContextMock = new Mock<IRequestContext>();
        _twoFactorServiceMock = new Mock<ITwoFactorService>();
        _captchaServiceMock = new Mock<ICaptchaService>();
        _cacheMock = new Mock<IDistributedCache>();
        _notificationServiceMock = new Mock<IAuthNotificationService>();
        _revokedDeviceServiceMock = new Mock<IRevokedDeviceService>();
        _geoLocationServiceMock = new Mock<IGeoLocationService>();
        _securityConfigMock = new Mock<ISecurityConfigProvider>();
        _loggerMock = new Mock<ILogger<LoginCommandHandler>>();

        // Default request context
        _requestContextMock.Setup(x => x.IpAddress).Returns("127.0.0.1");
        _requestContextMock.Setup(x => x.UserAgent).Returns("Mozilla/5.0 (Windows NT 10.0) Chrome/120.0");

        // Default security config
        _securityConfigMock.Setup(x => x.GetJwtExpiresMinutesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(60);
        _securityConfigMock.Setup(x => x.GetRefreshTokenDaysAsync(It.IsAny<CancellationToken>())).ReturnsAsync(7);
        _securityConfigMock.Setup(x => x.GetLockoutDurationMinutesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(15);
        _securityConfigMock.Setup(x => x.GetMaxLoginAttemptsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);

        // Default captcha (not required)
        _captchaServiceMock.Setup(x => x.IsCaptchaRequired(It.IsAny<int>())).Returns(false);

        // Default revoked device check (not revoked)
        _revokedDeviceServiceMock
            .Setup(x => x.CheckIfDeviceIsRevokedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevokedDeviceCheckResult { IsRevoked = false });

        // Default cache returns null (no failed attempts)
        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtGeneratorMock.Object,
            _refreshTokenRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _eventPublisherMock.Object,
            _requestContextMock.Object,
            _twoFactorServiceMock.Object,
            _captchaServiceMock.Object,
            _cacheMock.Object,
            _notificationServiceMock.Object,
            _revokedDeviceServiceMock.Object,
            _geoLocationServiceMock.Object,
            _securityConfigMock.Object,
            _loggerMock.Object
        );
    }

    #region Helpers

    private static readonly string TestUserId = Guid.NewGuid().ToString();

    private static ApplicationUser CreateTestUser(
        string? id = null,
        string username = "testuser",
        string email = "test@example.com",
        string passwordHash = "hashed-password",
        bool emailConfirmed = true,
        bool isTwoFactorEnabled = false)
    {
        var user = new ApplicationUser(username, email, passwordHash);
        // Use reflection to set Id since IdentityUser.Id has no public setter
        typeof(Microsoft.AspNetCore.Identity.IdentityUser)
            .GetProperty("Id")!
            .SetValue(user, id ?? TestUserId);
        user.EmailConfirmed = emailConfirmed;
        return user;
    }

    private static LoginCommand CreateValidCommand(string email = "test@example.com", string password = "Password123!")
    {
        return new LoginCommand(email, password);
    }

    private void SetupSuccessfulLogin(ApplicationUser user)
    {
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(user.Email!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), user.PasswordHash!))
            .Returns(true);

        _jwtGeneratorMock
            .Setup(x => x.GenerateToken(user, It.IsAny<int?>(), It.IsAny<string?>()))
            .Returns("access-token-123");

        _jwtGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token-123");

        _sessionRepositoryMock
            .Setup(x => x.GetActiveSessionByDeviceAsync(user.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSession?)null);
    }

    #endregion

    #region Successful Login

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsLoginResponse()
    {
        // Arrange
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(TestUserId);
        result.Email.Should().Be("test@example.com");
        result.AccessToken.Should().Be("access-token-123");
        result.RefreshToken.Should().Be("refresh-token-123");
        result.RequiresTwoFactor.Should().BeFalse();
        result.RequiresRevokedDeviceVerification.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidCredentials_SavesRefreshToken()
    {
        // Arrange
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Domain.Entities.RefreshToken>(rt =>
                rt.UserId == TestUserId && rt.Token == "refresh-token-123"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCredentials_CreatesNewSession()
    {
        // Arrange
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _sessionRepositoryMock.Verify(
            x => x.AddAsync(It.Is<UserSession>(s => s.UserId == TestUserId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCredentials_PublishesUserLoggedInEvent()
    {
        // Arrange
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<CarDealer.Contracts.Events.Auth.UserLoggedInEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ResetsFailedAccessCount()
    {
        // Arrange
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.AccessFailedCount == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Invalid Credentials

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedException()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var command = CreateValidCommand("nonexistent@example.com");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Credenciales inválidas.");
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser();
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(user.Email!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var command = CreateValidCommand(password: "WrongPassword123!");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Credenciales inválidas.");
    }

    [Fact]
    public async Task Handle_NullPasswordHash_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser(passwordHash: "temp");
        // Simulate null PasswordHash by setting it via reflection
        typeof(Microsoft.AspNetCore.Identity.IdentityUser)
            .GetProperty("PasswordHash")!
            .SetValue(user, null);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(user.Email!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = CreateValidCommand();

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Credenciales inválidas.");
    }

    #endregion

    #region Email Not Confirmed

    [Fact]
    public async Task Handle_EmailNotConfirmed_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser(emailConfirmed: false);
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(user.Email!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), user.PasswordHash!))
            .Returns(true);

        var command = CreateValidCommand();

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*verifica tu email*");
    }

    #endregion

    #region Locked Account

    [Fact]
    public async Task Handle_AccountLockedOut_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser();
        // Lock the user out
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(user.Email!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), user.PasswordHash!))
            .Returns(true);

        var command = CreateValidCommand();

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*temporalmente bloqueada*");
    }

    #endregion

    #region CAPTCHA Flow (US-18.3)

    [Fact]
    public async Task Handle_CaptchaRequiredButNoToken_ThrowsBadRequestException()
    {
        // Arrange
        // Simulate 3 failed attempts in cache
        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("3"));

        _captchaServiceMock.Setup(x => x.IsCaptchaRequired(3)).Returns(true);

        var command = new LoginCommand("test@example.com", "Password123!"); // No CaptchaToken

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*CAPTCHA*desafío*");
    }

    [Fact]
    public async Task Handle_CaptchaRequiredAndInvalidToken_ThrowsBadRequestException()
    {
        // Arrange
        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("3"));
        _captchaServiceMock.Setup(x => x.IsCaptchaRequired(3)).Returns(true);
        _captchaServiceMock
            .Setup(x => x.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(false);

        var command = new LoginCommand("test@example.com", "Password123!", "invalid-captcha-token");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*CAPTCHA*falló*");
    }

    [Fact]
    public async Task Handle_CaptchaRequiredAndValidToken_ProceedsWithLogin()
    {
        // Arrange
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("3"));
        _captchaServiceMock.Setup(x => x.IsCaptchaRequired(3)).Returns(true);
        _captchaServiceMock
            .Setup(x => x.VerifyAsync("valid-captcha", "login", "127.0.0.1"))
            .ReturnsAsync(true);

        var command = new LoginCommand("test@example.com", "Password123!", "valid-captcha");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeEmpty();
    }

    #endregion

    #region Two-Factor Authentication

    [Fact]
    public async Task Handle_TwoFactorEnabled_ReturnsTwoFactorResponse()
    {
        // Arrange
        var user = CreateTestUser();
        // Enable 2FA on the user via reflection/domain method
        var twoFactorAuth = new TwoFactorAuth(
            userId: user.Id,
            primaryMethod: Domain.Enums.TwoFactorAuthType.Authenticator
        );
        twoFactorAuth.Enable("test-secret", new List<string> { "code1", "code2" });
        user.EnableTwoFactorAuth(twoFactorAuth);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(user.Email!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), user.PasswordHash!))
            .Returns(true);
        _jwtGeneratorMock
            .Setup(x => x.GenerateTempToken(user.Id))
            .Returns("temp-2fa-token");

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RequiresTwoFactor.Should().BeTrue();
        result.TempToken.Should().Be("temp-2fa-token");
        result.TwoFactorType.Should().Be("authenticator");
        result.AccessToken.Should().BeEmpty();
        result.RefreshToken.Should().BeEmpty();
    }

    #endregion

    #region Revoked Device (AUTH-SEC-005)

    [Fact]
    public async Task Handle_RevokedDevice_ReturnsRevokedDeviceResponse()
    {
        // Arrange
        var user = CreateTestUser();
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(user.Email!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), user.PasswordHash!))
            .Returns(true);

        _revokedDeviceServiceMock
            .Setup(x => x.CheckIfDeviceIsRevokedAsync(user.Id, "127.0.0.1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevokedDeviceCheckResult
            {
                IsRevoked = true,
                DeviceFingerprint = "abc123fingerprint",
                RevokedAt = DateTime.UtcNow.AddHours(-1)
            });

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RequiresRevokedDeviceVerification.Should().BeTrue();
        result.DeviceFingerprint.Should().Be("abc123fingerprint");
        result.AccessToken.Should().BeEmpty();
        result.RefreshToken.Should().BeEmpty();
    }

    #endregion

    #region Session Reuse

    [Fact]
    public async Task Handle_ExistingSession_RenewsInsteadOfCreatingNew()
    {
        // Arrange
        var user = CreateTestUser();
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(user.Email!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), user.PasswordHash!))
            .Returns(true);
        _jwtGeneratorMock
            .Setup(x => x.GenerateToken(user, It.IsAny<int?>(), It.IsAny<string?>()))
            .Returns("access-token-123");
        _jwtGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token-123");

        var existingSession = new UserSession(
            userId: user.Id,
            refreshTokenId: "old-refresh-token-id",
            deviceInfo: "Desktop",
            browser: "Chrome",
            operatingSystem: "Windows 10/11",
            ipAddress: "127.0.0.1",
            expiresAt: DateTime.UtcNow.AddDays(7)
        );

        _sessionRepositoryMock
            .Setup(x => x.GetActiveSessionByDeviceAsync(user.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSession);

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _sessionRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _sessionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Failed Attempt Tracking

    [Fact]
    public async Task Handle_FailedLogin_IncrementsFailedAttemptsInCache()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var command = CreateValidCommand("bad@example.com");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
        _cacheMock.Verify(
            x => x.SetAsync(
                It.Is<string>(k => k.Contains("login_failed:")),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SuccessfulLogin_ClearsFailedAttempts()
    {
        // Arrange
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _cacheMock.Verify(
            x => x.RemoveAsync(
                It.Is<string>(k => k.Contains("login_failed:")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
