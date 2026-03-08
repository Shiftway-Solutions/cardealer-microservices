namespace VehiclesSaleService.Application.Interfaces;

/// <summary>
/// Vehicle technical specifications provider (Edmunds API integration).
/// Current implementation returns simulated data for testing.
/// Replace with real API client by swapping DI registration.
/// </summary>
public interface IVehicleSpecsService
{
    /// <summary>
    /// Gets full technical specifications for a vehicle by make/model/year/trim.
    /// </summary>
    Task<VehicleSpecification?> GetSpecsAsync(
        string make, string model, int year, string? trim = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets available trims for a make/model/year.
    /// </summary>
    Task<List<TrimInfo>> GetTrimsAsync(
        string make, string model, int year,
        CancellationToken ct = default);

    /// <summary>
    /// Gets available styles/configurations for a make/model/year.
    /// </summary>
    Task<List<VehicleStyle>> GetStylesAsync(
        string make, string model, int year,
        CancellationToken ct = default);

    /// <summary>
    /// Decodes a VIN and returns specs from the provider.
    /// </summary>
    Task<VehicleSpecification?> DecodeVinAsync(string vin, CancellationToken ct = default);
}

// ── DTOs ──────────────────────────────────────────────────────────

public record VehicleSpecification(
    string Make,
    string Model,
    int Year,
    string Trim,
    string BodyType,         // "Sedan" | "SUV" | "Truck" | "Coupe" | "Hatchback" ...
    int Doors,
    int Seats,
    EngineSpecs Engine,
    TransmissionSpecs Transmission,
    FuelEconomySpecs FuelEconomy,
    DimensionSpecs Dimensions,
    PerformanceSpecs Performance,
    SafetySpecs Safety,
    List<string> StandardFeatures,
    List<string> OptionalFeatures,
    decimal BaseMsrp,
    string Currency,          // "USD" | "DOP"
    string Provider           // "Edmunds" | "Mock"
);

public record EngineSpecs(
    string Type,              // "Inline-4" | "V6" | "V8" | "Electric" | "Hybrid"
    double DisplacementLiters,
    int Horsepower,
    int TorqueLbFt,
    string FuelType,          // "Gasoline" | "Diesel" | "Electric" | "Hybrid"
    int? Cylinders,
    bool Turbocharged,
    string? FuelSystem        // "Direct Injection" | "Port Injection" | "EFI"
);

public record TransmissionSpecs(
    string Type,              // "Automatic" | "Manual" | "CVT" | "DCT"
    int Speeds,
    string DriveType          // "FWD" | "RWD" | "AWD" | "4WD"
);

public record FuelEconomySpecs(
    double CityMpg,
    double HighwayMpg,
    double CombinedMpg,
    double? FuelTankGallons,
    int? ElectricRangeMiles   // For hybrids/EVs
);

public record DimensionSpecs(
    double LengthInches,
    double WidthInches,
    double HeightInches,
    double WheelbaseInches,
    double CurbWeightLbs,
    double? CargoVolumesCuFt,
    double? GroundClearanceInches
);

public record PerformanceSpecs(
    double? ZeroToSixtySeconds,
    int? TopSpeedMph,
    string? BrakingSystem,
    string? SuspensionFront,
    string? SuspensionRear
);

public record SafetySpecs(
    int? NhtsaOverallRating,    // 1-5 stars
    string? IihsRating,         // "Good" | "Acceptable" | "Marginal" | "Poor"
    List<string> StandardSafetyFeatures
);

public record TrimInfo(
    string Name,
    decimal BaseMsrp,
    string Currency,
    string? BodyType,
    string? DriveType
);

public record VehicleStyle(
    string StyleId,
    string Name,
    string BodyType,
    int Doors,
    string DriveType,
    decimal BaseMsrp,
    string Currency
);
