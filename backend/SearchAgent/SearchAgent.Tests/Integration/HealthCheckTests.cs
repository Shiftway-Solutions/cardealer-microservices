using System.Net;
using SearchAgent.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SearchAgent.Tests.Integration;

public class HealthCheckTests : IClassFixture<SearchAgentWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthCheckTests(SearchAgentWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheckLive_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheckReady_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Status_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/search-agent/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
