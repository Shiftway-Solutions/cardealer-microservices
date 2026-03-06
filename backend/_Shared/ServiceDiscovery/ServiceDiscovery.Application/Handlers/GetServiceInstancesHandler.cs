using MediatR;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Domain.Entities;

namespace ServiceDiscovery.Application.Handlers;

/// <summary>
/// Handler for getting service instances
/// </summary>
public class GetServiceInstancesHandler : IRequestHandler<Queries.GetServiceInstancesQuery, List<ServiceInstance>>
{
    private readonly IServiceDiscovery _discovery;

    public GetServiceInstancesHandler(IServiceDiscovery discovery)
    {
        _discovery = discovery;
    }

    public async Task<List<ServiceInstance>> Handle(Queries.GetServiceInstancesQuery request, CancellationToken cancellationToken)
    {
        if (request.OnlyHealthy)
        {
            return await _discovery.GetHealthyInstancesAsync(request.ServiceName, cancellationToken);
        }

        return await _discovery.GetServiceInstancesAsync(request.ServiceName, cancellationToken);
    }
}
