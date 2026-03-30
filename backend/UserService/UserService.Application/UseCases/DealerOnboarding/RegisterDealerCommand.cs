using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Shared.Exceptions;
using System.Text.Json;

namespace UserService.Application.UseCases.DealerOnboarding;

/// <summary>
/// Command to register a new dealer with Stripe integration
/// </summary>
public record RegisterDealerCommand(
    Guid UserId,
    string BusinessName,
    string Email,
    string? Phone,
    string Plan
) : IRequest<DealerOnboardingDto>;

public class RegisterDealerCommandHandler : IRequestHandler<RegisterDealerCommand, DealerOnboardingDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IDealerRepository _dealerRepository;
    private readonly IDealerOnboardingRepository _onboardingRepository;
    private readonly ILogger<RegisterDealerCommandHandler> _logger;
    // TODO: Inject IBillingServiceClient when available

    public RegisterDealerCommandHandler(
        IUserRepository userRepository,
        IDealerRepository dealerRepository,
        IDealerOnboardingRepository onboardingRepository,
        ILogger<RegisterDealerCommandHandler> logger)
    {
        _userRepository = userRepository;
        _dealerRepository = dealerRepository;
        _onboardingRepository = onboardingRepository;
        _logger = logger;
    }

    public async Task<DealerOnboardingDto> Handle(RegisterDealerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering dealer for user {UserId}", request.UserId);

        // Verify user exists
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            throw new NotFoundException($"User {request.UserId} not found");
        }

        // Check if dealer already exists for this user
        var existingDealer = await _dealerRepository.GetByOwnerIdAsync(request.UserId);

        if (existingDealer != null)
        {
            throw new BadRequestException("User already has a dealer profile");
        }

        // TODO: Parse plan and create Stripe customer/subscription
        // if (!Enum.TryParse<DealerPlan>(request.Plan, out var dealerPlan))
        // {
        //     throw new BadRequestException($"Invalid plan: {request.Plan}");
        // }
        // var stripeCustomerId = await _billingService.CreateCustomerAsync(...);

        // Create dealer
        var dealer = new Dealer
        {
            Id = Guid.NewGuid(),
            BusinessName = request.BusinessName,
            Slug = GenerateSlug(request.BusinessName),
            Email = request.Email,
            Phone = request.Phone ?? string.Empty,
            OwnerUserId = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        await _dealerRepository.AddAsync(dealer);

        // Create onboarding process
        var onboarding = new DealerOnboardingProcess
        {
            Id = Guid.NewGuid(),
            DealerId = dealer.Id,
            Status = OnboardingStatus.InProgress,
            CurrentStep = DealerOnboardingStep.BasicInfo,
            StartedAt = DateTime.UtcNow,
            StepsCompleted = JsonSerializer.Serialize(new List<string>()),
            StepsSkipped = JsonSerializer.Serialize(new List<string>())
        };

        await _onboardingRepository.CreateAsync(onboarding);

        _logger.LogInformation("Dealer {DealerId} registered successfully", dealer.Id);

        return new DealerOnboardingDto
        {
            DealerId = dealer.Id,
            Status = onboarding.Status.ToString(),
            CurrentStep = onboarding.CurrentStep.ToString(),
            StepsCompleted = new List<string>(),
            Progress = 0
        };
    }

    private static string GenerateSlug(string businessName, string? city = null)
    {
        var raw = string.IsNullOrWhiteSpace(city)
            ? businessName
            : $"{businessName} {city}";

        var slug = raw.ToLowerInvariant()
            .Replace('á', 'a').Replace('é', 'e').Replace('í', 'i')
            .Replace('ó', 'o').Replace('ú', 'u').Replace('ñ', 'n')
            .Replace('ü', 'u');

        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s-]+", "-");
        slug = slug.Trim('-');

        return slug;
    }
}
