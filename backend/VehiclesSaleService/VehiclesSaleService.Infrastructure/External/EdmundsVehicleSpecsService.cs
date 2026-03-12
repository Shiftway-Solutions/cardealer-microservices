using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VehiclesSaleService.Application.Interfaces;

namespace VehiclesSaleService.Infrastructure.External;

/// <summary>
/// Real Edmunds API integration for vehicle technical specifications.
/// 
/// Edmunds API documentation: https://developer.edmunds.com/api-documentation/overview/
/// 
/// Key endpoints:
///   GET /api/vehicle/v3/makes                           → All makes
///   GET /api/vehicle/v3/{make}/models                   → Models for a make
///   GET /api/vehicle/v3/{make}/{model}/{year}/styles     → Styles/trims
///   GET /api/vehicle/v2/vins/{vin}                      → VIN decode
///   GET /api/vehicle/v2/{make}/{model}/{year}/styles/{styleId}/equipment  → Equipment
///   GET /api/vehicle/v2/{make}/{model}/{year}/styles/{styleId}/engine     → Engine/Transmission
/// 
/// Configuration required:
///   ExternalApis:VehicleSpecs:Provider = "Edmunds"
///   ExternalApis:VehicleSpecs:ApiKey   = "your-edmunds-api-key"
///   ExternalApis:VehicleSpecs:BaseUrl  = "https://api.edmunds.com"  (default)
/// 
/// When ApiKey is not set, falls back to MockVehicleSpecsService if FallbackToMock=true.
/// </summary>
public class EdmundsVehicleSpecsService : IVehicleSpecsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EdmundsVehicleSpecsService> _logger;
    private readonly MockVehicleSpecsService _fallback;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly bool _fallbackToMock;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public EdmundsVehicleSpecsService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<EdmundsVehicleSpecsService> logger,
        ILogger<MockVehicleSpecsService> mockLogger)
    {
        _httpClient = httpClientFactory.CreateClient("Edmunds");
        _logger = logger;
        _fallback = new MockVehicleSpecsService(mockLogger);
        _apiKey = configuration["ExternalApis:VehicleSpecs:ApiKey"] ?? string.Empty;
        _baseUrl = configuration["ExternalApis:VehicleSpecs:BaseUrl"] ?? "https://api.edmunds.com";
        _fallbackToMock = bool.TryParse(
            configuration["ExternalApis:VehicleSpecs:FallbackToMock"], out var fb) ? fb : true;
    }

    public async Task<VehicleSpecification?> GetSpecsAsync(
        string make, string model, int year, string? trim = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Edmunds: GetSpecs({Make}, {Model}, {Year}, {Trim})", make, model, year, trim);

        if (!HasApiKey())
        {
            _logger.LogWarning("Edmunds: No API key configured, using fallback");
            return _fallbackToMock ? await _fallback.GetSpecsAsync(make, model, year, trim, ct) : null;
        }

        try
        {
            // Step 1: Get styles for the make/model/year
            var stylesUrl = $"{_baseUrl}/api/vehicle/v3/{Encode(make)}/{Encode(model)}/{year}/styles" +
                           $"?fmt=json&api_key={_apiKey}";

            var stylesResponse = await _httpClient.GetAsync(stylesUrl, ct);
            if (!stylesResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Edmunds: Styles API returned {Status}", stylesResponse.StatusCode);
                return _fallbackToMock ? await _fallback.GetSpecsAsync(make, model, year, trim, ct) : null;
            }

            var stylesData = await stylesResponse.Content.ReadFromJsonAsync<EdmundsStylesResponse>(JsonOptions, ct);
            if (stylesData?.Styles == null || stylesData.Styles.Count == 0)
            {
                _logger.LogWarning("Edmunds: No styles found for {Make} {Model} {Year}", make, model, year);
                return _fallbackToMock ? await _fallback.GetSpecsAsync(make, model, year, trim, ct) : null;
            }

            // Select the best matching style (prefer matching trim)
            var style = trim != null
                ? stylesData.Styles.FirstOrDefault(s =>
                    s.Trim?.Equals(trim, StringComparison.OrdinalIgnoreCase) == true)
                  ?? stylesData.Styles.First()
                : stylesData.Styles.First();

            return MapStyleToSpecification(make, model, year, style);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Edmunds: HTTP error fetching specs for {Make} {Model} {Year}", make, model, year);
            return _fallbackToMock ? await _fallback.GetSpecsAsync(make, model, year, trim, ct) : null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Edmunds: Request timeout");
            return _fallbackToMock ? await _fallback.GetSpecsAsync(make, model, year, trim, ct) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Edmunds: Unexpected error");
            return _fallbackToMock ? await _fallback.GetSpecsAsync(make, model, year, trim, ct) : null;
        }
    }

    public async Task<List<TrimInfo>> GetTrimsAsync(
        string make, string model, int year,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Edmunds: GetTrims({Make}, {Model}, {Year})", make, model, year);

        if (!HasApiKey())
        {
            _logger.LogWarning("Edmunds: No API key configured, using fallback");
            return _fallbackToMock ? await _fallback.GetTrimsAsync(make, model, year, ct) : new();
        }

        try
        {
            var url = $"{_baseUrl}/api/vehicle/v3/{Encode(make)}/{Encode(model)}/{year}/styles" +
                     $"?fmt=json&api_key={_apiKey}";

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return _fallbackToMock ? await _fallback.GetTrimsAsync(make, model, year, ct) : new();

            var data = await response.Content.ReadFromJsonAsync<EdmundsStylesResponse>(JsonOptions, ct);
            if (data?.Styles == null) return new();

            return data.Styles
                .GroupBy(s => s.Trim ?? "Base")
                .Select(g =>
                {
                    var first = g.First();
                    return new TrimInfo(
                        Name: g.Key,
                        BaseMsrp: first.Price?.BaseMsrp ?? 0,
                        Currency: "USD",
                        BodyType: first.Submodel?.Body,
                        DriveType: first.DrivenWheels ?? "FWD"
                    );
                }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Edmunds: Error fetching trims");
            return _fallbackToMock ? await _fallback.GetTrimsAsync(make, model, year, ct) : new();
        }
    }

    public async Task<List<VehicleStyle>> GetStylesAsync(
        string make, string model, int year,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Edmunds: GetStyles({Make}, {Model}, {Year})", make, model, year);

        if (!HasApiKey())
            return _fallbackToMock ? await _fallback.GetStylesAsync(make, model, year, ct) : new();

        try
        {
            var url = $"{_baseUrl}/api/vehicle/v3/{Encode(make)}/{Encode(model)}/{year}/styles" +
                     $"?fmt=json&api_key={_apiKey}";

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return _fallbackToMock ? await _fallback.GetStylesAsync(make, model, year, ct) : new();

            var data = await response.Content.ReadFromJsonAsync<EdmundsStylesResponse>(JsonOptions, ct);
            if (data?.Styles == null) return new();

            return data.Styles.Select(s => new VehicleStyle(
                StyleId: s.Id?.ToString() ?? $"edmunds-{make}-{model}-{year}",
                Name: s.Name ?? $"{year} {make} {model}",
                BodyType: s.Submodel?.Body ?? "Unknown",
                Doors: s.NumOfDoors ?? 4,
                DriveType: s.DrivenWheels ?? "FWD",
                BaseMsrp: s.Price?.BaseMsrp ?? 0,
                Currency: "USD"
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Edmunds: Error fetching styles");
            return _fallbackToMock ? await _fallback.GetStylesAsync(make, model, year, ct) : new();
        }
    }

    public async Task<VehicleSpecification?> DecodeVinAsync(string vin, CancellationToken ct = default)
    {
        _logger.LogInformation("Edmunds: DecodeVin({Vin})", vin);

        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17) return null;

        if (!HasApiKey())
        {
            _logger.LogWarning("Edmunds: No API key configured, using fallback");
            return _fallbackToMock ? await _fallback.DecodeVinAsync(vin, ct) : null;
        }

        try
        {
            var url = $"{_baseUrl}/api/vehicle/v2/vins/{vin}" +
                     $"?fmt=json&api_key={_apiKey}";

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Edmunds: VIN decode returned {Status} for {Vin}", response.StatusCode, vin);
                return _fallbackToMock ? await _fallback.DecodeVinAsync(vin, ct) : null;
            }

            var vinData = await response.Content.ReadFromJsonAsync<EdmundsVinDecodeResponse>(JsonOptions, ct);
            if (vinData == null) return null;

            // Use decoded make/model/year to fetch full specs
            if (!string.IsNullOrEmpty(vinData.Make?.NiceName) &&
                !string.IsNullOrEmpty(vinData.Model?.NiceName) &&
                vinData.Years?.FirstOrDefault()?.Year > 0)
            {
                return await GetSpecsAsync(
                    vinData.Make.NiceName,
                    vinData.Model.NiceName,
                    vinData.Years.First().Year,
                    vinData.Years.First().Styles?.FirstOrDefault()?.Trim?.Name,
                    ct);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Edmunds: Error decoding VIN {Vin}", vin);
            return _fallbackToMock ? await _fallback.DecodeVinAsync(vin, ct) : null;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────

    private bool HasApiKey() => !string.IsNullOrWhiteSpace(_apiKey);

    private static string Encode(string value) =>
        Uri.EscapeDataString(value.ToLowerInvariant().Replace(" ", "-"));

    private static VehicleSpecification MapStyleToSpecification(
        string make, string model, int year, EdmundsStyle style)
    {
        var engine = style.Engine;
        var transmission = style.Transmission;

        return new VehicleSpecification(
            Make: make,
            Model: model,
            Year: year,
            Trim: style.Trim ?? "Base",
            BodyType: style.Submodel?.Body ?? "Sedan",
            Doors: style.NumOfDoors ?? 4,
            Seats: style.NumOfDoors == 2 ? 4 : 5,
            Engine: new EngineSpecs(
                Type: engine?.Configuration ?? "Inline-4",
                DisplacementLiters: (engine?.DisplacementCc ?? 2000) / 1000.0,
                Horsepower: engine?.Horsepower ?? 170,
                TorqueLbFt: engine?.Torque ?? 155,
                FuelType: engine?.FuelType ?? "Gasoline",
                Cylinders: engine?.Cylinder ?? 4,
                Turbocharged: engine?.CompressorType?.Contains("turbo", StringComparison.OrdinalIgnoreCase) ?? false,
                FuelSystem: engine?.FuelSystem ?? "Direct Injection"
            ),
            Transmission: new TransmissionSpecs(
                Type: transmission?.TransmissionType ?? "Automatic",
                Speeds: transmission?.NumberOfSpeeds is int s && s > 0 ? s : 6,
                DriveType: style.DrivenWheels ?? "FWD"
            ),
            FuelEconomy: new FuelEconomySpecs(
                CityMpg: style.Mpg?.City ?? 28,
                HighwayMpg: style.Mpg?.Highway ?? 36,
                CombinedMpg: (style.Mpg?.City ?? 28 + style.Mpg?.Highway ?? 36) / 2.0,
                FuelTankGallons: null,
                ElectricRangeMiles: null
            ),
            Dimensions: new DimensionSpecs(
                LengthInches: 185.0, WidthInches: 72.0, HeightInches: 57.0,
                WheelbaseInches: 106.0, CurbWeightLbs: 3100,
                CargoVolumesCuFt: null, GroundClearanceInches: null
            ),
            Performance: new PerformanceSpecs(
                ZeroToSixtySeconds: null, TopSpeedMph: null,
                BrakingSystem: "Disc/Disc",
                SuspensionFront: null, SuspensionRear: null
            ),
            Safety: new SafetySpecs(
                NhtsaOverallRating: null, IihsRating: null,
                StandardSafetyFeatures: new List<string> { "Airbags", "ABS", "Stability Control" }
            ),
            StandardFeatures: style.StandardEquipment ?? new List<string>(),
            OptionalFeatures: style.OptionalEquipment ?? new List<string>(),
            BaseMsrp: style.Price?.BaseMsrp ?? 25_000m,
            Currency: "USD",
            Provider: "Edmunds"
        );
    }

    // ── Edmunds API Response Models ─────────────────────────────

    private class EdmundsStylesResponse
    {
        [JsonPropertyName("styles")]
        public List<EdmundsStyle>? Styles { get; set; }

        [JsonPropertyName("stylesCount")]
        public int? StylesCount { get; set; }
    }

    private class EdmundsStyle
    {
        public long? Id { get; set; }
        public string? Name { get; set; }
        public string? Trim { get; set; }

        [JsonPropertyName("submodel")]
        public EdmundsSubmodel? Submodel { get; set; }

        [JsonPropertyName("numOfDoors")]
        public int? NumOfDoors { get; set; }

        [JsonPropertyName("drivenWheels")]
        public string? DrivenWheels { get; set; }

        [JsonPropertyName("price")]
        public EdmundsPrice? Price { get; set; }

        [JsonPropertyName("engine")]
        public EdmundsEngine? Engine { get; set; }

        [JsonPropertyName("transmission")]
        public EdmundsTransmission? Transmission { get; set; }

        [JsonPropertyName("MPG")]
        public EdmundsMpg? Mpg { get; set; }

        [JsonPropertyName("standardEquipment")]
        public List<string>? StandardEquipment { get; set; }

        [JsonPropertyName("optionalEquipment")]
        public List<string>? OptionalEquipment { get; set; }
    }

    private class EdmundsSubmodel
    {
        public string? Body { get; set; }
        public string? ModelName { get; set; }
    }

    private class EdmundsPrice
    {
        [JsonPropertyName("baseMSRP")]
        public decimal? BaseMsrp { get; set; }

        [JsonPropertyName("baseInvoice")]
        public decimal? BaseInvoice { get; set; }
    }

    private class EdmundsEngine
    {
        public int? Cylinder { get; set; }
        public string? Configuration { get; set; }

        [JsonPropertyName("size")]
        public double? DisplacementCc { get; set; }

        [JsonPropertyName("horsepower")]
        public int? Horsepower { get; set; }

        [JsonPropertyName("torque")]
        public int? Torque { get; set; }

        public string? FuelType { get; set; }
        public string? CompressorType { get; set; }
        public string? FuelSystem { get; set; }
    }

    private class EdmundsTransmission
    {
        public string? TransmissionType { get; set; }
        public int? NumberOfSpeeds { get; set; }
    }

    private class EdmundsMpg
    {
        public double? City { get; set; }
        public double? Highway { get; set; }
    }

    private class EdmundsVinDecodeResponse
    {
        public EdmundsMakeRef? Make { get; set; }
        public EdmundsModelRef? Model { get; set; }
        public List<EdmundsYearRef>? Years { get; set; }
    }

    private class EdmundsMakeRef
    {
        public string? Name { get; set; }
        public string? NiceName { get; set; }
    }

    private class EdmundsModelRef
    {
        public string? Name { get; set; }
        public string? NiceName { get; set; }
    }

    private class EdmundsYearRef
    {
        public int Year { get; set; }
        public List<EdmundsStyleRef>? Styles { get; set; }
    }

    private class EdmundsStyleRef
    {
        public long? Id { get; set; }
        public string? Name { get; set; }
        public EdmundsTrimRef? Trim { get; set; }
    }

    private class EdmundsTrimRef
    {
        public string? Name { get; set; }
    }
}
