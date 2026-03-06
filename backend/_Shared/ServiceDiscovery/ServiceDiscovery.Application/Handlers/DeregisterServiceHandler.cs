using MediatR;
using ServiceDiscovery.Application.Interfaces;

namespace ServiceDiscovery.Application.Handlers;

/// <summary>
/// Handler for deregistering a service
/// </summary>
public class DeregisterServiceHandler : IRequestHandler<Commands.DeregisterServiceCommand, bool>
{
    private readonly IServiceRegistry _registry;

    public DeregisterServiceHandler(IServiceRegistry registry)
    {
        _registry = registry;
    }

    public async Task<bool> Handle(Commands.DeregisterServiceCommand request, CancellationToken cancellationToken)
    {
        return await _registry.DeregisterServiceAsync(request.InstanceId, cancellationToken);
    }
}
