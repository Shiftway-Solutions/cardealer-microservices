using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using ServiceDiscovery.Application.Interfaces;
using ServiceDiscovery.Infrastructure.Services;

namespace ServiceDiscovery.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering resilient health check services
/// </summary>
public static class ResilientHealthCheckExtensions
{
    /// <summary>
    /// Adds resilient health check services with circuit breaker and retry policies
    /// </summary>
    public static IServiceCollection AddResilientHealthCheck(
        this IServiceCollection services,
        Action<ResilientHealthCheckerOptions>? configureOptions = null)
    {
        var options = new ResilientHealthCheckerOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);

        // Register HttpClient with Polly policies for external calls
        services.AddHttpClient("HealthCheck")
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds + 5); // Extra buffer
            })
            .AddPolicyHandler(GetRetryPolicy(options))
            .AddPolicyHandler(GetCircuitBreakerPolicy(options));

        // Register resilient health checker
        services.AddSingleton<IHealthChecker, ResilientHealthChecker>();

        return services;
    }

    /// <summary>
    /// Adds resilient health check services with default options
    /// </summary>
    public static IServiceCollection AddResilientHealthCheck(this IServiceCollection services)
    {
        return services.AddResilientHealthCheck(null);
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ResilientHealthCheckerOptions options)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                options.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromMilliseconds(
                    options.RetryDelayMilliseconds * Math.Pow(2, retryAttempt - 1)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ResilientHealthCheckerOptions options)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: options.CircuitBreakerMinimumThroughput,
                durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreakerBreakDurationSeconds));
    }
}
