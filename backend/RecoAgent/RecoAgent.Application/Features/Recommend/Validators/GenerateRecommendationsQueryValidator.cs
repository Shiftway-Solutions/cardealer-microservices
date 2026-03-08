using FluentValidation;
using RecoAgent.Application.Features.Recommend.Queries;
using RecoAgent.Application.Validators;

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

        // Security validators on string inputs
        RuleFor(x => x.Request.InstruccionesAdicionales)
            .MaximumLength(2000).WithMessage("InstruccionesAdicionales no puede exceder 2000 caracteres.")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Request.InstruccionesAdicionales));

        RuleFor(x => x.Request.SessionId)
            .MaximumLength(128).WithMessage("SessionId no puede exceder 128 caracteres.")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Request.SessionId));

        RuleFor(x => x.UserId)
            .MaximumLength(128).WithMessage("UserId no puede exceder 128 caracteres.")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.UserId));

        RuleFor(x => x.Request.Perfil.UserId)
            .MaximumLength(128)
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Request.Perfil.UserId));

        RuleFor(x => x.Request.Perfil.TransmisionPreferida)
            .MaximumLength(50)
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Request.Perfil.TransmisionPreferida));

        RuleFor(x => x.Request.Perfil.EtapaCompra)
            .MaximumLength(50)
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Request.Perfil.EtapaCompra));

        RuleFor(x => x.Request.Perfil.MonedaPreferida)
            .MaximumLength(10)
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.Request.Perfil.MonedaPreferida));
    }
}
