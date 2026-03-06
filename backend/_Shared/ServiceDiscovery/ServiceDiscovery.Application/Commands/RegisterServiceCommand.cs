using MediatR;
using ServiceDiscovery.Domain.Entities;

namespace ServiceDiscovery.Application.Commands;

/// <summary>
/// Command to register a new service instance
/// </summary>
public class RegisterServiceCommand : IRequest<ServiceInstance>
{
    public string ServiceName { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string? HealthCheckUrl { get; set; }
    public int HealthCheckInterval { get; set; } = 10;
    public int HealthCheckTimeout { get; set; } = 5;
    public string? Version { get; set; }
}
