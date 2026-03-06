using ServiceDiscovery.Domain.Enums;

namespace ServiceDiscovery.Domain.Entities;

/// <summary>
/// Represents a single instance of a registered service
/// </summary>
public class ServiceInstance
{
    /// <summary>
    /// Unique identifier for this service instance
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name of the service (e.g., "authservice", "userservice")
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Host address (e.g., "localhost", "192.168.1.10")
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Port number where the service is listening
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Full address of the service (computed from Host and Port)
    /// </summary>
    public string Address => $"http://{Host}:{Port}";

    /// <summary>
    /// Current status of the service
    /// </summary>
    public ServiceStatus Status { get; set; } = ServiceStatus.Active;

    /// <summary>
    /// Current health status
    /// </summary>
    public HealthStatus HealthStatus { get; set; } = HealthStatus.Unknown;

    /// <summary>
    /// Tags for categorizing the service (e.g., "production", "v1", "api")
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Metadata for additional service information
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// URL path for health check endpoint (e.g., "/health")
    /// </summary>
    public string? HealthCheckUrl { get; set; }

    /// <summary>
    /// Interval in seconds between health checks
    /// </summary>
    public int HealthCheckInterval { get; set; } = 10;

    /// <summary>
    /// Timeout in seconds for health check requests
    /// </summary>
    public int HealthCheckTimeout { get; set; } = 5;

    /// <summary>
    /// When the service was registered
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the service was checked
    /// </summary>
    public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the service was seen healthy
    /// </summary>
    public DateTime? LastHealthyAt { get; set; }

    /// <summary>
    /// Version of the service
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Validates if the service instance is properly configured
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Id)
               && !string.IsNullOrWhiteSpace(ServiceName)
               && !string.IsNullOrWhiteSpace(Host)
               && Port > 0 && Port <= 65535;
    }

    /// <summary>
    /// Checks if the service instance is considered healthy
    /// </summary>
    public bool IsHealthy()
    {
        return Status == ServiceStatus.Active && HealthStatus == HealthStatus.Healthy;
    }

    /// <summary>
    /// Updates the health status of this instance
    /// </summary>
    public void UpdateHealth(HealthStatus newStatus)
    {
        HealthStatus = newStatus;
        LastCheckedAt = DateTime.UtcNow;

        if (newStatus == HealthStatus.Healthy)
        {
            LastHealthyAt = DateTime.UtcNow;
        }
    }
}
