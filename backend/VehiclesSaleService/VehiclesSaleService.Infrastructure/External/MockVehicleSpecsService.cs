using Microsoft.Extensions.Logging;
using VehiclesSaleService.Application.Interfaces;

namespace VehiclesSaleService.Infrastructure.External;

/// <summary>
/// Mock implementation of IVehicleSpecsService (Edmunds API).
/// Returns realistic specifications for popular DR-market vehicles.
/// 
/// TO SWAP FOR REAL API:
/// 1. Create EdmundsVehicleSpecsService in this folder
/// 2. Implement IVehicleSpecsService using the real Edmunds API
/// 3. Change DI registration in Program.cs:
///    builder.Services.AddHttpClient&lt;IVehicleSpecsService, EdmundsVehicleSpecsService&gt;(...)
/// 4. Add API key to appsettings/secrets:
///    "VehicleSpecs": { "Provider": "Edmunds", "ApiKey": "xxx", "BaseUrl": "https://api.edmunds.com" }
/// </summary>
public class MockVehicleSpecsService : IVehicleSpecsService
{
    private readonly ILogger<MockVehicleSpecsService> _logger;
    private static readonly Random _rng = new();

    public MockVehicleSpecsService(ILogger<MockVehicleSpecsService> logger)
    {
        _logger = logger;
    }

    public async Task<VehicleSpecification?> GetSpecsAsync(
        string make, string model, int year, string? trim = null, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] VehicleSpecs: GetSpecs({Make}, {Model}, {Year}, {Trim})",
            make, model, year, trim);
        await Task.Delay(150 + _rng.Next(200), ct);

        var key = $"{make}_{model}".ToUpper();
        if (SpecsDatabase.TryGetValue(key, out var specFactory))
            return specFactory(year, trim ?? "Base");

        // Generate generic specs based on make/model patterns
        return GenerateGenericSpecs(make, model, year, trim ?? "Base");
    }

    public async Task<List<TrimInfo>> GetTrimsAsync(
        string make, string model, int year, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] VehicleSpecs: GetTrims({Make}, {Model}, {Year})", make, model, year);
        await Task.Delay(100 + _rng.Next(100), ct);

        // Generate trims based on popular DR vehicles
        var key = $"{make}_{model}".ToUpper();
        return TrimDatabase.TryGetValue(key, out var trims) ? trims :
        [
            new TrimInfo("Base", 25_000m, "USD", null, "FWD"),
            new TrimInfo("Sport", 28_500m, "USD", null, "FWD"),
            new TrimInfo("Premium", 32_000m, "USD", null, "AWD"),
        ];
    }

    public async Task<List<VehicleStyle>> GetStylesAsync(
        string make, string model, int year, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] VehicleSpecs: GetStyles({Make}, {Model}, {Year})", make, model, year);
        await Task.Delay(100 + _rng.Next(100), ct);

        var trims = await GetTrimsAsync(make, model, year, ct);
        return trims.Select((t, i) => new VehicleStyle(
            StyleId: $"mock-{make.ToLower()}-{model.ToLower()}-{year}-{i}",
            Name: $"{year} {make} {model} {t.Name}",
            BodyType: t.BodyType ?? "Sedan",
            Doors: 4,
            DriveType: t.DriveType ?? "FWD",
            BaseMsrp: t.BaseMsrp,
            Currency: t.Currency
        )).ToList();
    }

    public async Task<VehicleSpecification?> DecodeVinAsync(string vin, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] VehicleSpecs: DecodeVin({Vin})", vin);
        await Task.Delay(200 + _rng.Next(200), ct);

        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17) return null;

        // Decode manufacturer from WMI (first 3 chars)
        var wmi = vin[..3].ToUpper();
        var (make, model) = wmi switch
        {
            "1HG" or "2HG" => ("Honda", "Civic"),
            "JTD" or "2T1" => ("Toyota", "Corolla"),
            "5YJ" => ("Tesla", "Model S"),
            "WBA" or "WBS" => ("BMW", "Serie 3"),
            "KMH" => ("Hyundai", "Tucson"),
            "KNA" or "KND" => ("Kia", "Sportage"),
            "1N4" or "3N1" => ("Nissan", "Sentra"),
            "JA4" => ("Mitsubishi", "Outlander"),
            "1C4" => ("Jeep", "Grand Cherokee"),
            "1FA" or "1FM" => ("Ford", "Explorer"),
            _ => ("Unknown", "Unknown"),
        };

        // Extract model year from VIN position 10
        var modelYearChar = vin[9];
        var modelYear = modelYearChar switch
        {
            >= 'A' and <= 'H' => 2010 + (modelYearChar - 'A'),
            'J' => 2018,
            'K' => 2019,
            'L' => 2020,
            'M' => 2021,
            'N' => 2022,
            'P' => 2023,
            'R' => 2024,
            'S' => 2025,
            'T' => 2026,
            >= '1' and <= '9' => 2000 + (modelYearChar - '0'),
            _ => DateTime.UtcNow.Year,
        };

        return await GetSpecsAsync(make, model, modelYear, null, ct);
    }

    // ── Specs Database ──────────────────────────────────────────

    private static readonly Dictionary<string, Func<int, string, VehicleSpecification>> SpecsDatabase = new()
    {
        ["TOYOTA_COROLLA"] = (year, trim) => new VehicleSpecification(
            Make: "Toyota", Model: "Corolla", Year: year, Trim: trim,
            BodyType: "Sedan", Doors: 4, Seats: 5,
            Engine: new EngineSpecs("Inline-4", 2.0, 169, 151, "Gasoline", 4, false, "Direct Injection"),
            Transmission: new TransmissionSpecs("CVT", 10, "FWD"),
            FuelEconomy: new FuelEconomySpecs(31, 40, 34, 13.2, null),
            Dimensions: new DimensionSpecs(182.3, 70.1, 56.5, 106.3, 3060, 13.1, 5.3),
            Performance: new PerformanceSpecs(8.2, 115, "Disc/Disc", "MacPherson Strut", "Multi-Link"),
            Safety: new SafetySpecs(5, "Good", new List<string>
                { "Toyota Safety Sense 3.0", "Pre-Collision System", "Lane Departure Alert", "Dynamic Radar Cruise Control", "Automatic High Beams", "8 Airbags" }),
            StandardFeatures: new List<string>
                { "7\" Touch Screen", "Apple CarPlay", "Android Auto", "Bluetooth", "USB-C Ports", "LED Headlights", "Keyless Entry" },
            OptionalFeatures: new List<string>
                { "Sunroof", "JBL Audio", "Wireless Charging", "Head-Up Display", "Heated Seats", "Blind Spot Monitor" },
            BaseMsrp: year >= 2024 ? 22_050m : 20_075m,
            Currency: "USD", Provider: "Mock"
        ),

        ["TOYOTA_RAV4"] = (year, trim) => new VehicleSpecification(
            Make: "Toyota", Model: "RAV4", Year: year, Trim: trim,
            BodyType: "SUV", Doors: 4, Seats: 5,
            Engine: new EngineSpecs("Inline-4", 2.5, 203, 184, "Gasoline", 4, false, "Direct Injection"),
            Transmission: new TransmissionSpecs("Automatic", 8, trim == "Limited" ? "AWD" : "FWD"),
            FuelEconomy: new FuelEconomySpecs(27, 35, 30, 14.5, null),
            Dimensions: new DimensionSpecs(180.9, 73.0, 67.0, 105.9, 3615, 37.6, 8.4),
            Performance: new PerformanceSpecs(8.0, 120, "Disc/Disc", "MacPherson Strut", "Double Wishbone"),
            Safety: new SafetySpecs(5, "Good", new List<string>
                { "Toyota Safety Sense 2.5+", "Pre-Collision System", "Lane Tracing Assist", "Road Sign Assist", "10 Airbags" }),
            StandardFeatures: new List<string>
                { "8\" Touch Screen", "Apple CarPlay", "Android Auto", "Dual Zone Climate", "Power Liftgate" },
            OptionalFeatures: new List<string>
                { "Panoramic Moonroof", "Digital Rearview Mirror", "Premium Audio", "Ventilated Seats" },
            BaseMsrp: year >= 2024 ? 28_475m : 27_575m,
            Currency: "USD", Provider: "Mock"
        ),

        ["HONDA_CIVIC"] = (year, trim) => new VehicleSpecification(
            Make: "Honda", Model: "Civic", Year: year, Trim: trim,
            BodyType: "Sedan", Doors: 4, Seats: 5,
            Engine: new EngineSpecs("Inline-4", trim == "Sport" ? 2.0 : 1.5, trim == "Sport" ? 158 : 180,
                trim == "Sport" ? 138 : 177, "Gasoline", 4, trim != "Sport", "Direct Injection"),
            Transmission: new TransmissionSpecs("CVT", 1, "FWD"),
            FuelEconomy: new FuelEconomySpecs(31, 40, 35, 12.4, null),
            Dimensions: new DimensionSpecs(184.0, 70.9, 55.7, 107.7, 2877, 14.8, 5.1),
            Performance: new PerformanceSpecs(7.8, 127, "Disc/Disc", "MacPherson Strut", "Multi-Link"),
            Safety: new SafetySpecs(5, "Good", new List<string>
                { "Honda Sensing", "Collision Mitigation Braking", "Road Departure Mitigation", "Adaptive Cruise Control", "Lane Keeping Assist", "10 Airbags" }),
            StandardFeatures: new List<string>
                { "7\" Touch Screen", "Apple CarPlay", "Android Auto", "Honda LaneWatch", "LED Headlights" },
            OptionalFeatures: new List<string>
                { "Sunroof", "Bose Audio", "Wireless Charging", "Navigation", "Heated Seats" },
            BaseMsrp: year >= 2024 ? 23_950m : 22_550m,
            Currency: "USD", Provider: "Mock"
        ),

        ["HYUNDAI_TUCSON"] = (year, trim) => new VehicleSpecification(
            Make: "Hyundai", Model: "Tucson", Year: year, Trim: trim,
            BodyType: "SUV", Doors: 4, Seats: 5,
            Engine: new EngineSpecs("Inline-4", 2.5, 187, 178, "Gasoline", 4, false, "Multi-Point Injection"),
            Transmission: new TransmissionSpecs("Automatic", 8, trim == "Limited" ? "AWD" : "FWD"),
            FuelEconomy: new FuelEconomySpecs(26, 33, 29, 14.3, null),
            Dimensions: new DimensionSpecs(182.3, 73.4, 66.1, 108.3, 3492, 38.7, 7.5),
            Performance: new PerformanceSpecs(8.4, 120, "Disc/Disc", "MacPherson Strut", "Multi-Link"),
            Safety: new SafetySpecs(5, "Good", new List<string>
                { "Forward Collision Avoidance", "Lane Keeping Assist", "Blind-Spot Collision Avoidance", "Rear Cross-Traffic Alert", "8 Airbags" }),
            StandardFeatures: new List<string>
                { "8\" Touch Screen", "Apple CarPlay", "Android Auto", "Dual Zone Climate", "LED Headlights", "Keyless Entry" },
            OptionalFeatures: new List<string>
                { "Panoramic Sunroof", "Bose Audio", "Ventilated Seats", "Digital Key", "Remote Smart Parking" },
            BaseMsrp: year >= 2024 ? 28_950m : 27_750m,
            Currency: "USD", Provider: "Mock"
        ),

        ["KIA_SPORTAGE"] = (year, trim) => new VehicleSpecification(
            Make: "Kia", Model: "Sportage", Year: year, Trim: trim,
            BodyType: "SUV", Doors: 4, Seats: 5,
            Engine: new EngineSpecs("Inline-4", 2.5, 187, 178, "Gasoline", 4, false, "Multi-Point Injection"),
            Transmission: new TransmissionSpecs("Automatic", 8, "FWD"),
            FuelEconomy: new FuelEconomySpecs(26, 32, 29, 14.0, null),
            Dimensions: new DimensionSpecs(183.5, 73.4, 66.1, 108.3, 3547, 39.6, 7.5),
            Performance: new PerformanceSpecs(8.6, 120, "Disc/Disc", "MacPherson Strut", "Multi-Link"),
            Safety: new SafetySpecs(5, "Good", new List<string>
                { "Forward Collision Avoidance", "Lane Keeping Assist", "Blind-Spot Collision Avoidance", "Safe Exit Warning", "8 Airbags" }),
            StandardFeatures: new List<string>
                { "8\" Touch Screen", "Apple CarPlay", "Android Auto", "Dual Zone Climate", "LED Headlights" },
            OptionalFeatures: new List<string>
                { "Dual Panoramic Display", "Harman Kardon Audio", "Ventilated Seats", "Digital Key 2.0" },
            BaseMsrp: year >= 2024 ? 29_990m : 28_990m,
            Currency: "USD", Provider: "Mock"
        ),
    };

    private static readonly Dictionary<string, List<TrimInfo>> TrimDatabase = new()
    {
        ["TOYOTA_COROLLA"] = new()
        {
            new TrimInfo("L", 22_050m, "USD", "Sedan", "FWD"),
            new TrimInfo("LE", 22_950m, "USD", "Sedan", "FWD"),
            new TrimInfo("SE", 25_550m, "USD", "Sedan", "FWD"),
            new TrimInfo("XSE", 27_550m, "USD", "Sedan", "FWD"),
        },
        ["TOYOTA_RAV4"] = new()
        {
            new TrimInfo("LE", 28_475m, "USD", "SUV", "FWD"),
            new TrimInfo("XLE", 30_175m, "USD", "SUV", "FWD"),
            new TrimInfo("XLE Premium", 33_275m, "USD", "SUV", "AWD"),
            new TrimInfo("Limited", 37_775m, "USD", "SUV", "AWD"),
        },
        ["HONDA_CIVIC"] = new()
        {
            new TrimInfo("LX", 23_950m, "USD", "Sedan", "FWD"),
            new TrimInfo("Sport", 25_950m, "USD", "Sedan", "FWD"),
            new TrimInfo("EX", 27_300m, "USD", "Sedan", "FWD"),
            new TrimInfo("Touring", 29_950m, "USD", "Sedan", "FWD"),
        },
        ["HYUNDAI_TUCSON"] = new()
        {
            new TrimInfo("SE", 28_950m, "USD", "SUV", "FWD"),
            new TrimInfo("SEL", 30_450m, "USD", "SUV", "FWD"),
            new TrimInfo("XRT", 32_950m, "USD", "SUV", "FWD"),
            new TrimInfo("Limited", 36_450m, "USD", "SUV", "AWD"),
        },
        ["KIA_SPORTAGE"] = new()
        {
            new TrimInfo("LX", 29_990m, "USD", "SUV", "FWD"),
            new TrimInfo("EX", 32_090m, "USD", "SUV", "FWD"),
            new TrimInfo("SX", 35_990m, "USD", "SUV", "AWD"),
            new TrimInfo("SX Prestige", 38_590m, "USD", "SUV", "AWD"),
        },
    };

    private static VehicleSpecification GenerateGenericSpecs(string make, string model, int year, string trim)
    {
        // Generate reasonable default specs for unknown vehicles
        return new VehicleSpecification(
            Make: make, Model: model, Year: year, Trim: trim,
            BodyType: "Sedan", Doors: 4, Seats: 5,
            Engine: new EngineSpecs("Inline-4", 2.0, 170, 155, "Gasoline", 4, false, "Multi-Point Injection"),
            Transmission: new TransmissionSpecs("Automatic", 6, "FWD"),
            FuelEconomy: new FuelEconomySpecs(28, 36, 31, 13.0, null),
            Dimensions: new DimensionSpecs(185.0, 72.0, 57.0, 106.0, 3100, 14.0, 5.5),
            Performance: new PerformanceSpecs(8.5, 120, "Disc/Disc", "MacPherson Strut", "Multi-Link"),
            Safety: new SafetySpecs(4, "Good", new List<string>
                { "Forward Collision Warning", "Lane Departure Warning", "6 Airbags" }),
            StandardFeatures: new List<string>
                { "Touch Screen", "Bluetooth", "USB Ports", "Air Conditioning" },
            OptionalFeatures: new List<string>
                { "Sunroof", "Premium Audio", "Navigation", "Leather Seats" },
            BaseMsrp: 25_000m,
            Currency: "USD", Provider: "Mock"
        );
    }
}
