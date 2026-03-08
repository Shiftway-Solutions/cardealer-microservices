using FluentValidation;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Validators;

public class CreatePriceAlertRequestValidator : AbstractValidator<CreatePriceAlertRequest>
{
    private static readonly string[] ValidFrequencies = { "instant", "daily", "weekly" };

    public CreatePriceAlertRequestValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Vehicle ID is required.");

        RuleFor(x => x.VehicleTitle)
            .MaximumLength(200).WithMessage("Vehicle title must not exceed 200 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.VehicleTitle));

        RuleFor(x => x.TargetPrice)
            .GreaterThan(0).WithMessage("Target price must be greater than zero.")
            .LessThanOrEqualTo(50_000_000).WithMessage("Target price exceeds maximum allowed value.");

        RuleFor(x => x.CurrentPrice)
            .GreaterThan(0).WithMessage("Current price must be greater than zero.")
            .When(x => x.CurrentPrice.HasValue);

        RuleFor(x => x.PriceDropPercentage)
            .InclusiveBetween(1, 100).WithMessage("Price drop percentage must be between 1 and 100.")
            .When(x => x.PriceDropPercentage.HasValue);
    }
}

public class UpdatePriceAlertRequestValidator : AbstractValidator<UpdatePriceAlertRequest>
{
    public UpdatePriceAlertRequestValidator()
    {
        RuleFor(x => x.TargetPrice)
            .GreaterThan(0).WithMessage("Target price must be greater than zero.")
            .LessThanOrEqualTo(50_000_000).WithMessage("Target price exceeds maximum allowed value.")
            .When(x => x.TargetPrice.HasValue);

        RuleFor(x => x.PriceDropPercentage)
            .InclusiveBetween(1, 100).WithMessage("Price drop percentage must be between 1 and 100.")
            .When(x => x.PriceDropPercentage.HasValue);
    }
}

public class CreateSavedSearchRequestValidator : AbstractValidator<CreateSavedSearchRequest>
{
    private static readonly string[] ValidFrequencies = { "instant", "daily", "weekly" };

    public CreateSavedSearchRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Search name is required.")
            .MaximumLength(100).WithMessage("Search name must not exceed 100 characters.")
            .NoSqlInjection().NoXss();

        RuleFor(x => x.Criteria)
            .NotNull().WithMessage("Search criteria is required.");

        RuleFor(x => x.Criteria.Make)
            .MaximumLength(50).NoSqlInjection().NoXss()
            .When(x => x.Criteria != null && !string.IsNullOrWhiteSpace(x.Criteria.Make));

        RuleFor(x => x.Criteria.Model)
            .MaximumLength(50).NoSqlInjection().NoXss()
            .When(x => x.Criteria != null && !string.IsNullOrWhiteSpace(x.Criteria.Model));

        RuleFor(x => x.Criteria.BodyType)
            .MaximumLength(30).NoSqlInjection().NoXss()
            .When(x => x.Criteria != null && !string.IsNullOrWhiteSpace(x.Criteria.BodyType));

        RuleFor(x => x.Criteria.FuelType)
            .MaximumLength(30).NoSqlInjection().NoXss()
            .When(x => x.Criteria != null && !string.IsNullOrWhiteSpace(x.Criteria.FuelType));

        RuleFor(x => x.Criteria.Transmission)
            .MaximumLength(30).NoSqlInjection().NoXss()
            .When(x => x.Criteria != null && !string.IsNullOrWhiteSpace(x.Criteria.Transmission));

        RuleFor(x => x.Criteria.Location)
            .MaximumLength(100).NoSqlInjection().NoXss()
            .When(x => x.Criteria != null && !string.IsNullOrWhiteSpace(x.Criteria.Location));

        RuleFor(x => x.Criteria.MinYear)
            .InclusiveBetween(1900, 2100)
            .When(x => x.Criteria?.MinYear.HasValue == true);

        RuleFor(x => x.Criteria.MaxYear)
            .InclusiveBetween(1900, 2100)
            .When(x => x.Criteria?.MaxYear.HasValue == true);

        RuleFor(x => x.Criteria.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Criteria?.MinPrice.HasValue == true);

        RuleFor(x => x.Criteria.MaxPrice)
            .GreaterThan(0).LessThanOrEqualTo(50_000_000)
            .When(x => x.Criteria?.MaxPrice.HasValue == true);

        RuleFor(x => x.NotificationFrequency)
            .Must(f => ValidFrequencies.Contains(f))
            .WithMessage("Notification frequency must be 'instant', 'daily', or 'weekly'.")
            .When(x => !string.IsNullOrWhiteSpace(x.NotificationFrequency));
    }
}

public class UpdateSavedSearchRequestValidator : AbstractValidator<UpdateSavedSearchRequest>
{
    private static readonly string[] ValidFrequencies = { "instant", "daily", "weekly" };

    public UpdateSavedSearchRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Search name must not exceed 100 characters.")
            .NoSqlInjection().NoXss()
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Criteria!.Make)
            .MaximumLength(50).NoSqlInjection().NoXss()
            .When(x => x.Criteria != null && !string.IsNullOrWhiteSpace(x.Criteria.Make));

        RuleFor(x => x.Criteria!.Model)
            .MaximumLength(50).NoSqlInjection().NoXss()
            .When(x => x.Criteria != null && !string.IsNullOrWhiteSpace(x.Criteria.Model));

        RuleFor(x => x.Criteria!.Location)
            .MaximumLength(100).NoSqlInjection().NoXss()
            .When(x => x.Criteria != null && !string.IsNullOrWhiteSpace(x.Criteria.Location));

        RuleFor(x => x.NotificationFrequency)
            .Must(f => ValidFrequencies.Contains(f))
            .WithMessage("Notification frequency must be 'instant', 'daily', or 'weekly'.")
            .When(x => !string.IsNullOrWhiteSpace(x.NotificationFrequency));
    }
}
