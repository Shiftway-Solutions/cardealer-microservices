using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecoAgent.Domain.Interfaces;
using RecoAgent.Infrastructure.Persistence;
using RecoAgent.Infrastructure.Repositories;
using RecoAgent.Infrastructure.Services;
using CarDealer.Shared.Resilience.Extensions;

namespace RecoAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<RecoAgentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IRecoAgentConfigRepository, RecoAgentConfigRepository>();
        services.AddScoped<IRecommendationLogRepository, RecommendationLogRepository>();

        // Claude API HTTP Client
        services.AddHttpClient("ClaudeApi", client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/");
            client.Timeout = TimeSpan.FromSeconds(45); // Higher timeout for Sonnet 4.5
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }).AddStandardResilience(configuration);
        services.AddScoped<IClaudeRecoService, ClaudeRecoService>();

        // Redis Cache
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "RecoAgent:";
            });
        }
        else
        {
            // Fallback to in-memory cache for development
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<IRecoCacheService, RecoCacheService>();

        return services;
    }
}
