using MediatR;
using ServiceDiscovery.Application.Interfaces;

namespace ServiceDiscovery.Application.Handlers;

/// <summary>
/// Handler for getting all service names
/// </summary>
public class GetServiceNamesHandler : IRequestHandler<Queries.GetServiceNamesQuery, List<string>>
{
    private readonly IServiceDiscovery _discovery;

    public GetServiceNamesHandler(IServiceDiscovery discovery)
    {
        _discovery = discovery;
    }

    public async Task<List<string>> Handle(Queries.GetServiceNamesQuery request, CancellationToken cancellationToken)
    {
        return await _discovery.GetServiceNamesAsync(cancellationToken);
    }
}
