using MediatR;

namespace ServiceDiscovery.Application.Commands;

/// <summary>
/// Command to deregister a service instance
/// </summary>
public class DeregisterServiceCommand : IRequest<bool>
{
    public string InstanceId { get; set; } = string.Empty;
}
