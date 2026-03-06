using ServiceDiscovery.Domain.Entities;

namespace ServiceDiscovery.Application.Interfaces;

/// <summary>
/// Service for registering and deregistering services
/// </summary>
public interface IServiceRegistry
{
    /// <summary>
    /// Registers a new service instance
    /// </summary>
    Task<ServiceInstance> RegisterServiceAsync(ServiceInstance instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters a service instance by ID
    /// </summary>
    Task<bool> DeregisterServiceAsync(string instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing service instance
    /// </summary>
    Task<ServiceInstance?> UpdateServiceAsync(string instanceId, ServiceInstance updatedInstance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Passes a health check (TTL) to keep the service registered
    /// </summary>
    Task<bool> PassHealthCheckAsync(string instanceId, CancellationToken cancellationToken = default);
}
