using FluentValidation;

namespace ErrorService.Application.UseCases.GetErrorStats;

/// <summary>
/// Validator for GetErrorStatsQuery — ensures date range parameters are valid.
/// </summary>
public class GetErrorStatsQueryValidator : AbstractValidator<GetErrorStatsQuery>
{
    public GetErrorStatsQueryValidator()
    {
        // If both From and To are provided, From must be <= To
        RuleFor(x => x.Request)
            .Must(r => !r.From.HasValue || !r.To.HasValue || r.From <= r.To)
            .WithMessage("'From' date cannot be after 'To' date.");

        // From date should not be unreasonably far in the past (max 5 years)
        RuleFor(x => x.Request.From)
            .Must(from => from >= DateTime.UtcNow.AddYears(-5))
            .WithMessage("'From' date cannot be more than 5 years in the past.")
            .When(x => x.Request.From.HasValue);

        // To date should not be in the future (with a small tolerance of 1 day)
        RuleFor(x => x.Request.To)
            .Must(to => to <= DateTime.UtcNow.AddDays(1))
            .WithMessage("'To' date cannot be in the future.")
            .When(x => x.Request.To.HasValue);
    }
}
