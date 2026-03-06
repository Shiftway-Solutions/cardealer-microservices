using Consul;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Domain.Entities;
using ServiceDiscovery.Domain.Enums;

namespace ServiceDiscovery.Infrastructure.Services;

/// <summary>
/// Consul-based implementation of service registry
/// </summary>
public class ConsulServiceRegistry : IServiceRegistry
{
    private readonly IConsulClient _consulClient;

    public ConsulServiceRegistry(IConsulClient consulClient)
    {
        _consulClient = consulClient;
    }

    public async Task<ServiceInstance> RegisterServiceAsync(ServiceInstance instance, CancellationToken cancellationToken = default)
    {
        var registration = new AgentServiceRegistration
        {
            ID = instance.Id,
            Name = instance.ServiceName,
            Address = instance.Host,
            Port = instance.Port,
            Tags = instance.Tags.ToArray(),
            Meta = instance.Metadata
        };

        // Configure health check if URL is provided
        if (!string.IsNullOrWhiteSpace(instance.HealthCheckUrl))
        {
            var healthCheckUrl = $"http://{instance.Host}:{instance.Port}{instance.HealthCheckUrl}";

            registration.Check = new AgentServiceCheck
            {
                HTTP = healthCheckUrl,
                Interval = TimeSpan.FromSeconds(instance.HealthCheckInterval),
                Timeout = TimeSpan.FromSeconds(instance.HealthCheckTimeout),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
            };
        }

        await _consulClient.Agent.ServiceRegister(registration, cancellationToken);

        instance.Status = ServiceStatus.Active;
        return instance;
    }

    public async Task<bool> DeregisterServiceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        var response = await _consulClient.Agent.ServiceDeregister(instanceId, cancellationToken);
        return response.StatusCode == System.Net.HttpStatusCode.OK;
    }

    public async Task<ServiceInstance?> UpdateServiceAsync(string instanceId, ServiceInstance updatedInstance, CancellationToken cancellationToken = default)
    {
        // Consul doesn't have direct update - we deregister and re-register
        await DeregisterServiceAsync(instanceId, cancellationToken);
        updatedInstance.Id = instanceId; // Keep the same ID
        return await RegisterServiceAsync(updatedInstance, cancellationToken);
    }

    public async Task<bool> PassHealthCheckAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        await _consulClient.Agent.PassTTL($"service:{instanceId}", "Service is alive", cancellationToken);
        return true;
    }
}
