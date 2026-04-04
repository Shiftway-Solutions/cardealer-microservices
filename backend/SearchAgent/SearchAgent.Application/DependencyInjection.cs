using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SearchAgent.Application.Behaviors;
using SearchAgent.Application.Services;

namespace SearchAgent.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Background warmup: pre-heats the Claude API TCP connection + system-prompt cache
        // so first real-user requests don't incur cold-start latency (UF-168 fix)
        services.AddHostedService<SearchAgentWarmupService>();

        return services;
    }
}
