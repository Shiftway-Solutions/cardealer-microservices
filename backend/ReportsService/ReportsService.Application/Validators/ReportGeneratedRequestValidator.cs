using FluentValidation;
using ReportsService.Application.DTOs;

namespace ReportsService.Application.Validators;

public class ReportGeneratedRequestValidator : AbstractValidator<ReportGeneratedRequest>
{
    public ReportGeneratedRequestValidator()
    {
        RuleFor(x => x.FilePath)
            .NotEmpty().WithMessage("File path is required.")
            .MaximumLength(1000).WithMessage("File path must not exceed 1000 characters.")
            .NoSqlInjection().NoXss()
            .Matches(@"^[a-zA-Z0-9\-_/\\.]+$").WithMessage("File path contains invalid characters.");
    }
}
