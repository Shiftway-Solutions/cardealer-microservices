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
using Microsoft.AspNetCore.Mvc;
using VehicleDriveType = VehiclesSaleService.Domain.Entities.DriveType;

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

        _controller = new VehiclesController(
            _vehicleRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object,
            _context,
            _configClientMock.Object
        );
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
    public async Task Publish_DraftVehicle_WithValidData_PublishesSuccessfully()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateValidVehicle(vehicleId, VehicleStatus.Draft);
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Publish(vehicleId, null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result.Result!).Value as PublishVehicleResponse;
        response.Should().NotBeNull();
        response!.Status.Should().Be(VehicleStatus.Active);
        response.Message.Should().Contain("published successfully");
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
