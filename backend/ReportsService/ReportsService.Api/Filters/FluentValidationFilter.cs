using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ReportsService.Api.Filters;

/// <summary>
/// Action filter that automatically validates request body DTOs using FluentValidation.
/// Since ReportsService does not use MediatR pipeline behaviors, this filter ensures
/// all [FromBody] parameters are validated before reaching controller actions.
/// Returns RFC 7807 ProblemDetails on validation failure.
/// </summary>
public class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments)
        {
            if (argument.Value is null)
                continue;

            var argumentType = argument.Value.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
                continue;

            var validationContext = new ValidationContext<object>(argument.Value);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                var problemDetails = new ValidationProblemDetails(errors)
                {
                    Type = "https://tools.ietf.org/html/rfc7807",
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = context.HttpContext.Request.Path
                };

                context.Result = new BadRequestObjectResult(problemDetails);
                return;
            }
        }

        await next();
    }
}
