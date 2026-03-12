using CarDealer.Shared.Idempotency.Interfaces;
using CarDealer.Shared.Idempotency.Middleware;
using CarDealer.Shared.Idempotency.Models;
using CarDealer.Shared.Idempotency.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CarDealer.Shared.Idempotency.Extensions;

/// <summary>
/// Extension methods for configuring idempotency
/// </summary>
public static class IdempotencyExtensions
{
    /// <summary>
    /// Adds idempotency services using Redis
    /// </summary>
    public static IServiceCollection AddIdempotency(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new IdempotencyOptions();
        configuration.GetSection(IdempotencyOptions.SectionName).Bind(options);
        services.Configure<IdempotencyOptions>(configuration.GetSection(IdempotencyOptions.SectionName));

        // Check if Redis is already registered
        var existingRedis = services.FirstOrDefault(s => s.ServiceType == typeof(IConnectionMultiplexer));
        if (existingRedis == null)
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                return ConnectionMultiplexer.Connect(options.RedisConnection);
            });
        }

        services.AddScoped<IIdempotencyClient, RedisIdempotencyClient>();

        return services;
    }

    /// <summary>
    /// Adds idempotency services with existing Redis connection
    /// </summary>
    public static IServiceCollection AddIdempotency(
        this IServiceCollection services,
        IConfiguration configuration,
        IConnectionMultiplexer existingRedisConnection)
    {
        services.Configure<IdempotencyOptions>(configuration.GetSection(IdempotencyOptions.SectionName));

        // Only add if not already registered
        var existingRedis = services.FirstOrDefault(s => s.ServiceType == typeof(IConnectionMultiplexer));
        if (existingRedis == null)
        {
            services.AddSingleton(existingRedisConnection);
        }

        services.AddScoped<IIdempotencyClient, RedisIdempotencyClient>();

        return services;
    }

    /// <summary>
    /// Adds idempotency with custom options
    /// </summary>
    public static IServiceCollection AddIdempotency(
        this IServiceCollection services,
        Action<IdempotencyOptions> configureOptions)
    {
        services.Configure(configureOptions);

        var options = new IdempotencyOptions();
        configureOptions(options);

        // Check if Redis is already registered
        var existingRedis = services.FirstOrDefault(s => s.ServiceType == typeof(IConnectionMultiplexer));
        if (existingRedis == null)
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                return ConnectionMultiplexer.Connect(options.RedisConnection);
            });
        }

        services.AddScoped<IIdempotencyClient, RedisIdempotencyClient>();

        return services;
    }

    /// <summary>
    /// Use idempotency middleware in the pipeline
    /// </summary>
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
    {
        return app.UseMiddleware<IdempotencyMiddleware>();
    }
}
