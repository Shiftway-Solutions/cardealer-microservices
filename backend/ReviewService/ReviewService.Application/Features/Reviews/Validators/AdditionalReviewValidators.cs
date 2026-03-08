using FluentValidation;
using ReviewService.Application.Features.Reviews.Commands;
using ReviewService.Application.Validators;

namespace ReviewService.Application.Features.Reviews.Validators;

/// <summary>
/// Validador para RespondToReviewCommand.
/// El vendedor responde a una review del comprador.
/// </summary>
public class RespondToReviewCommandValidator : AbstractValidator<RespondToReviewCommand>
{
    public RespondToReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("ReviewId es requerido");

        RuleFor(x => x.SellerId)
            .NotEmpty()
            .WithMessage("SellerId es requerido");

        RuleFor(x => x.ResponseText)
            .NotEmpty()
            .WithMessage("El texto de respuesta es requerido")
            .MinimumLength(5)
            .WithMessage("La respuesta debe tener al menos 5 caracteres")
            .MaximumLength(1000)
            .WithMessage("La respuesta no puede exceder 1000 caracteres")
            .NoSqlInjection()
            .NoXss();
    }
}

/// <summary>
/// Validador para VoteHelpfulCommand.
/// Un usuario vota si una review fue útil.
/// </summary>
public class VoteHelpfulCommandValidator : AbstractValidator<VoteHelpfulCommand>
{
    public VoteHelpfulCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("ReviewId es requerido");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId es requerido");

        RuleFor(x => x.UserIpAddress)
            .MaximumLength(45)
            .WithMessage("La dirección IP no puede exceder 45 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.UserIpAddress));

        RuleFor(x => x.UserAgent)
            .MaximumLength(500)
            .WithMessage("El User-Agent no puede exceder 500 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.UserAgent));
    }
}

/// <summary>
/// Validador para ReportReviewCommand.
/// Un usuario reporta una review inapropiada.
/// </summary>
public class ReportReviewCommandValidator : AbstractValidator<ReportReviewCommand>
{
    public ReportReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("ReviewId es requerido");

        RuleFor(x => x.ReportedByUserId)
            .NotEmpty()
            .WithMessage("ReportedByUserId es requerido");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("La razón del reporte es requerida")
            .MinimumLength(10)
            .WithMessage("La razón debe tener al menos 10 caracteres")
            .MaximumLength(500)
            .WithMessage("La razón no puede exceder 500 caracteres")
            .NoSqlInjection()
            .NoXss();
    }
}

/// <summary>
/// Validador para SendReviewRequestCommand.
/// Envía una solicitud de review a un comprador después de una compra.
/// </summary>
public class SendReviewRequestCommandValidator : AbstractValidator<SendReviewRequestCommand>
{
    public SendReviewRequestCommandValidator()
    {
        RuleFor(x => x.BuyerId)
            .NotEmpty()
            .WithMessage("BuyerId es requerido");

        RuleFor(x => x.SellerId)
            .NotEmpty()
            .WithMessage("SellerId es requerido");

        RuleFor(x => x.VehicleId)
            .NotEmpty()
            .WithMessage("VehicleId es requerido");

        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("OrderId es requerido");

        RuleFor(x => x.BuyerEmail)
            .NotEmpty()
            .WithMessage("El email del comprador es requerido")
            .EmailAddress()
            .WithMessage("El email del comprador no es válido")
            .MaximumLength(254)
            .WithMessage("El email no puede exceder 254 caracteres")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.BuyerName)
            .NotEmpty()
            .WithMessage("El nombre del comprador es requerido")
            .MaximumLength(100)
            .WithMessage("El nombre no puede exceder 100 caracteres")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.VehicleTitle)
            .NotEmpty()
            .WithMessage("El título del vehículo es requerido")
            .MaximumLength(200)
            .WithMessage("El título del vehículo no puede exceder 200 caracteres")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.SellerName)
            .NotEmpty()
            .WithMessage("El nombre del vendedor es requerido")
            .MaximumLength(100)
            .WithMessage("El nombre del vendedor no puede exceder 100 caracteres")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.PurchaseDate)
            .NotEmpty()
            .WithMessage("La fecha de compra es requerida")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("La fecha de compra no puede ser en el futuro");
    }
}
