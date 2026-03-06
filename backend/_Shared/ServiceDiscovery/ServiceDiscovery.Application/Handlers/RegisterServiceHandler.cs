using MediatR;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Domain.Entities;

namespace ServiceDiscovery.Application.Handlers;

/// <summary>
/// Handler for registering a service
/// </summary>
public class RegisterServiceHandler : IRequestHandler<Commands.RegisterServiceCommand, ServiceInstance>
{
    private readonly IServiceRegistry _registry;

    public RegisterServiceHandler(IServiceRegistry registry)
    {
        _registry = registry;
    }

    public async Task<ServiceInstance> Handle(Commands.RegisterServiceCommand request, CancellationToken cancellationToken)
    {
        var instance = new ServiceInstance
        {
            Id = Guid.NewGuid().ToString(),
            ServiceName = request.ServiceName,
            Host = request.Host,
            Port = request.Port,
            Tags = request.Tags,
            Metadata = request.Metadata,
            HealthCheckUrl = request.HealthCheckUrl,
            HealthCheckInterval = request.HealthCheckInterval,
            HealthCheckTimeout = request.HealthCheckTimeout,
            Version = request.Version,
            RegisteredAt = DateTime.UtcNow
        };

        if (!instance.IsValid())
        {
            throw new ArgumentException("Invalid service instance configuration");
        }

        return await _registry.RegisterServiceAsync(instance, cancellationToken);
    }
}
