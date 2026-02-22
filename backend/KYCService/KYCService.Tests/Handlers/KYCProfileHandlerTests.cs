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
/// Tests unitarios para CreateKYCProfileHandler
/// </summary>
public class CreateKYCProfileHandlerTests
{
    private readonly Mock<IKYCProfileRepository> _repositoryMock;
    private readonly CreateKYCProfileHandler _handler;

    public CreateKYCProfileHandlerTests()
    {
        _repositoryMock = new Mock<IKYCProfileRepository>();
        _handler = new CreateKYCProfileHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateProfile()
    {
        // Arrange
        var command = new CreateKYCProfileCommand
        {
            UserId = Guid.NewGuid(),
            FullName = "Juan Pérez",
            LastName = "Pérez",
            EntityType = EntityType.Individual,
            PrimaryDocumentType = DocumentType.Cedula,
            PrimaryDocumentNumber = "00112345678",
            Email = "juan@test.com",
            Phone = "809-555-1234",
            Address = "Calle Principal 123",
            City = "Santo Domingo",
            Province = "Distrito Nacional",
            Country = "DO"
        };

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile p, CancellationToken ct) => p);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be("Juan Pérez");
        result.Status.Should().Be(KYCStatus.Pending);
        result.RiskLevel.Should().Be(RiskLevel.Low);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PEPUser_ShouldAssignHighRisk()
    {
        // Arrange
        var command = new CreateKYCProfileCommand
        {
            UserId = Guid.NewGuid(),
            FullName = "Político Importante",
            EntityType = EntityType.Individual,
            IsPEP = true,
            PEPPosition = "Senador de la República",
            PrimaryDocumentType = DocumentType.Cedula,
            Country = "DO"
        };

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile p, CancellationToken ct) => p);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsPEP.Should().BeTrue();
        // PEP status triggers high risk
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<KYCProfile>(p => p.RiskLevel == RiskLevel.High && p.RiskScore >= 70),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BusinessEntity_ShouldSetEntityType()
    {
        // Arrange
        var command = new CreateKYCProfileCommand
        {
            UserId = Guid.NewGuid(),
            FullName = "Empresa Test SRL",
            EntityType = EntityType.Business,
            BusinessName = "Empresa Test SRL",
            RNC = "123456789",
            BusinessType = "Comercio",
            PrimaryDocumentType = DocumentType.RNC,
            Country = "DO"
        };

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile p, CancellationToken ct) => p);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.EntityType.Should().Be(EntityType.Business);
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<KYCProfile>(p => p.BusinessName == "Empresa Test SRL"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldSetCreatedAt()
    {
        // Arrange
        var command = new CreateKYCProfileCommand
        {
            UserId = Guid.NewGuid(),
            FullName = "Test User",
            EntityType = EntityType.Individual,
            PrimaryDocumentType = DocumentType.Cedula
        };

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile p, CancellationToken ct) => p);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

/// <summary>
/// Tests unitarios para UpdateKYCProfileHandler
/// </summary>
public class UpdateKYCProfileHandlerTests
{
    private readonly Mock<IKYCProfileRepository> _repositoryMock;
    private readonly UpdateKYCProfileHandler _handler;

    public UpdateKYCProfileHandlerTests()
    {
        _repositoryMock = new Mock<IKYCProfileRepository>();
        _handler = new UpdateKYCProfileHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingProfile_ShouldUpdateFields()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var existingProfile = new KYCProfile
        {
            Id = profileId,
            UserId = Guid.NewGuid(),
            FullName = "Original Name",
            Email = "original@test.com",
            Status = KYCStatus.Pending
        };

        var command = new UpdateKYCProfileCommand
        {
            Id = profileId,
            FullName = "Updated Name",
            Email = "updated@test.com"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile p, CancellationToken ct) => p);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be("Updated Name");
        result.Email.Should().Be("updated@test.com");
    }

    [Fact]
    public async Task Handle_NonExistingProfile_ShouldThrowException()
    {
        // Arrange
        var command = new UpdateKYCProfileCommand
        {
            Id = Guid.NewGuid(),
            FullName = "Test"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PartialUpdate_ShouldOnlyUpdateProvidedFields()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var existingProfile = new KYCProfile
        {
            Id = profileId,
            UserId = Guid.NewGuid(),
            FullName = "Original Name",
            Email = "original@test.com",
            Phone = "809-555-1234",
            Address = "Original Address"
        };

        var command = new UpdateKYCProfileCommand
        {
            Id = profileId,
            Phone = "809-555-9999" // Only updating phone
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile p, CancellationToken ct) => p);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Phone.Should().Be("809-555-9999");
        result.FullName.Should().Be("Original Name"); // Should remain unchanged
        result.Email.Should().Be("original@test.com"); // Should remain unchanged
    }
}

/// <summary>
/// Tests unitarios para ApproveKYCProfileHandler
/// </summary>
public class ApproveKYCProfileHandlerTests
{
    private readonly Mock<IKYCProfileRepository> _repositoryMock;
    private readonly Mock<IKYCEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<ApproveKYCProfileHandler>> _loggerMock;
    private readonly ApproveKYCProfileHandler _handler;

    public ApproveKYCProfileHandlerTests()
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
    public async Task Handle_PendingProfile_ShouldApprove()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var existingProfile = new KYCProfile
        {
            Id = profileId,
            UserId = Guid.NewGuid(),
            FullName = "Test User",
            Status = KYCStatus.UnderReview
        };

        var command = new ApproveKYCProfileCommand
        {
            Id = profileId,
            ApprovedBy = Guid.NewGuid(),
            ApprovedByName = "Admin User",
            ValidityDays = 365
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile p, CancellationToken ct) => p);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(KYCStatus.Approved);
        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<KYCProfile>(p => p.ApprovedBy == command.ApprovedBy),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistingProfile_ShouldThrowException()
    {
        // Arrange
        var command = new ApproveKYCProfileCommand
        {
            Id = Guid.NewGuid(),
            ApprovedBy = Guid.NewGuid(),
            ApprovedByName = "Admin"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Approval_ShouldSetExpiryDate()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var existingProfile = new KYCProfile
        {
            Id = profileId,
            Status = KYCStatus.UnderReview
        };

        var command = new ApproveKYCProfileCommand
        {
            Id = profileId,
            ApprovedBy = Guid.NewGuid(),
            ApprovedByName = "Admin",
            ValidityDays = 365
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile p, CancellationToken ct) => p);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ExpiresAt.Should().NotBeNull();
        result.ExpiresAt!.Value.Should().BeCloseTo(DateTime.UtcNow.AddDays(365), TimeSpan.FromMinutes(1));
    }
}

/// <summary>
/// Tests unitarios para RejectKYCProfileHandler
/// </summary>
public class RejectKYCProfileHandlerTests
{
    private readonly Mock<IKYCProfileRepository> _repositoryMock;
    private readonly Mock<IKYCEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<RejectKYCProfileHandler>> _loggerMock;
    private readonly RejectKYCProfileHandler _handler;

    public RejectKYCProfileHandlerTests()
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
    public async Task Handle_PendingProfile_ShouldReject()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var existingProfile = new KYCProfile
        {
            Id = profileId,
            UserId = Guid.NewGuid(),
            FullName = "Test User",
            Status = KYCStatus.UnderReview
        };

        var command = new RejectKYCProfileCommand
        {
            Id = profileId,
            RejectedBy = Guid.NewGuid(),
            RejectedByName = "Compliance Officer",
            RejectionReason = "Documentos ilegibles"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<KYCProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile p, CancellationToken ct) => p);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(KYCStatus.Rejected);
        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<KYCProfile>(p => p.RejectionReason == "Documentos ilegibles"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistingProfile_ShouldThrowException()
    {
        // Arrange
        var command = new RejectKYCProfileCommand
        {
            Id = Guid.NewGuid(),
            RejectedBy = Guid.NewGuid(),
            RejectedByName = "Admin",
            RejectionReason = "Test reason"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KYCProfile?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }
}
