using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SearchAgent.Domain.Interfaces;
using SearchAgent.Infrastructure.Persistence;
using SearchAgent.Infrastructure.Repositories;
using SearchAgent.Infrastructure.Services;
using CarDealer.Shared.Resilience.Extensions;

namespace SearchAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<SearchAgentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<ISearchAgentConfigRepository, SearchAgentConfigRepository>();
        services.AddScoped<ISearchQueryRepository, SearchQueryRepository>();

        // Claude API HTTP Client
        services.AddHttpClient("ClaudeApi", client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }).AddStandardResilience(configuration);
        services.AddScoped<IClaudeSearchService, ClaudeSearchService>();

        // Redis Cache
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "SearchAgent:";
            });
        }
        else
        {
            // Fallback to in-memory cache for development
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<ISearchCacheService, SearchCacheService>();

        // In-memory cache for config and other short-lived data (avoids DB round-trip per request)
        services.AddMemoryCache();

        return services;
    }
}
