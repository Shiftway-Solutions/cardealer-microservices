using ServiceDiscovery.Domain.Enums;

namespace ServiceDiscovery.Domain.Entities;

/// <summary>
/// Represents the result of a health check operation
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// The service instance that was checked
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// The health status determined
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// When the health check was performed
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// HTTP status code returned (if applicable)
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Optional message describing the health check result
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error message if the check failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Creates a successful health check result
    /// </summary>
    public static HealthCheckResult Healthy(string instanceId, long responseTimeMs, int statusCode)
    {
        return new HealthCheckResult
        {
            InstanceId = instanceId,
            Status = HealthStatus.Healthy,
            ResponseTimeMs = responseTimeMs,
            StatusCode = statusCode,
            Message = "Service is healthy"
        };
    }

    /// <summary>
    /// Creates an unhealthy health check result
    /// </summary>
    public static HealthCheckResult Unhealthy(string instanceId, string error)
    {
        return new HealthCheckResult
        {
            InstanceId = instanceId,
            Status = HealthStatus.Unhealthy,
            Error = error,
            Message = "Service is unhealthy"
        };
    }

    /// <summary>
    /// Creates a degraded health check result
    /// </summary>
    public static HealthCheckResult Degraded(string instanceId, string reason)
    {
        return new HealthCheckResult
        {
            InstanceId = instanceId,
            Status = HealthStatus.Degraded,
            Message = reason
        };
    }
}
