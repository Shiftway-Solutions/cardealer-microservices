using FluentAssertions;
using Gateway.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Gateway.Tests.Unit.Middleware;

public class HealthCheckMiddlewareTests
{
    private readonly HealthCheckMiddleware _middleware;
    private bool _nextCalled;

    public HealthCheckMiddlewareTests()
    {
        _nextCalled = false;
        _middleware = new HealthCheckMiddleware(async (context) =>
        {
            _nextCalled = true;
            await Task.CompletedTask;
        });
    }

    [Fact]
    public async Task InvokeAsync_WithHealthPath_ShouldReturnHealthy()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        context.Response.Body = new MemoryStream();

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        context.Response.ContentType.Should().Be("text/plain");
        _nextCalled.Should().BeFalse();

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseText = await reader.ReadToEndAsync();
        responseText.Should().Be("Gateway is healthy");
    }

    [Fact]
    public async Task InvokeAsync_WithHealthPathAndOrigin_ShouldSetCorsHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        context.Request.Headers["Origin"] = "http://localhost:5173";
        context.Response.Body = new MemoryStream();

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        context.Response.Headers["Access-Control-Allow-Origin"].ToString().Should().Be("http://localhost:5173");
        context.Response.Headers["Access-Control-Allow-Methods"].Should().NotBeEmpty();
        context.Response.Headers["Access-Control-Allow-Credentials"].ToString().Should().Be("true");
    }

    [Fact]
    public async Task InvokeAsync_WithProductionOrigin_ShouldSetCorsHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        context.Request.Headers["Origin"] = "https://inelcasrl.com.do";
        context.Response.Body = new MemoryStream();

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        context.Response.Headers["Access-Control-Allow-Origin"].ToString().Should().Be("https://inelcasrl.com.do");
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedOrigin_ShouldNotSetCorsHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        context.Request.Headers["Origin"] = "https://malicious-site.com";
        context.Response.Body = new MemoryStream();

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        context.Response.Headers.Should().NotContainKey("Access-Control-Allow-Origin");
    }

    [Fact]
    public async Task InvokeAsync_WithOptionsRequest_ShouldReturnNoContent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        context.Request.Method = "OPTIONS";
        context.Request.Headers["Origin"] = "http://localhost:5173";

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(204);
        _nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WithNonHealthPath_ShouldCallNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/vehicles";

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithCaseInsensitiveHealthPath_ShouldReturnHealthy()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/HEALTH";
        context.Response.Body = new MemoryStream();

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        _nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WithHealthPathNoOrigin_ShouldNotSetCorsHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        context.Response.Body = new MemoryStream();

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        context.Response.Headers.Should().NotContainKey("Access-Control-Allow-Origin");
    }
}
