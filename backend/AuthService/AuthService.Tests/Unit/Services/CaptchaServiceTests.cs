using System.Net;
using System.Text.Json;
using AuthService.Domain.Interfaces.Services;
using AuthService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace AuthService.Tests.Unit.Services;

/// <summary>
/// US-18.3: Unit tests for CaptchaService (Google reCAPTCHA v3).
/// </summary>
public class CaptchaServiceTests
{
    private readonly Mock<ILogger<CaptchaService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;

    public CaptchaServiceTests()
    {
        _loggerMock = new Mock<ILogger<CaptchaService>>();
        _configurationMock = new Mock<IConfiguration>();
    }

    private CaptchaService CreateService(
        HttpMessageHandler? httpHandler = null,
        string? secretKey = null,
        bool enabled = true,
        decimal minScore = 0.5m)
    {
        _configurationMock.Setup(c => c["ReCaptcha:SecretKey"]).Returns(secretKey);
        _configurationMock.Setup(c => c["ReCaptcha:Enabled"]).Returns(enabled.ToString());
        _configurationMock.Setup(c => c["ReCaptcha:MinScore"]).Returns(minScore.ToString());

        var httpClient = httpHandler != null
            ? new HttpClient(httpHandler)
            : new HttpClient();

        return new CaptchaService(httpClient, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task VerifyAsync_WhenDisabled_ReturnsTrue()
    {
        // Arrange
        var service = CreateService(secretKey: "test-key", enabled: false);

        // Act
        var result = await service.VerifyAsync("any-token", "login");

        // Assert
        result.Should().BeTrue();
        service.LastScore.Should().Be(1.0m);
    }

    [Fact]
    public async Task VerifyAsync_WhenNoSecretKey_ReturnsTrue()
    {
        // Arrange
        var service = CreateService(secretKey: null, enabled: true);

        // Act
        var result = await service.VerifyAsync("any-token", "login");

        // Assert
        result.Should().BeTrue();
        service.LastScore.Should().Be(1.0m);
    }

    [Fact]
    public async Task VerifyAsync_WithEmptyToken_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(secretKey: "test-secret-key", enabled: true);

        // Act
        var result = await service.VerifyAsync("", "login");

        // Assert
        result.Should().BeFalse();
        service.LastScore.Should().Be(0.0m);
    }

    [Fact]
    public async Task VerifyAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            success = true,
            score = 0.9,
            action = "login",
            hostname = "okla.com.do"
        });

        var handler = CreateMockHttpHandler(HttpStatusCode.OK, responseJson);
        var service = CreateService(handler, secretKey: "test-secret-key", enabled: true);

        // Act
        var result = await service.VerifyAsync("valid-token", "login", "192.168.1.1");

        // Assert
        result.Should().BeTrue();
        service.LastScore.Should().BeGreaterThan(0.5m);
    }

    [Fact]
    public async Task VerifyAsync_WithLowScore_ReturnsFalse()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            success = true,
            score = 0.2, // Below 0.5 threshold
            action = "login"
        });

        var handler = CreateMockHttpHandler(HttpStatusCode.OK, responseJson);
        var service = CreateService(handler, secretKey: "test-secret-key", enabled: true, minScore: 0.5m);

        // Act
        var result = await service.VerifyAsync("bot-token", "login");

        // Assert
        result.Should().BeFalse();
        service.LastScore.Should().Be(0.2m);
    }

    [Fact]
    public async Task VerifyAsync_WhenApiReturnsFailure_ReturnsFalse()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            success = false,
            error_codes = new[] { "timeout-or-duplicate" }
        });

        var handler = CreateMockHttpHandler(HttpStatusCode.OK, responseJson);
        var service = CreateService(handler, secretKey: "test-secret-key", enabled: true);

        // Act
        var result = await service.VerifyAsync("expired-token", "login");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyAsync_WhenHttpError_ReturnsFalse()
    {
        // Arrange
        var handler = CreateMockHttpHandler(HttpStatusCode.InternalServerError, "Server Error");
        var service = CreateService(handler, secretKey: "test-secret-key", enabled: true);

        // Act
        var result = await service.VerifyAsync("valid-token", "login");

        // Assert
        result.Should().BeFalse();
        service.LastScore.Should().Be(0.0m);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(5, true)]
    public void IsCaptchaRequired_BasedOnFailedAttempts(int failedAttempts, bool expected)
    {
        // Arrange
        var service = CreateService(secretKey: "test-key", enabled: true);

        // Act
        var result = service.IsCaptchaRequired(failedAttempts);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsCaptchaRequired_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(secretKey: "test-key", enabled: false);

        // Act
        var result = service.IsCaptchaRequired(5);

        // Assert
        result.Should().BeFalse();
    }

    private static HttpMessageHandler CreateMockHttpHandler(HttpStatusCode statusCode, string content)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

        return mockHandler.Object;
    }
}
