using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Domain.Entities;
using ServiceDiscovery.Domain.Enums;
using System.Diagnostics;

namespace ServiceDiscovery.Infrastructure.Services;

/// <summary>
/// Implementation of health checker that performs HTTP health checks
/// </summary>
public class HttpHealthChecker : IHealthChecker
{
    private readonly IServiceDiscovery _discovery;
    private readonly HttpClient _httpClient;

    public HttpHealthChecker(IServiceDiscovery discovery, IHttpClientFactory httpClientFactory)
    {
        _discovery = discovery;
        _httpClient = httpClientFactory.CreateClient("HealthCheck");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(ServiceInstance instance, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instance.HealthCheckUrl))
        {
            return HealthCheckResult.Degraded(instance.Id, "No health check URL configured");
        }

        var healthCheckUrl = $"http://{instance.Host}:{instance.Port}{instance.HealthCheckUrl}";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(instance.HealthCheckTimeout));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

            var response = await _httpClient.GetAsync(healthCheckUrl, linkedCts.Token);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy(instance.Id, stopwatch.ElapsedMilliseconds, (int)response.StatusCode);
            }

            return HealthCheckResult.Unhealthy(instance.Id, $"Health check returned {response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy(instance.Id, $"Health check timed out after {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy(instance.Id, $"HTTP error: {ex.Message}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy(instance.Id, $"Unexpected error: {ex.Message}");
        }
    }

    public async Task<List<HealthCheckResult>> CheckServiceHealthAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var instances = await _discovery.GetServiceInstancesAsync(serviceName, cancellationToken);
        var tasks = instances.Select(instance => CheckHealthAsync(instance, cancellationToken));

        return (await Task.WhenAll(tasks)).ToList();
    }

    public async Task<List<HealthCheckResult>> CheckAllServicesHealthAsync(CancellationToken cancellationToken = default)
    {
        var serviceNames = await _discovery.GetServiceNamesAsync(cancellationToken);
        var allResults = new List<HealthCheckResult>();

        foreach (var serviceName in serviceNames)
        {
            var results = await CheckServiceHealthAsync(serviceName, cancellationToken);
            allResults.AddRange(results);
        }

        return allResults;
    }
}
