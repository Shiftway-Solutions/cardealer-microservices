using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VehiclesSaleService.Application.Interfaces;

namespace VehiclesSaleService.Infrastructure.External;

/// <summary>
/// Real VinAudit integration for vehicle history reports.
/// VinAudit provides NMVTIS-sourced data including title history,
/// accident records, odometer readings, and auction history (COPART/IAAI).
/// 
/// API: https://www.vinaudit.com/api/
/// Cost: ~$0.20/report (most cost-effective for DR market)
/// 
/// Configuration required:
///   ExternalApis:VehicleHistory:Provider = "VinAudit"
///   ExternalApis:VehicleHistory:ApiKey = "your-api-key"
///   ExternalApis:VehicleHistory:BaseUrl = "https://api.vinaudit.com/v1"
/// </summary>
public class VinAuditVehicleHistoryService : IVehicleHistoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VinAuditVehicleHistoryService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public VinAuditVehicleHistoryService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<VinAuditVehicleHistoryService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("VinAudit");
        _logger = logger;
        _apiKey = configuration["ExternalApis:VehicleHistory:ApiKey"]
            ?? throw new InvalidOperationException("VinAudit API key not configured. Set ExternalApis:VehicleHistory:ApiKey");
        _baseUrl = configuration["ExternalApis:VehicleHistory:BaseUrl"] ?? "https://api.vinaudit.com/v1";
    }

    public async Task<VehicleHistoryReport?> GetHistoryByVinAsync(string vin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
        {
            _logger.LogWarning("VinAudit: Invalid VIN format: {Vin}", vin);
            return null;
        }

        try
        {
            _logger.LogInformation("VinAudit: Fetching history for VIN {Vin}", vin);

            var url = $"{_baseUrl}/vehicle-history?vin={vin}&key={_apiKey}&format=json";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("VinAudit: API returned {Status} for VIN {Vin}", response.StatusCode, vin);
                return null;
            }

            var vinAuditResponse = await response.Content.ReadFromJsonAsync<VinAuditResponse>(JsonOptions, ct);
            if (vinAuditResponse == null || !vinAuditResponse.Success)
            {
                _logger.LogWarning("VinAudit: Empty or failed response for VIN {Vin}", vin);
                return null;
            }

            var report = MapToHistoryReport(vin, vinAuditResponse);

            _logger.LogInformation(
                "VinAudit: Report for VIN {Vin} — {Owners} owners, {Accidents} accidents, Title={Title}",
                vin, report.OwnerCount, report.AccidentCount, report.TitleStatus);

            return report;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "VinAudit: HTTP error fetching VIN {Vin}", vin);
            return null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("VinAudit: Request timeout for VIN {Vin}", vin);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VinAudit: Unexpected error for VIN {Vin}", vin);
            return null;
        }
    }

    public async Task<VehicleHistorySummary?> GetSummaryByVinAsync(string vin, CancellationToken ct = default)
    {
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
            Provider: "VinAudit"
        );
    }

    public async Task<bool> IsReportAvailableAsync(string vin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17) return false;

        try
        {
            var url = $"{_baseUrl}/vehicle-availability?vin={vin}&key={_apiKey}&format=json";
            var response = await _httpClient.GetAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ── Mapping: VinAudit API → Our Domain Model ────────────────

    private static VehicleHistoryReport MapToHistoryReport(string vin, VinAuditResponse data)
    {
        var titleStatus = MapTitleStatus(data.TitleRecords);
        var accidentCount = data.AccidentRecords?.Count ?? 0;
        var hasFloodDamage = data.TitleRecords?.Any(t =>
            t.Brand?.Contains("flood", StringComparison.OrdinalIgnoreCase) == true) ?? false;
        var hasFireDamage = data.TitleRecords?.Any(t =>
            t.Brand?.Contains("fire", StringComparison.OrdinalIgnoreCase) == true) ?? false;
        var isStolen = data.TheftRecords?.Any() ?? false;
        var odometerRollback = DetectOdometerRollback(data.OdometerRecords);

        var serviceHistory = (data.ServiceRecords ?? new List<VinAuditServiceRecord>())
            .Select(s => new ServiceRecord(
                Date: ParseDate(s.Date),
                Description: s.Description ?? "Servicio registrado",
                Mileage: s.Mileage ?? 0,
                Location: s.Facility ?? "N/A"
            )).OrderBy(s => s.Date).ToList();

        var ownershipHistory = (data.OwnerRecords ?? new List<VinAuditOwnerRecord>())
            .Select((o, i) => new OwnershipRecord(
                OwnerNumber: i + 1,
                PurchaseDate: ParseDate(o.PurchaseDate),
                SaleDate: !string.IsNullOrEmpty(o.SaleDate) ? ParseDate(o.SaleDate) : null,
                State: o.State ?? "Unknown",
                UsageType: o.UsageType ?? "Personal"
            )).ToList();

        var titleHistory = (data.TitleRecords ?? new List<VinAuditTitleRecord>())
            .Select(t => new TitleRecord(
                ParseDate(t.Date),
                t.Type ?? "Clean",
                t.State ?? "Unknown",
                t.Mileage ?? 0
            )).ToList();

        var lastMileage = data.OdometerRecords?
            .OrderByDescending(o => ParseDate(o.Date))
            .FirstOrDefault()?.Mileage ?? 0;

        return new VehicleHistoryReport(
            Vin: vin.ToUpper(),
            Provider: "VinAudit",
            OwnerCount: data.OwnerRecords?.Count ?? 1,
            TitleStatus: titleStatus,
            HasAccidents: accidentCount > 0,
            AccidentCount: accidentCount,
            HasFloodDamage: hasFloodDamage,
            HasFireDamage: hasFireDamage,
            IsStolen: isStolen,
            HasOpenRecalls: false, // VinAudit doesn't provide recall status; use NHTSA API for this
            RecallCount: 0,
            ServiceHistory: serviceHistory,
            OwnershipHistory: ownershipHistory,
            TitleHistory: titleHistory,
            LastReportedMileage: lastMileage,
            LastReportedMileageDate: data.OdometerRecords?
                .OrderByDescending(o => ParseDate(o.Date))
                .Select(o => ParseDate(o.Date))
                .FirstOrDefault() ?? DateTime.UtcNow,
            OdometerRollback: odometerRollback,
            ReportUrl: $"https://www.vinaudit.com/vehicle-history/{vin}",
            GeneratedAt: DateTime.UtcNow
        );
    }

    private static string MapTitleStatus(List<VinAuditTitleRecord>? titleRecords)
    {
        if (titleRecords == null || titleRecords.Count == 0) return "Clean";

        // Check the most recent title for brand/type
        var latest = titleRecords.OrderByDescending(t => ParseDate(t.Date)).First();
        var brand = (latest.Brand ?? "").ToLower();
        var type = (latest.Type ?? "").ToLower();

        if (brand.Contains("salvage") || type.Contains("salvage")) return "Salvage";
        if (brand.Contains("rebuilt") || type.Contains("rebuilt")) return "Rebuilt";
        if (brand.Contains("flood")) return "Flood";
        if (brand.Contains("junk") || type.Contains("junk")) return "Junk";

        return "Clean";
    }

    private static bool DetectOdometerRollback(List<VinAuditOdometerRecord>? records)
    {
        if (records == null || records.Count < 2) return false;

        var sorted = records
            .Where(r => r.Mileage > 0)
            .OrderBy(r => ParseDate(r.Date))
            .ToList();

        for (int i = 1; i < sorted.Count; i++)
        {
            // If mileage decreased by more than 500 miles, flag as rollback
            if (sorted[i].Mileage < sorted[i - 1].Mileage - 500)
            {
                return true;
            }
        }

        return false;
    }

    private static DateTime ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return DateTime.MinValue;
        return DateTime.TryParse(dateStr, out var dt) ? dt : DateTime.MinValue;
    }

    // ── VinAudit API Response Models ────────────────────────────

    private class VinAuditResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }

        [JsonPropertyName("title_records")]
        public List<VinAuditTitleRecord>? TitleRecords { get; set; }

        [JsonPropertyName("owner_records")]
        public List<VinAuditOwnerRecord>? OwnerRecords { get; set; }

        [JsonPropertyName("accident_records")]
        public List<VinAuditAccidentRecord>? AccidentRecords { get; set; }

        [JsonPropertyName("odometer_records")]
        public List<VinAuditOdometerRecord>? OdometerRecords { get; set; }

        [JsonPropertyName("service_records")]
        public List<VinAuditServiceRecord>? ServiceRecords { get; set; }

        [JsonPropertyName("theft_records")]
        public List<VinAuditTheftRecord>? TheftRecords { get; set; }
    }

    private class VinAuditTitleRecord
    {
        public string? Date { get; set; }
        public string? Type { get; set; }
        public string? Brand { get; set; }
        public string? State { get; set; }
        public decimal? Mileage { get; set; }
    }

    private class VinAuditOwnerRecord
    {
        [JsonPropertyName("purchase_date")]
        public string? PurchaseDate { get; set; }

        [JsonPropertyName("sale_date")]
        public string? SaleDate { get; set; }

        public string? State { get; set; }

        [JsonPropertyName("usage_type")]
        public string? UsageType { get; set; }
    }

    private class VinAuditAccidentRecord
    {
        public string? Date { get; set; }
        public string? Severity { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
    }

    private class VinAuditOdometerRecord
    {
        public string? Date { get; set; }
        public decimal Mileage { get; set; }
        public string? Source { get; set; }
    }

    private class VinAuditServiceRecord
    {
        public string? Date { get; set; }
        public string? Description { get; set; }
        public decimal? Mileage { get; set; }
        public string? Facility { get; set; }
    }

    private class VinAuditTheftRecord
    {
        public string? Date { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; } // "Recovered" | "Outstanding"
    }
}
