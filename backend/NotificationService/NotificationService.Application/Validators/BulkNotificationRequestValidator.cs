using FluentValidation;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Validators;

public class BulkNotificationRequestValidator : AbstractValidator<BulkNotificationRequest>
{
    public BulkNotificationRequestValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("At least one user ID is required.")
            .Must(ids => ids.Count <= 1000).WithMessage("Cannot send to more than 1000 users at once.");

        RuleForEach(x => x.UserIds)
            .NotEmpty().WithMessage("User ID cannot be empty.")
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
    }
}
