using MediatR;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Domain.Entities;

namespace ServiceDiscovery.Application.Handlers;

/// <summary>
/// Handler for getting a service instance by ID
/// </summary>
public class GetServiceInstanceByIdHandler : IRequestHandler<Queries.GetServiceInstanceByIdQuery, ServiceInstance?>
{
    private readonly IServiceDiscovery _discovery;

    public GetServiceInstanceByIdHandler(IServiceDiscovery discovery)
    {
        _discovery = discovery;
    }

    public async Task<ServiceInstance?> Handle(Queries.GetServiceInstanceByIdQuery request, CancellationToken cancellationToken)
    {
        return await _discovery.GetServiceInstanceByIdAsync(request.InstanceId, cancellationToken);
    }
}
