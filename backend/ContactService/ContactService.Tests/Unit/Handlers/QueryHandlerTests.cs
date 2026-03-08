using ContactService.Application.DTOs;
using ContactService.Application.Features.ContactRequests.Queries;
using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using MediatR;
using Moq;
using FluentAssertions;

namespace ContactService.Tests.Unit.Handlers;

// ============================================================================
// GET CONTACT REQUESTS BY BUYER
// ============================================================================

public class GetContactRequestsByBuyerQueryHandlerTests
{
    private readonly Mock<IContactRequestRepository> _contactRequestRepo = new();
    private readonly GetContactRequestsByBuyerQueryHandler _sut;

    public GetContactRequestsByBuyerQueryHandlerTests()
    {
        _sut = new GetContactRequestsByBuyerQueryHandler(_contactRequestRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsListOfSummaryDtos()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var request1 = new ContactRequest(Guid.NewGuid(), buyerId, Guid.NewGuid(), "Car 1", "Juan", "j@t.com", "Hi");
        var request2 = new ContactRequest(Guid.NewGuid(), buyerId, Guid.NewGuid(), "Car 2", "Juan", "j@t.com", "Hello");
        request1.Messages.Add(new ContactMessage(request1.Id, buyerId, "Hi", true));
        request2.Messages.Add(new ContactMessage(request2.Id, buyerId, "Hello", true));
        request2.Messages.Add(new ContactMessage(request2.Id, Guid.NewGuid(), "Reply", false));

        _contactRequestRepo.Setup(r => r.GetByBuyerIdAsync(buyerId))
            .ReturnsAsync(new List<ContactRequest> { request1, request2 });

        var query = new GetContactRequestsByBuyerQuery { BuyerId = buyerId };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Subject.Should().Be("Car 1");
        result[0].MessageCount.Should().Be(1);
        result[1].Subject.Should().Be("Car 2");
        result[1].MessageCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmptyList()
    {
        var buyerId = Guid.NewGuid();
        _contactRequestRepo.Setup(r => r.GetByBuyerIdAsync(buyerId))
            .ReturnsAsync(new List<ContactRequest>());

        var query = new GetContactRequestsByBuyerQuery { BuyerId = buyerId };

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsLastMessageCorrectly()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var request = new ContactRequest(Guid.NewGuid(), buyerId, Guid.NewGuid(), "Test", "Juan", "j@t.com", "First");

        var msg1 = new ContactMessage(request.Id, buyerId, "First message", true);
        // Set an older time for msg1
        msg1.SentAt = DateTime.UtcNow.AddMinutes(-10);

        var msg2 = new ContactMessage(request.Id, Guid.NewGuid(), "Latest reply", false);
        msg2.SentAt = DateTime.UtcNow;

        request.Messages.Add(msg1);
        request.Messages.Add(msg2);

        _contactRequestRepo.Setup(r => r.GetByBuyerIdAsync(buyerId))
            .ReturnsAsync(new List<ContactRequest> { request });

        // Act
        var result = await _sut.Handle(
            new GetContactRequestsByBuyerQuery { BuyerId = buyerId },
            CancellationToken.None);

        // Assert
        result[0].LastMessage.Should().Be("Latest reply");
    }

    [Fact]
    public async Task Handle_NullMessages_ReturnsZeroMessageCount()
    {
        var buyerId = Guid.NewGuid();
        var request = new ContactRequest(Guid.NewGuid(), buyerId, Guid.NewGuid(), "Test", "Juan", "j@t.com", "Hi");
        // Clear the Messages collection to trigger null-safe operator
        request.Messages = null!;

        _contactRequestRepo.Setup(r => r.GetByBuyerIdAsync(buyerId))
            .ReturnsAsync(new List<ContactRequest> { request });

        var result = await _sut.Handle(
            new GetContactRequestsByBuyerQuery { BuyerId = buyerId },
            CancellationToken.None);

        result[0].MessageCount.Should().Be(0);
        result[0].LastMessage.Should().BeNull();
    }
}

// ============================================================================
// GET CONTACT REQUESTS BY SELLER
// ============================================================================

public class GetContactRequestsBySellerQueryHandlerTests
{
    private readonly Mock<IContactRequestRepository> _contactRequestRepo = new();
    private readonly GetContactRequestsBySellerQueryHandler _sut;

    public GetContactRequestsBySellerQueryHandlerTests()
    {
        _sut = new GetContactRequestsBySellerQueryHandler(_contactRequestRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsListWithUnreadCount()
    {
        // Arrange
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var request = new ContactRequest(Guid.NewGuid(), buyerId, sellerId, "Car inquiry", "Juan", "j@t.com", "Info?");

        // 2 unread messages from buyer, 1 read message from buyer
        var unread1 = new ContactMessage(request.Id, buyerId, "Question 1", true) { IsRead = false };
        var unread2 = new ContactMessage(request.Id, buyerId, "Question 2", true) { IsRead = false };
        var read1 = new ContactMessage(request.Id, buyerId, "Thanks", true) { IsRead = true };
        var sellerMsg = new ContactMessage(request.Id, sellerId, "Reply", false) { IsRead = true };

        request.Messages.Add(unread1);
        request.Messages.Add(unread2);
        request.Messages.Add(read1);
        request.Messages.Add(sellerMsg);

        _contactRequestRepo.Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(new List<ContactRequest> { request });

        // Act
        var result = await _sut.Handle(
            new GetContactRequestsBySellerQuery { SellerId = sellerId },
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].MessageCount.Should().Be(4);
        result[0].UnreadCount.Should().Be(2); // only unread from buyer
    }

    [Fact]
    public async Task Handle_EmptyResults_ReturnsEmptyList()
    {
        var sellerId = Guid.NewGuid();
        _contactRequestRepo.Setup(r => r.GetBySellerIdAsync(sellerId))
            .ReturnsAsync(new List<ContactRequest>());

        var result = await _sut.Handle(
            new GetContactRequestsBySellerQuery { SellerId = sellerId },
            CancellationToken.None);

        result.Should().BeEmpty();
    }
}

// ============================================================================
// GET CONTACT REQUEST DETAIL
// ============================================================================

public class GetContactRequestDetailQueryHandlerTests
{
    private readonly Mock<IContactRequestRepository> _contactRequestRepo = new();
    private readonly Mock<IContactMessageRepository> _contactMessageRepo = new();
    private readonly GetContactRequestDetailQueryHandler _sut;

    public GetContactRequestDetailQueryHandlerTests()
    {
        _sut = new GetContactRequestDetailQueryHandler(
            _contactRequestRepo.Object,
            _contactMessageRepo.Object);
    }

    [Fact]
    public async Task Handle_AuthorizedBuyer_ReturnsDetailWithMessages()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var request = new ContactRequest(Guid.NewGuid(), buyerId, sellerId, "Honda Civic 2024", "Juan", "j@t.com", "Hi");

        var messages = new List<ContactMessage>
        {
            new(request.Id, buyerId, "Is it available?", true),
            new(request.Id, sellerId, "Yes it is!", false)
        };

        _contactRequestRepo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);
        _contactMessageRepo.Setup(r => r.GetByContactRequestIdAsync(request.Id)).ReturnsAsync(messages);

        var query = new GetContactRequestDetailQuery
        {
            ContactRequestId = request.Id,
            CurrentUserId = buyerId
        };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(request.Id);
        result.Subject.Should().Be("Honda Civic 2024");
        result.Messages.Should().HaveCount(2);
        result.Messages[0].IsFromBuyer.Should().BeTrue();
        result.Messages[1].IsFromBuyer.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AuthorizedSeller_ReturnsDetail()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var request = new ContactRequest(Guid.NewGuid(), buyerId, sellerId, "Test", "Juan", "j@t.com", "Hi");

        _contactRequestRepo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);
        _contactMessageRepo.Setup(r => r.GetByContactRequestIdAsync(request.Id))
            .ReturnsAsync(new List<ContactMessage>());

        var query = new GetContactRequestDetailQuery
        {
            ContactRequestId = request.Id,
            CurrentUserId = sellerId
        };

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNull()
    {
        var requestId = Guid.NewGuid();
        _contactRequestRepo.Setup(r => r.GetByIdAsync(requestId)).ReturnsAsync((ContactRequest?)null);

        var query = new GetContactRequestDetailQuery
        {
            ContactRequestId = requestId,
            CurrentUserId = Guid.NewGuid()
        };

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var request = new ContactRequest(Guid.NewGuid(), buyerId, sellerId, "Test", "Juan", "j@t.com", "Hi");

        _contactRequestRepo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);

        var query = new GetContactRequestDetailQuery
        {
            ContactRequestId = request.Id,
            CurrentUserId = Guid.NewGuid() // random user
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(query, CancellationToken.None));
    }
}

// ============================================================================
// GET UNREAD COUNT
// ============================================================================

public class GetUnreadCountQueryHandlerTests
{
    private readonly Mock<IContactMessageRepository> _contactMessageRepo = new();
    private readonly GetUnreadCountQueryHandler _sut;

    public GetUnreadCountQueryHandlerTests()
    {
        _sut = new GetUnreadCountQueryHandler(_contactMessageRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsUnreadCount()
    {
        var userId = Guid.NewGuid();
        _contactMessageRepo.Setup(r => r.GetUnreadCountForUserAsync(userId)).ReturnsAsync(5);

        var result = await _sut.Handle(
            new GetUnreadCountQuery { UserId = userId },
            CancellationToken.None);

        result.Should().Be(5);
    }

    [Fact]
    public async Task Handle_NoUnread_ReturnsZero()
    {
        var userId = Guid.NewGuid();
        _contactMessageRepo.Setup(r => r.GetUnreadCountForUserAsync(userId)).ReturnsAsync(0);

        var result = await _sut.Handle(
            new GetUnreadCountQuery { UserId = userId },
            CancellationToken.None);

        result.Should().Be(0);
    }
}

// ============================================================================
// MARK MESSAGE AS READ
// ============================================================================

public class MarkMessageAsReadCommandHandlerTests
{
    private readonly Mock<IContactMessageRepository> _contactMessageRepo = new();
    private readonly Mock<IContactRequestRepository> _contactRequestRepo = new();
    private readonly MarkMessageAsReadCommandHandler _sut;

    public MarkMessageAsReadCommandHandlerTests()
    {
        _sut = new MarkMessageAsReadCommandHandler(
            _contactMessageRepo.Object,
            _contactRequestRepo.Object);
    }

    [Fact]
    public async Task Handle_AuthorizedUser_MarksMessageAsRead()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var message = new ContactMessage(requestId, buyerId, "Hi", true);
        var contactRequest = new ContactRequest(Guid.NewGuid(), buyerId, sellerId, "Test", "B", "b@t.com", "Hi");

        // Override the auto-generated Id to match requestId
        typeof(ContactRequest).GetProperty("Id")!.SetValue(contactRequest, requestId);

        _contactMessageRepo.Setup(r => r.GetByIdAsync(message.Id)).ReturnsAsync(message);
        _contactRequestRepo.Setup(r => r.GetByIdAsync(requestId)).ReturnsAsync(contactRequest);

        var command = new MarkMessageAsReadCommand
        {
            MessageId = message.Id,
            CurrentUserId = sellerId // seller marks buyer's message as read
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(MediatR.Unit.Value);
        _contactMessageRepo.Verify(r => r.MarkAsReadAsync(message.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_MessageNotFound_ThrowsKeyNotFoundException()
    {
        var messageId = Guid.NewGuid();
        _contactMessageRepo.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync((ContactMessage?)null);

        var command = new MarkMessageAsReadCommand
        {
            MessageId = messageId,
            CurrentUserId = Guid.NewGuid()
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ContactRequestNotFound_ThrowsKeyNotFoundException()
    {
        var requestId = Guid.NewGuid();
        var message = new ContactMessage(requestId, Guid.NewGuid(), "Hi", true);

        _contactMessageRepo.Setup(r => r.GetByIdAsync(message.Id)).ReturnsAsync(message);
        _contactRequestRepo.Setup(r => r.GetByIdAsync(requestId)).ReturnsAsync((ContactRequest?)null);

        var command = new MarkMessageAsReadCommand
        {
            MessageId = message.Id,
            CurrentUserId = Guid.NewGuid()
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var message = new ContactMessage(requestId, buyerId, "Hi", true);
        var contactRequest = new ContactRequest(Guid.NewGuid(), buyerId, sellerId, "Test", "B", "b@t.com", "Hi");
        typeof(ContactRequest).GetProperty("Id")!.SetValue(contactRequest, requestId);

        _contactMessageRepo.Setup(r => r.GetByIdAsync(message.Id)).ReturnsAsync(message);
        _contactRequestRepo.Setup(r => r.GetByIdAsync(requestId)).ReturnsAsync(contactRequest);

        var command = new MarkMessageAsReadCommand
        {
            MessageId = message.Id,
            CurrentUserId = Guid.NewGuid() // random user
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(command, CancellationToken.None));
    }
}
