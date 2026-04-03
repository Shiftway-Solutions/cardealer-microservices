using CarDealer.Contracts.Enums;
using ContactService.Application.DTOs;
using ContactService.Application.Features.ContactRequests.Commands;
using ContactService.Domain.Entities;
using ContactService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace ContactService.Tests.Unit.Handlers;

// ============================================================================
// CREATE CONTACT REQUEST
// ============================================================================

public class CreateContactRequestCommandHandlerTests
{
    private readonly Mock<IContactRequestRepository> _contactRequestRepo = new();
    private readonly Mock<IContactMessageRepository> _contactMessageRepo = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IConversationUsageTracker> _usageTracker = new();
    private readonly Mock<IDealerPlanResolver> _planResolver = new();
    private readonly Mock<IConversationOverageRepository> _overageRepo = new();
    private readonly Mock<ILogger<CreateContactRequestCommandHandler>> _logger = new();
    private readonly CreateContactRequestCommandHandler _sut;

    public CreateContactRequestCommandHandlerTests()
    {
        // Default: resolve dealer plan as "elite" and return normal usage
        _planResolver
            .Setup(r => r.GetDealerPlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("elite");
        _usageTracker
            .Setup(t => t.IncrementAndCheckAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationUsageResult
            {
                CurrentCount = 1,
                MaxAllowed = 2000,
                Status = ConversationUsageStatus.Normal,
                JustCrossedWarningThreshold = false,
                JustReachedLimit = false,
                OverageCount = 0,
                ProjectedMonthlyTotal = 30
            });

        _sut = new CreateContactRequestCommandHandler(
            _contactRequestRepo.Object,
            _contactMessageRepo.Object,
            _eventPublisher.Object,
            _usageTracker.Object,
            _planResolver.Object,
            _overageRepo.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesRequestAndInitialMessage()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var command = new CreateContactRequestCommand
        {
            VehicleId = vehicleId,
            SellerId = sellerId,
            BuyerId = buyerId,
            Subject = "Interested in Honda Civic",
            BuyerName = "Juan Perez",
            BuyerEmail = "juan@test.com",
            BuyerPhone = "809-555-1234",
            Message = "Is this still available?"
        };

        _contactRequestRepo
            .Setup(r => r.CreateAsync(It.IsAny<ContactRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactRequest cr, CancellationToken _) => cr);

        _contactMessageRepo
            .Setup(r => r.CreateAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactMessage m, CancellationToken _) => m);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Subject.Should().Be("Interested in Honda Civic");
        result.BuyerName.Should().Be("Juan Perez");
        result.BuyerEmail.Should().Be("juan@test.com");
        result.BuyerPhone.Should().Be("809-555-1234");
        result.VehicleId.Should().Be(vehicleId);
        result.Status.Should().Be("Open");
        result.MessageCount.Should().Be(1);

        _contactRequestRepo.Verify(r => r.CreateAsync(It.IsAny<ContactRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _contactMessageRepo.Verify(r => r.CreateAsync(It.Is<ContactMessage>(
            m => m.Message == "Is this still available?" && m.IsFromBuyer == true), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsContactRequestSummaryDto()
    {
        // Arrange
        var command = new CreateContactRequestCommand
        {
            VehicleId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            BuyerId = Guid.NewGuid(),
            Subject = "Test",
            BuyerName = "Test User",
            BuyerEmail = "test@test.com",
            Message = "Hello"
        };

        _contactRequestRepo
            .Setup(r => r.CreateAsync(It.IsAny<ContactRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactRequest cr, CancellationToken _) => cr);
        _contactMessageRepo
            .Setup(r => r.CreateAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactMessage m, CancellationToken _) => m);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ContactRequestSummaryDto>();
        result.Id.Should().NotBe(Guid.Empty);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_NoBuyerPhone_CreatesRequestWithoutPhone()
    {
        // Arrange
        var command = new CreateContactRequestCommand
        {
            VehicleId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            BuyerId = Guid.NewGuid(),
            Subject = "Test",
            BuyerName = "Test",
            BuyerEmail = "test@test.com",
            BuyerPhone = null,
            Message = "No phone"
        };

        _contactRequestRepo
            .Setup(r => r.CreateAsync(It.IsAny<ContactRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactRequest cr, CancellationToken _) => cr);
        _contactMessageRepo
            .Setup(r => r.CreateAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactMessage m, CancellationToken _) => m);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.BuyerPhone.Should().BeNull();
    }
}

// ============================================================================
// REPLY TO CONTACT REQUEST
// ============================================================================

public class ReplyToContactRequestCommandHandlerTests
{
    private readonly Mock<IContactRequestRepository> _contactRequestRepo = new();
    private readonly Mock<IContactMessageRepository> _contactMessageRepo = new();
    private readonly Mock<ILogger<ReplyToContactRequestCommandHandler>> _logger = new();
    private readonly ReplyToContactRequestCommandHandler _sut;

    public ReplyToContactRequestCommandHandlerTests()
    {
        _sut = new ReplyToContactRequestCommandHandler(
            _contactRequestRepo.Object,
            _contactMessageRepo.Object,
            _logger.Object);
    }

    private ContactRequest CreateTestRequest(Guid buyerId, Guid sellerId, string status = "Open")
    {
        var cr = new ContactRequest(Guid.NewGuid(), buyerId, sellerId, "Test", "Buyer", "buyer@test.com", "Hi");
        cr.Status = status;
        return cr;
    }

    [Fact]
    public async Task Handle_BuyerReplies_CreatesMessageFromBuyer()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var contactRequest = CreateTestRequest(buyerId, sellerId);

        _contactRequestRepo.Setup(r => r.GetByIdAsync(contactRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contactRequest);
        _contactMessageRepo.Setup(r => r.CreateAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactMessage m, CancellationToken _) => m);

        var command = new ReplyToContactRequestCommand
        {
            ContactRequestId = contactRequest.Id,
            CurrentUserId = buyerId,
            Message = "Still interested"
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFromBuyer.Should().BeTrue();
        result.Message.Should().Be("Still interested");
        result.SenderId.Should().Be(buyerId);
        _contactRequestRepo.Verify(r => r.UpdateAsync(It.IsAny<ContactRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SellerRepliesOpenRequest_UpdatesStatusToResponded()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var contactRequest = CreateTestRequest(buyerId, sellerId, "Open");

        _contactRequestRepo.Setup(r => r.GetByIdAsync(contactRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contactRequest);
        _contactMessageRepo.Setup(r => r.CreateAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactMessage m, CancellationToken _) => m);
        _contactRequestRepo.Setup(r => r.UpdateAsync(It.IsAny<ContactRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactRequest cr, CancellationToken _) => cr);

        var command = new ReplyToContactRequestCommand
        {
            ContactRequestId = contactRequest.Id,
            CurrentUserId = sellerId,
            Message = "Yes, still available"
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFromBuyer.Should().BeFalse();
        contactRequest.Status.Should().Be("Responded");
        contactRequest.RespondedAt.Should().NotBeNull();
        _contactRequestRepo.Verify(r => r.UpdateAsync(contactRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SellerRepliesAlreadyResponded_DoesNotUpdateStatus()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var contactRequest = CreateTestRequest(buyerId, sellerId, "Responded");

        _contactRequestRepo.Setup(r => r.GetByIdAsync(contactRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contactRequest);
        _contactMessageRepo.Setup(r => r.CreateAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactMessage m, CancellationToken _) => m);

        var command = new ReplyToContactRequestCommand
        {
            ContactRequestId = contactRequest.Id,
            CurrentUserId = sellerId,
            Message = "Follow up"
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _contactRequestRepo.Verify(r => r.UpdateAsync(It.IsAny<ContactRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        _contactRequestRepo.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>())).ReturnsAsync((ContactRequest?)null);

        var command = new ReplyToContactRequestCommand
        {
            ContactRequestId = requestId,
            CurrentUserId = Guid.NewGuid(),
            Message = "Test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var randomUserId = Guid.NewGuid();
        var contactRequest = CreateTestRequest(buyerId, sellerId);

        _contactRequestRepo.Setup(r => r.GetByIdAsync(contactRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contactRequest);

        var command = new ReplyToContactRequestCommand
        {
            ContactRequestId = contactRequest.Id,
            CurrentUserId = randomUserId,
            Message = "I shouldn't be here"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(command, CancellationToken.None));
    }
}

// ============================================================================
// UPDATE CONTACT REQUEST STATUS
// ============================================================================

public class UpdateContactRequestStatusCommandHandlerTests
{
    private readonly Mock<IContactRequestRepository> _contactRequestRepo = new();
    private readonly Mock<IContactMessageRepository> _contactMessageRepo = new();
    private readonly UpdateContactRequestStatusCommandHandler _sut;

    public UpdateContactRequestStatusCommandHandlerTests()
    {
        _sut = new UpdateContactRequestStatusCommandHandler(_contactRequestRepo.Object, _contactMessageRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesStatus()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var contactRequest = new ContactRequest(Guid.NewGuid(), buyerId, sellerId, "Test", "Buyer", "b@t.com", "Hi");

        _contactRequestRepo.Setup(r => r.GetByIdAsync(contactRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contactRequest);
        _contactRequestRepo.Setup(r => r.UpdateAsync(It.IsAny<ContactRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactRequest cr, CancellationToken _) => cr);

        var command = new UpdateContactRequestStatusCommand
        {
            ContactRequestId = contactRequest.Id,
            CurrentUserId = buyerId,
            NewStatus = "Closed"
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(MediatR.Unit.Value);
        contactRequest.Status.Should().Be("Closed");
        _contactRequestRepo.Verify(r => r.UpdateAsync(contactRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsKeyNotFoundException()
    {
        var requestId = Guid.NewGuid();
        _contactRequestRepo.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>())).ReturnsAsync((ContactRequest?)null);

        var command = new UpdateContactRequestStatusCommand
        {
            ContactRequestId = requestId,
            CurrentUserId = Guid.NewGuid(),
            NewStatus = "Closed"
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var contactRequest = new ContactRequest(Guid.NewGuid(), buyerId, sellerId, "Test", "B", "b@t.com", "Hi");

        _contactRequestRepo.Setup(r => r.GetByIdAsync(contactRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contactRequest);

        var command = new UpdateContactRequestStatusCommand
        {
            ContactRequestId = contactRequest.Id,
            CurrentUserId = Guid.NewGuid(), // random user
            NewStatus = "Closed"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(command, CancellationToken.None));
    }
}

// ============================================================================
// DELETE CONTACT REQUEST
// ============================================================================

public class DeleteContactRequestCommandHandlerTests
{
    private readonly Mock<IContactRequestRepository> _contactRequestRepo = new();
    private readonly DeleteContactRequestCommandHandler _sut;

    public DeleteContactRequestCommandHandlerTests()
    {
        _sut = new DeleteContactRequestCommandHandler(_contactRequestRepo.Object);
    }

    [Fact]
    public async Task Handle_AuthorizedBuyer_DeletesRequest()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var contactRequest = new ContactRequest(Guid.NewGuid(), buyerId, sellerId, "Test", "B", "b@t.com", "Hi");

        _contactRequestRepo.Setup(r => r.GetByIdAsync(contactRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contactRequest);

        var command = new DeleteContactRequestCommand
        {
            ContactRequestId = contactRequest.Id,
            CurrentUserId = buyerId
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(MediatR.Unit.Value);
        _contactRequestRepo.Verify(r => r.DeleteAsync(contactRequest.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AuthorizedSeller_DeletesRequest()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var contactRequest = new ContactRequest(Guid.NewGuid(), buyerId, sellerId, "Test", "B", "b@t.com", "Hi");

        _contactRequestRepo.Setup(r => r.GetByIdAsync(contactRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contactRequest);

        var command = new DeleteContactRequestCommand
        {
            ContactRequestId = contactRequest.Id,
            CurrentUserId = sellerId
        };

        await _sut.Handle(command, CancellationToken.None);

        _contactRequestRepo.Verify(r => r.DeleteAsync(contactRequest.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsKeyNotFoundException()
    {
        var requestId = Guid.NewGuid();
        _contactRequestRepo.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>())).ReturnsAsync((ContactRequest?)null);

        var command = new DeleteContactRequestCommand
        {
            ContactRequestId = requestId,
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
        var contactRequest = new ContactRequest(Guid.NewGuid(), buyerId, sellerId, "Test", "B", "b@t.com", "Hi");

        _contactRequestRepo.Setup(r => r.GetByIdAsync(contactRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contactRequest);

        var command = new DeleteContactRequestCommand
        {
            ContactRequestId = contactRequest.Id,
            CurrentUserId = Guid.NewGuid()
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(command, CancellationToken.None));
    }
}
