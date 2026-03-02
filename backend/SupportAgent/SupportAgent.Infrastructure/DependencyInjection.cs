using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupportAgent.Domain.Interfaces;
using SupportAgent.Infrastructure.Persistence;
using SupportAgent.Infrastructure.Repositories;
using SupportAgent.Infrastructure.Services;

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

        // Claude API HttpClient
        services.AddHttpClient("ClaudeApi", client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/");
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddScoped<IClaudeSupportService, ClaudeSupportService>();

        return services;
    }
}
