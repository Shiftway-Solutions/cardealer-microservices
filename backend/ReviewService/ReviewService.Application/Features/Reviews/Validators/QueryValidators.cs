using FluentValidation;
using ReviewService.Application.Features.Reviews.Queries;
using ReviewService.Application.Validators;

namespace ReviewService.Application.Features.Reviews.Validators;

/// <summary>
/// Validador para GetAdminReviewsQuery.
/// Valida los campos de búsqueda y filtrado contra SQLi/XSS.
/// </summary>
public class GetAdminReviewsQueryValidator : AbstractValidator<GetAdminReviewsQuery>
{
    private static readonly string[] AllowedStatuses = new[]
    {
        "pending", "approved", "rejected", "flagged", "all"
    };

    public GetAdminReviewsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La página debe ser al menos 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("El tamaño de página debe estar entre 1 y 100");

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .WithMessage("El término de búsqueda no puede exceder 200 caracteres")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Search));

        RuleFor(x => x.Status)
            .MaximumLength(50)
            .WithMessage("El estado no puede exceder 50 caracteres")
            .Must(status => string.IsNullOrEmpty(status) || AllowedStatuses.Contains(status.ToLowerInvariant()))
            .WithMessage($"El estado debe ser uno de: {string.Join(", ", AllowedStatuses)}")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Status));
    }
}
