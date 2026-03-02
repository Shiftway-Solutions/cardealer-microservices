using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace SearchAgent.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CarDealer.Shared.Validation.ValidationBehavior<,>));

        return services;
    }
}
