using MediatR;
using ServiceDiscovery.Domain.Entities;

namespace ServiceDiscovery.Application.Queries;

/// <summary>
/// Query to get all instances of a service
/// </summary>
public class GetServiceInstancesQuery : IRequest<List<ServiceInstance>>
{
    public string ServiceName { get; set; } = string.Empty;
    public bool OnlyHealthy { get; set; } = false;
}
