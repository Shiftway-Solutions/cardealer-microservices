namespace ServiceDiscovery.Domain.Enums;

/// <summary>
/// Represents the health status of a service instance
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Service is healthy and operational
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// Service is degraded but still functional
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// Service is unhealthy and not responding
    /// </summary>
    Unhealthy = 2,

    /// <summary>
    /// Service health status is unknown
    /// </summary>
    Unknown = 3
}
