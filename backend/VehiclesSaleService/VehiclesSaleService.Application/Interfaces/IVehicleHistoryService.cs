namespace VehiclesSaleService.Application.Interfaces;

/// <summary>
/// Vehicle history provider (CARFAX / VinAudit integration).
/// Current implementation returns simulated data for testing.
/// Replace with real API client by swapping DI registration.
/// </summary>
public interface IVehicleHistoryService
{
    /// <summary>
    /// Gets a full vehicle history report by VIN.
    /// </summary>
    Task<VehicleHistoryReport?> GetHistoryByVinAsync(string vin, CancellationToken ct = default);

    /// <summary>
    /// Gets a quick summary (accident count, owner count, title status) by VIN.
    /// Cheaper/faster call for previews.
    /// </summary>
    Task<VehicleHistorySummary?> GetSummaryByVinAsync(string vin, CancellationToken ct = default);

    /// <summary>
    /// Checks if a report is available for the given VIN.
    /// </summary>
    Task<bool> IsReportAvailableAsync(string vin, CancellationToken ct = default);
}

// ── DTOs ──────────────────────────────────────────────────────────

public record VehicleHistoryReport(
    string Vin,
    string Provider,          // "CARFAX" | "VinAudit" | "Mock"
    int OwnerCount,
    string TitleStatus,       // "Clean" | "Salvage" | "Rebuilt" | "Flood" | "Lemon"
    bool HasAccidents,
    int AccidentCount,
    bool HasFloodDamage,
    bool HasFireDamage,
    bool IsStolen,
    bool HasOpenRecalls,
    int RecallCount,
    List<ServiceRecord> ServiceHistory,
    List<OwnershipRecord> OwnershipHistory,
    List<TitleRecord> TitleHistory,
    decimal? LastReportedMileage,
    DateTime? LastReportedMileageDate,
    bool OdometerRollback,
    string? ReportUrl,
    DateTime GeneratedAt
);

public record ServiceRecord(
    DateTime Date,
    string Description,
    decimal? Mileage,
    string? Location
);

public record OwnershipRecord(
    int OwnerNumber,
    DateTime? PurchaseDate,
    DateTime? SaleDate,
    string? State,
    string? UsageType    // "Personal" | "Commercial" | "Rental" | "Fleet" | "Government"
);

public record TitleRecord(
    DateTime Date,
    string TitleType,     // "Clean" | "Salvage" | "Rebuilt"
    string? State,
    decimal? OdometerReading
);

public record VehicleHistorySummary(
    string Vin,
    int OwnerCount,
    string TitleStatus,
    int AccidentCount,
    bool HasOpenRecalls,
    decimal? LastReportedMileage,
    bool OdometerRollback,
    string Provider
);
