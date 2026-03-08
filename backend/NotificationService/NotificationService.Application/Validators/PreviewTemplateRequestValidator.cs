using FluentValidation;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Validators;

public class PreviewTemplateRequestValidator : AbstractValidator<PreviewTemplateRequest>
{
    public PreviewTemplateRequestValidator()
    {
        RuleFor(x => x.TemplateId)
            .MaximumLength(50).WithMessage("Template ID must not exceed 50 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.TemplateId));

        RuleFor(x => x.TemplateContent)
            .MaximumLength(50000).WithMessage("Template content must not exceed 50000 characters.")
            .NoSqlInjection()
            .When(x => !string.IsNullOrWhiteSpace(x.TemplateContent));

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.TemplateId) || !string.IsNullOrWhiteSpace(x.TemplateContent))
            .WithMessage("Either TemplateId or TemplateContent must be provided.");
    }
}
