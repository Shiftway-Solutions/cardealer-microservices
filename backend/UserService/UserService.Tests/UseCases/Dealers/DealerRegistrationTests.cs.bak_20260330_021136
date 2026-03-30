using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Application.Metrics;
using UserService.Application.UseCases.Dealers.CreateDealer;
using UserService.Application.UseCases.Dealers.VerifyDealer;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using Xunit;

namespace UserService.Tests.UseCases.Dealers;

public class CreateDealerTests
{
    private readonly Mock<IDealerRepository> _dealerRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IEventPublisher> _eventPublisherMock = new();
    private readonly Mock<IAuditServiceClient> _auditClientMock = new();
    private readonly UserServiceMetrics _metrics = new();
    private readonly Mock<ILogger<CreateDealerCommandHandler>> _loggerMock = new();
    private readonly CreateDealerCommandHandler _handler;

    public CreateDealerTests()
    {
        _handler = new CreateDealerCommandHandler(
            _dealerRepoMock.Object,
            _userRepoMock.Object,
            _eventPublisherMock.Object,
            _auditClientMock.Object,
            _metrics,
            _loggerMock.Object);
    }

    private CreateDealerRequest CreateValidRequest(Guid? ownerUserId = null)
    {
        return new CreateDealerRequest
        {
            OwnerUserId = ownerUserId ?? Guid.NewGuid(),
            BusinessName = "Auto Premium RD",
            TradeName = "Auto Premium",
            Description = "Dealer de vehículos premium en Santo Domingo",
            DealerType = DealerType.Independent,
            Email = "info@autopremiumrd.com",
            Phone = "809-555-1234",
            WhatsApp = "809-555-1234",
            Address = "Av. 27 de Febrero #100",
            City = "Santo Domingo",
            State = "Distrito Nacional",
            Country = "DO",
            BusinessRegistrationNumber = "123456789",
            TaxId = "RNC-12345678901"
        };
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesDealerWithPendingStatus()
    {
        // Arrange
        var request = CreateValidRequest();
        var user = new User { Id = request.OwnerUserId, Email = "owner@test.com", FirstName = "Test", LastName = "User" };

        _userRepoMock.Setup(r => r.GetByIdAsync(request.OwnerUserId)).ReturnsAsync(user);
        _dealerRepoMock.Setup(r => r.GetByOwnerIdAsync(request.OwnerUserId)).ReturnsAsync((Dealer?)null);
        _dealerRepoMock.Setup(r => r.AddAsync(It.IsAny<Dealer>()))
            .ReturnsAsync((Dealer d) => d);

        var command = new CreateDealerCommand(request);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.BusinessName.Should().Be("Auto Premium RD");
        result.VerificationStatus.Should().Be(DealerVerificationStatus.Pending);
        result.IsActive.Should().BeFalse("Dealer should be inactive until admin approves");
        result.OwnerUserId.Should().Be(request.OwnerUserId);
        result.Email.Should().Be("info@autopremiumrd.com");
        result.City.Should().Be("Santo Domingo");

        _dealerRepoMock.Verify(r => r.AddAsync(It.Is<Dealer>(d =>
            d.BusinessName == "Auto Premium RD" &&
            d.VerificationStatus == DealerVerificationStatus.Pending &&
            d.IsActive == false
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidRequest_PublishesDealerRegistrationRequestedEvent()
    {
        // Arrange
        var request = CreateValidRequest();
        var user = new User { Id = request.OwnerUserId, Email = "owner@test.com" };

        _userRepoMock.Setup(r => r.GetByIdAsync(request.OwnerUserId)).ReturnsAsync(user);
        _dealerRepoMock.Setup(r => r.GetByOwnerIdAsync(request.OwnerUserId)).ReturnsAsync((Dealer?)null);

        var command = new CreateDealerCommand(request);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _eventPublisherMock.Verify(p =>
            p.PublishAsync(
                It.Is<CarDealer.Contracts.Events.Dealer.DealerRegistrationRequestedEvent>(e =>
                    e.CompanyName == "Auto Premium RD" &&
                    e.OwnerUserId == request.OwnerUserId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidRequest_LogsAudit()
    {
        // Arrange
        var request = CreateValidRequest();
        var user = new User { Id = request.OwnerUserId, Email = "owner@test.com" };

        _userRepoMock.Setup(r => r.GetByIdAsync(request.OwnerUserId)).ReturnsAsync(user);
        _dealerRepoMock.Setup(r => r.GetByOwnerIdAsync(request.OwnerUserId)).ReturnsAsync((Dealer?)null);

        var command = new CreateDealerCommand(request);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _auditClientMock.Verify(a =>
            a.LogDealerRegistrationAsync(
                It.IsAny<Guid>(),
                request.OwnerUserId,
                "Auto Premium RD",
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = CreateValidRequest();
        _userRepoMock.Setup(r => r.GetByIdAsync(request.OwnerUserId)).ReturnsAsync((User?)null);

        var command = new CreateDealerCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserAlreadyHasDealer_ThrowsAlreadyDealerException()
    {
        // Arrange
        var request = CreateValidRequest();
        var user = new User { Id = request.OwnerUserId, Email = "owner@test.com" };
        var existingDealer = new Dealer { Id = Guid.NewGuid(), OwnerUserId = request.OwnerUserId };

        _userRepoMock.Setup(r => r.GetByIdAsync(request.OwnerUserId)).ReturnsAsync(user);
        _dealerRepoMock.Setup(r => r.GetByOwnerIdAsync(request.OwnerUserId)).ReturnsAsync(existingDealer);

        var command = new CreateDealerCommand(request);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        ex.Message.Should().Be("ALREADY_DEALER");
    }

    [Fact]
    public async Task Handle_EventPublishFails_DealerStillCreated()
    {
        // Arrange
        var request = CreateValidRequest();
        var user = new User { Id = request.OwnerUserId, Email = "owner@test.com" };

        _userRepoMock.Setup(r => r.GetByIdAsync(request.OwnerUserId)).ReturnsAsync(user);
        _dealerRepoMock.Setup(r => r.GetByOwnerIdAsync(request.OwnerUserId)).ReturnsAsync((Dealer?)null);
        _eventPublisherMock
            .Setup(p => p.PublishAsync(It.IsAny<CarDealer.Contracts.Events.Dealer.DealerRegistrationRequestedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("RabbitMQ down"));

        var command = new CreateDealerCommand(request);

        // Act — should NOT throw despite event failure
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.BusinessName.Should().Be("Auto Premium RD");
        _dealerRepoMock.Verify(r => r.AddAsync(It.IsAny<Dealer>()), Times.Once);
    }
}

public class VerifyDealerTests
{
    private readonly Mock<IDealerRepository> _dealerRepoMock = new();
    private readonly Mock<IEventPublisher> _eventPublisherMock = new();
    private readonly Mock<IAuditServiceClient> _auditClientMock = new();
    private readonly UserServiceMetrics _metrics = new();
    private readonly Mock<ILogger<VerifyDealerCommandHandler>> _loggerMock = new();
    private readonly VerifyDealerCommandHandler _handler;

    public VerifyDealerTests()
    {
        _handler = new VerifyDealerCommandHandler(
            _dealerRepoMock.Object,
            _eventPublisherMock.Object,
            _auditClientMock.Object,
            _metrics,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ApproveDealer_SetsVerifiedAndActive()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var dealer = new Dealer
        {
            Id = dealerId,
            OwnerUserId = Guid.NewGuid(),
            BusinessName = "Test Dealer",
            VerificationStatus = DealerVerificationStatus.Pending,
            IsActive = false,
            Email = "test@dealer.com",
            Phone = "809-555-0000",
            Address = "Test",
            City = "SD",
            State = "DN"
        };

        _dealerRepoMock.Setup(r => r.GetByIdAsync(dealerId)).ReturnsAsync(dealer);

        var command = new VerifyDealerCommand(dealerId, new VerifyDealerRequest
        {
            IsVerified = true,
            VerifiedByUserId = adminId,
            Notes = "All documents verified"
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.VerificationStatus.Should().Be(DealerVerificationStatus.Verified);
        result.IsActive.Should().BeTrue();
        dealer.VerifiedAt.Should().NotBeNull();
        dealer.VerifiedByUserId.Should().Be(adminId);

        _dealerRepoMock.Verify(r => r.UpdateAsync(It.Is<Dealer>(d =>
            d.VerificationStatus == DealerVerificationStatus.Verified &&
            d.IsActive == true
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_ApproveDealer_PublishesDealerCreatedEvent()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var dealer = new Dealer
        {
            Id = dealerId,
            OwnerUserId = ownerId,
            BusinessName = "Test Dealer",
            VerificationStatus = DealerVerificationStatus.Pending,
            Email = "test@dealer.com",
            Phone = "809-555-0000",
            Address = "Test",
            City = "SD",
            State = "DN"
        };

        _dealerRepoMock.Setup(r => r.GetByIdAsync(dealerId)).ReturnsAsync(dealer);

        var command = new VerifyDealerCommand(dealerId, new VerifyDealerRequest
        {
            IsVerified = true,
            VerifiedByUserId = Guid.NewGuid()
        });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _eventPublisherMock.Verify(p =>
            p.PublishAsync(
                It.Is<CarDealer.Contracts.Events.Dealer.DealerCreatedEvent>(e =>
                    e.DealerId == dealerId &&
                    e.OwnerUserId == ownerId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RejectDealer_SetsRejectedAndInactive()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var dealer = new Dealer
        {
            Id = dealerId,
            OwnerUserId = Guid.NewGuid(),
            BusinessName = "Bad Dealer",
            VerificationStatus = DealerVerificationStatus.Pending,
            IsActive = false,
            Email = "test@dealer.com",
            Phone = "809-555-0000",
            Address = "Test",
            City = "SD",
            State = "DN"
        };

        _dealerRepoMock.Setup(r => r.GetByIdAsync(dealerId)).ReturnsAsync(dealer);

        var command = new VerifyDealerCommand(dealerId, new VerifyDealerRequest
        {
            IsVerified = false,
            VerifiedByUserId = Guid.NewGuid(),
            Notes = "Documentos inválidos"
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.VerificationStatus.Should().Be(DealerVerificationStatus.Rejected);
        result.IsActive.Should().BeFalse();
        dealer.RejectionReason.Should().Be("Documentos inválidos");

        // Should NOT publish DealerCreatedEvent on rejection
        _eventPublisherMock.Verify(p =>
            p.PublishAsync(
                It.IsAny<CarDealer.Contracts.Events.Dealer.DealerCreatedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_DealerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        _dealerRepoMock.Setup(r => r.GetByIdAsync(dealerId)).ReturnsAsync((Dealer?)null);

        var command = new VerifyDealerCommand(dealerId, new VerifyDealerRequest
        {
            IsVerified = true,
            VerifiedByUserId = Guid.NewGuid()
        });

        // Act & Assert
        await Assert.ThrowsAsync<UserService.Shared.Exceptions.NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ApproveDealer_LogsAudit()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var dealer = new Dealer
        {
            Id = dealerId,
            OwnerUserId = Guid.NewGuid(),
            BusinessName = "Test Dealer",
            VerificationStatus = DealerVerificationStatus.Pending,
            Email = "test@dealer.com",
            Phone = "809-555-0000",
            Address = "Test",
            City = "SD",
            State = "DN"
        };

        _dealerRepoMock.Setup(r => r.GetByIdAsync(dealerId)).ReturnsAsync(dealer);

        var command = new VerifyDealerCommand(dealerId, new VerifyDealerRequest
        {
            IsVerified = true,
            VerifiedByUserId = adminId
        });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _auditClientMock.Verify(a =>
            a.LogDealerVerificationAsync(
                dealerId,
                true,
                It.IsAny<string>()),
            Times.Once);
    }
}
