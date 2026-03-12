using Xunit;
using FluentAssertions;
using AuthService.Application.Features.Auth.Commands.RequestPasswordSetup;
using AuthService.Application.Features.Auth.Commands.ValidatePasswordSetupToken;
using AuthService.Application.Features.Auth.Commands.SetPasswordForOAuthUser;

namespace AuthService.Tests.Unit.Handlers;

/// <summary>
/// Unit tests for AUTH-PWD-001: Set Password for OAuth User
/// Tests validators for all three commands in the flow
/// </summary>
public class SetPasswordForOAuthUserTests
{
    #region RequestPasswordSetupCommand Validator Tests

    [Fact]
    public void RequestPasswordSetupValidator_ValidRequest_Passes()
    {
        // Arrange
        var validator = new RequestPasswordSetupCommandValidator();
        var command = new RequestPasswordSetupCommand(
            UserId: "user-123",
            Email: "user@example.com",
            IpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RequestPasswordSetupValidator_EmptyUserId_Fails()
    {
        // Arrange
        var validator = new RequestPasswordSetupCommandValidator();
        var command = new RequestPasswordSetupCommand(
            UserId: "",
            Email: "user@example.com",
            IpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void RequestPasswordSetupValidator_InvalidEmail_Fails()
    {
        // Arrange
        var validator = new RequestPasswordSetupCommandValidator();
        var command = new RequestPasswordSetupCommand(
            UserId: "user-123",
            Email: "not-an-email",
            IpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void RequestPasswordSetupValidator_EmptyEmail_Fails()
    {
        // Arrange
        var validator = new RequestPasswordSetupCommandValidator();
        var command = new RequestPasswordSetupCommand(
            UserId: "user-123",
            Email: "",
            IpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region ValidatePasswordSetupTokenCommand Validator Tests

    [Fact]
    public void ValidatePasswordSetupTokenValidator_ValidToken_Passes()
    {
        // Arrange
        var validator = new ValidatePasswordSetupTokenCommandValidator();
        var command = new ValidatePasswordSetupTokenCommand("valid-token-12345678901234567890");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidatePasswordSetupTokenValidator_ShortToken_Fails()
    {
        // Arrange
        var validator = new ValidatePasswordSetupTokenCommandValidator();
        var command = new ValidatePasswordSetupTokenCommand("short");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Token");
    }

    [Fact]
    public void ValidatePasswordSetupTokenValidator_EmptyToken_Fails()
    {
        // Arrange
        var validator = new ValidatePasswordSetupTokenCommandValidator();
        var command = new ValidatePasswordSetupTokenCommand("");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region SetPasswordForOAuthUserCommand Validator Tests

    [Fact]
    public void SetPasswordValidator_ValidRequest_Passes()
    {
        // Arrange
        var validator = new SetPasswordForOAuthUserCommandValidator();
        var command = new SetPasswordForOAuthUserCommand(
            Token: "valid-token-12345678901234567890",
            NewPassword: "SecureP@ssw0rd!",
            ConfirmPassword: "SecureP@ssw0rd!"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SetPasswordValidator_EmptyToken_Fails()
    {
        // Arrange
        var validator = new SetPasswordForOAuthUserCommandValidator();
        var command = new SetPasswordForOAuthUserCommand(
            Token: "",
            NewPassword: "SecureP@ssw0rd!",
            ConfirmPassword: "SecureP@ssw0rd!"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Token");
    }

    [Fact]
    public void SetPasswordValidator_PasswordMismatch_Fails()
    {
        // Arrange
        var validator = new SetPasswordForOAuthUserCommandValidator();
        var command = new SetPasswordForOAuthUserCommand(
            Token: "valid-token-12345678901234567890",
            NewPassword: "SecureP@ssw0rd!",
            ConfirmPassword: "DifferentP@ssw0rd!"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmPassword");
    }

    [Theory]
    [InlineData("", false, "Empty password")]
    [InlineData("short1!", false, "Too short")]
    [InlineData("nouppercase1!", false, "No uppercase")]
    [InlineData("NOLOWERCASE1!", false, "No lowercase")]
    [InlineData("NoNumbers!", false, "No number")]
    [InlineData("NoSpecial1", false, "No special char")]
    [InlineData("ValidP@ss1", true, "Valid password")]
    [InlineData("SecureP@ssw0rd!", true, "Valid complex password")]
    public void SetPasswordValidator_PasswordComplexity_ValidatesCorrectly(
        string password, bool shouldBeValid, string scenario)
    {
        // Arrange
        var validator = new SetPasswordForOAuthUserCommandValidator();
        var command = new SetPasswordForOAuthUserCommand(
            Token: "valid-token-12345678901234567890",
            NewPassword: password,
            ConfirmPassword: password
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().Be(shouldBeValid, because: scenario);
    }

    #endregion
}
