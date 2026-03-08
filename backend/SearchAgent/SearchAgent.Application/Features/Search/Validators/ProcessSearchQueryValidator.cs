using FluentValidation;
using SearchAgent.Application.Features.Search.Queries;
using SearchAgent.Application.Validators;

namespace SearchAgent.Application.Features.Search.Validators;

public class ProcessSearchQueryValidator : AbstractValidator<ProcessSearchQuery>
{
    public ProcessSearchQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("La consulta de búsqueda es requerida.")
            .MinimumLength(2).WithMessage("La consulta debe tener al menos 2 caracteres.")
            .MaximumLength(500).WithMessage("La consulta no puede exceder 500 caracteres.")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser al menos 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(8, 40).WithMessage("El tamaño de página debe estar entre 8 y 40.");

        RuleFor(x => x.SessionId)
            .MaximumLength(128).WithMessage("SessionId no puede exceder 128 caracteres.")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.SessionId));

        RuleFor(x => x.UserId)
            .MaximumLength(128).WithMessage("UserId no puede exceder 128 caracteres.")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.UserId));
    }
}
