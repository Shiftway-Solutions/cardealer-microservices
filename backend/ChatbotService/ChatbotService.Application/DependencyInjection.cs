using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ChatbotService.Application.Behaviors;

namespace ChatbotService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Register MediatR handlers from this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Register all FluentValidation validators (SecurityValidators applied via these)
        services.AddValidatorsFromAssembly(assembly);

        // Register MediatR validation pipeline behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
