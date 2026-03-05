using CarDealer.Shared.RateLimiting.Interfaces;
using CarDealer.Shared.RateLimiting.Middleware;
using CarDealer.Shared.RateLimiting.Models;
using CarDealer.Shared.RateLimiting.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CarDealer.Shared.RateLimiting.Extensions;

/// <summary>
/// Extension methods for configuring rate limiting
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Adds rate limiting services using Redis
    /// </summary>
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new RateLimitOptions();
        configuration.GetSection(RateLimitOptions.SectionName).Bind(options);
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));

        if (options.Mode.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            // Use HTTP client to call RateLimitingService
            services.AddHttpClient<IRateLimitClient, HttpRateLimitClient>();
        }
        else
        {
            // Use direct Redis connection
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                return ConnectionMultiplexer.Connect(options.RedisConnection);
            });
            services.AddScoped<IRateLimitClient, RedisRateLimitClient>();
        }

        return services;
    }

    /// <summary>
    /// Adds rate limiting services with Redis connection already configured
    /// </summary>
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        IConnectionMultiplexer existingRedisConnection)
    {
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));
        services.AddSingleton(existingRedisConnection);
        services.AddScoped<IRateLimitClient, RedisRateLimitClient>();

        return services;
    }

    /// <summary>
    /// Adds rate limiting with custom options
    /// </summary>
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        Action<RateLimitOptions> configureOptions)
    {
        services.Configure(configureOptions);
        
        var options = new RateLimitOptions();
        configureOptions(options);

        if (options.Mode.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IRateLimitClient, HttpRateLimitClient>();
        }
        else
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                return ConnectionMultiplexer.Connect(options.RedisConnection);
            });
            services.AddScoped<IRateLimitClient, RedisRateLimitClient>();
        }

        return services;
    }

    /// <summary>
    /// Use rate limiting middleware in the pipeline
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitingMiddleware>();
    }

    /// <summary>
    /// Adds default API rate limiting policies
    /// </summary>
    public static RateLimitOptions AddDefaultApiPolicies(this RateLimitOptions options)
    {
        // Auth endpoints - general limit for all auth mutations
        options.Policies["auth"] = new EndpointRateLimitPolicy
        {
            Pattern = "POST:/api/auth/*",
            Limit = 10,
            WindowSeconds = 60,
            TierLimits = new Dictionary<string, int>
            {
                ["anonymous"] = 5,
                ["authenticated"] = 20
            }
        };

        // Login endpoint - strict limit to prevent brute-force / credential stuffing (OWASP)
        options.Policies["auth-login"] = new EndpointRateLimitPolicy
        {
            Pattern = "POST:/api/auth/login",
            Limit = 5,
            WindowSeconds = 60,
            TierLimits = new Dictionary<string, int>
            {
                ["anonymous"] = 5,
                ["authenticated"] = 5
            }
        };

        // Register endpoint - strict limit to prevent mass account creation
        options.Policies["auth-register"] = new EndpointRateLimitPolicy
        {
            Pattern = "POST:/api/auth/register",
            Limit = 3,
            WindowSeconds = 60,
            TierLimits = new Dictionary<string, int>
            {
                ["anonymous"] = 3
            }
        };

        // Forgot-password endpoint - strict limit to prevent email enumeration / abuse
        options.Policies["auth-forgot-password"] = new EndpointRateLimitPolicy
        {
            Pattern = "POST:/api/auth/forgot-password",
            Limit = 3,
            WindowSeconds = 60,
            TierLimits = new Dictionary<string, int>
            {
                ["anonymous"] = 3
            }
        };

        // Payment endpoints - very strict
        options.Policies["payments"] = new EndpointRateLimitPolicy
        {
            Pattern = "POST:/api/payments/*",
            Limit = 10,
            WindowSeconds = 60,
            TierLimits = new Dictionary<string, int>
            {
                ["anonymous"] = 0,
                ["authenticated"] = 10,
                ["dealer"] = 50
            }
        };

        // Vehicle listing - moderate
        options.Policies["vehicles-read"] = new EndpointRateLimitPolicy
        {
            Pattern = "GET:/api/vehicles/*",
            Limit = 200,
            WindowSeconds = 60,
            TierLimits = new Dictionary<string, int>
            {
                ["anonymous"] = 100,
                ["authenticated"] = 200,
                ["premium"] = 500
            }
        };

        // Vehicle create/update - moderate
        options.Policies["vehicles-write"] = new EndpointRateLimitPolicy
        {
            Pattern = "POST:/api/vehicles/*",
            Limit = 20,
            WindowSeconds = 60,
            TierLimits = new Dictionary<string, int>
            {
                ["authenticated"] = 20,
                ["dealer"] = 100
            }
        };

        // Search endpoints
        options.Policies["search"] = new EndpointRateLimitPolicy
        {
            Pattern = "GET:/api/search/*",
            Limit = 60,
            WindowSeconds = 60
        };

        return options;
    }
}
