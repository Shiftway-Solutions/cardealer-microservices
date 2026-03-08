using Microsoft.Extensions.Logging;
using VehiclesSaleService.Application.Interfaces;

namespace VehiclesSaleService.Infrastructure.External;

/// <summary>
/// Mock implementation of IVehicleHistoryService (CARFAX/VinAudit).
/// Returns realistic simulated data for Dominican Republic vehicles.
/// 
/// TO SWAP FOR REAL API:
/// 1. Create CarfaxVehicleHistoryService or VinAuditVehicleHistoryService in this folder
/// 2. Implement IVehicleHistoryService using the real API
/// 3. Change DI registration in Program.cs:
///    builder.Services.AddHttpClient&lt;IVehicleHistoryService, CarfaxVehicleHistoryService&gt;(...)
/// 4. Add API key to appsettings/secrets:
///    "VehicleHistory": { "Provider": "CARFAX", "ApiKey": "xxx", "BaseUrl": "https://api.carfax.com" }
/// </summary>
public class MockVehicleHistoryService : IVehicleHistoryService
{
    private readonly ILogger<MockVehicleHistoryService> _logger;
    // Thread-safe: Random.Shared is safe for concurrent access in .NET 8+
    private static Random Rng => Random.Shared;

    // ── Static lookup arrays (must be declared BEFORE _vinDatabase so they're initialized first) ──

    private static readonly string[] ServiceDescriptions =
    {
        "Cambio de aceite y filtro",
        "Rotación de neumáticos",
        "Reemplazo de pastillas de freno",
        "Alineación y balanceo",
        "Mantenimiento preventivo 30,000 km",
        "Cambio de correa de distribución",
        "Revisión de transmisión",
        "Cambio de batería",
        "Inspección general — estado: Bueno",
        "Cambio de filtro de aire y habitáculo",
        "Reparación de A/C",
        "Inspección pre-compra",
    };

    private static readonly string[] DrLocations =
    {
        "Toyota RD — Santiago", "Honda Place Santo Domingo", "AutoMax La Vega",
        "Taller Central San Cristóbal", "MotoPlaza Puerto Plata",
        "AutoCenter Punta Cana", "RD Imports La Romana",
    };

    private static readonly string[] DrProvinces =
    {
        "Santo Domingo", "Santiago", "La Vega", "San Cristóbal",
        "Puerto Plata", "La Romana", "La Altagracia", "Duarte",
    };

    private static readonly string[] UsageTypes =
    {
        "Personal", "Commercial", "Rental", "Fleet",
    };

    // Simulated VIN database for testing (uses the static arrays above)
    private static readonly Dictionary<string, VehicleHistoryReport> _vinDatabase = new()
    {
        ["1HGBH41JXMN109186"] = CreateReport("1HGBH41JXMN109186", "Clean", 2, 0, false, 45_230m),
        ["5YJSA1DN5DFP14705"] = CreateReport("5YJSA1DN5DFP14705", "Clean", 1, 0, false, 32_100m),
        ["JTDKN3DU5A0123456"] = CreateReport("JTDKN3DU5A0123456", "Salvage", 3, 2, false, 89_400m),
        ["2T1BURHE3JC012345"] = CreateReport("2T1BURHE3JC012345", "Clean", 1, 0, false, 15_600m),
        ["WBA3A5C55CF256789"] = CreateReport("WBA3A5C55CF256789", "Rebuilt", 4, 1, true, 120_000m),
    };

    public MockVehicleHistoryService(ILogger<MockVehicleHistoryService> logger)
    {
        _logger = logger;
    }

    public async Task<VehicleHistoryReport?> GetHistoryByVinAsync(string vin, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] VehicleHistory: GetHistoryByVin({Vin})", vin);

        // Simulate API latency
        await Task.Delay(200 + Rng.Next(300), ct);

        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
        {
            _logger.LogWarning("[MOCK] VehicleHistory: Invalid VIN format: {Vin}", vin);
            return null;
        }

        // Return from database if known, otherwise generate deterministic data
        if (_vinDatabase.TryGetValue(vin.ToUpper(), out var cached))
            return cached;

        // Generate deterministic mock data based on VIN hash
        var hash = Math.Abs(vin.GetHashCode());
        var ownerCount = (hash % 4) + 1;
        var accidentCount = hash % 5 == 0 ? (hash % 3) : 0;
        var titleStatus = hash % 10 == 0 ? "Salvage" : hash % 15 == 0 ? "Rebuilt" : "Clean";
        var mileage = 10_000m + (hash % 150_000);

        var report = CreateReport(vin.ToUpper(), titleStatus, ownerCount, accidentCount, hash % 20 == 0, mileage);
        _logger.LogInformation("[MOCK] VehicleHistory: Generated report for {Vin} — {Owners} owners, {Accidents} accidents, {Title}",
            vin, ownerCount, accidentCount, titleStatus);

        return report;
    }

    public async Task<VehicleHistorySummary?> GetSummaryByVinAsync(string vin, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] VehicleHistory: GetSummaryByVin({Vin})", vin);
        await Task.Delay(100 + Rng.Next(150), ct);

        var report = await GetHistoryByVinAsync(vin, ct);
        if (report == null) return null;

        return new VehicleHistorySummary(
            Vin: report.Vin,
            OwnerCount: report.OwnerCount,
            TitleStatus: report.TitleStatus,
            AccidentCount: report.AccidentCount,
            HasOpenRecalls: report.HasOpenRecalls,
            LastReportedMileage: report.LastReportedMileage,
            OdometerRollback: report.OdometerRollback,
            Provider: "Mock"
        );
    }

    public async Task<bool> IsReportAvailableAsync(string vin, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] VehicleHistory: IsReportAvailable({Vin})", vin);
        await Task.Delay(50, ct);
        return !string.IsNullOrWhiteSpace(vin) && vin.Length == 17;
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static VehicleHistoryReport CreateReport(
        string vin, string titleStatus, int ownerCount, int accidentCount, bool floodDamage, decimal mileage)
    {
        var baseDate = DateTime.UtcNow.AddYears(-ownerCount * 3);

        var serviceHistory = Enumerable.Range(0, Rng.Next(3, 12)).Select(i => new ServiceRecord(
            Date: baseDate.AddMonths(i * 6 + Rng.Next(3)),
            Description: ServiceDescriptions[Rng.Next(ServiceDescriptions.Length)],
            Mileage: mileage * (i + 1) / 12,
            Location: DrLocations[Rng.Next(DrLocations.Length)]
        )).OrderBy(s => s.Date).ToList();

        var ownershipHistory = Enumerable.Range(1, ownerCount).Select(i => new OwnershipRecord(
            OwnerNumber: i,
            PurchaseDate: baseDate.AddYears(i - 1),
            SaleDate: i < ownerCount ? baseDate.AddYears(i) : null,
            State: DrProvinces[Rng.Next(DrProvinces.Length)],
            UsageType: i == 1 ? "Personal" : UsageTypes[Rng.Next(UsageTypes.Length)]
        )).ToList();

        var titleHistory = new List<TitleRecord>
        {
            new(baseDate, "Clean", "Santo Domingo", 0),
        };
        if (titleStatus != "Clean")
        {
            titleHistory.Add(new TitleRecord(
                baseDate.AddYears(ownerCount - 1),
                titleStatus,
                DrProvinces[Rng.Next(DrProvinces.Length)],
                mileage * 0.8m
            ));
        }

        return new VehicleHistoryReport(
            Vin: vin,
            Provider: "Mock",
            OwnerCount: ownerCount,
            TitleStatus: titleStatus,
            HasAccidents: accidentCount > 0,
            AccidentCount: accidentCount,
            HasFloodDamage: floodDamage,
            HasFireDamage: false,
            IsStolen: false,
            HasOpenRecalls: Rng.Next(5) == 0,
            RecallCount: Rng.Next(5) == 0 ? Rng.Next(1, 3) : 0,
            ServiceHistory: serviceHistory,
            OwnershipHistory: ownershipHistory,
            TitleHistory: titleHistory,
            LastReportedMileage: mileage,
            LastReportedMileageDate: DateTime.UtcNow.AddDays(-Rng.Next(30, 180)),
            OdometerRollback: false,
            ReportUrl: $"https://mock.carfax.com/report/{vin}",
            GeneratedAt: DateTime.UtcNow
        );
    }
}
