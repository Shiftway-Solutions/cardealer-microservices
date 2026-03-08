using FluentValidation;
using RecoAgent.Application.Features.Feedback.Commands;
using RecoAgent.Application.Validators;

namespace RecoAgent.Application.Features.Feedback.Validators;

public class RecordFeedbackCommandValidator : AbstractValidator<RecordFeedbackCommand>
{
    private static readonly string[] ValidFeedbackTypes = { "thumbs_up", "thumbs_down", "dismiss", "click", "neutral" };

    public RecordFeedbackCommandValidator()
    {
        RuleFor(x => x.Feedback)
            .NotNull().WithMessage("Feedback es requerido.");

        RuleFor(x => x.Feedback.UserId)
            .NotEmpty().WithMessage("UserId es requerido.")
            .MaximumLength(128)
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.Feedback.VehiculoId)
            .NotEmpty().WithMessage("VehiculoId es requerido.")
            .MaximumLength(128)
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.Feedback.FeedbackType)
            .NotEmpty()
            .Must(type => ValidFeedbackTypes.Contains(type))
            .WithMessage("FeedbackType debe ser: thumbs_up, thumbs_down, dismiss, click o neutral.");

        RuleFor(x => x.Feedback.SessionId)
            .MaximumLength(128)
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Feedback.SessionId));
    }
}
