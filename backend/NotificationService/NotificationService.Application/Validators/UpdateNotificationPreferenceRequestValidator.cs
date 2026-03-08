using FluentValidation;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Validators;

public class UpdateNotificationPreferenceRequestValidator : AbstractValidator<UpdateNotificationPreferenceRequest>
{
    public UpdateNotificationPreferenceRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Preference type is required.")
            .MaximumLength(100).WithMessage("Type must not exceed 100 characters.")
            .NoSqlInjection().NoXss();

        RuleForEach(x => x.Channels)
            .MaximumLength(50).WithMessage("Channel name must not exceed 50 characters.")
            .NoSqlInjection().NoXss()
            .When(x => x.Channels != null && x.Channels.Any());
    }
}
