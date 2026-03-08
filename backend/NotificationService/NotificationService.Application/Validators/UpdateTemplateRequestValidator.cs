using FluentValidation;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Validators;

public class UpdateTemplateRequestValidator : AbstractValidator<UpdateTemplateRequest>
{
    public UpdateTemplateRequestValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required.")
            .MaximumLength(500).WithMessage("Subject must not exceed 500 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .MaximumLength(50000).WithMessage("Body must not exceed 50000 characters.")
            .NoSqlInjection();
        // Note: NoXss() intentionally skipped on Body — HTML template content

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Tags)
            .MaximumLength(500).WithMessage("Tags must not exceed 500 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Tags));

        RuleFor(x => x.PreviewData)
            .MaximumLength(5000).WithMessage("Preview data must not exceed 5000 characters.")
            .NoSqlInjection()
            .When(x => !string.IsNullOrWhiteSpace(x.PreviewData));
    }
}
