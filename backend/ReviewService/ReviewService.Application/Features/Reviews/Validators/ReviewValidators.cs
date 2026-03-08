using FluentValidation;
using ReviewService.Application.Features.Reviews.Commands;
using ReviewService.Application.Validators;

namespace ReviewService.Application.Features.Reviews.Validators;

/// <summary>
/// Validador para CreateReviewCommand
/// </summary>
public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.BuyerId)
            .NotEmpty()
            .WithMessage("BuyerId es requerido");

        RuleFor(x => x.SellerId)
            .NotEmpty()
            .WithMessage("SellerId es requerido");

        RuleFor(x => x.BuyerId)
            .NotEqual(x => x.SellerId)
            .WithMessage("No puedes dejarte una review a ti mismo");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("El rating debe estar entre 1 y 5 estrellas");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("El título es requerido")
            .MaximumLength(200)
            .WithMessage("El título no puede exceder 200 caracteres")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("El contenido es requerido")
            .MinimumLength(10)
            .WithMessage("El contenido debe tener al menos 10 caracteres")
            .MaximumLength(2000)
            .WithMessage("El contenido no puede exceder 2000 caracteres")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.BuyerName)
            .NotEmpty()
            .WithMessage("El nombre del comprador es requerido")
            .MaximumLength(100)
            .WithMessage("El nombre no puede exceder 100 caracteres")
            .NoSqlInjection()
            .NoXss();
    }
}

/// <summary>
/// Validador para UpdateReviewCommand
/// </summary>
public class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("ReviewId es requerido");

        RuleFor(x => x.BuyerId)
            .NotEmpty()
            .WithMessage("BuyerId es requerido");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("El rating debe estar entre 1 y 5 estrellas");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("El título es requerido")
            .MaximumLength(200)
            .WithMessage("El título no puede exceder 200 caracteres")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("El contenido es requerido")
            .MinimumLength(10)
            .WithMessage("El contenido debe tener al menos 10 caracteres")
            .MaximumLength(2000)
            .WithMessage("El contenido no puede exceder 2000 caracteres")
            .NoSqlInjection()
            .NoXss();
    }
}

/// <summary>
/// Validador para ModerateReviewCommand
/// </summary>
public class ModerateReviewCommandValidator : AbstractValidator<ModerateReviewCommand>
{
    public ModerateReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("ReviewId es requerido");

        RuleFor(x => x.ModeratorId)
            .NotEmpty()
            .WithMessage("ModeratorId es requerido");

        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .When(x => !x.IsApproved)
            .WithMessage("La razón de rechazo es requerida cuando se rechaza una review")
            .MaximumLength(500)
            .WithMessage("La razón de rechazo no puede exceder 500 caracteres")
            .NoSqlInjection()
            .NoXss();
    }
}