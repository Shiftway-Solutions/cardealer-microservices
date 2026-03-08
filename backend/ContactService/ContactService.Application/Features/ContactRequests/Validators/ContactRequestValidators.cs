using ContactService.Application.Features.ContactRequests.Commands;
using ContactService.Application.Validators;
using FluentValidation;

namespace ContactService.Application.Features.ContactRequests.Validators;

public class CreateContactRequestCommandValidator : AbstractValidator<CreateContactRequestCommand>
{
    public CreateContactRequestCommandValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("VehicleId is required.");

        RuleFor(x => x.SellerId)
            .NotEmpty().WithMessage("SellerId is required.");

        RuleFor(x => x.BuyerId)
            .NotEmpty().WithMessage("BuyerId is required.");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required.")
            .MaximumLength(200).WithMessage("Subject must be 200 characters or less.")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.BuyerName)
            .NotEmpty().WithMessage("BuyerName is required.")
            .MaximumLength(100).WithMessage("BuyerName must be 100 characters or less.")
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.BuyerEmail)
            .NotEmpty().WithMessage("BuyerEmail is required.")
            .EmailAddress().WithMessage("BuyerEmail must be a valid email address.")
            .MaximumLength(254)
            .NoSqlInjection()
            .NoXss();

        RuleFor(x => x.BuyerPhone)
            .MaximumLength(20).WithMessage("BuyerPhone must be 20 characters or less.")
            .NoSqlInjection()
            .NoXss()
            .When(x => !string.IsNullOrEmpty(x.BuyerPhone));

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(5000).WithMessage("Message must be 5000 characters or less.")
            .NoSqlInjection()
            .NoXss();
    }
}

public class ReplyToContactRequestCommandValidator : AbstractValidator<ReplyToContactRequestCommand>
{
    public ReplyToContactRequestCommandValidator()
    {
        RuleFor(x => x.ContactRequestId)
            .NotEmpty().WithMessage("ContactRequestId is required.");

        RuleFor(x => x.CurrentUserId)
            .NotEmpty().WithMessage("CurrentUserId is required.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(5000).WithMessage("Message must be 5000 characters or less.")
            .NoSqlInjection()
            .NoXss();
    }
}

public class UpdateContactRequestStatusCommandValidator : AbstractValidator<UpdateContactRequestStatusCommand>
{
    private static readonly string[] ValidStatuses = { "Open", "Responded", "Read", "Archived", "Closed" };

    public UpdateContactRequestStatusCommandValidator()
    {
        RuleFor(x => x.ContactRequestId)
            .NotEmpty().WithMessage("ContactRequestId is required.");

        RuleFor(x => x.CurrentUserId)
            .NotEmpty().WithMessage("CurrentUserId is required.");

        RuleFor(x => x.NewStatus)
            .NotEmpty().WithMessage("NewStatus is required.")
            .Must(status => ValidStatuses.Contains(status))
            .WithMessage($"NewStatus must be one of: {string.Join(", ", ValidStatuses)}.")
            .NoSqlInjection()
            .NoXss();
    }
}

public class DeleteContactRequestCommandValidator : AbstractValidator<DeleteContactRequestCommand>
{
    public DeleteContactRequestCommandValidator()
    {
        RuleFor(x => x.ContactRequestId)
            .NotEmpty().WithMessage("ContactRequestId is required.");

        RuleFor(x => x.CurrentUserId)
            .NotEmpty().WithMessage("CurrentUserId is required.");
    }
}
