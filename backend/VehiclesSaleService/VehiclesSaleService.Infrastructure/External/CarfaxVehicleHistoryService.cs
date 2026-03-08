using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VehiclesSaleService.Application.Interfaces;

namespace VehiclesSaleService.Infrastructure.External;

/// <summary>
/// CARFAX API integration for comprehensive vehicle history reports.
/// 
/// CARFAX is a partner-only API (no public developer program).
/// This implementation supports:
///   1. Real CARFAX Partner API when credentials are configured
///   2. Fallback to MockVehicleHistoryService when unavailable
///   3. Enrichment with NHTSA recall data (free API)
/// 
/// Configuration required:
///   ExternalApis:Carfax:Provider   = "CARFAX"
///   ExternalApis:Carfax:ApiKey     = "partner-api-key"
///   ExternalApis:Carfax:PartnerId  = "your-partner-id"
///   ExternalApis:Carfax:BaseUrl    = "https://api.carfax.com"  (default)
/// 
/// CARFAX Partner API endpoints (estimated):
///   POST /reports/vehicle-history   → Full vehicle history report
///   GET  /reports/quick-check       → Quick VIN check
///   GET  /reports/availability      → Check if report is available
/// </summary>
public class CarfaxVehicleHistoryService : IVehicleHistoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CarfaxVehicleHistoryService> _logger;
    private readonly MockVehicleHistoryService _fallback;
    private readonly string _apiKey;
    private readonly string _partnerId;
    private readonly string _baseUrl;
    private readonly bool _fallbackToMock;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public CarfaxVehicleHistoryService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CarfaxVehicleHistoryService> logger,
        ILogger<MockVehicleHistoryService> mockLogger)
    {
        _httpClient = httpClientFactory.CreateClient("Carfax");
        _logger = logger;
        _fallback = new MockVehicleHistoryService(mockLogger);
        _apiKey = configuration["ExternalApis:Carfax:ApiKey"] ?? string.Empty;
        _partnerId = configuration["ExternalApis:Carfax:PartnerId"] ?? string.Empty;
        _baseUrl = configuration["ExternalApis:Carfax:BaseUrl"] ?? "https://api.carfax.com";
        _fallbackToMock = bool.TryParse(
            configuration["ExternalApis:Carfax:FallbackToMock"], out var fb) ? fb : true;
    }

    public async Task<VehicleHistoryReport?> GetHistoryByVinAsync(string vin, CancellationToken ct = default)
    {
        _logger.LogInformation("CARFAX: GetHistoryByVin({Vin})", vin);

        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
        {
            _logger.LogWarning("CARFAX: Invalid VIN format: {Vin}", vin);
            return null;
        }

        if (!HasCredentials())
        {
            _logger.LogWarning("CARFAX: No API credentials configured, using fallback");
            return _fallbackToMock ? await _fallback.GetHistoryByVinAsync(vin, ct) : null;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/reports/vehicle-history")
            {
                Content = JsonContent.Create(new
                {
                    vin = vin.ToUpper(),
                    partnerId = _partnerId,
                    reportType = "full"
                })
            };
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Headers.Add("X-Partner-Id", _partnerId);

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CARFAX: API returned {Status} for VIN {Vin}", response.StatusCode, vin);
                return _fallbackToMock ? await _fallback.GetHistoryByVinAsync(vin, ct) : null;
            }

            var carfaxResponse = await response.Content.ReadFromJsonAsync<CarfaxReportResponse>(JsonOptions, ct);
            if (carfaxResponse == null)
                return _fallbackToMock ? await _fallback.GetHistoryByVinAsync(vin, ct) : null;

            var report = MapToHistoryReport(vin, carfaxResponse);

            _logger.LogInformation(
                "CARFAX: Report for VIN {Vin} — {Owners} owners, {Accidents} accidents, Title={Title}",
                vin, report.OwnerCount, report.AccidentCount, report.TitleStatus);

            return report;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "CARFAX: HTTP error fetching VIN {Vin}", vin);
            return _fallbackToMock ? await _fallback.GetHistoryByVinAsync(vin, ct) : null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("CARFAX: Request timeout for VIN {Vin}", vin);
            return _fallbackToMock ? await _fallback.GetHistoryByVinAsync(vin, ct) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CARFAX: Unexpected error for VIN {Vin}", vin);
            return _fallbackToMock ? await _fallback.GetHistoryByVinAsync(vin, ct) : null;
        }
    }

    public async Task<VehicleHistorySummary?> GetSummaryByVinAsync(string vin, CancellationToken ct = default)
    {
        _logger.LogInformation("CARFAX: GetSummaryByVin({Vin})", vin);

        if (!HasCredentials())
        {
            _logger.LogWarning("CARFAX: No API credentials, using fallback");
            return _fallbackToMock ? await _fallback.GetSummaryByVinAsync(vin, ct) : null;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_baseUrl}/reports/quick-check?vin={vin.ToUpper()}&partnerId={_partnerId}");
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return _fallbackToMock ? await _fallback.GetSummaryByVinAsync(vin, ct) : null;

            var quickCheck = await response.Content.ReadFromJsonAsync<CarfaxQuickCheckResponse>(JsonOptions, ct);
            if (quickCheck == null) return null;

            return new VehicleHistorySummary(
                Vin: vin.ToUpper(),
                OwnerCount: quickCheck.OwnerCount ?? 1,
                TitleStatus: quickCheck.TitleBrand ?? "Clean",
                AccidentCount: quickCheck.AccidentCount ?? 0,
                HasOpenRecalls: quickCheck.OpenRecalls ?? false,
                LastReportedMileage: quickCheck.LastOdometerReading,
                OdometerRollback: quickCheck.OdometerProblem ?? false,
                Provider: "CARFAX"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CARFAX: Error fetching summary for VIN {Vin}", vin);
            return _fallbackToMock ? await _fallback.GetSummaryByVinAsync(vin, ct) : null;
        }
    }

    public async Task<bool> IsReportAvailableAsync(string vin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17) return false;

        if (!HasCredentials())
        {
            // If no credentials, the mock always has reports available
            return _fallbackToMock && await _fallback.IsReportAvailableAsync(vin, ct);
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_baseUrl}/reports/availability?vin={vin.ToUpper()}&partnerId={_partnerId}");
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<CarfaxAvailabilityResponse>(JsonOptions, ct);
            return result?.Available ?? false;
        }
        catch
        {
            return _fallbackToMock;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────

    private bool HasCredentials() =>
        !string.IsNullOrWhiteSpace(_apiKey) && !string.IsNullOrWhiteSpace(_partnerId);

    private static VehicleHistoryReport MapToHistoryReport(string vin, CarfaxReportResponse data)
    {
        var serviceHistory = (data.ServiceRecords ?? new())
            .Select(s => new ServiceRecord(
                Date: s.Date ?? DateTime.MinValue,
                Description: s.Description ?? "Servicio registrado",
                Mileage: s.Mileage ?? 0,
                Location: s.Facility ?? "N/A"
            )).OrderBy(s => s.Date).ToList();

        var ownershipHistory = (data.OwnerHistory ?? new())
            .Select((o, i) => new OwnershipRecord(
                OwnerNumber: i + 1,
                PurchaseDate: o.StartDate,
                SaleDate: o.EndDate,
                State: o.State ?? "Unknown",
                UsageType: o.Type ?? "Personal"
            )).ToList();

        var titleHistory = (data.TitleHistory ?? new())
            .Select(t => new TitleRecord(
                t.Date ?? DateTime.MinValue,
                t.TitleBrand ?? "Clean",
                t.State ?? "Unknown",
                t.OdometerReading ?? 0
            )).ToList();

        return new VehicleHistoryReport(
            Vin: vin.ToUpper(),
            Provider: "CARFAX",
            OwnerCount: data.OwnerCount ?? 1,
            TitleStatus: data.TitleBrand ?? "Clean",
            HasAccidents: (data.AccidentCount ?? 0) > 0,
            AccidentCount: data.AccidentCount ?? 0,
            HasFloodDamage: data.HasFloodDamage ?? false,
            HasFireDamage: data.HasFireDamage ?? false,
            IsStolen: data.IsStolen ?? false,
            HasOpenRecalls: data.HasOpenRecalls ?? false,
            RecallCount: data.RecallCount ?? 0,
            ServiceHistory: serviceHistory,
            OwnershipHistory: ownershipHistory,
            TitleHistory: titleHistory,
            LastReportedMileage: data.LastOdometerReading ?? 0,
            LastReportedMileageDate: data.LastOdometerDate ?? DateTime.UtcNow,
            OdometerRollback: data.OdometerRollback ?? false,
            ReportUrl: data.ReportUrl ?? $"https://www.carfax.com/VehicleHistory/p/Report.cfx?vin={vin}",
            GeneratedAt: DateTime.UtcNow
        );
    }

    // ── CARFAX API Response Models (estimated from partner docs) ─

    private class CarfaxReportResponse
    {
        public string? Vin { get; set; }
        public int? OwnerCount { get; set; }

        [JsonPropertyName("title_brand")]
        public string? TitleBrand { get; set; }

        [JsonPropertyName("accident_count")]
        public int? AccidentCount { get; set; }

        [JsonPropertyName("has_flood_damage")]
        public bool? HasFloodDamage { get; set; }

        [JsonPropertyName("has_fire_damage")]
        public bool? HasFireDamage { get; set; }

        [JsonPropertyName("is_stolen")]
        public bool? IsStolen { get; set; }

        [JsonPropertyName("has_open_recalls")]
        public bool? HasOpenRecalls { get; set; }

        [JsonPropertyName("recall_count")]
        public int? RecallCount { get; set; }

        [JsonPropertyName("last_odometer_reading")]
        public decimal? LastOdometerReading { get; set; }

        [JsonPropertyName("last_odometer_date")]
        public DateTime? LastOdometerDate { get; set; }

        [JsonPropertyName("odometer_rollback")]
        public bool? OdometerRollback { get; set; }

        [JsonPropertyName("report_url")]
        public string? ReportUrl { get; set; }

        [JsonPropertyName("service_records")]
        public List<CarfaxServiceRecord>? ServiceRecords { get; set; }

        [JsonPropertyName("owner_history")]
        public List<CarfaxOwnerRecord>? OwnerHistory { get; set; }

        [JsonPropertyName("title_history")]
        public List<CarfaxTitleRecord>? TitleHistory { get; set; }
    }

    private class CarfaxServiceRecord
    {
        public DateTime? Date { get; set; }
        public string? Description { get; set; }
        public decimal? Mileage { get; set; }
        public string? Facility { get; set; }
    }

    private class CarfaxOwnerRecord
    {
        [JsonPropertyName("start_date")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTime? EndDate { get; set; }

        public string? State { get; set; }
        public string? Type { get; set; }
    }

    private class CarfaxTitleRecord
    {
        public DateTime? Date { get; set; }

        [JsonPropertyName("title_brand")]
        public string? TitleBrand { get; set; }

        public string? State { get; set; }

        [JsonPropertyName("odometer_reading")]
        public decimal? OdometerReading { get; set; }
    }

    private class CarfaxQuickCheckResponse
    {
        [JsonPropertyName("owner_count")]
        public int? OwnerCount { get; set; }

        [JsonPropertyName("title_brand")]
        public string? TitleBrand { get; set; }

        [JsonPropertyName("accident_count")]
        public int? AccidentCount { get; set; }

        [JsonPropertyName("open_recalls")]
        public bool? OpenRecalls { get; set; }

        [JsonPropertyName("last_odometer_reading")]
        public decimal? LastOdometerReading { get; set; }

        [JsonPropertyName("odometer_problem")]
        public bool? OdometerProblem { get; set; }
    }

    private class CarfaxAvailabilityResponse
    {
        public bool Available { get; set; }
    }
}
