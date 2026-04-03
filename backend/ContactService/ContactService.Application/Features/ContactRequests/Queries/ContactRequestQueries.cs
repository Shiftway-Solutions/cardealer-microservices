using ContactService.Application.DTOs;
using ContactService.Domain.Interfaces;
using MediatR;

namespace ContactService.Application.Features.ContactRequests.Queries;

// ============================================================================
// GET CONTACT REQUESTS BY BUYER (MY INQUIRIES)
// ============================================================================

public record GetContactRequestsByBuyerQuery : IRequest<List<ContactRequestSummaryDto>>
{
    public Guid BuyerId { get; init; }
}

public class GetContactRequestsByBuyerQueryHandler
    : IRequestHandler<GetContactRequestsByBuyerQuery, List<ContactRequestSummaryDto>>
{
    private readonly IContactRequestRepository _contactRequestRepository;

    public GetContactRequestsByBuyerQueryHandler(IContactRequestRepository contactRequestRepository)
    {
        _contactRequestRepository = contactRequestRepository;
    }

    public async Task<List<ContactRequestSummaryDto>> Handle(
        GetContactRequestsByBuyerQuery request,
        CancellationToken cancellationToken)
    {
        var inquiries = await _contactRequestRepository.GetByBuyerIdAsync(request.BuyerId, cancellationToken);

        return inquiries.Select(i => new ContactRequestSummaryDto
        {
            Id = i.Id,
            VehicleId = i.VehicleId,
            Subject = i.Subject,
            Status = i.Status,
            BuyerName = i.BuyerName,
            BuyerEmail = i.BuyerEmail,
            BuyerPhone = i.BuyerPhone,
            CreatedAt = i.CreatedAt,
            RespondedAt = i.RespondedAt,
            MessageCount = i.Messages?.Count ?? 0,
            UnreadCount = i.Messages?.Count(m => !m.IsFromBuyer && !m.IsRead) ?? 0,  // BUG-S22-4: count unread seller messages for buyer
            LastMessage = i.Messages?.OrderByDescending(m => m.SentAt).FirstOrDefault()?.Message
        }).ToList();
    }
}

// ============================================================================
// GET CONTACT REQUESTS BY SELLER (RECEIVED)
// ============================================================================

public record GetContactRequestsBySellerQuery : IRequest<List<ContactRequestSummaryDto>>
{
    public Guid SellerId { get; init; }
}

public class GetContactRequestsBySellerQueryHandler
    : IRequestHandler<GetContactRequestsBySellerQuery, List<ContactRequestSummaryDto>>
{
    private readonly IContactRequestRepository _contactRequestRepository;

    public GetContactRequestsBySellerQueryHandler(IContactRequestRepository contactRequestRepository)
    {
        _contactRequestRepository = contactRequestRepository;
    }

    public async Task<List<ContactRequestSummaryDto>> Handle(
        GetContactRequestsBySellerQuery request,
        CancellationToken cancellationToken)
    {
        var inquiries = await _contactRequestRepository.GetBySellerIdAsync(request.SellerId, cancellationToken);

        return inquiries.Select(i => new ContactRequestSummaryDto
        {
            Id = i.Id,
            VehicleId = i.VehicleId,
            Subject = i.Subject,
            Status = i.Status,
            BuyerName = i.BuyerName,
            BuyerEmail = i.BuyerEmail,
            BuyerPhone = i.BuyerPhone,
            CreatedAt = i.CreatedAt,
            RespondedAt = i.RespondedAt,
            MessageCount = i.Messages?.Count ?? 0,
            UnreadCount = i.Messages?.Count(m => m.IsFromBuyer && !m.IsRead) ?? 0
        }).ToList();
    }
}

// ============================================================================
// GET CONTACT REQUEST DETAIL
// ============================================================================

public record GetContactRequestDetailQuery : IRequest<ContactRequestDetailDto?>
{
    public Guid ContactRequestId { get; init; }
    public Guid CurrentUserId { get; init; }
}

public class GetContactRequestDetailQueryHandler
    : IRequestHandler<GetContactRequestDetailQuery, ContactRequestDetailDto?>
{
    private readonly IContactRequestRepository _contactRequestRepository;
    private readonly IContactMessageRepository _contactMessageRepository;

    public GetContactRequestDetailQueryHandler(
        IContactRequestRepository contactRequestRepository,
        IContactMessageRepository contactMessageRepository)
    {
        _contactRequestRepository = contactRequestRepository;
        _contactMessageRepository = contactMessageRepository;
    }

    public async Task<ContactRequestDetailDto?> Handle(
        GetContactRequestDetailQuery request,
        CancellationToken cancellationToken)
    {
        var contactRequest = await _contactRequestRepository.GetByIdAsync(request.ContactRequestId, cancellationToken);
        if (contactRequest == null) return null;

        // Authorization check — only buyer or seller can view
        if (contactRequest.BuyerId != request.CurrentUserId &&
            contactRequest.SellerId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this contact request.");
        }

        var messages = await _contactMessageRepository.GetByContactRequestIdAsync(request.ContactRequestId, cancellationToken);

        return new ContactRequestDetailDto
        {
            Id = contactRequest.Id,
            VehicleId = contactRequest.VehicleId,
            Subject = contactRequest.Subject,
            BuyerName = contactRequest.BuyerName,
            BuyerEmail = contactRequest.BuyerEmail,
            BuyerPhone = contactRequest.BuyerPhone,
            Status = contactRequest.Status,
            CreatedAt = contactRequest.CreatedAt,
            RespondedAt = contactRequest.RespondedAt,
            Messages = messages.Select(m => new ContactMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                Message = m.Message,
                IsFromBuyer = m.IsFromBuyer,
                IsRead = m.IsRead,
                SentAt = m.SentAt
            }).ToList()
        };
    }
}

// ============================================================================
// GET UNREAD COUNT
// ============================================================================

public record GetUnreadCountQuery : IRequest<int>
{
    public Guid UserId { get; init; }
}

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly IContactMessageRepository _contactMessageRepository;

    public GetUnreadCountQueryHandler(IContactMessageRepository contactMessageRepository)
    {
        _contactMessageRepository = contactMessageRepository;
    }

    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        return await _contactMessageRepository.GetUnreadCountForUserAsync(request.UserId, cancellationToken);
    }
}

// ============================================================================
// MARK MESSAGE AS READ
// ============================================================================

public record MarkMessageAsReadCommand : IRequest<Unit>
{
    public Guid MessageId { get; init; }
    public Guid CurrentUserId { get; init; }
}

public class MarkMessageAsReadCommandHandler : IRequestHandler<MarkMessageAsReadCommand, Unit>
{
    private readonly IContactMessageRepository _contactMessageRepository;
    private readonly IContactRequestRepository _contactRequestRepository;

    public MarkMessageAsReadCommandHandler(
        IContactMessageRepository contactMessageRepository,
        IContactRequestRepository contactRequestRepository)
    {
        _contactMessageRepository = contactMessageRepository;
        _contactRequestRepository = contactRequestRepository;
    }

    public async Task<Unit> Handle(MarkMessageAsReadCommand request, CancellationToken cancellationToken)
    {
        var message = await _contactMessageRepository.GetByIdAsync(request.MessageId, cancellationToken)
            ?? throw new KeyNotFoundException($"Message {request.MessageId} not found.");

        var contactRequest = await _contactRequestRepository.GetByIdAsync(message.ContactRequestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Contact request {message.ContactRequestId} not found.");

        // Only the other party (not the sender) should mark messages as read
        if (contactRequest.BuyerId != request.CurrentUserId &&
            contactRequest.SellerId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to mark this message as read.");
        }

        await _contactMessageRepository.MarkAsReadAsync(request.MessageId, cancellationToken);

        return Unit.Value;
    }
}
