using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupportAgent.Domain.Interfaces;
using SupportAgent.Infrastructure.Persistence;
using SupportAgent.Infrastructure.Repositories;
using SupportAgent.Infrastructure.Services;
using CarDealer.Shared.Resilience.Extensions;

namespace SupportAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<SupportAgentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<ISupportAgentConfigRepository, SupportAgentConfigRepository>();

        // FAQ Response Cache (in-memory, 5-min TTL, bounded at ~50 MB)
        services.AddMemoryCache(options => options.SizeLimit = 50_000_000);
        services.AddSingleton<IFaqResponseCache, InMemoryFaqResponseCache>();

        // Claude API HttpClient
        services.AddHttpClient("ClaudeApi", client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/");
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }).AddStandardResilience(configuration);
        services.AddScoped<IClaudeSupportService, ClaudeSupportService>();

        return services;
    }
}
