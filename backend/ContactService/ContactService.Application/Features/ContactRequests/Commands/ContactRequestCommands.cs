using ContactService.Application.DTOs;
using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
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
}

public class CreateContactRequestCommandHandler
    : IRequestHandler<CreateContactRequestCommand, ContactRequestSummaryDto>
{
    private readonly IContactRequestRepository _contactRequestRepository;
    private readonly IContactMessageRepository _contactMessageRepository;
    private readonly ILogger<CreateContactRequestCommandHandler> _logger;

    public CreateContactRequestCommandHandler(
        IContactRequestRepository contactRequestRepository,
        IContactMessageRepository contactMessageRepository,
        ILogger<CreateContactRequestCommandHandler> logger)
    {
        _contactRequestRepository = contactRequestRepository;
        _contactMessageRepository = contactMessageRepository;
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

        var created = await _contactRequestRepository.CreateAsync(contactRequest, cancellationToken);

        // Create initial message
        var initialMessage = new ContactMessage(created.Id, request.BuyerId, request.Message, true);
        await _contactMessageRepository.CreateAsync(initialMessage, cancellationToken);

        _logger.LogInformation(
            "Contact request {ContactRequestId} created by buyer {BuyerId} for seller {SellerId}",
            created.Id, request.BuyerId, request.SellerId);

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
        var message = new ContactMessage(request.ContactRequestId, request.CurrentUserId, request.Message, isFromBuyer);
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

    public UpdateContactRequestStatusCommandHandler(IContactRequestRepository contactRequestRepository)
    {
        _contactRequestRepository = contactRequestRepository;
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
