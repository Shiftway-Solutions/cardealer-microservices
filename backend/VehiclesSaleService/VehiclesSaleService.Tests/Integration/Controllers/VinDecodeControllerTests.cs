using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using VehiclesSaleService.Api.Controllers;
using VehiclesSaleService.Domain.Interfaces;
using CarDealer.Shared.Caching.Interfaces;

namespace VehiclesSaleService.Tests.Integration.Controllers;

/// <summary>
/// Tests for VIN decode endpoint in CatalogController
/// </summary>
public class VinDecodeControllerTests
{
    private readonly CatalogController _controller;
    private readonly Mock<IVehicleCatalogRepository> _catalogRepositoryMock;
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock;
    private readonly Mock<ILogger<CatalogController>> _loggerMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

    public VinDecodeControllerTests()
    {
        _catalogRepositoryMock = new Mock<IVehicleCatalogRepository>();
        _vehicleRepositoryMock = new Mock<IVehicleRepository>();
        _loggerMock = new Mock<ILogger<CatalogController>>();
        _cacheServiceMock = new Mock<ICacheService>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        _controller = new CatalogController(
            _catalogRepositoryMock.Object,
            _vehicleRepositoryMock.Object,
            _loggerMock.Object,
            _cacheServiceMock.Object,
            _httpClientFactoryMock.Object
        );
    }

    #region VIN Validation Tests

    [Fact]
    public async Task DecodeVin_EmptyVin_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.DecodeVin("");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DecodeVin_NullVin_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.DecodeVin(null!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DecodeVin_ShortVin_ReturnsBadRequest()
    {
        // Arrange - VIN with only 10 characters
        var shortVin = "1HGCV1F32L";

        // Act
        var result = await _controller.DecodeVin(shortVin);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DecodeVin_LongVin_ReturnsBadRequest()
    {
        // Arrange - VIN with 20 characters
        var longVin = "1HGCV1F32LA00000123";

        // Act
        var result = await _controller.DecodeVin(longVin);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DecodeVin_VinWithLetterI_ReturnsBadRequest()
    {
        // Arrange - VIN with invalid character I
        var invalidVin = "1HGCV1F32IA000001";

        // Act
        var result = await _controller.DecodeVin(invalidVin);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result.Result!;
        badRequest.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DecodeVin_VinWithLetterO_ReturnsBadRequest()
    {
        // Arrange - VIN with invalid character O
        var invalidVin = "1HGCV1F32OA000001";

        // Act
        var result = await _controller.DecodeVin(invalidVin);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DecodeVin_VinWithLetterQ_ReturnsBadRequest()
    {
        // Arrange - VIN with invalid character Q
        var invalidVin = "1HGCV1F32QA000001";

        // Act
        var result = await _controller.DecodeVin(invalidVin);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Valid VIN Tests (Integration with NHTSA)

    // Note: These tests make real HTTP calls to NHTSA API
    // They may fail if NHTSA service is unavailable
    
    [Fact]
    public async Task DecodeVin_ValidHondaAccordVin_ReturnsVehicleInfo()
    {
        // Arrange - Real Honda Accord VIN
        var validVin = "1HGCV1F32LA000001";

        // Act
        var result = await _controller.DecodeVin(validVin);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result.Result!).Value as VinDecodeResponse;
        response.Should().NotBeNull();
        response!.VIN.Should().Be(validVin);
        response.Make.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DecodeVin_ValidToyotaCamryVin_ReturnsMakeAndModel()
    {
        // Arrange - Toyota Camry VIN pattern
        var validVin = "4T1B11HK5JU123456";

        // Act
        var result = await _controller.DecodeVin(validVin);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result.Result!).Value as VinDecodeResponse;
        response.Should().NotBeNull();
        response!.VIN.Should().Be(validVin);
        // Toyota VINs starting with 4T are from Toyota
        response.Make.ToUpperInvariant().Should().Contain("TOYOTA");
    }

    [Fact]
    public async Task DecodeVin_ValidVin_IncludesSuggestedData()
    {
        // Arrange
        var validVin = "1HGCV1F32LA000001";

        // Act
        var result = await _controller.DecodeVin(validVin);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result.Result!).Value as VinDecodeResponse;
        response.Should().NotBeNull();
        response!.SuggestedData.Should().NotBeNull();
        response.SuggestedData!.VehicleType.Should().NotBeNullOrEmpty();
        response.SuggestedData.BodyStyle.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Fuel Type Mapping Tests

    [Theory]
    [InlineData("Gasoline", "gasoline")]
    [InlineData("Diesel", "diesel")]
    [InlineData("Electric", "electric")]
    [InlineData("Hybrid", "hybrid")]
    [InlineData("Plug-in Hybrid Electric (PHEV)", "plugin_hybrid")]
    [InlineData("Flex Fuel", "flex_fuel")]
    [InlineData("Hydrogen", "lpg")]
    [InlineData("Compressed Natural Gas (CNG)", "lpg")]
    [InlineData(null, "gasoline")]
    [InlineData("", "gasoline")]
    public void MapFuelType_ReturnsCorrectMapping(string? input, string expected)
    {
        // Use reflection to test private method
        var method = typeof(CatalogController).GetMethod("MapFuelType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var result = method?.Invoke(null, new object?[] { input }) as string;
        
        result.Should().Be(expected);
    }

    #endregion

    #region Transmission Mapping Tests

    [Theory]
    [InlineData("Automatic", "automatic")]
    [InlineData("Manual", "manual")]
    [InlineData("CVT", "cvt")]
    [InlineData("Dual Clutch Transmission (DCT)", "dct")]
    [InlineData("Automated Manual", "semi-automatic")]
    [InlineData(null, "automatic")]
    public void MapTransmission_ReturnsCorrectMapping(string? input, string expected)
    {
        var method = typeof(CatalogController).GetMethod("MapTransmission", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var result = method?.Invoke(null, new object?[] { input }) as string;
        
        result.Should().Be(expected);
    }

    #endregion

    #region Drive Type Mapping Tests

    [Theory]
    [InlineData("Front-Wheel Drive", "FWD")]
    [InlineData("Rear-Wheel Drive", "RWD")]
    [InlineData("All-Wheel Drive", "AWD")]
    [InlineData("4x4", "4WD")]
    [InlineData("4WD/4-Wheel Drive/4x4", "4WD")]
    [InlineData(null, "FWD")]
    public void MapDriveType_ReturnsCorrectMapping(string? input, string expected)
    {
        var method = typeof(CatalogController).GetMethod("MapDriveType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var result = method?.Invoke(null, new object?[] { input }) as string;
        
        result.Should().Be(expected);
    }

    #endregion

    #region Body Style Mapping Tests

    [Theory]
    [InlineData("Sedan", "sedan")]
    [InlineData("Sport Utility Vehicle (SUV)", "suv")]
    [InlineData("Pickup", "pickup")]
    [InlineData("Minivan", "minivan")]
    [InlineData("Van", "van")]
    [InlineData("Coupe", "coupe")]
    [InlineData("Convertible", "convertible")]
    [InlineData("Hatchback", "hatchback")]
    [InlineData("Station Wagon", "wagon")]
    [InlineData("Crossover", "crossover")]
    [InlineData(null, "sedan")]
    public void MapBodyStyle_ReturnsCorrectMapping(string? input, string expected)
    {
        var method = typeof(CatalogController).GetMethod("MapBodyStyle", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var result = method?.Invoke(null, new object?[] { input }) as string;
        
        result.Should().Be(expected);
    }

    #endregion

    #region Vehicle Type Mapping Tests

    [Theory]
    [InlineData("Passenger Car", "Car")]
    [InlineData("Truck", "Truck")]
    [InlineData("Multipurpose Passenger Vehicle (MPV)", "SUV")]
    [InlineData("Van", "Van")]
    [InlineData("Motorcycle", "Motorcycle")]
    [InlineData("Bus", "Commercial")]
    [InlineData(null, "Car")]
    public void MapVehicleType_ReturnsCorrectMapping(string? input, string expected)
    {
        var method = typeof(CatalogController).GetMethod("MapVehicleType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var result = method?.Invoke(null, new object?[] { input }) as string;
        
        result.Should().Be(expected);
    }

    #endregion
}

















