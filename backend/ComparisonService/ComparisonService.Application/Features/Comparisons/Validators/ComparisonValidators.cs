using ComparisonService.Application.Features.Comparisons.Commands;
using ComparisonService.Application.Validators;
using FluentValidation;

namespace ComparisonService.Application.Features.Comparisons.Validators;

/// <summary>
/// Validator for CreateComparisonCommand.
/// Validates Name against SQLi/XSS and enforces business rules.
/// </summary>
public class CreateComparisonCommandValidator : AbstractValidator<CreateComparisonCommand>
{
    public CreateComparisonCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la comparación es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El ID del usuario es requerido");

        RuleFor(x => x.VehicleIds)
            .Must(ids => ids.Count <= 3)
            .WithMessage("No se pueden comparar más de 3 vehículos");
    }
}

/// <summary>
/// Validator for RenameComparisonCommand.
/// </summary>
public class RenameComparisonCommandValidator : AbstractValidator<RenameComparisonCommand>
{
    public RenameComparisonCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la comparación es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.ComparisonId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}

/// <summary>
/// Validator for UpdateComparisonVehiclesCommand.
/// </summary>
public class UpdateComparisonVehiclesCommandValidator : AbstractValidator<UpdateComparisonVehiclesCommand>
{
    public UpdateComparisonVehiclesCommandValidator()
    {
        RuleFor(x => x.ComparisonId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.VehicleIds)
            .Must(ids => ids.Count <= 3)
            .WithMessage("No se pueden comparar más de 3 vehículos");
    }
}

/// <summary>
/// Validator for ShareComparisonCommand.
/// </summary>
public class ShareComparisonCommandValidator : AbstractValidator<ShareComparisonCommand>
{
    public ShareComparisonCommandValidator()
    {
        RuleFor(x => x.ComparisonId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .NoSqlInjection()
            .NoXss();
    }
}

/// <summary>
/// Validator for DeleteComparisonCommand.
/// </summary>
public class DeleteComparisonCommandValidator : AbstractValidator<DeleteComparisonCommand>
{
    public DeleteComparisonCommandValidator()
    {
        RuleFor(x => x.ComparisonId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
