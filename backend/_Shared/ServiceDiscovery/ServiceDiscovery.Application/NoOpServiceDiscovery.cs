using ServiceDiscovery.Domain.Entities;
using ServiceDiscovery.Domain.Enums;

namespace ServiceDiscovery.Application.Interfaces;

/// <summary>
/// No-op implementation of IServiceDiscovery for development without Consul
/// Returns fallback URLs from configuration
/// </summary>
public class NoOpServiceDiscovery : IServiceDiscovery
{
    private readonly Dictionary<string, string> _serviceUrls;

    public NoOpServiceDiscovery()
    {
        // Default fallback URLs for development
        _serviceUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AuthService"] = "http://authservice:80",
            ["NotificationService"] = "http://notificationservice:80",
            ["ErrorService"] = "http://errorservice:80",
            ["UserService"] = "http://userservice:80",
            ["RoleService"] = "http://roleservice:80",
            ["ProductService"] = "http://productservice:80",
            ["MediaService"] = "http://mediaservice:80"
        };
    }

    public Task<List<ServiceInstance>> GetServiceInstancesAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        if (_serviceUrls.TryGetValue(serviceName, out var url))
        {
            var uri = new Uri(url);
            var instance = new ServiceInstance
            {
                Id = $"{serviceName}-1",
                ServiceName = serviceName,
                Host = uri.Host,
                Port = uri.Port,
                Status = ServiceStatus.Active,
                HealthStatus = HealthStatus.Healthy
            };
            return Task.FromResult(new List<ServiceInstance> { instance });
        }
        return Task.FromResult(new List<ServiceInstance>());
    }

    public Task<List<ServiceInstance>> GetHealthyInstancesAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return GetServiceInstancesAsync(serviceName, cancellationToken);
    }

    public Task<List<string>> GetServiceNamesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_serviceUrls.Keys.ToList());
    }

    public Task<ServiceInstance?> GetServiceInstanceByIdAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ServiceInstance?>(null);
    }

    public Task<ServiceInstance?> FindServiceInstanceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var instances = GetServiceInstancesAsync(serviceName, cancellationToken).Result;
        return Task.FromResult(instances.FirstOrDefault());
    }
}

/// <summary>
/// No-op implementation of IServiceRegistry for development without Consul
/// </summary>
public class NoOpServiceRegistry : IServiceRegistry
{
    public Task<ServiceInstance> RegisterServiceAsync(ServiceInstance instance, CancellationToken cancellationToken = default)
    {
        // Just return the instance as-is (no actual registration)
        return Task.FromResult(instance);
    }

    public Task<bool> DeregisterServiceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<ServiceInstance?> UpdateServiceAsync(string instanceId, ServiceInstance updatedInstance, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ServiceInstance?>(updatedInstance);
    }

    public Task<bool> PassHealthCheckAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
