using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using CarDealer.Contracts.Events.KYC;
using KYCService.Application.Commands;
using KYCService.Application.Handlers;
using KYCService.Domain.Entities;
using KYCService.Domain.Interfaces;

namespace KYCService.Tests.Handlers;

/// <summary>
/// Tests for ApproveKYCProfileHandler with notification integration
/// </summary>
public class ApproveKYCProfileHandlerNotificationTests
{
    private readonly Mock<IKYCProfileRepository> _repositoryMock;
    private readonly Mock<IKYCEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<ApproveKYCProfileHandler>> _loggerMock;
    private readonly ApproveKYCProfileHandler _handler;

    public ApproveKYCProfileHandlerNotificationTests()
    {
        _repositoryMock = new Mock<IKYCProfileRepository>();
        _eventPublisherMock = new Mock<IKYCEventPublisher>();
        _loggerMock = new Mock<ILogger<ApproveKYCProfileHandler>>();
        _handler = new ApproveKYCProfileHandler(
            _repositoryMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidApproval_ShouldUpdateStatusAndNotify()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var profile = new KYCProfile
        {
            Id = profileId,
            UserId = userId,
            Status = KYCStatus.UnderReview,
            FullName = "Juan Pérez",
            Email = "juan@test.com"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile p, CancellationToken _) => p);

        var command = new ApproveKYCProfileCommand
        {
            Id = profileId,
            ApprovedBy = Guid.NewGuid(),
            Notes = "All documents verified",
            ValidityDays = 365
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(KYCStatus.Approved);
        result.ApprovedAt.Should().NotBeNull();
        result.ExpiresAt.Should().NotBeNull();

        _repositoryMock.Verify(
            r => r.UpdateAsync(It.Is<KYCProfile>(p =>
                p.Status == KYCStatus.Approved &&
                p.ApprovedBy == command.ApprovedBy &&
                p.ApprovalNotes == "All documents verified"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentProfile_ShouldThrow()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile?)null);

        var command = new ApproveKYCProfileCommand
        {
            Id = Guid.NewGuid(),
            ApprovedBy = Guid.NewGuid(),
            ValidityDays = 365
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}

/// <summary>
/// Tests for RejectKYCProfileHandler with notification integration
/// </summary>
public class RejectKYCProfileHandlerNotificationTests
{
    private readonly Mock<IKYCProfileRepository> _repositoryMock;
    private readonly Mock<IKYCEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<RejectKYCProfileHandler>> _loggerMock;
    private readonly RejectKYCProfileHandler _handler;

    public RejectKYCProfileHandlerNotificationTests()
    {
        _repositoryMock = new Mock<IKYCProfileRepository>();
        _eventPublisherMock = new Mock<IKYCEventPublisher>();
        _loggerMock = new Mock<ILogger<RejectKYCProfileHandler>>();
        _handler = new RejectKYCProfileHandler(
            _repositoryMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRejection_ShouldUpdateStatusAndSetReason()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = new KYCProfile
        {
            Id = profileId,
            UserId = Guid.NewGuid(),
            Status = KYCStatus.UnderReview,
            FullName = "María García",
            Email = "maria@test.com"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile p, CancellationToken _) => p);

        var command = new RejectKYCProfileCommand
        {
            Id = profileId,
            RejectedBy = Guid.NewGuid(),
            RejectionReason = "Document image is blurry"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(KYCStatus.Rejected);

        _repositoryMock.Verify(
            r => r.UpdateAsync(It.Is<KYCProfile>(p =>
                p.Status == KYCStatus.Rejected &&
                p.RejectedBy == command.RejectedBy &&
                p.RejectionReason == "Document image is blurry"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentProfile_ShouldThrow()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile?)null);

        var command = new RejectKYCProfileCommand
        {
            Id = Guid.NewGuid(),
            RejectedBy = Guid.NewGuid(),
            RejectionReason = "Test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}

/// <summary>
/// Tests for KYCProfileDraftRepository
/// </summary>
public class KYCProfileDraftEntityTests
{
    [Fact]
    public void KYCProfileDraft_DefaultValues_AreCorrect()
    {
        // Act
        var draft = new KYCProfileDraft();

        // Assert
        draft.CurrentStep.Should().Be(1);
        draft.FormData.Should().Be("{}");
        draft.IsSubmitted.Should().BeFalse();
        draft.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        draft.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void KYCProfileDraft_CanStoreFormDataJson()
    {
        // Arrange
        var formJson = """{"personalInfo":{"firstName":"Juan","lastName":"Pérez"},"address":{"city":"Santo Domingo"}}""";

        // Act
        var draft = new KYCProfileDraft
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CurrentStep = 2,
            FormData = formJson
        };

        // Assert
        draft.FormData.Should().Contain("Juan");
        draft.FormData.Should().Contain("Santo Domingo");
        draft.CurrentStep.Should().Be(2);
    }
}

/// <summary>
/// Tests that verify the RabbitMQ event publisher is called on approve / reject
/// </summary>
public class KYCEventPublisherInvocationTests
{
    [Fact]
    public async Task ApproveHandler_ShouldPublishStatusChangedEvent()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var userId    = Guid.NewGuid();
        var profile   = new KYCProfile
        {
            Id       = profileId,
            UserId   = userId,
            Status   = KYCStatus.UnderReview,
            FullName = "Juan Pérez",
            Email    = "juan@test.com"
        };

        var repositoryMock     = new Mock<IKYCProfileRepository>();
        var eventPublisherMock = new Mock<IKYCEventPublisher>();
        var loggerMock         = new Mock<ILogger<ApproveKYCProfileHandler>>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile p, CancellationToken _) => p);

        eventPublisherMock
            .Setup(p => p.PublishStatusChangedAsync(
                It.IsAny<KYCProfileStatusChangedEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ApproveKYCProfileHandler(
            repositoryMock.Object, eventPublisherMock.Object, loggerMock.Object);

        var command = new ApproveKYCProfileCommand
        {
            Id = profileId, ApprovedBy = Guid.NewGuid(), Notes = "OK", ValidityDays = 365
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — publisher called exactly once with NewStatus = "Approved"
        eventPublisherMock.Verify(
            p => p.PublishStatusChangedAsync(
                It.Is<KYCProfileStatusChangedEvent>(e =>
                    e.NewStatus == "Approved" &&
                    e.ProfileId == profileId &&
                    e.UserId    == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RejectHandler_ShouldPublishStatusChangedEvent()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile   = new KYCProfile
        {
            Id       = profileId,
            UserId   = Guid.NewGuid(),
            Status   = KYCStatus.UnderReview,
            FullName = "María García",
            Email    = "maria@test.com"
        };

        var repositoryMock     = new Mock<IKYCProfileRepository>();
        var eventPublisherMock = new Mock<IKYCEventPublisher>();
        var loggerMock         = new Mock<ILogger<RejectKYCProfileHandler>>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile p, CancellationToken _) => p);

        eventPublisherMock
            .Setup(p => p.PublishStatusChangedAsync(
                It.IsAny<KYCProfileStatusChangedEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new RejectKYCProfileHandler(
            repositoryMock.Object, eventPublisherMock.Object, loggerMock.Object);

        var command = new RejectKYCProfileCommand
        {
            Id = profileId, RejectedBy = Guid.NewGuid(),
            RejectionReason = "Imágenes borrosas"
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — publisher called exactly once with NewStatus = "Rejected"
        eventPublisherMock.Verify(
            p => p.PublishStatusChangedAsync(
                It.Is<KYCProfileStatusChangedEvent>(e =>
                    e.NewStatus   == "Rejected" &&
                    e.ProfileId   == profileId &&
                    e.Reason      == "Imágenes borrosas"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
