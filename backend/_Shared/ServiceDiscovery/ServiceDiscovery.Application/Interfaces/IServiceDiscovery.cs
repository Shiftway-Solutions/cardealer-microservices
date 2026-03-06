using ServiceDiscovery.Domain.Entities;

namespace ServiceDiscovery.Application.Interfaces;

/// <summary>
/// Service for discovering and querying registered services
/// </summary>
public interface IServiceDiscovery
{
    /// <summary>
    /// Gets all instances of a specific service
    /// </summary>
    Task<List<ServiceInstance>> GetServiceInstancesAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only healthy instances of a service
    /// </summary>
    Task<List<ServiceInstance>> GetHealthyInstancesAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered service names
    /// </summary>
    Task<List<string>> GetServiceNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific service instance by ID
    /// </summary>
    Task<ServiceInstance?> GetServiceInstanceByIdAsync(string instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a service instance using round-robin load balancing
    /// </summary>
    Task<ServiceInstance?> FindServiceInstanceAsync(string serviceName, CancellationToken cancellationToken = default);
}
