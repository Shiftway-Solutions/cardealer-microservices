namespace ServiceDiscovery.Domain.Enums;

/// <summary>
/// Represents the registration status of a service
/// </summary>
public enum ServiceStatus
{
    /// <summary>
    /// Service is registered and active
    /// </summary>
    Active = 0,

    /// <summary>
    /// Service is temporarily inactive
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// Service has been deregistered
    /// </summary>
    Deregistered = 2
}
