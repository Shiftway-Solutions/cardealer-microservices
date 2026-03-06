using Consul;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Domain.Entities;
using ServiceDiscovery.Domain.Enums;

namespace ServiceDiscovery.Infrastructure.Services;

/// <summary>
/// Consul-based implementation of service discovery
/// </summary>
public class ConsulServiceDiscovery : IServiceDiscovery
{
    private readonly IConsulClient _consulClient;
    private readonly Dictionary<string, int> _roundRobinCounters = new();
    private readonly object _lock = new();

    public ConsulServiceDiscovery(IConsulClient consulClient)
    {
        _consulClient = consulClient;
    }

    public async Task<List<ServiceInstance>> GetServiceInstancesAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var services = await _consulClient.Health.Service(serviceName, null, false, cancellationToken);

        return services.Response.Select(MapToServiceInstance).ToList();
    }

    public async Task<List<ServiceInstance>> GetHealthyInstancesAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var services = await _consulClient.Health.Service(serviceName, null, true, cancellationToken);

        return services.Response.Select(MapToServiceInstance).ToList();
    }

    public async Task<List<string>> GetServiceNamesAsync(CancellationToken cancellationToken = default)
    {
        var services = await _consulClient.Catalog.Services(cancellationToken);

        return services.Response.Keys.ToList();
    }

    public async Task<ServiceInstance?> GetServiceInstanceByIdAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        var services = await _consulClient.Agent.Services(cancellationToken);

        if (services.Response.TryGetValue(instanceId, out var service))
        {
            return MapToServiceInstance(service);
        }

        return null;
    }

    public async Task<ServiceInstance?> FindServiceInstanceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var instances = await GetHealthyInstancesAsync(serviceName, cancellationToken);

        if (instances.Count == 0)
        {
            return null;
        }

        // Round-robin load balancing
        lock (_lock)
        {
            if (!_roundRobinCounters.ContainsKey(serviceName))
            {
                _roundRobinCounters[serviceName] = 0;
            }

            var index = _roundRobinCounters[serviceName] % instances.Count;
            _roundRobinCounters[serviceName]++;

            return instances[index];
        }
    }

    private ServiceInstance MapToServiceInstance(ServiceEntry entry)
    {
        var healthStatus = entry.Checks.All(c => c.Status == Consul.HealthStatus.Passing)
            ? Domain.Enums.HealthStatus.Healthy
            : entry.Checks.Any(c => c.Status == Consul.HealthStatus.Critical)
                ? Domain.Enums.HealthStatus.Unhealthy
                : Domain.Enums.HealthStatus.Degraded;

        return new ServiceInstance
        {
            Id = entry.Service.ID,
            ServiceName = entry.Service.Service,
            Host = entry.Service.Address,
            Port = entry.Service.Port,
            Tags = entry.Service.Tags?.ToList() ?? new List<string>(),
            Metadata = entry.Service.Meta?.ToDictionary(k => k.Key, v => v.Value) ?? new Dictionary<string, string>(),
            HealthStatus = healthStatus,
            Status = ServiceStatus.Active
        };
    }

    private ServiceInstance MapToServiceInstance(AgentService service)
    {
        return new ServiceInstance
        {
            Id = service.ID,
            ServiceName = service.Service,
            Host = service.Address,
            Port = service.Port,
            Tags = service.Tags?.ToList() ?? new List<string>(),
            Metadata = service.Meta?.ToDictionary(k => k.Key, v => v.Value) ?? new Dictionary<string, string>(),
            Status = ServiceStatus.Active
        };
    }
}
