using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using VehiclesSaleService.Application.Interfaces;
using VehiclesSaleService.Infrastructure.External;
using Xunit;

namespace VehiclesSaleService.Tests.Infrastructure.External;

/// <summary>
/// Unit tests for all external vehicle data API service implementations.
/// Tests mock services directly and verifies real services fall back to mock
/// when no API key is configured.
/// </summary>
public class VehicleDataServicesTests
{
    /// <summary>
    /// Creates a mock IExchangeRateService that returns a fixed fallback rate.
    /// Used across all tests that require currency conversion.
    /// </summary>
    private static IExchangeRateService CreateMockExchangeRateService(decimal rate = 60.50m)
    {
        var mock = new Mock<IExchangeRateService>();
        mock.Setup(x => x.GetDopUsdRateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExchangeRateResult(rate, DateTimeOffset.UtcNow, ExchangeRateSource.Fallback));
        return mock.Object;
    }
    // ══════════════════════════════════════════════════════════════
    //  MockVehicleHistoryService Tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task MockVehicleHistory_GetHistoryByVin_ValidVin_ShouldReturnReport()
    {
        // Arrange
        var logger = new Mock<ILogger<MockVehicleHistoryService>>();
        var sut = new MockVehicleHistoryService(logger.Object);
        var vin = "1HGBH41JXMN109186"; // Known VIN in mock database

        // Act
        var result = await sut.GetHistoryByVinAsync(vin);

        // Assert
        result.Should().NotBeNull();
        result!.Vin.Should().Be(vin);
        result.Provider.Should().Be("Mock");
        result.OwnerCount.Should().BeGreaterThan(0);
        result.TitleStatus.Should().BeOneOf("Clean", "Salvage", "Rebuilt", "Flood", "Lemon");
        result.ServiceHistory.Should().NotBeEmpty();
        result.OwnershipHistory.Should().NotBeEmpty();
        result.TitleHistory.Should().NotBeEmpty();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task MockVehicleHistory_GetHistoryByVin_UnknownVin_ShouldGenerateDeterministic()
    {
        // Arrange
        var logger = new Mock<ILogger<MockVehicleHistoryService>>();
        var sut = new MockVehicleHistoryService(logger.Object);
        var vin = "WVWZZZ3CZWE123456"; // Unknown VIN

        // Act
        var result1 = await sut.GetHistoryByVinAsync(vin);
        var result2 = await sut.GetHistoryByVinAsync(vin);

        // Assert — same VIN should produce consistent OwnerCount and TitleStatus
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.OwnerCount.Should().Be(result2!.OwnerCount);
        result1.TitleStatus.Should().Be(result2.TitleStatus);
        result1.AccidentCount.Should().Be(result2.AccidentCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("TOOSHORT")]
    [InlineData(null)]
    public async Task MockVehicleHistory_GetHistoryByVin_InvalidVin_ShouldReturnNull(string? vin)
    {
        // Arrange
        var logger = new Mock<ILogger<MockVehicleHistoryService>>();
        var sut = new MockVehicleHistoryService(logger.Object);

        // Act
        var result = await sut.GetHistoryByVinAsync(vin!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MockVehicleHistory_GetSummaryByVin_ShouldReturnSummary()
    {
        // Arrange
        var logger = new Mock<ILogger<MockVehicleHistoryService>>();
        var sut = new MockVehicleHistoryService(logger.Object);
        var vin = "1HGBH41JXMN109186";

        // Act
        var result = await sut.GetSummaryByVinAsync(vin);

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("Mock");
        result.Vin.Should().Be(vin);
    }

    [Fact]
    public async Task MockVehicleHistory_IsReportAvailable_ValidVin_ShouldReturnTrue()
    {
        // Arrange
        var logger = new Mock<ILogger<MockVehicleHistoryService>>();
        var sut = new MockVehicleHistoryService(logger.Object);

        // Act
        var result = await sut.IsReportAvailableAsync("1HGBH41JXMN109186");

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════
    //  MockMarketPriceService Tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task MockMarketPrice_GetMarketPrice_KnownVehicle_ShouldReturnAnalysis()
    {
        // Arrange
        var logger = new Mock<ILogger<MockMarketPriceService>>();
        var sut = new MockMarketPriceService(logger.Object, CreateMockExchangeRateService());

        // Act
        var result = await sut.GetMarketPriceAsync("Toyota", "Corolla", 2023);

        // Assert
        result.Should().NotBeNull();
        result!.Make.Should().Be("Toyota");
        result.Model.Should().Be("Corolla");
        result.Year.Should().Be(2023);
        result.Currency.Should().Be("DOP");
        result.Provider.Should().Be("Mock");
        result.AveragePrice.Should().BeGreaterThan(0);
        result.MinPrice.Should().BeLessThan(result.AveragePrice);
        result.MaxPrice.Should().BeGreaterThan(result.AveragePrice);
        result.SampleSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MockMarketPrice_GetMarketPrice_UnknownVehicle_ShouldReturnDefaultPrice()
    {
        // Arrange
        var logger = new Mock<ILogger<MockMarketPriceService>>();
        var sut = new MockMarketPriceService(logger.Object, CreateMockExchangeRateService());

        // Act
        var result = await sut.GetMarketPriceAsync("UnknownMake", "UnknownModel", 2023);

        // Assert
        result.Should().NotBeNull();
        result!.AveragePrice.Should().BeGreaterThan(0);
        result.Provider.Should().Be("Mock");
    }

    [Fact]
    public async Task MockMarketPrice_GetComparableListings_ShouldReturnListings()
    {
        // Arrange
        var logger = new Mock<ILogger<MockMarketPriceService>>();
        var sut = new MockMarketPriceService(logger.Object, CreateMockExchangeRateService());

        // Act
        var result = await sut.GetComparableListingsAsync("Honda", "Civic", 2023, limit: 5);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessThanOrEqualTo(5);
        result.Should().AllSatisfy(l =>
        {
            l.Currency.Should().Be("DOP");
            l.Price.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public async Task MockMarketPrice_GetMarketTrend_ShouldReturnTrendData()
    {
        // Arrange
        var logger = new Mock<ILogger<MockMarketPriceService>>();
        var sut = new MockMarketPriceService(logger.Object, CreateMockExchangeRateService());

        // Act
        var result = await sut.GetMarketTrendAsync("Toyota", "RAV4", monthsBack: 6);

        // Assert
        result.Should().NotBeNull();
        result!.PriceHistory.Should().HaveCountGreaterThan(0);
        result.TrendLabel.Should().BeOneOf("Rising", "Stable", "Declining");
        result.Provider.Should().Be("Mock");
    }

    [Fact]
    public async Task MockMarketPrice_GetPriceRecommendation_ShouldReturnRecommendation()
    {
        // Arrange
        var logger = new Mock<ILogger<MockMarketPriceService>>();
        var sut = new MockMarketPriceService(logger.Object, CreateMockExchangeRateService());

        // Act
        var result = await sut.GetPriceRecommendationAsync(
            "Toyota", "Corolla", 2022, 45_000m, "Bueno", "Santiago");

        // Assert
        result.Should().NotBeNull();
        result!.Currency.Should().Be("DOP");
        result.RecommendedPrice.Should().BeGreaterThan(0);
        result.QuickSalePrice.Should().BeLessThan(result.RecommendedPrice);
        result.PremiumPrice.Should().BeGreaterThan(result.RecommendedPrice);
        result.ConfidenceScore.Should().BeInRange(0m, 1m);
        result.DaysToSellEstimate.Should().BeGreaterThan(0);
        result.Explanation.Should().Contain("Toyota");
    }

    [Fact]
    public async Task MockMarketPrice_ProvinceAdjustment_ShouldAffectPrice()
    {
        // Arrange
        var logger = new Mock<ILogger<MockMarketPriceService>>();
        var sut = new MockMarketPriceService(logger.Object, CreateMockExchangeRateService());

        // Act
        var sdPrice = await sut.GetMarketPriceAsync("Toyota", "Corolla", 2023, province: "Santo Domingo");
        var interiorPrice = await sut.GetMarketPriceAsync("Toyota", "Corolla", 2023, province: "La Vega");

        // Assert — Santo Domingo should have slightly higher prices
        sdPrice.Should().NotBeNull();
        interiorPrice.Should().NotBeNull();
        // SD has 1.03x multiplier, La Vega has no multiplier
        sdPrice!.AveragePrice.Should().BeGreaterThanOrEqualTo(interiorPrice!.AveragePrice);
    }

    // ══════════════════════════════════════════════════════════════
    //  MockVehicleSpecsService Tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task MockVehicleSpecs_GetSpecs_KnownVehicle_ShouldReturnDetailedSpecs()
    {
        // Arrange
        var logger = new Mock<ILogger<MockVehicleSpecsService>>();
        var sut = new MockVehicleSpecsService(logger.Object);

        // Act
        var result = await sut.GetSpecsAsync("Toyota", "Corolla", 2024);

        // Assert
        result.Should().NotBeNull();
        result!.Make.Should().Be("Toyota");
        result.Model.Should().Be("Corolla");
        result.Year.Should().Be(2024);
        result.Provider.Should().Be("Mock");
        result.Engine.Should().NotBeNull();
        result.Engine.Horsepower.Should().BeGreaterThan(0);
        result.Transmission.Should().NotBeNull();
        result.FuelEconomy.Should().NotBeNull();
        result.Safety.Should().NotBeNull();
        result.StandardFeatures.Should().NotBeEmpty();
        result.BaseMsrp.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MockVehicleSpecs_GetSpecs_UnknownVehicle_ShouldReturnGenericSpecs()
    {
        // Arrange
        var logger = new Mock<ILogger<MockVehicleSpecsService>>();
        var sut = new MockVehicleSpecsService(logger.Object);

        // Act
        var result = await sut.GetSpecsAsync("UnknownMake", "UnknownModel", 2024);

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("Mock");
        result.Engine.Should().NotBeNull();
        result.BaseMsrp.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MockVehicleSpecs_GetTrims_KnownVehicle_ShouldReturnTrims()
    {
        // Arrange
        var logger = new Mock<ILogger<MockVehicleSpecsService>>();
        var sut = new MockVehicleSpecsService(logger.Object);

        // Act
        var result = await sut.GetTrimsAsync("Toyota", "Corolla", 2024);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().AllSatisfy(t => t.BaseMsrp.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task MockVehicleSpecs_DecodeVin_ValidVin_ShouldReturnSpecs()
    {
        // Arrange
        var logger = new Mock<ILogger<MockVehicleSpecsService>>();
        var sut = new MockVehicleSpecsService(logger.Object);
        // Honda Civic VIN (WMI = 1HG, year char = M = 2021)
        var vin = "1HGBH41JXMN109186";

        // Act
        var result = await sut.DecodeVinAsync(vin);

        // Assert
        result.Should().NotBeNull();
        result!.Make.Should().Be("Honda");
        result.Model.Should().Be("Civic");
    }

    [Theory]
    [InlineData("")]
    [InlineData("SHORT")]
    [InlineData(null)]
    public async Task MockVehicleSpecs_DecodeVin_InvalidVin_ShouldReturnNull(string? vin)
    {
        // Arrange
        var logger = new Mock<ILogger<MockVehicleSpecsService>>();
        var sut = new MockVehicleSpecsService(logger.Object);

        // Act
        var result = await sut.DecodeVinAsync(vin!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MockVehicleSpecs_GetStyles_ShouldReturnStyles()
    {
        // Arrange
        var logger = new Mock<ILogger<MockVehicleSpecsService>>();
        var sut = new MockVehicleSpecsService(logger.Object);

        // Act
        var result = await sut.GetStylesAsync("Honda", "Civic", 2024);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().AllSatisfy(s =>
        {
            s.StyleId.Should().NotBeNullOrEmpty();
            s.Name.Should().Contain("Civic");
            s.BaseMsrp.Should().BeGreaterThan(0);
        });
    }

    // ══════════════════════════════════════════════════════════════
    //  EdmundsVehicleSpecsService Tests (Fallback behavior)
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task EdmundsSpecs_NoApiKey_ShouldFallbackToMock()
    {
        // Arrange
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient("Edmunds"))
            .Returns(new HttpClient());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExternalApis:VehicleSpecs:ApiKey"] = "",
                ["ExternalApis:VehicleSpecs:FallbackToMock"] = "true",
            })
            .Build();

        var logger = new Mock<ILogger<EdmundsVehicleSpecsService>>();
        var mockLogger = new Mock<ILogger<MockVehicleSpecsService>>();

        var sut = new EdmundsVehicleSpecsService(
            httpClientFactory.Object, config, logger.Object, mockLogger.Object);

        // Act
        var result = await sut.GetSpecsAsync("Toyota", "Corolla", 2024);

        // Assert — should get Mock data since no API key
        result.Should().NotBeNull();
        result!.Provider.Should().Be("Mock");
    }

    [Fact]
    public async Task EdmundsSpecs_NoApiKey_FallbackDisabled_ShouldReturnNull()
    {
        // Arrange
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient("Edmunds"))
            .Returns(new HttpClient());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExternalApis:VehicleSpecs:ApiKey"] = "",
                ["ExternalApis:VehicleSpecs:FallbackToMock"] = "false",
            })
            .Build();

        var logger = new Mock<ILogger<EdmundsVehicleSpecsService>>();
        var mockLogger = new Mock<ILogger<MockVehicleSpecsService>>();

        var sut = new EdmundsVehicleSpecsService(
            httpClientFactory.Object, config, logger.Object, mockLogger.Object);

        // Act
        var result = await sut.GetSpecsAsync("Toyota", "Corolla", 2024);

        // Assert
        result.Should().BeNull();
    }

    // ══════════════════════════════════════════════════════════════
    //  MarketCheckPriceService Tests (Fallback behavior)
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task MarketCheckPrice_NoApiKey_ShouldFallbackToMock()
    {
        // Arrange
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient("MarketCheck"))
            .Returns(new HttpClient());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExternalApis:MarketPrice:ApiKey"] = "",
                ["ExternalApis:MarketPrice:FallbackToMock"] = "true",
            })
            .Build();

        var logger = new Mock<ILogger<MarketCheckPriceService>>();
        var mockLogger = new Mock<ILogger<MockMarketPriceService>>();

        var sut = new MarketCheckPriceService(
            httpClientFactory.Object, config, logger.Object, mockLogger.Object, CreateMockExchangeRateService());

        // Act
        var result = await sut.GetMarketPriceAsync("Toyota", "Corolla", 2023);

        // Assert — should get Mock data since no API key
        result.Should().NotBeNull();
        result!.Provider.Should().Be("Mock");
    }

    [Fact]
    public async Task MarketCheckPrice_NoApiKey_GetComparables_ShouldFallbackToMock()
    {
        // Arrange
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient("MarketCheck"))
            .Returns(new HttpClient());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExternalApis:MarketPrice:ApiKey"] = "",
                ["ExternalApis:MarketPrice:FallbackToMock"] = "true",
            })
            .Build();

        var logger = new Mock<ILogger<MarketCheckPriceService>>();
        var mockLogger = new Mock<ILogger<MockMarketPriceService>>();

        var sut = new MarketCheckPriceService(
            httpClientFactory.Object, config, logger.Object, mockLogger.Object, CreateMockExchangeRateService());

        // Act
        var result = await sut.GetComparableListingsAsync("Honda", "Civic", 2023, limit: 3);

        // Assert
        result.Should().NotBeEmpty();
    }

    // ══════════════════════════════════════════════════════════════
    //  CarfaxVehicleHistoryService Tests (Fallback behavior)
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CarfaxHistory_NoCredentials_ShouldFallbackToMock()
    {
        // Arrange
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient("Carfax"))
            .Returns(new HttpClient());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExternalApis:Carfax:ApiKey"] = "",
                ["ExternalApis:Carfax:PartnerId"] = "",
                ["ExternalApis:Carfax:FallbackToMock"] = "true",
            })
            .Build();

        var logger = new Mock<ILogger<CarfaxVehicleHistoryService>>();
        var mockLogger = new Mock<ILogger<MockVehicleHistoryService>>();

        var sut = new CarfaxVehicleHistoryService(
            httpClientFactory.Object, config, logger.Object, mockLogger.Object);

        // Act
        var result = await sut.GetHistoryByVinAsync("1HGBH41JXMN109186");

        // Assert — should get Mock data since no credentials
        result.Should().NotBeNull();
        result!.Provider.Should().Be("Mock");
    }

    [Theory]
    [InlineData("")]
    [InlineData("TOOSHORT")]
    [InlineData(null)]
    public async Task CarfaxHistory_InvalidVin_ShouldReturnNull(string? vin)
    {
        // Arrange
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient("Carfax"))
            .Returns(new HttpClient());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExternalApis:Carfax:ApiKey"] = "test-key",
                ["ExternalApis:Carfax:PartnerId"] = "test-partner",
            })
            .Build();

        var logger = new Mock<ILogger<CarfaxVehicleHistoryService>>();
        var mockLogger = new Mock<ILogger<MockVehicleHistoryService>>();

        var sut = new CarfaxVehicleHistoryService(
            httpClientFactory.Object, config, logger.Object, mockLogger.Object);

        // Act
        var result = await sut.GetHistoryByVinAsync(vin!);

        // Assert
        result.Should().BeNull();
    }

    // ══════════════════════════════════════════════════════════════
    //  DTO Integrity Tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void VehicleHistoryReport_ShouldHaveAllRequiredFields()
    {
        // Arrange & Act
        var report = new VehicleHistoryReport(
            Vin: "TEST12345678901234",
            Provider: "Test",
            OwnerCount: 2,
            TitleStatus: "Clean",
            HasAccidents: false,
            AccidentCount: 0,
            HasFloodDamage: false,
            HasFireDamage: false,
            IsStolen: false,
            HasOpenRecalls: false,
            RecallCount: 0,
            ServiceHistory: new List<ServiceRecord>(),
            OwnershipHistory: new List<OwnershipRecord>(),
            TitleHistory: new List<TitleRecord>(),
            LastReportedMileage: 50_000m,
            LastReportedMileageDate: DateTime.UtcNow,
            OdometerRollback: false,
            ReportUrl: "https://test.com",
            GeneratedAt: DateTime.UtcNow
        );

        // Assert
        report.Vin.Should().Be("TEST12345678901234");
        report.Provider.Should().Be("Test");
        report.OwnerCount.Should().Be(2);
        report.TitleStatus.Should().Be("Clean");
    }

    [Fact]
    public void MarketPriceAnalysis_ShouldHaveAllRequiredFields()
    {
        // Arrange & Act
        var analysis = new MarketPriceAnalysis(
            Make: "Toyota",
            Model: "Corolla",
            Year: 2024,
            Trim: "LE",
            AveragePrice: 1_400_000m,
            MedianPrice: 1_380_000m,
            MinPrice: 1_200_000m,
            MaxPrice: 1_600_000m,
            Currency: "DOP",
            SampleSize: 45,
            PriceAboveMarket: null,
            MarketPosition: "At Market",
            DepreciationRate: 10m,
            AnalyzedAt: DateTime.UtcNow,
            Provider: "Mock"
        );

        // Assert
        analysis.Currency.Should().Be("DOP");
        analysis.SampleSize.Should().Be(45);
        analysis.AveragePrice.Should().BeGreaterThan(analysis.MinPrice);
        analysis.AveragePrice.Should().BeLessThan(analysis.MaxPrice);
    }

    [Fact]
    public void VehicleSpecification_ShouldHaveNestedObjects()
    {
        // Arrange & Act
        var spec = new VehicleSpecification(
            Make: "Honda", Model: "Civic", Year: 2024, Trim: "EX",
            BodyType: "Sedan", Doors: 4, Seats: 5,
            Engine: new EngineSpecs("Inline-4", 1.5, 180, 177, "Gasoline", 4, true, "Direct Injection"),
            Transmission: new TransmissionSpecs("CVT", 1, "FWD"),
            FuelEconomy: new FuelEconomySpecs(31, 40, 35, 12.4, null),
            Dimensions: new DimensionSpecs(184.0, 70.9, 55.7, 107.7, 2877, 14.8, 5.1),
            Performance: new PerformanceSpecs(7.8, 127, "Disc/Disc", null, null),
            Safety: new SafetySpecs(5, "Good", new List<string> { "ABS", "ESC" }),
            StandardFeatures: new List<string> { "Apple CarPlay" },
            OptionalFeatures: new List<string> { "Sunroof" },
            BaseMsrp: 27_300m,
            Currency: "USD",
            Provider: "Mock"
        );

        // Assert
        spec.Engine.Turbocharged.Should().BeTrue();
        spec.Engine.Horsepower.Should().Be(180);
        spec.Transmission.Type.Should().Be("CVT");
        spec.FuelEconomy.CombinedMpg.Should().Be(35);
        spec.Safety.NhtsaOverallRating.Should().Be(5);
    }

    // ══════════════════════════════════════════════════════════════
    //  Market Price Depreciation Tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task MockMarketPrice_NewerVehicle_ShouldBeMoreExpensive()
    {
        // Arrange
        var logger = new Mock<ILogger<MockMarketPriceService>>();
        var sut = new MockMarketPriceService(logger.Object, CreateMockExchangeRateService());

        // Act
        var price2024 = await sut.GetMarketPriceAsync("Toyota", "Corolla", 2024);
        var price2020 = await sut.GetMarketPriceAsync("Toyota", "Corolla", 2020);

        // Assert
        price2024.Should().NotBeNull();
        price2020.Should().NotBeNull();
        price2024!.AveragePrice.Should().BeGreaterThan(price2020!.AveragePrice);
    }

    [Fact]
    public async Task MockMarketPrice_ExcellenteCondition_ShouldBePricedHigher()
    {
        // Arrange
        var logger = new Mock<ILogger<MockMarketPriceService>>();
        var sut = new MockMarketPriceService(logger.Object, CreateMockExchangeRateService());

        // Act
        var excellent = await sut.GetMarketPriceAsync("Toyota", "Corolla", 2023, condition: "Excelente");
        var regular = await sut.GetMarketPriceAsync("Toyota", "Corolla", 2023, condition: "Regular");

        // Assert
        excellent.Should().NotBeNull();
        regular.Should().NotBeNull();
        excellent!.AveragePrice.Should().BeGreaterThan(regular!.AveragePrice);
    }
}
