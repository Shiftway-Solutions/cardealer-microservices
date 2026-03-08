using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VehiclesSaleService.Infrastructure.External;

/// <summary>
/// NHTSA vPIC (Vehicle Product Information Catalog) API integration.
/// 
/// 🆓 This API is COMPLETELY FREE and requires NO API key!
/// 
/// NHTSA API documentation: https://vpic.nhtsa.dot.gov/api/
/// 
/// Key endpoints:
///   GET /api/vehicles/DecodeVin/{vin}?format=json      → Full VIN decode
///   GET /api/vehicles/GetModelsForMake/{make}?format=json → Models for a make
///   GET /api/vehicles/GetAllMakes?format=json           → All makes
///   GET /api/vehicles/GetParts?type=575&fromDate=...    → Recall campaigns
///   
/// Recalls API:
///   GET /api/Recalls/vehicle/modelyear/{year}/make/{make}/model/{model}?format=json
///   
/// Complaints API:
///   GET /api/Complaints/vehicle/modelyear/{year}/make/{make}/model/{model}?format=json
/// 
/// This service is used to:
///   1. Decode VINs for free (complementing Edmunds VIN decode)
///   2. Check for recalls (complementing CARFAX/VinAudit recall data)
///   3. Get safety ratings and complaints
///   4. Enrich vehicle data from other providers
/// </summary>
public interface INhtsaVehicleDataService
{
    /// <summary>
    /// Decode a VIN using NHTSA's free database.
    /// Returns make, model, year, body class, engine, transmission, and more.
    /// </summary>
    Task<NhtsaVinDecodeResult?> DecodeVinAsync(string vin, CancellationToken ct = default);

    /// <summary>
    /// Get all recalls for a specific vehicle (make/model/year).
    /// </summary>
    Task<List<NhtsaRecall>> GetRecallsAsync(
        string make, string model, int year,
        CancellationToken ct = default);

    /// <summary>
    /// Get safety complaints for a specific vehicle.
    /// </summary>
    Task<List<NhtsaComplaint>> GetComplaintsAsync(
        string make, string model, int year,
        CancellationToken ct = default);

    /// <summary>
    /// Get all available makes from NHTSA database.
    /// </summary>
    Task<List<NhtsaMake>> GetAllMakesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get all models for a specific make.
    /// </summary>
    Task<List<NhtsaModel>> GetModelsForMakeAsync(string make, CancellationToken ct = default);
}

// ── DTOs ──────────────────────────────────────────────────────────

public record NhtsaVinDecodeResult(
    string Vin,
    string Make,
    string Model,
    int ModelYear,
    string? Trim,
    string? BodyClass,           // "Sedan", "SUV", "Pickup", etc.
    string? DriveType,           // "FWD", "RWD", "AWD", "4WD"
    string? EngineDisplacement,
    int? EngineCylinders,
    string? FuelType,            // "Gasoline", "Diesel", "Electric"
    string? TransmissionStyle,
    int? TransmissionSpeeds,
    string? PlantCountry,
    string? PlantCity,
    string? Manufacturer,
    string? VehicleType,         // "PASSENGER CAR", "TRUCK", "MULTIPURPOSE PASSENGER VEHICLE"
    int? Doors,
    int? GrossVehicleWeightRating,
    string? ErrorCode,           // "0" = success, non-zero = decode error
    List<string> Errors
);

public record NhtsaRecall(
    string CampaignNumber,
    DateTime? ReportReceivedDate,
    string Component,
    string Summary,
    string Consequence,
    string Remedy,
    int? PotentialUnitsAffected,
    string? ManufacturerName
);

public record NhtsaComplaint(
    int OdiNumber,
    DateTime? DateComplaintFiled,
    string Component,
    string Summary,
    bool CrashReported,
    bool InjuryReported,
    int? Speed,
    decimal? OdometerReading
);

public record NhtsaMake(
    int MakeId,
    string MakeName
);

public record NhtsaModel(
    int ModelId,
    string ModelName,
    int MakeId,
    string MakeName
);

// ── Implementation ──────────────────────────────────────────────

public class NhtsaVehicleDataService : INhtsaVehicleDataService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NhtsaVehicleDataService> _logger;
    private readonly string _baseUrl;

    // Cache TTLs — NHTSA data is static/semi-static reference data
    private static readonly TimeSpan VIN_CACHE_TTL = TimeSpan.FromDays(7);        // VIN specs don't change
    private static readonly TimeSpan MAKES_CACHE_TTL = TimeSpan.FromHours(24);    // Makes list rarely changes
    private static readonly TimeSpan MODELS_CACHE_TTL = TimeSpan.FromHours(24);   // Models per make
    private static readonly TimeSpan RECALLS_CACHE_TTL = TimeSpan.FromHours(6);   // New recalls are rare

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public NhtsaVehicleDataService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<NhtsaVehicleDataService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("NHTSA");
        _cache = cache;
        _logger = logger;
        _baseUrl = configuration["ExternalApis:Nhtsa:BaseUrl"]
            ?? "https://vpic.nhtsa.dot.gov/api";
    }

    public async Task<NhtsaVinDecodeResult?> DecodeVinAsync(string vin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
        {
            _logger.LogWarning("NHTSA: Invalid VIN format: {Vin}", vin);
            return null;
        }

        // VIN decode results never change — cache for 7 days
        var cacheKey = $"nhtsa:vin:{vin.ToUpperInvariant()}";
        if (_cache.TryGetValue(cacheKey, out NhtsaVinDecodeResult? cached))
        {
            _logger.LogDebug("NHTSA: VIN {Vin} served from cache", vin);
            return cached;
        }

        try
        {
            _logger.LogInformation("NHTSA: Decoding VIN {Vin}", vin);

            var url = $"{_baseUrl}/vehicles/DecodeVin/{vin}?format=json";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NHTSA: API returned {Status} for VIN {Vin}", response.StatusCode, vin);
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<NhtsaApiResponse>(JsonOptions, ct);
            if (data?.Results == null || data.Results.Count == 0)
            {
                _logger.LogWarning("NHTSA: Empty response for VIN {Vin}", vin);
                return null;
            }

            var result = MapVinDecodeResult(vin, data.Results);
            if (result != null)
                _cache.Set(cacheKey, result, VIN_CACHE_TTL);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "NHTSA: HTTP error decoding VIN {Vin}", vin);
            return null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("NHTSA: Request timeout for VIN {Vin}", vin);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NHTSA: Unexpected error decoding VIN {Vin}", vin);
            return null;
        }
    }

    public async Task<List<NhtsaRecall>> GetRecallsAsync(
        string make, string model, int year, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("NHTSA: Fetching recalls for {Make} {Model} {Year}", make, model, year);

            var url = $"https://api.nhtsa.gov/recalls/recallsByVehicle" +
                     $"?make={Uri.EscapeDataString(make)}" +
                     $"&model={Uri.EscapeDataString(model)}" +
                     $"&modelYear={year}";

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NHTSA: Recalls API returned {Status}", response.StatusCode);
                return new();
            }

            var data = await response.Content.ReadFromJsonAsync<NhtsaRecallsResponse>(JsonOptions, ct);
            if (data?.Results == null) return new();

            return data.Results.Select(r => new NhtsaRecall(
                CampaignNumber: r.NHTSACampaignNumber ?? "Unknown",
                ReportReceivedDate: ParseDate(r.ReportReceivedDate),
                Component: r.Component ?? "Unknown",
                Summary: r.Summary ?? "No description available",
                Consequence: r.Consequence ?? "No information",
                Remedy: r.Remedy ?? "Contact dealer",
                PotentialUnitsAffected: r.PotentialNumberOfUnitsAffected,
                ManufacturerName: r.Manufacturer
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NHTSA: Error fetching recalls for {Make} {Model} {Year}", make, model, year);
            return new();
        }
    }

    public async Task<List<NhtsaComplaint>> GetComplaintsAsync(
        string make, string model, int year, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("NHTSA: Fetching complaints for {Make} {Model} {Year}", make, model, year);

            var url = $"https://api.nhtsa.gov/complaints/complaintsByVehicle" +
                     $"?make={Uri.EscapeDataString(make)}" +
                     $"&model={Uri.EscapeDataString(model)}" +
                     $"&modelYear={year}";

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return new();

            var data = await response.Content.ReadFromJsonAsync<NhtsaComplaintsResponse>(JsonOptions, ct);
            if (data?.Results == null) return new();

            return data.Results.Select(c => new NhtsaComplaint(
                OdiNumber: c.OdiNumber ?? 0,
                DateComplaintFiled: ParseDate(c.DateComplaintFiled),
                Component: c.Components ?? "Unknown",
                Summary: c.Summary ?? "No description",
                CrashReported: c.Crash?.Equals("Y", StringComparison.OrdinalIgnoreCase) ?? false,
                InjuryReported: c.Injuries > 0,
                Speed: c.Speed,
                OdometerReading: null
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NHTSA: Error fetching complaints");
            return new();
        }
    }

    public async Task<List<NhtsaMake>> GetAllMakesAsync(CancellationToken ct = default)
    {
        // Makes list is static reference data — cache for 24 hours
        const string cacheKey = "nhtsa:makes:all";
        if (_cache.TryGetValue(cacheKey, out List<NhtsaMake>? cachedMakes) && cachedMakes != null)
            return cachedMakes;

        try
        {
            _logger.LogInformation("NHTSA: Fetching all makes");

            var url = $"{_baseUrl}/vehicles/GetAllMakes?format=json";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode) return new();

            var data = await response.Content.ReadFromJsonAsync<NhtsaMakesResponse>(JsonOptions, ct);
            if (data?.Results == null) return new();

            var makes = data.Results
                .Where(m => !string.IsNullOrWhiteSpace(m.Make_Name))
                .Select(m => new NhtsaMake(
                    MakeId: m.Make_ID ?? 0,
                    MakeName: m.Make_Name!
                ))
                .OrderBy(m => m.MakeName)
                .ToList();

            if (makes.Count > 0)
                _cache.Set(cacheKey, makes, MAKES_CACHE_TTL);
            return makes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NHTSA: Error fetching makes");
            return new();
        }
    }

    public async Task<List<NhtsaModel>> GetModelsForMakeAsync(string make, CancellationToken ct = default)
    {
        // Models per make is static reference data — cache for 24 hours
        var cacheKey = $"nhtsa:models:{make.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out List<NhtsaModel>? cachedModels) && cachedModels != null)
            return cachedModels;

        try
        {
            _logger.LogInformation("NHTSA: Fetching models for {Make}", make);

            var url = $"{_baseUrl}/vehicles/GetModelsForMake/{Uri.EscapeDataString(make)}?format=json";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode) return new();

            var data = await response.Content.ReadFromJsonAsync<NhtsaModelsResponse>(JsonOptions, ct);
            if (data?.Results == null) return new();

            var models = data.Results
                .Where(m => !string.IsNullOrWhiteSpace(m.Model_Name))
                .Select(m => new NhtsaModel(
                    ModelId: m.Model_ID ?? 0,
                    ModelName: m.Model_Name!,
                    MakeId: m.Make_ID ?? 0,
                    MakeName: m.Make_Name ?? make
                ))
                .OrderBy(m => m.ModelName)
                .ToList();

            if (models.Count > 0)
                _cache.Set(cacheKey, models, MODELS_CACHE_TTL);
            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NHTSA: Error fetching models for {Make}", make);
            return new();
        }
    }

    // ── Mapping ─────────────────────────────────────────────────

    private static NhtsaVinDecodeResult MapVinDecodeResult(string vin, List<NhtsaVinDecodeVariable> results)
    {
        string? GetValue(int variableId) =>
            results.FirstOrDefault(r => r.VariableId == variableId)?.Value;

        string? GetValueByName(string name) =>
            results.FirstOrDefault(r =>
                r.Variable?.Equals(name, StringComparison.OrdinalIgnoreCase) == true)?.Value;

        var errors = results
            .Where(r => r.VariableId == 143 && !string.IsNullOrWhiteSpace(r.Value) && r.Value != "0")
            .Select(r => r.Value!)
            .ToList();

        int.TryParse(GetValue(29), out var modelYear);
        int.TryParse(GetValueByName("Engine Number of Cylinders"), out var cylinders);
        int.TryParse(GetValueByName("Number of Doors") ?? GetValueByName("Doors"), out var doors);

        return new NhtsaVinDecodeResult(
            Vin: vin.ToUpper(),
            Make: GetValue(26) ?? GetValueByName("Make") ?? "Unknown",
            Model: GetValue(28) ?? GetValueByName("Model") ?? "Unknown",
            ModelYear: modelYear,
            Trim: GetValue(109) ?? GetValueByName("Trim"),
            BodyClass: GetValue(5) ?? GetValueByName("Body Class"),
            DriveType: GetValue(15) ?? GetValueByName("Drive Type"),
            EngineDisplacement: GetValueByName("Displacement (L)"),
            EngineCylinders: cylinders > 0 ? cylinders : null,
            FuelType: GetValue(24) ?? GetValueByName("Fuel Type - Primary"),
            TransmissionStyle: GetValue(37) ?? GetValueByName("Transmission Style"),
            TransmissionSpeeds: int.TryParse(
                GetValueByName("Transmission Speeds"), out var speeds) ? speeds : null,
            PlantCountry: GetValue(75) ?? GetValueByName("Plant Country"),
            PlantCity: GetValue(76) ?? GetValueByName("Plant City"),
            Manufacturer: GetValue(27) ?? GetValueByName("Manufacturer Name"),
            VehicleType: GetValue(39) ?? GetValueByName("Vehicle Type"),
            Doors: doors > 0 ? doors : null,
            GrossVehicleWeightRating: null,
            ErrorCode: GetValue(143),
            Errors: errors
        );
    }

    private static DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return null;
        return DateTime.TryParse(dateStr, out var dt) ? dt : null;
    }

    // ── NHTSA API Response Models ───────────────────────────────

    private class NhtsaApiResponse
    {
        public int? Count { get; set; }
        public string? Message { get; set; }
        public string? SearchCriteria { get; set; }
        public List<NhtsaVinDecodeVariable>? Results { get; set; }
    }

    private class NhtsaVinDecodeVariable
    {
        public string? Value { get; set; }
        public string? ValueId { get; set; }
        public string? Variable { get; set; }
        public int? VariableId { get; set; }
    }

    private class NhtsaRecallsResponse
    {
        public int? Count { get; set; }
        public List<NhtsaRecallItem>? Results { get; set; }
    }

    private class NhtsaRecallItem
    {
        public string? NHTSACampaignNumber { get; set; }
        public string? ReportReceivedDate { get; set; }
        public string? Component { get; set; }
        public string? Summary { get; set; }
        public string? Consequence { get; set; }
        public string? Remedy { get; set; }

        [JsonPropertyName("PotentialNumberOfUnitsAffected")]
        public int? PotentialNumberOfUnitsAffected { get; set; }

        public string? Manufacturer { get; set; }
    }

    private class NhtsaComplaintsResponse
    {
        public int? Count { get; set; }
        public List<NhtsaComplaintItem>? Results { get; set; }
    }

    private class NhtsaComplaintItem
    {
        [JsonPropertyName("odiNumber")]
        public int? OdiNumber { get; set; }

        [JsonPropertyName("dateComplaintFiled")]
        public string? DateComplaintFiled { get; set; }

        [JsonPropertyName("components")]
        public string? Components { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("crash")]
        public string? Crash { get; set; }

        [JsonPropertyName("injuries")]
        public int Injuries { get; set; }

        [JsonPropertyName("speed")]
        public int? Speed { get; set; }
    }

    private class NhtsaMakesResponse
    {
        public int? Count { get; set; }
        public List<NhtsaMakeItem>? Results { get; set; }
    }

    private class NhtsaMakeItem
    {
        public int? Make_ID { get; set; }
        public string? Make_Name { get; set; }
    }

    private class NhtsaModelsResponse
    {
        public int? Count { get; set; }
        public List<NhtsaModelItem>? Results { get; set; }
    }

    private class NhtsaModelItem
    {
        public int? Make_ID { get; set; }
        public string? Make_Name { get; set; }
        public int? Model_ID { get; set; }
        public string? Model_Name { get; set; }
    }
}
