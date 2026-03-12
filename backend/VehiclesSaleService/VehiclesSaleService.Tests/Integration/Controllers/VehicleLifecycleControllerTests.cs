using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VehiclesSaleService.Api.Controllers;
using VehiclesSaleService.Domain.Entities;
using VehiclesSaleService.Domain.Interfaces;
using VehiclesSaleService.Infrastructure.Messaging;
using VehiclesSaleService.Infrastructure.Persistence;
using CarDealer.Shared.MultiTenancy;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Caching.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using VehicleDriveType = VehiclesSaleService.Domain.Entities.DriveType;
using System.Net.Http;

namespace VehiclesSaleService.Tests.Integration.Controllers;

/// <summary>
/// Tests for vehicle lifecycle endpoints: publish, unpublish, sold, feature, views
/// </summary>
public class VehicleLifecycleControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly VehiclesController _controller;
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<VehiclesController>> _loggerMock;
    private readonly Mock<IConfigurationServiceClient> _configClientMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<VehiclesSaleService.Application.Interfaces.IDealerVerificationClient> _dealerVerificationClientMock;
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    public VehicleLifecycleControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"VehiclesTestDb_{Guid.NewGuid()}")
            .Options;

        // Create mock tenant context for testing
        // Setting CurrentDealerId to null allows all vehicles to pass the query filter
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.CurrentDealerId).Returns((Guid?)null);
        tenantContextMock.Setup(t => t.HasDealerContext).Returns(false);

        _context = new ApplicationDbContext(options, tenantContextMock.Object);
        _vehicleRepositoryMock = new Mock<IVehicleRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<VehiclesController>>();
        _configClientMock = new Mock<IConfigurationServiceClient>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _dealerVerificationClientMock = new Mock<VehiclesSaleService.Application.Interfaces.IDealerVerificationClient>();

        // Return the default value for any config key so validation uses production-safe defaults
        _configClientMock
            .Setup(c => c.GetDecimalAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, decimal defaultValue, CancellationToken _) => defaultValue);
        _configClientMock
            .Setup(c => c.GetIntAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, int defaultValue, CancellationToken _) => defaultValue);
        _configClientMock
            .Setup(c => c.GetValueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, string defaultValue, CancellationToken _) => defaultValue);

        // Default: dealers are verified (tests that don't test KYC shouldn't fail due to this)
        _dealerVerificationClientMock
            .Setup(c => c.IsDealerVerifiedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _controller = new VehiclesController(
            _vehicleRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object,
            _context,
            _configClientMock.Object,
            _httpClientFactoryMock.Object,
            _dealerVerificationClientMock.Object,
            _cacheServiceMock.Object
        );

        // Set up HttpContext with Admin role so IsOwnerOrAdmin() succeeds
        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminUser }
        };
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Publish Tests

    [Fact]
    public async Task Publish_VehicleNotFound_ReturnsNotFound()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();

        // Act
        var result = await _controller.Publish(vehicleId, null);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Publish_DraftVehicle_WithValidData_SetsPendingReview()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Draft);
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Publish(vehicleId, new PublishVehicleRequest { DisclaimerAccepted = true });

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result.Result!).Value as PublishVehicleResponse;
        response.Should().NotBeNull();
        response!.Status.Should().Be(VehicleStatus.PendingReview);
        response.Message.Should().Contain("revisión");
    }

    [Fact]
    public async Task Publish_AlreadyActiveVehicle_ReturnsBadRequest()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Active);
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Publish(vehicleId, null);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Publish_VehicleWithoutImages_ReturnsBadRequest()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Draft);
        vehicle.Images.Clear(); // Remove images
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Publish(vehicleId, null);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Publish_VehicleWithoutContactInfo_ReturnsBadRequest()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Draft);
        vehicle.SellerPhone = null;
        vehicle.SellerEmail = null;
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Publish(vehicleId, null);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Publish_RejectedVehicle_ResubmitsToPendingReview()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Rejected);
        vehicle.RejectionReason = "Previous issue";
        vehicle.RejectionCount = 1;
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Publish(vehicleId, new PublishVehicleRequest { DisclaimerAccepted = true });

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result.Result!).Value as PublishVehicleResponse;
        response.Should().NotBeNull();
        response!.Status.Should().Be(VehicleStatus.PendingReview);
    }

    #endregion

    #region Approve / Reject Tests

    [Fact]
    public async Task Approve_PendingVehicle_SetsActive()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.PendingReview);
        vehicle.SubmittedForReviewAt = DateTime.UtcNow.AddHours(-1);
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        var request = new ApproveVehicleRequest
        {
            ModeratorId = Guid.NewGuid(),
            Notes = "Looks good"
        };

        // Act
        var result = await _controller.Approve(vehicleId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var updatedVehicle = await _context.Vehicles.FindAsync(vehicleId);
        updatedVehicle!.Status.Should().Be(VehicleStatus.Active);
        updatedVehicle.ApprovedAt.Should().NotBeNull();
        updatedVehicle.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Approve_NonPendingVehicle_ReturnsBadRequest()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Draft);
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Approve(vehicleId, null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Reject_PendingVehicle_SetsRejectedWithReason()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.PendingReview);
        vehicle.SubmittedForReviewAt = DateTime.UtcNow.AddHours(-1);
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        var request = new RejectVehicleRequest
        {
            ModeratorId = Guid.NewGuid(),
            Reason = "Photos are blurry",
            Notes = "Please retake photos"
        };

        // Act
        var result = await _controller.Reject(vehicleId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var updatedVehicle = await _context.Vehicles.FindAsync(vehicleId);
        updatedVehicle!.Status.Should().Be(VehicleStatus.Rejected);
        updatedVehicle.RejectionReason.Should().Be("Photos are blurry");
        updatedVehicle.RejectedAt.Should().NotBeNull();
        updatedVehicle.RejectionCount.Should().Be(1);
    }

    [Fact]
    public async Task Reject_WithoutReason_ReturnsBadRequest()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.PendingReview);
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        var request = new RejectVehicleRequest { Reason = "" };

        // Act
        var result = await _controller.Reject(vehicleId, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Unpublish Tests

    [Fact]
    public async Task Unpublish_VehicleNotFound_ReturnsNotFound()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();

        // Act
        var result = await _controller.Unpublish(vehicleId, null);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Unpublish_ActiveVehicle_UnpublishesSuccessfully()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Active);
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Unpublish(vehicleId, new UnpublishVehicleRequest { Reason = "Testing" });

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result.Result!).Value as UnpublishVehicleResponse;
        response.Should().NotBeNull();
        response!.Status.Should().Be(VehicleStatus.Archived);
    }

    [Fact]
    public async Task Unpublish_DraftVehicle_ReturnsBadRequest()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Draft);
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Unpublish(vehicleId, null);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Mark As Sold Tests

    [Fact]
    public async Task MarkAsSold_VehicleNotFound_ReturnsNotFound()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();

        // Act
        var result = await _controller.MarkAsSold(vehicleId, null);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkAsSold_ActiveVehicle_MarksSoldSuccessfully()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Active);
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        var request = new MarkVehicleSoldRequest { SalePrice = 25000m };

        // Act
        var result = await _controller.MarkAsSold(vehicleId, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result.Result!).Value as MarkVehicleSoldResponse;
        response.Should().NotBeNull();
        response!.Status.Should().Be(VehicleStatus.Sold);
        response.SalePrice.Should().Be(25000m);
    }

    [Fact]
    public async Task MarkAsSold_DraftVehicle_ReturnsBadRequest()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Draft);
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.MarkAsSold(vehicleId, null);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Feature Tests

    [Fact]
    public async Task Feature_VehicleNotFound_ReturnsNotFound()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();

        // Act
        var result = await _controller.Feature(vehicleId, new FeatureVehicleRequest { IsFeatured = true });

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Feature_Vehicle_FeatureSuccessfully()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Active);
        vehicle.IsFeatured = false;
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Feature(vehicleId, new FeatureVehicleRequest
        {
            IsFeatured = true,
            HomepageSections = HomepageSection.Destacados | HomepageSection.Carousel
        });

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result.Result!).Value as FeatureVehicleResponse;
        response.Should().NotBeNull();
        response!.IsFeatured.Should().BeTrue();
        response.HomepageSections.Should().HaveFlag(HomepageSection.Destacados);
    }

    [Fact]
    public async Task Feature_VehicleWithAutoSections_SetsDestacadosAutomatically()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Active);
        vehicle.IsFeatured = false;
        vehicle.HomepageSections = HomepageSection.None;
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act - Feature without specifying sections
        var result = await _controller.Feature(vehicleId, new FeatureVehicleRequest { IsFeatured = true });

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result.Result!).Value as FeatureVehicleResponse;
        response!.HomepageSections.Should().HaveFlag(HomepageSection.Destacados);
    }

    #endregion

    #region Register View Tests

    [Fact]
    public async Task RegisterView_VehicleNotFound_ReturnsNotFound()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();

        // Act
        var result = await _controller.RegisterView(vehicleId, null);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RegisterView_ValidVehicle_IncrementsViewCount()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Active);
        vehicle.ViewCount = 10;
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RegisterView(vehicleId, new RegisterViewRequest
        {
            SessionId = "test-session"
        });

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result.Result!).Value as RegisterViewResponse;
        response.Should().NotBeNull();
        response!.TotalViews.Should().Be(11);
    }

    [Fact]
    public async Task RegisterView_MultipleViews_AccumulatesCorrectly()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Active);
        vehicle.ViewCount = 0;
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act - Register 3 views
        await _controller.RegisterView(vehicleId, null);
        await _controller.RegisterView(vehicleId, null);
        var result = await _controller.RegisterView(vehicleId, null);

        // Assert
        var response = ((OkObjectResult)result.Result!).Value as RegisterViewResponse;
        response!.TotalViews.Should().Be(3);
    }

    #endregion

    #region Helper Methods

    private static Vehicle CreateValidVehicle(Guid id, VehicleStatus status)
    {
        var vehicle = new Vehicle
        {
            Id = id,
            Title = "2023 Toyota Camry SE - Excellent Condition",
            Description = "Beautiful car in great condition",
            Price = 28000m,
            Currency = "USD",
            Status = status,
            Make = "Toyota",
            Model = "Camry",
            Trim = "SE",
            Year = 2023,
            Mileage = 15000,
            MileageUnit = MileageUnit.Miles,
            VehicleType = VehicleType.Car,
            BodyStyle = BodyStyle.Sedan,
            FuelType = FuelType.Gasoline,
            Transmission = TransmissionType.Automatic,
            DriveType = VehicleDriveType.FWD,
            Condition = VehicleCondition.Used,
            City = "Miami",
            State = "Florida",
            SellerId = Guid.NewGuid(),
            SellerName = "Test Seller",
            SellerPhone = "+1-555-123-4567",
            SellerEmail = "seller@test.com",
            DealerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add images
        vehicle.Images.Add(new VehicleImage
        {
            Id = Guid.NewGuid(),
            VehicleId = id,
            Url = "https://cdn.example.com/car1.jpg",
            IsPrimary = true,
            SortOrder = 0
        });

        return vehicle;
    }

    #endregion
}
