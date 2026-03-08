using FluentValidation;
using NotificationService.Application.Validators;
using NotificationService.Application.UseCases.GetNotifications;

namespace NotificationService.Application.UseCases.GetNotifications;

public class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsQueryValidator()
    {
        RuleFor(x => x.Request.Recipient)
            .MaximumLength(254)
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Request.Recipient));

        RuleFor(x => x.Request.Type)
            .MaximumLength(50)
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Request.Type));

        RuleFor(x => x.Request.Status)
            .MaximumLength(50)
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Request.Status));

        RuleFor(x => x.Request.Page)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.Request.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.Request)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("From date cannot be after To date");
    }
}