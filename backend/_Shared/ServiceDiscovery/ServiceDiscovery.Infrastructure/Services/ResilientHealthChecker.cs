using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Domain.Entities;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ServiceDiscovery.Infrastructure.Services;

/// <summary>
/// Resilient implementation of health checker with circuit breaker and retry logic using Polly
/// </summary>
public class ResilientHealthChecker : IHealthChecker
{
    private readonly IServiceDiscovery _discovery;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResilientHealthChecker> _logger;
    private readonly ResilientHealthCheckerOptions _options;

    // Per-service circuit breakers
    private readonly ConcurrentDictionary<string, ResiliencePipeline<HealthCheckResult>> _servicePipelines = new();

    public ResilientHealthChecker(
        IServiceDiscovery discovery,
        IHttpClientFactory httpClientFactory,
        ILogger<ResilientHealthChecker> logger,
        ResilientHealthCheckerOptions? options = null)
    {
        _discovery = discovery;
        _httpClient = httpClientFactory.CreateClient("HealthCheck");
        _logger = logger;
        _options = options ?? new ResilientHealthCheckerOptions();
    }

    /// <summary>
    /// Gets or creates a resilience pipeline for a specific service
    /// </summary>
    private ResiliencePipeline<HealthCheckResult> GetOrCreatePipeline(string serviceId)
    {
        return _servicePipelines.GetOrAdd(serviceId, id => CreateResiliencePipeline(id));
    }

    /// <summary>
    /// Creates a resilience pipeline with retry, circuit breaker, and timeout
    /// </summary>
    private ResiliencePipeline<HealthCheckResult> CreateResiliencePipeline(string serviceId)
    {
        return new ResiliencePipelineBuilder<HealthCheckResult>()
            // Timeout policy - outermost
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds),
                OnTimeout = args =>
                {
                    _logger.LogWarning("Health check timeout for service {ServiceId} after {Timeout}s",
                        serviceId, _options.TimeoutSeconds);
                    return default;
                }
            })
            // Retry policy
            .AddRetry(new RetryStrategyOptions<HealthCheckResult>
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(_options.RetryDelayMilliseconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HealthCheckResult>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(r => r.Status == Domain.Enums.HealthStatus.Unhealthy),
                OnRetry = args =>
                {
                    _logger.LogDebug("Retry attempt {Attempt} for service {ServiceId}",
                        args.AttemptNumber, serviceId);
                    return default;
                }
            })
            // Circuit breaker policy - innermost
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HealthCheckResult>
            {
                FailureRatio = _options.CircuitBreakerFailureRatio,
                SamplingDuration = TimeSpan.FromSeconds(_options.CircuitBreakerSamplingDurationSeconds),
                MinimumThroughput = _options.CircuitBreakerMinimumThroughput,
                BreakDuration = TimeSpan.FromSeconds(_options.CircuitBreakerBreakDurationSeconds),
                ShouldHandle = new PredicateBuilder<HealthCheckResult>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(r => r.Status == Domain.Enums.HealthStatus.Unhealthy),
                OnOpened = args =>
                {
                    _logger.LogWarning("Circuit breaker OPENED for service {ServiceId}. Break duration: {Duration}s",
                        serviceId, _options.CircuitBreakerBreakDurationSeconds);
                    return default;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker CLOSED for service {ServiceId}", serviceId);
                    return default;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("Circuit breaker HALF-OPEN for service {ServiceId}", serviceId);
                    return default;
                }
            })
            .Build();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(ServiceInstance instance, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instance.HealthCheckUrl))
        {
            return HealthCheckResult.Degraded(instance.Id, "No health check URL configured");
        }

        var pipeline = GetOrCreatePipeline(instance.Id);

        try
        {
            return await pipeline.ExecuteAsync(async token =>
            {
                return await PerformHealthCheckAsync(instance, token);
            }, cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit breaker is open for service {ServiceId}, returning unhealthy", instance.Id);
            return HealthCheckResult.Unhealthy(instance.Id, "Circuit breaker is open - service temporarily unavailable");
        }
        catch (TimeoutRejectedException)
        {
            _logger.LogWarning("Health check timed out for service {ServiceId}", instance.Id);
            return HealthCheckResult.Unhealthy(instance.Id, $"Health check timed out after {_options.TimeoutSeconds}s");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking health for service {ServiceId}", instance.Id);
            return HealthCheckResult.Unhealthy(instance.Id, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs the actual HTTP health check
    /// </summary>
    private async Task<HealthCheckResult> PerformHealthCheckAsync(ServiceInstance instance, CancellationToken cancellationToken)
    {
        var healthCheckUrl = BuildHealthCheckUrl(instance);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await _httpClient.GetAsync(healthCheckUrl, cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Health check successful for {ServiceId} in {Duration}ms",
                    instance.Id, stopwatch.ElapsedMilliseconds);
                return HealthCheckResult.Healthy(instance.Id, stopwatch.ElapsedMilliseconds, (int)response.StatusCode);
            }

            _logger.LogWarning("Health check failed for {ServiceId}: {StatusCode}",
                instance.Id, response.StatusCode);
            return HealthCheckResult.Unhealthy(instance.Id, $"Health check returned {response.StatusCode}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Health check timed out for {ServiceId} after {Duration}ms",
                instance.Id, stopwatch.ElapsedMilliseconds);
            throw; // Rethrow for retry policy to handle
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "HTTP error during health check for {ServiceId}", instance.Id);
            throw; // Rethrow for retry policy to handle
        }
    }

    private static string BuildHealthCheckUrl(ServiceInstance instance)
    {
        var scheme = instance.Port == 443 ? "https" : "http";
        var path = instance.HealthCheckUrl?.TrimStart('/') ?? "health";
        return $"{scheme}://{instance.Host}:{instance.Port}/{path}";
    }

    public async Task<List<HealthCheckResult>> CheckServiceHealthAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var instances = await _discovery.GetServiceInstancesAsync(serviceName, cancellationToken);
        var tasks = instances.Select(instance => CheckHealthAsync(instance, cancellationToken));

        return (await Task.WhenAll(tasks)).ToList();
    }

    public async Task<List<HealthCheckResult>> CheckAllServicesHealthAsync(CancellationToken cancellationToken = default)
    {
        var serviceNames = await _discovery.GetServiceNamesAsync(cancellationToken);
        var allResults = new List<HealthCheckResult>();

        foreach (var serviceName in serviceNames)
        {
            var results = await CheckServiceHealthAsync(serviceName, cancellationToken);
            allResults.AddRange(results);
        }

        return allResults;
    }

    /// <summary>
    /// Gets the current state of the circuit breaker for a specific service
    /// </summary>
    public CircuitBreakerState GetCircuitBreakerState(string serviceId)
    {
        if (!_servicePipelines.TryGetValue(serviceId, out _))
        {
            return CircuitBreakerState.Unknown;
        }

        // Note: Polly v8 doesn't expose circuit state directly like v7
        // This would need custom implementation to track state
        return CircuitBreakerState.Closed;
    }

    /// <summary>
    /// Resets the circuit breaker for a specific service
    /// </summary>
    public void ResetCircuitBreaker(string serviceId)
    {
        if (_servicePipelines.TryRemove(serviceId, out _))
        {
            _logger.LogInformation("Circuit breaker reset for service {ServiceId}", serviceId);
        }
    }

    /// <summary>
    /// Resets all circuit breakers
    /// </summary>
    public void ResetAllCircuitBreakers()
    {
        _servicePipelines.Clear();
        _logger.LogInformation("All circuit breakers reset");
    }
}

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitBreakerState
{
    Unknown,
    Closed,
    Open,
    HalfOpen
}

/// <summary>
/// Configuration options for resilient health checker
/// </summary>
public class ResilientHealthCheckerOptions
{
    /// <summary>
    /// Maximum number of retry attempts (default: 3)
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Initial delay between retries in milliseconds (default: 200)
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 200;

    /// <summary>
    /// Timeout for health check in seconds (default: 10)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Failure ratio threshold to open circuit breaker (default: 0.5 = 50%)
    /// </summary>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    /// <summary>
    /// Sampling duration for circuit breaker in seconds (default: 30)
    /// </summary>
    public int CircuitBreakerSamplingDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Minimum throughput before circuit breaker can open (default: 3)
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 3;

    /// <summary>
    /// Duration circuit breaker stays open in seconds (default: 30)
    /// </summary>
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;
}
