// =============================================================================
// LLM Gateway — Dependency Injection Extensions
//
// Usage in any service's Program.cs:
//   builder.Services.AddLlmGateway(builder.Configuration);
//
// Configuration in appsettings.json:
//   "LlmGateway": {
//     "ProviderTimeout": "00:00:00.500",
//     "EnableCacheFallback": true,
//     "Claude": { "ApiKey": "sk-ant-...", "Model": "claude-sonnet-4-20250514" },
//     "Gemini": { "ApiKey": "AIza...", "Model": "gemini-1.5-flash" },
//     "Llama": { "BaseUrl": "http://llm-server:8000", "Model": "llama-3.1-70b" },
//     "RedisConnectionString": "redis:6379"
//   }
// =============================================================================

using CarDealer.Shared.LlmGateway.Abstractions;
using CarDealer.Shared.LlmGateway.Configuration;
using CarDealer.Shared.LlmGateway.Providers;
using CarDealer.Shared.LlmGateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using StackExchange.Redis;

namespace CarDealer.Shared.LlmGateway.Extensions;

public static class LlmGatewayExtensions
{
    // Polly requires timeout to be between 10ms and 24h. Clamp to avoid startup failures
    // caused by misconfigured appsettings (e.g. integer 500 parsed as 500 ticks = ~0.05ms).
    private static readonly TimeSpan _minPollyTimeout = TimeSpan.FromMilliseconds(100);

    private static TimeSpan SafeTimeout(TimeSpan? overrideValue, TimeSpan defaultValue)
    {
        var t = overrideValue ?? defaultValue;
        return t < _minPollyTimeout ? _minPollyTimeout : t;
    }

    /// <summary>
    /// Register the LLM Gateway with cascade fallback and all configured providers.
    /// </summary>
    public static IServiceCollection AddLlmGateway(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var section = configuration.GetSection(LlmGatewayOptions.SectionName);
        services.Configure<LlmGatewayOptions>(section);

        var options = section.Get<LlmGatewayOptions>() ?? new LlmGatewayOptions();

        // ── REDIS (for cache fallback) ───────────────────────────────────────
        if (options.EnableCacheFallback && !string.IsNullOrEmpty(options.RedisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                try
                {
                    return ConnectionMultiplexer.Connect(options.RedisConnectionString);
                }
                catch
                {
                    // Redis unavailable — gateway will work without cache
                    return null!;
                }
            });
        }

        // ── CLAUDE PROVIDER ──────────────────────────────────────────────────
        if (options.Claude.Enabled)
        {
            services.AddHttpClient<ClaudeProvider>("LlmGateway-Claude")
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    MaxConnectionsPerServer = 20
                })
                .AddResilienceHandler("claude-resilience", builder =>
                {
                    builder.AddTimeout(new TimeoutStrategyOptions
                    {
                        Timeout = SafeTimeout(options.Claude.Timeout, options.ProviderTimeout)
                    });
                });

            services.AddSingleton<ILlmProvider>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("LlmGateway-Claude");
                var opts = sp.GetRequiredService<IOptions<LlmGatewayOptions>>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ClaudeProvider>>();
                return new ClaudeProvider(httpClient, opts, logger);
            });
        }

        // ── GEMINI PROVIDER ──────────────────────────────────────────────────
        if (options.Gemini.Enabled)
        {
            services.AddHttpClient<GeminiProvider>("LlmGateway-Gemini")
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    MaxConnectionsPerServer = 20
                })
                .AddResilienceHandler("gemini-resilience", builder =>
                {
                    builder.AddTimeout(new TimeoutStrategyOptions
                    {
                        Timeout = SafeTimeout(options.Gemini.Timeout, options.ProviderTimeout)
                    });
                });

            services.AddSingleton<ILlmProvider>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("LlmGateway-Gemini");
                var opts = sp.GetRequiredService<IOptions<LlmGatewayOptions>>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<GeminiProvider>>();
                return new GeminiProvider(httpClient, opts, logger);
            });
        }

        // ── LLAMA PROVIDER ───────────────────────────────────────────────────
        if (options.Llama.Enabled)
        {
            services.AddHttpClient<LlamaProvider>("LlmGateway-Llama")
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    MaxConnectionsPerServer = 10
                })
                .AddResilienceHandler("llama-resilience", builder =>
                {
                    builder.AddTimeout(new TimeoutStrategyOptions
                    {
                        Timeout = SafeTimeout(options.Llama.Timeout, TimeSpan.FromSeconds(15))
                    });
                });

            services.AddSingleton<ILlmProvider>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("LlmGateway-Llama");
                var opts = sp.GetRequiredService<IOptions<LlmGatewayOptions>>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LlamaProvider>>();
                return new LlamaProvider(httpClient, opts, logger);
            });
        }

        // ── GATEWAY ORCHESTRATOR ─────────────────────────────────────────────
        services.AddSingleton<ILlmGateway, LlmGatewayService>();
        // ── API COST TRACKER ─────────────────────────────────────────────
        if (options.CostAlerts.Enabled)
        {
            services.AddSingleton<IApiCostTracker, ApiCostTracker>();
        }
        return services;
    }

    /// <summary>
    /// Register the LLM Gateway health check endpoint.
    /// </summary>
    public static IServiceCollection AddLlmGatewayHealthCheck(this IServiceCollection services)
    {
        // Can be extended with IHealthCheck implementations
        return services;
    }
}
