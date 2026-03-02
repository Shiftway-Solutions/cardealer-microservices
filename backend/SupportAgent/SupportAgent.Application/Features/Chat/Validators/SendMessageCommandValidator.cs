using FluentValidation;
using SupportAgent.Application.Features.Chat.Commands;

namespace SupportAgent.Application.Features.Chat.Validators;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("El mensaje no puede estar vacío.")
            .MinimumLength(1).WithMessage("El mensaje debe tener al menos 1 carácter.")
            .MaximumLength(2000).WithMessage("El mensaje no puede exceder 2000 caracteres.");

        RuleFor(x => x.SessionId)
            .MaximumLength(64).When(x => x.SessionId != null)
            .WithMessage("El ID de sesión no puede exceder 64 caracteres.");
    }
}
