using FluentValidation;
using SearchAgent.Application.Features.Search.Queries;

namespace SearchAgent.Application.Features.Search.Validators;

public class ProcessSearchQueryValidator : AbstractValidator<ProcessSearchQuery>
{
    public ProcessSearchQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("La consulta de búsqueda es requerida.")
            .MinimumLength(2).WithMessage("La consulta debe tener al menos 2 caracteres.")
            .MaximumLength(500).WithMessage("La consulta no puede exceder 500 caracteres.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser al menos 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(8, 40).WithMessage("El tamaño de página debe estar entre 8 y 40.");
    }
}
