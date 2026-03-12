using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Shared.Exceptions;

namespace UserService.Application.UseCases.DealerModules;

/// <summary>
/// Command to subscribe a dealer to a module
/// </summary>
public record SubscribeModuleCommand(
    Guid DealerId,
    Guid ModuleId,
    int DurationMonths = 1
) : IRequest<Guid>;

public class SubscribeModuleCommandHandler : IRequestHandler<SubscribeModuleCommand, Guid>
{
    private readonly IDealerRepository _dealerRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly IDealerModuleRepository _dealerModuleRepository;
    private readonly ILogger<SubscribeModuleCommandHandler> _logger;
    // TODO: Inject IBillingServiceClient when available

    public SubscribeModuleCommandHandler(
        IDealerRepository dealerRepository,
        IModuleRepository moduleRepository,
        IDealerModuleRepository dealerModuleRepository,
        ILogger<SubscribeModuleCommandHandler> logger)
    {
        _dealerRepository = dealerRepository;
        _moduleRepository = moduleRepository;
        _dealerModuleRepository = dealerModuleRepository;
        _logger = logger;
    }

    public async Task<Guid> Handle(SubscribeModuleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subscribing dealer {DealerId} to module {ModuleId}",
            request.DealerId, request.ModuleId);

        // Verify dealer exists
        var dealer = await _dealerRepository.GetByIdAsync(request.DealerId);
        if (dealer == null)
        {
            throw new NotFoundException($"Dealer {request.DealerId} not found");
        }

        // Verify module exists and is active
        var module = await _moduleRepository.GetByIdAsync(request.ModuleId);
        if (module == null)
        {
            throw new NotFoundException($"Module {request.ModuleId} not found");
        }

        if (!module.IsActive)
        {
            throw new BadRequestException($"Module {module.Name} is not available");
        }

        // Check if already subscribed
        var isSubscribed = await _dealerModuleRepository.IsSubscribedAsync(request.DealerId, request.ModuleId);
        if (isSubscribed)
        {
            throw new BadRequestException($"Dealer is already subscribed to module {module.Name}");
        }

        // TODO: Create billing charge via BillingService
        // var charge = await _billingService.ChargeModuleAsync(dealer.Id, module.Price * request.DurationMonths);

        // Create subscription
        var dealerModule = new DealerModule
        {
            Id = Guid.NewGuid(),
            DealerId = request.DealerId,
            ModuleId = request.ModuleId,
            IsActive = true,
            ActivatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMonths(request.DurationMonths)
        };

        await _dealerModuleRepository.AddAsync(dealerModule);

        _logger.LogInformation("Dealer {DealerId} successfully subscribed to module {ModuleName}",
            request.DealerId, module.Name);

        return dealerModule.Id;
    }
}
