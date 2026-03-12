using FluentValidation;
using AuditService.Shared;

namespace AuditService.Application.Features.Audit.Commands.CreateAudit;

public class CreateAuditCommandValidator : AbstractValidator<CreateAuditCommand>
{
    public CreateAuditCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required")
            .MaximumLength(255).WithMessage("UserId must not exceed 255 characters");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required")
            .MaximumLength(100).WithMessage("Action must not exceed 100 characters")
            .Must(ValidationPatterns.IsValidActionName).WithMessage("Action contains invalid characters");

        RuleFor(x => x.Resource)
            .NotEmpty().WithMessage("Resource is required")
            .MaximumLength(255).WithMessage("Resource must not exceed 255 characters")
            .Must(ValidationPatterns.IsValidResourceName).WithMessage("Resource contains invalid characters");

        RuleFor(x => x.UserIp)
            .NotEmpty().WithMessage("UserIp is required")
            .MaximumLength(45).WithMessage("UserIp must not exceed 45 characters")
            .Must(ValidationPatterns.IsValidIpAddress).WithMessage("UserIp must be a valid IP address");

        RuleFor(x => x.UserAgent)
            .NotEmpty().WithMessage("UserAgent is required")
            .MaximumLength(500).WithMessage("UserAgent must not exceed 500 characters")
            .Must(ValidationPatterns.IsValidUserAgent).WithMessage("UserAgent contains invalid characters");

        RuleFor(x => x.ServiceName)
            .NotEmpty().WithMessage("ServiceName is required")
            .MaximumLength(50).WithMessage("ServiceName must not exceed 50 characters")
            .Must(ValidationPatterns.IsValidServiceName).WithMessage("ServiceName contains invalid characters");

        RuleFor(x => x.CorrelationId)
            .MaximumLength(100).WithMessage("CorrelationId must not exceed 100 characters")
            .Must(correlationId => string.IsNullOrEmpty(correlationId) || ValidationPatterns.IsValidCorrelationId(correlationId))
            .WithMessage("CorrelationId contains invalid characters");

        When(x => !x.Success, () =>
        {
            RuleFor(x => x.ErrorMessage)
                .NotEmpty().WithMessage("ErrorMessage is required when Success is false")
                .MaximumLength(1000).WithMessage("ErrorMessage must not exceed 1000 characters");
        });

        RuleFor(x => x.DurationMs)
            .GreaterThanOrEqualTo(0).WithMessage("DurationMs must be positive")
            .When(x => x.DurationMs.HasValue);

        RuleFor(x => x.AdditionalData)
            .Must(data => data == null || data.Count <= 50)
            .WithMessage("AdditionalData cannot contain more than 50 items")
            .Must(data => data == null || data.All(kv => kv.Key.Length <= 100 && kv.Value != null))
            .WithMessage("AdditionalData keys must not exceed 100 characters and values cannot be null");
    }
}