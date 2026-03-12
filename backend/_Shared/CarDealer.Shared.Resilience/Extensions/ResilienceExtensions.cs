using CarDealer.Shared.Resilience.Configuration;
using CarDealer.Shared.Resilience.Policies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Polly;
using System.Threading.RateLimiting;

namespace CarDealer.Shared.Resilience.Extensions;

/// <summary>
/// Extensiones para agregar resiliencia a HttpClient.
/// Pipeline completo: Total Timeout → Bulkhead → Retry → Circuit Breaker → Per-Request Timeout
/// </summary>
public static class ResilienceExtensions
{
    /// <summary>
    /// Agrega un HttpClient con políticas de resiliencia estándar (lectura de config)
    /// </summary>
    public static IHttpClientBuilder AddResilientHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? clientName = null,
        Action<HttpClient>? configureClient = null)
        where TClient : class
        where TImplementation : class, TClient
    {
        var options = new ResilienceOptions();
        configuration.GetSection(ResilienceOptions.SectionName).Bind(options);

        return services.AddResilientHttpClient<TClient, TImplementation>(
            clientName ?? typeof(TClient).Name,
            options,
            configureClient);
    }

    /// <summary>
    /// Agrega un HttpClient con políticas de resiliencia con opciones manuales.
    /// Pipeline: Total Timeout → Bulkhead → Retry (exp backoff + jitter) → Circuit Breaker → Per-Request Timeout
    /// </summary>
    public static IHttpClientBuilder AddResilientHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        string clientName,
        ResilienceOptions options,
        Action<HttpClient>? configureClient = null)
        where TClient : class
        where TImplementation : class, TClient
    {
        var builder = services.AddHttpClient<TClient, TImplementation>(clientName, client =>
        {
            configureClient?.Invoke(client);
        });

        if (options.Enabled)
        {
            builder.AddResilienceHandler($"{clientName}-resilience", (resilienceBuilder, context) =>
            {
                var logger = context.ServiceProvider.GetService<ILogger<TClient>>();
                BuildResiliencePipeline(resilienceBuilder, options, clientName, logger);
            });
        }

        return builder;
    }

    /// <summary>
    /// Agrega resiliencia a un HttpClientBuilder existente (lectura de config)
    /// </summary>
    public static IHttpClientBuilder AddStandardResilience(
        this IHttpClientBuilder builder,
        IConfiguration configuration)
    {
        var options = new ResilienceOptions();
        configuration.GetSection(ResilienceOptions.SectionName).Bind(options);

        return builder.AddStandardResilience(options);
    }

    /// <summary>
    /// Agrega resiliencia con opciones manuales
    /// </summary>
    public static IHttpClientBuilder AddStandardResilience(
        this IHttpClientBuilder builder,
        ResilienceOptions options)
    {
        if (!options.Enabled)
            return builder;

        builder.AddResilienceHandler("standard-resilience", resilienceBuilder =>
        {
            BuildResiliencePipeline(resilienceBuilder, options, builder.Name, logger: null);
        });

        return builder;
    }

    /// <summary>
    /// Agrega resiliencia con configuración fluent
    /// </summary>
    public static IHttpClientBuilder AddStandardResilience(
        this IHttpClientBuilder builder,
        Action<ResilienceOptions> configure)
    {
        var options = new ResilienceOptions();
        configure(options);
        return builder.AddStandardResilience(options);
    }

    /// <summary>
    /// Construye el pipeline de resiliencia completo.
    /// Orden: Total Timeout → Rate Limiter → Bulkhead → Retry → Circuit Breaker → Per-Request Timeout
    /// </summary>
    private static void BuildResiliencePipeline(
        ResiliencePipelineBuilder<HttpResponseMessage> resilienceBuilder,
        ResilienceOptions options,
        string clientName,
        ILogger? logger)
    {
        // 1. Total Timeout (wrapping) — límite absoluto para toda la operación
        resilienceBuilder.AddTimeout(new Polly.Timeout.TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(options.Timeout.TotalTimeoutSeconds),
            OnTimeout = args =>
            {
                logger?.LogError(
                    "[Resilience:{Client}] Total timeout exceeded after {Timeout}s",
                    clientName, options.Timeout.TotalTimeoutSeconds);
                return ValueTask.CompletedTask;
            }
        });

        // 2. Rate Limiter (outgoing) — controla velocidad de requests salientes
        if (options.RateLimiter.Enabled)
        {
            resilienceBuilder.AddRateLimiter(new Polly.RateLimiting.RateLimiterStrategyOptions
            {
                DefaultRateLimiterOptions = new ConcurrencyLimiterOptions
                {
                    PermitLimit = options.RateLimiter.PermitLimit,
                    QueueLimit = options.RateLimiter.QueueLimit
                },
                OnRejected = args =>
                {
                    logger?.LogWarning(
                        "[Resilience:{Client}] Rate limited — too many outgoing requests",
                        clientName);
                    return ValueTask.CompletedTask;
                }
            });
        }

        // 3. Bulkhead Isolation — limita concurrencia para evitar cascading failures
        if (options.Bulkhead.Enabled)
        {
            resilienceBuilder.AddConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = options.Bulkhead.MaxParallelization,
                QueueLimit = options.Bulkhead.MaxQueuingActions
            });
        }

        // 4. Retry con exponential backoff + jitter (evita thundering herd)
        resilienceBuilder.AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = options.Retry.MaxRetries,
            Delay = TimeSpan.FromSeconds(options.Retry.DelaySeconds),
            BackoffType = options.Retry.UseExponentialBackoff
                ? DelayBackoffType.Exponential
                : DelayBackoffType.Constant,
            UseJitter = options.Retry.UseJitter,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<Polly.Timeout.TimeoutRejectedException>()
                .HandleResult(response => options.Retry.RetryStatusCodes.Contains((int)response.StatusCode)),
            OnRetry = args =>
            {
                logger?.LogWarning(
                    "[Resilience:{Client}] Retry {Attempt}/{Max} after {Delay}ms — Status: {Status}",
                    clientName,
                    args.AttemptNumber,
                    options.Retry.MaxRetries,
                    args.RetryDelay.TotalMilliseconds,
                    args.Outcome.Result?.StatusCode);
                return ValueTask.CompletedTask;
            }
        });

        // 5. Circuit Breaker — detiene requests a servicio caído
        resilienceBuilder.AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            FailureRatio = options.CircuitBreaker.FailureRatio,
            MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
            SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDurationSeconds),
            BreakDuration = TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds),
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .HandleResult(response => !response.IsSuccessStatusCode),
            OnOpened = args =>
            {
                logger?.LogWarning(
                    "[Resilience:{Client}] Circuit OPENED — break for {Duration}s",
                    clientName, options.CircuitBreaker.BreakDurationSeconds);
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                logger?.LogInformation(
                    "[Resilience:{Client}] Circuit CLOSED — normal operation resumed",
                    clientName);
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                logger?.LogInformation(
                    "[Resilience:{Client}] Circuit HALF-OPEN — testing with next request",
                    clientName);
                return ValueTask.CompletedTask;
            }
        });

        // 6. Per-request Timeout — timeout individual por intento
        resilienceBuilder.AddTimeout(new Polly.Timeout.TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(options.Timeout.TimeoutSeconds),
            OnTimeout = args =>
            {
                logger?.LogWarning(
                    "[Resilience:{Client}] Request timeout after {Timeout}s",
                    clientName, options.Timeout.TimeoutSeconds);
                return ValueTask.CompletedTask;
            }
        });
    }
}
