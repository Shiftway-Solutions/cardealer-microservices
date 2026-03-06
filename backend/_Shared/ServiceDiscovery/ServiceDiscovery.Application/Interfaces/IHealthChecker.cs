using ServiceDiscovery.Domain.Entities;

namespace ServiceDiscovery.Application.Interfaces;

/// <summary>
/// Service for performing health checks on registered services
/// </summary>
public interface IHealthChecker
{
    /// <summary>
    /// Performs a health check on a specific service instance
    /// </summary>
    Task<HealthCheckResult> CheckHealthAsync(ServiceInstance instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs health checks on all instances of a service
    /// </summary>
    Task<List<HealthCheckResult>> CheckServiceHealthAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs health checks on all registered services
    /// </summary>
    Task<List<HealthCheckResult>> CheckAllServicesHealthAsync(CancellationToken cancellationToken = default);
}
