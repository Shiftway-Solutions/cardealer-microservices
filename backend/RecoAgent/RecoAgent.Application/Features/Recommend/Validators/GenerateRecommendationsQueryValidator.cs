using FluentValidation;
using RecoAgent.Application.Features.Recommend.Queries;

namespace RecoAgent.Application.Features.Recommend.Validators;

public class GenerateRecommendationsQueryValidator : AbstractValidator<GenerateRecommendationsQuery>
{
    public GenerateRecommendationsQueryValidator()
    {
        RuleFor(x => x.Request)
            .NotNull().WithMessage("El request de recomendaciones es requerido.");

        RuleFor(x => x.Request.Perfil)
            .NotNull().WithMessage("El perfil del usuario es requerido.");

        RuleFor(x => x.Request.Perfil.ColdStartLevel)
            .InclusiveBetween(0, 3).WithMessage("El nivel de cold start debe estar entre 0 y 3.");

        RuleFor(x => x.Request.Candidatos)
            .NotNull().WithMessage("La lista de candidatos es requerida.");

        RuleFor(x => x.Request.Candidatos.Count)
            .LessThanOrEqualTo(50).WithMessage("Máximo 50 candidatos por solicitud.");
    }
}
