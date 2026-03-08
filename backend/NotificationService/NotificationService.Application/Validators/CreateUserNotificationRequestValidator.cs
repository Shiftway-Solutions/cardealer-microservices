using FluentValidation;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Validators;

public class CreateUserNotificationRequestValidator : AbstractValidator<CreateUserNotificationRequest>
{
    public CreateUserNotificationRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.")
            .MaximumLength(100).WithMessage("User ID must not exceed 100 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Notification type is required.")
            .MaximumLength(100).WithMessage("Type must not exceed 100 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(2000).WithMessage("Message must not exceed 2000 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Icon)
            .MaximumLength(200).WithMessage("Icon must not exceed 200 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Icon));

        RuleFor(x => x.Link)
            .MaximumLength(500).WithMessage("Link must not exceed 500 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Link));

        RuleFor(x => x.DealerId)
            .MaximumLength(100).WithMessage("Dealer ID must not exceed 100 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.DealerId));

        RuleFor(x => x.ExpiresAt)
            .MaximumLength(30).WithMessage("ExpiresAt must not exceed 30 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.ExpiresAt));
    }
}
