using System.Globalization;
using ContactService.Application.DTOs;
using ContactService.Domain.Entities;
using ContactService.Domain.Events;
using ContactService.Domain.Interfaces;
using CarDealer.Contracts.Enums;
using CarDealer.Contracts.Events.Vehicle;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ContactService.Application.Features.ContactRequests.Commands;

// ============================================================================
// CREATE CONTACT REQUEST
// ============================================================================

public record CreateContactRequestCommand : IRequest<ContactRequestSummaryDto>
{
    public Guid VehicleId { get; init; }
    public Guid SellerId { get; init; }
    public Guid BuyerId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string BuyerName { get; init; } = string.Empty;
    public string BuyerEmail { get; init; } = string.Empty;
    public string? BuyerPhone { get; init; }
    public string Message { get; init; } = string.Empty;

    // UTM Attribution — SEM FIX
    public string? UtmSource { get; init; }
    public string? UtmMedium { get; init; }
    public string? UtmCampaign { get; init; }
    public string? UtmTerm { get; init; }
    public string? UtmContent { get; init; }
    public string? Gclid { get; init; }
    public string? Fbclid { get; init; }
    public string? LandingPage { get; init; }
}

public class CreateContactRequestCommandHandler
    : IRequestHandler<CreateContactRequestCommand, ContactRequestSummaryDto>
{
    private readonly IContactRequestRepository _contactRequestRepository;
    private readonly IContactMessageRepository _contactMessageRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IConversationUsageTracker _usageTracker;
    private readonly IDealerPlanResolver _planResolver;
    private readonly IConversationOverageRepository _overageRepo;
    private readonly ILogger<CreateContactRequestCommandHandler> _logger;

    public CreateContactRequestCommandHandler(
        IContactRequestRepository contactRequestRepository,
        IContactMessageRepository contactMessageRepository,
        IEventPublisher eventPublisher,
        IConversationUsageTracker usageTracker,
        IDealerPlanResolver planResolver,
        IConversationOverageRepository overageRepo,
        ILogger<CreateContactRequestCommandHandler> logger)
    {
        _contactRequestRepository = contactRequestRepository;
        _contactMessageRepository = contactMessageRepository;
        _eventPublisher = eventPublisher;
        _usageTracker = usageTracker;
        _planResolver = planResolver;
        _overageRepo = overageRepo;
        _logger = logger;
    }

    public async Task<ContactRequestSummaryDto> Handle(
        CreateContactRequestCommand request,
        CancellationToken cancellationToken)
    {
        var contactRequest = new ContactRequest(
            request.VehicleId,
            request.BuyerId,
            request.SellerId,
            request.Subject,
            request.BuyerName,
            request.BuyerEmail,
            request.Message
        );
        contactRequest.BuyerPhone = request.BuyerPhone;

        // SEM FIX: Map UTM attribution fields
        contactRequest.UtmSource = request.UtmSource;
        contactRequest.UtmMedium = request.UtmMedium;
        contactRequest.UtmCampaign = request.UtmCampaign;
        contactRequest.UtmTerm = request.UtmTerm;
        contactRequest.UtmContent = request.UtmContent;
        contactRequest.Gclid = request.Gclid;
        contactRequest.Fbclid = request.Fbclid;
        contactRequest.LandingPage = request.LandingPage;

        var created = await _contactRequestRepository.CreateAsync(contactRequest, cancellationToken);

        // Create initial message
        var initialMessage = new ContactMessage(created.Id, request.BuyerId, request.Message, true, request.BuyerName);
        await _contactMessageRepository.CreateAsync(initialMessage, cancellationToken);

        _logger.LogInformation(
            "Contact request {ContactRequestId} created by buyer {BuyerId} for seller {SellerId}",
            created.Id, request.BuyerId, request.SellerId);

        // Detect if this is the dealer's very first inquiry (for celebration notification)
        var sellerLeadCount = await _contactRequestRepository.CountBySellerIdAsync(created.SellerId, cancellationToken);
        var isFirstInquiry = sellerLeadCount == 1; // 1 = the one we just created

        // Publish LeadCreatedEvent → NotificationService (email + WhatsApp to dealer)
        try
        {
            await _eventPublisher.PublishAsync(new LeadCreatedEvent
            {
                LeadId = created.Id,
                VehicleId = created.VehicleId ?? Guid.Empty,
                SellerId = created.SellerId,
                BuyerId = created.BuyerId,
                BuyerName = created.BuyerName,
                BuyerEmail = created.BuyerEmail,
                BuyerPhone = created.BuyerPhone,
                Message = request.Message,
                VehicleTitle = created.Subject,
                CreatedAt = created.CreatedAt,
                IsFirstInquiry = isFirstInquiry
            }, cancellationToken);

            _logger.LogInformation(
                "LeadCreatedEvent published for ContactRequestId={ContactRequestId}",
                created.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish LeadCreatedEvent for ContactRequestId={ContactRequestId}. Lead was saved but notification may be delayed.",
                created.Id);
            // Don't throw — the lead was saved successfully, notification failure is non-critical
        }

        // ── CONTRA #5 FIX: Conversation limit enforcement for ÉLITE plan ──────
        // Track usage, check thresholds, and publish events for notifications.
        try
        {
            var dealerPlan = await _planResolver.GetDealerPlanAsync(created.SellerId, cancellationToken);
            var usageResult = await _usageTracker.IncrementAndCheckAsync(
                created.SellerId, dealerPlan, cancellationToken);

            // Publish warning threshold event (80% = 1,600 for Elite)
            if (usageResult.JustCrossedWarningThreshold)
            {
                await _eventPublisher.PublishAsync(new ConversationWarningThresholdEvent(
                    dealerId: created.SellerId,
                    dealerPlan: dealerPlan,
                    currentCount: usageResult.CurrentCount,
                    maxAllowed: usageResult.MaxAllowed,
                    projectedMonthlyTotal: usageResult.ProjectedMonthlyTotal
                ), cancellationToken);

                _logger.LogWarning(
                    "[CONTRA#5] Dealer {DealerId} reached 80% conversation limit: {Count}/{Max} (plan={Plan}). " +
                    "Projected monthly total: {Projected}. Warning notification sent.",
                    created.SellerId, usageResult.CurrentCount, usageResult.MaxAllowed,
                    dealerPlan, usageResult.ProjectedMonthlyTotal);
            }

            // Publish hard limit reached event (100% = 2,000 for Elite)
            if (usageResult.JustReachedLimit)
            {
                await _eventPublisher.PublishAsync(new ConversationLimitReachedEvent(
                    dealerId: created.SellerId,
                    dealerPlan: dealerPlan,
                    limitReached: usageResult.MaxAllowed
                ), cancellationToken);

                _logger.LogWarning(
                    "[CONTRA#5] Dealer {DealerId} REACHED conversation hard limit: {Max} (plan={Plan}). " +
                    "ChatAgent will enter basic mode until next billing cycle.",
                    created.SellerId, usageResult.MaxAllowed, dealerPlan);
            }

            // ── OVERAGE DETAIL LOGGING ──────────────────────────────────────
            // If the conversation exceeds the plan limit, persist the overage
            // detail for billing and downloadable report.
            if (usageResult.OverageCount > 0 && usageResult.MaxAllowed > 0)
            {
                var overageDetail = new ConversationOverageDetail
                {
                    DealerId = created.SellerId,
                    ContactRequestId = created.Id,
                    BuyerId = created.BuyerId,
                    VehicleId = created.VehicleId,
                    Subject = created.Subject,
                    DealerPlan = dealerPlan,
                    BillingPeriod = DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    ConversationNumber = usageResult.CurrentCount,
                    PlanLimit = usageResult.MaxAllowed,
                    UnitCost = CarDealer.Contracts.Enums.PlanFeatureLimits.OverageCostPerConversation,
                    OccurredAtUtc = DateTime.UtcNow
                };

                await _overageRepo.CreateAsync(overageDetail, cancellationToken);

                _logger.LogWarning(
                    "[CONTRA#5-OVERAGE] Dealer {DealerId} overage conversation #{ConvNo} logged. " +
                    "ContactRequest={ContactRequestId} Cost=${UnitCost}",
                    created.SellerId, usageResult.CurrentCount,
                    created.Id, overageDetail.UnitCost);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[CONTRA#5] Failed to track conversation usage for dealer {DealerId}. " +
                "Contact request was saved but limit enforcement may be delayed.",
                created.SellerId);
            // Don't throw — contact request was saved, usage tracking failure is non-critical
        }

        return new ContactRequestSummaryDto
        {
            Id = created.Id,
            VehicleId = created.VehicleId,
            Subject = created.Subject,
            Status = created.Status,
            BuyerName = created.BuyerName,
            BuyerEmail = created.BuyerEmail,
            BuyerPhone = created.BuyerPhone,
            CreatedAt = created.CreatedAt,
            MessageCount = 1
        };
    }
}

// ============================================================================
// REPLY TO CONTACT REQUEST
// ============================================================================

public record ReplyToContactRequestCommand : IRequest<ContactMessageDto>
{
    public Guid ContactRequestId { get; init; }
    public Guid CurrentUserId { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class ReplyToContactRequestCommandHandler
    : IRequestHandler<ReplyToContactRequestCommand, ContactMessageDto>
{
    private readonly IContactRequestRepository _contactRequestRepository;
    private readonly IContactMessageRepository _contactMessageRepository;
    private readonly ILogger<ReplyToContactRequestCommandHandler> _logger;

    public ReplyToContactRequestCommandHandler(
        IContactRequestRepository contactRequestRepository,
        IContactMessageRepository contactMessageRepository,
        ILogger<ReplyToContactRequestCommandHandler> logger)
    {
        _contactRequestRepository = contactRequestRepository;
        _contactMessageRepository = contactMessageRepository;
        _logger = logger;
    }

    public async Task<ContactMessageDto> Handle(
        ReplyToContactRequestCommand request,
        CancellationToken cancellationToken)
    {
        var contactRequest = await _contactRequestRepository.GetByIdAsync(request.ContactRequestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Contact request {request.ContactRequestId} not found.");

        if (contactRequest.BuyerId != request.CurrentUserId &&
            contactRequest.SellerId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to reply to this contact request.");
        }

        var isFromBuyer = contactRequest.BuyerId == request.CurrentUserId;
        var senderName = isFromBuyer ? contactRequest.BuyerName : string.Empty;
        var message = new ContactMessage(request.ContactRequestId, request.CurrentUserId, request.Message, isFromBuyer, senderName);
        await _contactMessageRepository.CreateAsync(message, cancellationToken);

        // Update contact request status when seller responds
        if (!isFromBuyer && contactRequest.Status == "Open")
        {
            contactRequest.Status = "Responded";
            contactRequest.RespondedAt = DateTime.UtcNow;
            await _contactRequestRepository.UpdateAsync(contactRequest, cancellationToken);
        }

        _logger.LogInformation(
            "Reply added to contact request {ContactRequestId} by user {UserId}",
            request.ContactRequestId, request.CurrentUserId);

        return new ContactMessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            Message = message.Message,
            IsFromBuyer = message.IsFromBuyer,
            IsRead = message.IsRead,
            SentAt = message.SentAt
        };
    }
}

// ============================================================================
// UPDATE CONTACT REQUEST STATUS
// ============================================================================

public record UpdateContactRequestStatusCommand : IRequest<Unit>
{
    public Guid ContactRequestId { get; init; }
    public Guid CurrentUserId { get; init; }
    public string NewStatus { get; init; } = string.Empty;
}

public class UpdateContactRequestStatusCommandHandler
    : IRequestHandler<UpdateContactRequestStatusCommand, Unit>
{
    private readonly IContactRequestRepository _contactRequestRepository;
    private readonly IContactMessageRepository _contactMessageRepository;

    public UpdateContactRequestStatusCommandHandler(
        IContactRequestRepository contactRequestRepository,
        IContactMessageRepository contactMessageRepository)
    {
        _contactRequestRepository = contactRequestRepository;
        _contactMessageRepository = contactMessageRepository;
    }

    public async Task<Unit> Handle(
        UpdateContactRequestStatusCommand request,
        CancellationToken cancellationToken)
    {
        var contactRequest = await _contactRequestRepository.GetByIdAsync(request.ContactRequestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Contact request {request.ContactRequestId} not found.");

        if (contactRequest.BuyerId != request.CurrentUserId &&
            contactRequest.SellerId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to update this contact request.");
        }

        contactRequest.Status = request.NewStatus;
        await _contactRequestRepository.UpdateAsync(contactRequest, cancellationToken);

        // BUG-S22-2 FIX: when opening a conversation, also mark individual messages as read
        if (request.NewStatus == "Read")
        {
            var readerIsBuyer = contactRequest.BuyerId == request.CurrentUserId;
            await _contactMessageRepository.MarkConversationMessagesAsReadAsync(
                request.ContactRequestId, request.CurrentUserId, readerIsBuyer, cancellationToken);
        }

        return Unit.Value;
    }
}

// ============================================================================
// DELETE CONTACT REQUEST
// ============================================================================

public record DeleteContactRequestCommand : IRequest<Unit>
{
    public Guid ContactRequestId { get; init; }
    public Guid CurrentUserId { get; init; }
}

public class DeleteContactRequestCommandHandler
    : IRequestHandler<DeleteContactRequestCommand, Unit>
{
    private readonly IContactRequestRepository _contactRequestRepository;

    public DeleteContactRequestCommandHandler(IContactRequestRepository contactRequestRepository)
    {
        _contactRequestRepository = contactRequestRepository;
    }

    public async Task<Unit> Handle(
        DeleteContactRequestCommand request,
        CancellationToken cancellationToken)
    {
        var contactRequest = await _contactRequestRepository.GetByIdAsync(request.ContactRequestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Contact request {request.ContactRequestId} not found.");

        if (contactRequest.BuyerId != request.CurrentUserId &&
            contactRequest.SellerId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this contact request.");
        }

        await _contactRequestRepository.DeleteAsync(request.ContactRequestId, cancellationToken);

        return Unit.Value;
    }
}
