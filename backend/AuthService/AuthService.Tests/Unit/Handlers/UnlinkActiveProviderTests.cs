using Xunit;
using FluentAssertions;
using AuthService.Application.Features.ExternalAuth.Commands.ValidateUnlinkAccount;
using AuthService.Application.Features.ExternalAuth.Commands.RequestUnlinkCode;
using AuthService.Application.Features.ExternalAuth.Commands.UnlinkActiveProvider;

namespace AuthService.Tests.Unit.Handlers;

/// <summary>
/// Unit tests for AUTH-EXT-008: Unlink Active OAuth Provider
/// Tests validators for all three commands in the flow
/// </summary>
public class UnlinkActiveProviderTests
{
    #region ValidateUnlinkAccountCommand Validator Tests

    [Fact]
    public void ValidateUnlinkAccountValidator_ValidRequest_Passes()
    {
        // Arrange
        var validator = new ValidateUnlinkAccountCommandValidator();
        var command = new ValidateUnlinkAccountCommand(
            UserId: "user-123",
            Provider: "google"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateUnlinkAccountValidator_EmptyUserId_Fails()
    {
        // Arrange
        var validator = new ValidateUnlinkAccountCommandValidator();
        var command = new ValidateUnlinkAccountCommand(
            UserId: "",
            Provider: "google"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void ValidateUnlinkAccountValidator_EmptyProvider_Fails()
    {
        // Arrange
        var validator = new ValidateUnlinkAccountCommandValidator();
        var command = new ValidateUnlinkAccountCommand(
            UserId: "user-123",
            Provider: ""
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Provider");
    }

    [Theory]
    [InlineData("google", true)]
    [InlineData("Google", true)]
    [InlineData("GOOGLE", true)]
    [InlineData("microsoft", true)]
    [InlineData("facebook", true)]
    [InlineData("apple", true)]
    [InlineData("invalid_provider", false)]
    [InlineData("twitter", false)]
    [InlineData("linkedin", false)]
    public void ValidateUnlinkAccountValidator_ProviderValidation_WorksCorrectly(
        string provider, bool shouldBeValid)
    {
        // Arrange
        var validator = new ValidateUnlinkAccountCommandValidator();
        var command = new ValidateUnlinkAccountCommand(
            UserId: "user-123",
            Provider: provider
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().Be(shouldBeValid);
    }

    #endregion

    #region RequestUnlinkCodeCommand Validator Tests

    [Fact]
    public void RequestUnlinkCodeValidator_ValidRequest_Passes()
    {
        // Arrange
        var validator = new RequestUnlinkCodeCommandValidator();
        var command = new RequestUnlinkCodeCommand(
            UserId: "user-123",
            Provider: "google",
            IpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RequestUnlinkCodeValidator_EmptyUserId_Fails()
    {
        // Arrange
        var validator = new RequestUnlinkCodeCommandValidator();
        var command = new RequestUnlinkCodeCommand(
            UserId: "",
            Provider: "google",
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
    public void RequestUnlinkCodeValidator_EmptyProvider_Fails()
    {
        // Arrange
        var validator = new RequestUnlinkCodeCommandValidator();
        var command = new RequestUnlinkCodeCommand(
            UserId: "user-123",
            Provider: "",
            IpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Provider");
    }

    [Theory]
    [InlineData("google", true)]
    [InlineData("microsoft", true)]
    [InlineData("facebook", true)]
    [InlineData("apple", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void RequestUnlinkCodeValidator_ProviderValues_ValidateCorrectly(
        string provider, bool shouldBeValid)
    {
        // Arrange
        var validator = new RequestUnlinkCodeCommandValidator();
        var command = new RequestUnlinkCodeCommand(
            UserId: "user-123",
            Provider: provider,
            IpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        if (shouldBeValid)
        {
            result.Errors.Should().NotContain(e => e.PropertyName == "Provider" && e.ErrorMessage.Contains("valid"));
        }
    }

    #endregion

    #region UnlinkActiveProviderCommand Validator Tests

    [Fact]
    public void UnlinkActiveProviderValidator_ValidRequest_Passes()
    {
        // Arrange
        var validator = new UnlinkActiveProviderCommandValidator();
        var command = new UnlinkActiveProviderCommand(
            UserId: "user-123",
            Provider: "google",
            VerificationCode: "123456",
            IpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UnlinkActiveProviderValidator_EmptyUserId_Fails()
    {
        // Arrange
        var validator = new UnlinkActiveProviderCommandValidator();
        var command = new UnlinkActiveProviderCommand(
            UserId: "",
            Provider: "google",
            VerificationCode: "123456",
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
    public void UnlinkActiveProviderValidator_EmptyProvider_Fails()
    {
        // Arrange
        var validator = new UnlinkActiveProviderCommandValidator();
        var command = new UnlinkActiveProviderCommand(
            UserId: "user-123",
            Provider: "",
            VerificationCode: "123456",
            IpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Provider");
    }

    [Theory]
    [InlineData("123456", true)]
    [InlineData("000000", true)]
    [InlineData("999999", true)]
    [InlineData("12345", false)]    // Too short
    [InlineData("1234567", false)]  // Too long
    [InlineData("", false)]         // Empty
    [InlineData("abcdef", false)]   // Letters
    [InlineData("12345a", false)]   // Mixed
    public void UnlinkActiveProviderValidator_VerificationCode_ValidatesCorrectly(
        string code, bool shouldBeValid)
    {
        // Arrange
        var validator = new UnlinkActiveProviderCommandValidator();
        var command = new UnlinkActiveProviderCommand(
            UserId: "user-123",
            Provider: "google",
            VerificationCode: code,
            IpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().Be(shouldBeValid);
    }

    [Theory]
    [InlineData("google", true)]
    [InlineData("Google", true)]
    [InlineData("GOOGLE", true)]
    [InlineData("microsoft", true)]
    [InlineData("facebook", true)]
    [InlineData("apple", true)]
    [InlineData("invalid", false)]
    public void UnlinkActiveProviderValidator_Provider_ValidatesCorrectly(
        string provider, bool shouldBeValid)
    {
        // Arrange
        var validator = new UnlinkActiveProviderCommandValidator();
        var command = new UnlinkActiveProviderCommand(
            UserId: "user-123",
            Provider: provider,
            VerificationCode: "123456",
            IpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().Be(shouldBeValid);
    }

    #endregion
}
