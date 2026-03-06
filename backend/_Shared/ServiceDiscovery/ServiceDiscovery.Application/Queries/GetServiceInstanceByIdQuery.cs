using MediatR;
using ServiceDiscovery.Domain.Entities;

namespace ServiceDiscovery.Application.Queries;

/// <summary>
/// Query to get a specific service instance by ID
/// </summary>
public class GetServiceInstanceByIdQuery : IRequest<ServiceInstance?>
{
    public string InstanceId { get; set; } = string.Empty;
}
