using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Application.Metrics;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Shared.Exceptions;
using CarDealer.Contracts.Events.Dealer;

namespace UserService.Application.UseCases.Dealers.VerifyDealer;

public record VerifyDealerCommand(Guid DealerId, VerifyDealerRequest Request) : IRequest<DealerDto>;

public class VerifyDealerCommandHandler : IRequestHandler<VerifyDealerCommand, DealerDto>
{
    private readonly IDealerRepository _dealerRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditServiceClient _auditClient;
    private readonly UserServiceMetrics _metrics;
    private readonly ILogger<VerifyDealerCommandHandler> _logger;

    public VerifyDealerCommandHandler(
        IDealerRepository dealerRepository,
        IEventPublisher eventPublisher,
        IAuditServiceClient auditClient,
        UserServiceMetrics metrics,
        ILogger<VerifyDealerCommandHandler> logger)
    {
        _dealerRepository = dealerRepository;
        _eventPublisher = eventPublisher;
        _auditClient = auditClient;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<DealerDto> Handle(VerifyDealerCommand command, CancellationToken cancellationToken)
    {
        var dealer = await _dealerRepository.GetByIdAsync(command.DealerId);
        if (dealer == null)
        {
            throw new NotFoundException($"Dealer {command.DealerId} not found");
        }

        var request = command.Request;

        if (request.IsVerified)
        {
            dealer.VerificationStatus = DealerVerificationStatus.Verified;
            dealer.VerifiedAt = DateTime.UtcNow;
            dealer.VerifiedByUserId = request.VerifiedByUserId;
            dealer.VerificationNotes = request.Notes;
            dealer.RejectionReason = null;
            dealer.IsActive = true;

            _logger.LogInformation("Dealer {DealerId} verified by admin {VerifiedByUserId}",
                dealer.Id, request.VerifiedByUserId);

            // Publish DealerCreatedEvent on approval
            try
            {
                await _eventPublisher.PublishAsync(new DealerCreatedEvent
                {
                    DealerId = dealer.Id,
                    OwnerUserId = dealer.OwnerUserId,
                    ApprovedAt = dealer.VerifiedAt.Value
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to publish DealerCreatedEvent for dealer {DealerId}. Event will be retried via DLQ.",
                    dealer.Id);
            }

            _metrics.RecordDealerApproved();
        }
        else
        {
            dealer.VerificationStatus = DealerVerificationStatus.Rejected;
            dealer.RejectionReason = request.Notes;
            dealer.VerifiedAt = null;
            dealer.VerifiedByUserId = null;
            dealer.IsActive = false;

            _logger.LogInformation("Dealer {DealerId} rejected. Reason: {Reason}",
                dealer.Id, request.Notes);

            _metrics.RecordDealerRejected();
        }

        dealer.UpdatedAt = DateTime.UtcNow;

        await _dealerRepository.UpdateAsync(dealer);

        // Audit log
        try
        {
            await _auditClient.LogDealerVerificationAsync(
                dealer.Id, request.IsVerified, request.VerifiedByUserId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log audit for dealer verification {DealerId}", dealer.Id);
        }

        return CreateDealer.CreateDealerCommandHandler.MapToDto(dealer);
    }
}
