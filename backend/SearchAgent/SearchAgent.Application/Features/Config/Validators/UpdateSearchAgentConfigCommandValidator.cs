using FluentValidation;
using SearchAgent.Application.Features.Config.Commands;
using SearchAgent.Application.Validators;

namespace SearchAgent.Application.Features.Config.Validators;

public class UpdateSearchAgentConfigCommandValidator : AbstractValidator<UpdateSearchAgentConfigCommand>
{
    public UpdateSearchAgentConfigCommandValidator()
    {
        RuleFor(x => x.UpdatedBy)
            .NotEmpty().WithMessage("UpdatedBy es requerido.")
            .MaximumLength(256)
            .NoSqlInjection()
            .NoXss();

        // Validate nested ConfigUpdate fields when present
        RuleFor(x => x.ConfigUpdate.Model)
            .MaximumLength(100).WithMessage("Model no puede exceder 100 caracteres.")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.ConfigUpdate.Model));

        RuleFor(x => x.ConfigUpdate.SponsoredLabel)
            .MaximumLength(100).WithMessage("SponsoredLabel no puede exceder 100 caracteres.")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.ConfigUpdate.SponsoredLabel));

        RuleFor(x => x.ConfigUpdate.SponsoredPositions)
            .MaximumLength(500).WithMessage("SponsoredPositions no puede exceder 500 caracteres.")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.ConfigUpdate.SponsoredPositions));

        RuleFor(x => x.ConfigUpdate.SystemPromptOverride)
            .MaximumLength(10000).WithMessage("SystemPromptOverride no puede exceder 10000 caracteres.")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.ConfigUpdate.SystemPromptOverride));

        RuleFor(x => x.ConfigUpdate.Temperature)
            .InclusiveBetween(0f, 2f).WithMessage("Temperature debe estar entre 0 y 2.")
            .When(x => x.ConfigUpdate.Temperature.HasValue);

        RuleFor(x => x.ConfigUpdate.MaxTokens)
            .InclusiveBetween(100, 32000).WithMessage("MaxTokens debe estar entre 100 y 32000.")
            .When(x => x.ConfigUpdate.MaxTokens.HasValue);

        RuleFor(x => x.ConfigUpdate.MaxResultsPerPage)
            .InclusiveBetween(8, 100).WithMessage("MaxResultsPerPage debe estar entre 8 y 100.")
            .When(x => x.ConfigUpdate.MaxResultsPerPage.HasValue);

        RuleFor(x => x.ConfigUpdate.CacheTtlSeconds)
            .InclusiveBetween(0, 86400).WithMessage("CacheTtlSeconds debe estar entre 0 y 86400.")
            .When(x => x.ConfigUpdate.CacheTtlSeconds.HasValue);
    }
}
