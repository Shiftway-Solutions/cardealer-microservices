using MediatR;

namespace ServiceDiscovery.Application.Queries;

/// <summary>
/// Query to get all registered service names
/// </summary>
public class GetServiceNamesQuery : IRequest<List<string>>
{
}
