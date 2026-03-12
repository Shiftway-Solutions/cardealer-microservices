using CarDealer.Shared.Resilience.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace CarDealer.Shared.Resilience.Policies;

/// <summary>
/// Fábrica de estrategias de resiliencia
/// </summary>
public static class ResilienceStrategyFactory
{
    /// <summary>
    /// Crea una estrategia de retry con backoff exponencial
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateRetryPipeline(
        RetryOptions options,
        ILogger? logger = null)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = options.MaxRetries,
                Delay = TimeSpan.FromSeconds(options.DelaySeconds),
                BackoffType = options.UseExponentialBackoff
                    ? DelayBackoffType.Exponential
                    : DelayBackoffType.Constant,
                UseJitter = options.UseJitter,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => options.RetryStatusCodes.Contains((int)response.StatusCode)),
                OnRetry = args =>
                {
                    logger?.LogWarning(
                        "Retry attempt {Attempt} after {Delay}ms. Status: {Status}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Result?.StatusCode);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Crea una estrategia de circuit breaker
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateCircuitBreakerPipeline(
        CircuitBreakerOptions options,
        ILogger? logger = null)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = options.FailureRatio,
                MinimumThroughput = options.MinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(options.SamplingDurationSeconds),
                BreakDuration = TimeSpan.FromSeconds(options.BreakDurationSeconds),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => !response.IsSuccessStatusCode),
                OnOpened = args =>
                {
                    logger?.LogWarning(
                        "Circuit breaker OPENED for {Duration}s due to {BreakDuration} failures",
                        options.BreakDurationSeconds,
                        args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    logger?.LogInformation("Circuit breaker CLOSED - normal operation resumed");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    logger?.LogInformation("Circuit breaker HALF-OPEN - testing with next request");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Crea una estrategia de timeout
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateTimeoutPipeline(
        TimeoutOptions options,
        ILogger? logger = null)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                OnTimeout = args =>
                {
                    logger?.LogWarning(
                        "Request timed out after {Timeout}s",
                        options.TimeoutSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Crea un pipeline combinado con todas las estrategias
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateCombinedPipeline(
        ResilienceOptions options,
        ILogger? logger = null)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

        // Timeout total (wrapping)
        builder.AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(options.Timeout.TotalTimeoutSeconds),
            OnTimeout = args =>
            {
                logger?.LogError(
                    "Total timeout exceeded after {Timeout}s",
                    options.Timeout.TotalTimeoutSeconds);
                return ValueTask.CompletedTask;
            }
        });

        // Retry con backoff exponencial
        builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = options.Retry.MaxRetries,
            Delay = TimeSpan.FromSeconds(options.Retry.DelaySeconds),
            BackoffType = options.Retry.UseExponentialBackoff
                ? DelayBackoffType.Exponential
                : DelayBackoffType.Constant,
            UseJitter = options.Retry.UseJitter,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>()
                .Handle<BrokenCircuitException>()
                .HandleResult(response => options.Retry.RetryStatusCodes.Contains((int)response.StatusCode)),
            OnRetry = args =>
            {
                logger?.LogWarning(
                    "Retry {Attempt}/{Max} after {Delay}ms",
                    args.AttemptNumber,
                    options.Retry.MaxRetries,
                    args.RetryDelay.TotalMilliseconds);
                return ValueTask.CompletedTask;
            }
        });

        // Circuit breaker
        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            FailureRatio = options.CircuitBreaker.FailureRatio,
            MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
            SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDurationSeconds),
            BreakDuration = TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds),
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>()
                .HandleResult(response => !response.IsSuccessStatusCode),
            OnOpened = args =>
            {
                logger?.LogWarning("Circuit OPENED for {Duration}s", options.CircuitBreaker.BreakDurationSeconds);
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                logger?.LogInformation("Circuit CLOSED");
                return ValueTask.CompletedTask;
            }
        });

        // Timeout por request
        builder.AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(options.Timeout.TimeoutSeconds),
            OnTimeout = args =>
            {
                logger?.LogWarning("Request timeout after {Timeout}s", options.Timeout.TimeoutSeconds);
                return ValueTask.CompletedTask;
            }
        });

        return builder.Build();
    }
}
