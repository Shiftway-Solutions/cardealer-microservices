using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehiclesSaleService.Application.Interfaces;
using VehiclesSaleService.Infrastructure.External;

namespace VehiclesSaleService.Api.Controllers;

/// <summary>
/// External vehicle data APIs (CARFAX/VinAudit, Edmunds, MarketCheck, NHTSA).
/// Config-driven provider switching — set ExternalApis:{Service}:Provider in appsettings.
/// All providers fall back to Mock when API keys are not configured.
/// </summary>
[ApiController]
[Route("api/vehicle-data")]
public class VehicleDataController : ControllerBase
{
    private readonly IVehicleHistoryService _historyService;
    private readonly IVehicleSpecsService _specsService;
    private readonly IMarketPriceService _marketPriceService;
    private readonly INhtsaVehicleDataService? _nhtsaService;
    private readonly ILogger<VehicleDataController> _logger;
    private readonly IConfiguration _configuration;

    public VehicleDataController(
        IVehicleHistoryService historyService,
        IVehicleSpecsService specsService,
        IMarketPriceService marketPriceService,
        ILogger<VehicleDataController> logger,
        IConfiguration configuration,
        INhtsaVehicleDataService? nhtsaService = null)
    {
        _historyService = historyService;
        _specsService = specsService;
        _marketPriceService = marketPriceService;
        _nhtsaService = nhtsaService;
        _logger = logger;
        _configuration = configuration;
    }

    // ══════════════════════════════════════════════════════════════
    //  VEHICLE HISTORY (CARFAX / VinAudit)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get full vehicle history report by VIN (CARFAX/VinAudit).
    /// </summary>
    [HttpGet("history/{vin}")]
    [Authorize]
    public async Task<IActionResult> GetVehicleHistory(string vin, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
            return BadRequest(new { error = "VIN must be exactly 17 characters" });

        var report = await _historyService.GetHistoryByVinAsync(vin.ToUpper(), ct);
        if (report == null)
            return NotFound(new { error = "No history report available for this VIN" });

        return Ok(new { success = true, data = report });
    }

    /// <summary>
    /// Get quick vehicle history summary by VIN (cheaper/faster).
    /// </summary>
    [HttpGet("history/{vin}/summary")]
    [Authorize]
    public async Task<IActionResult> GetVehicleHistorySummary(string vin, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
            return BadRequest(new { error = "VIN must be exactly 17 characters" });

        var summary = await _historyService.GetSummaryByVinAsync(vin.ToUpper(), ct);
        if (summary == null)
            return NotFound(new { error = "No history available for this VIN" });

        return Ok(new { success = true, data = summary });
    }

    /// <summary>
    /// Check if a history report is available for a VIN.
    /// </summary>
    [HttpGet("history/{vin}/available")]
    [AllowAnonymous]
    public async Task<IActionResult> IsHistoryAvailable(string vin, CancellationToken ct)
    {
        var available = await _historyService.IsReportAvailableAsync(vin?.ToUpper() ?? "", ct);
        return Ok(new { success = true, data = new { vin, available } });
    }

    // ══════════════════════════════════════════════════════════════
    //  VEHICLE SPECS (Edmunds)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get vehicle technical specifications by make/model/year.
    /// </summary>
    [HttpGet("specs/{make}/{model}/{year}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVehicleSpecs(
        string make, string model, int year,
        [FromQuery] string? trim = null, CancellationToken ct = default)
    {
        if (year < 1990 || year > DateTime.UtcNow.Year + 2)
            return BadRequest(new { error = "Year must be between 1990 and current year + 1" });

        var specs = await _specsService.GetSpecsAsync(make, model, year, trim, ct);
        if (specs == null)
            return NotFound(new { error = $"No specs found for {make} {model} {year}" });

        return Ok(new { success = true, data = specs });
    }

    /// <summary>
    /// Get available trims for a make/model/year.
    /// </summary>
    [HttpGet("specs/{make}/{model}/{year}/trims")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTrims(
        string make, string model, int year, CancellationToken ct)
    {
        var trims = await _specsService.GetTrimsAsync(make, model, year, ct);
        return Ok(new { success = true, data = trims });
    }

    /// <summary>
    /// Get available styles/configurations for a make/model/year.
    /// </summary>
    [HttpGet("specs/{make}/{model}/{year}/styles")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStyles(
        string make, string model, int year, CancellationToken ct)
    {
        var styles = await _specsService.GetStylesAsync(make, model, year, ct);
        return Ok(new { success = true, data = styles });
    }

    /// <summary>
    /// Decode a VIN and return specs from provider.
    /// </summary>
    [HttpGet("specs/decode/{vin}")]
    [AllowAnonymous]
    public async Task<IActionResult> DecodeVinSpecs(string vin, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
            return BadRequest(new { error = "VIN must be exactly 17 characters" });

        var specs = await _specsService.DecodeVinAsync(vin.ToUpper(), ct);
        if (specs == null)
            return NotFound(new { error = "Could not decode VIN" });

        return Ok(new { success = true, data = specs });
    }

    // ══════════════════════════════════════════════════════════════
    //  MARKET PRICES (MarketCheck)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get market price analysis for a vehicle.
    /// </summary>
    [HttpGet("market-price/{make}/{model}/{year}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMarketPrice(
        string make, string model, int year,
        [FromQuery] string? trim = null,
        [FromQuery] decimal? mileage = null,
        [FromQuery] string? condition = null,
        [FromQuery] string? province = null,
        CancellationToken ct = default)
    {
        var analysis = await _marketPriceService.GetMarketPriceAsync(
            make, model, year, trim, mileage, condition, province, ct);

        if (analysis == null)
            return NotFound(new { error = "No market data available" });

        return Ok(new { success = true, data = analysis });
    }

    /// <summary>
    /// Get comparable market listings for a vehicle.
    /// </summary>
    [HttpGet("market-price/{make}/{model}/{year}/comparables")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComparableListings(
        string make, string model, int year,
        [FromQuery] string? trim = null,
        [FromQuery] decimal? maxMileage = null,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var listings = await _marketPriceService.GetComparableListingsAsync(
            make, model, year, trim, maxMileage, limit, ct);

        return Ok(new { success = true, data = listings, count = listings.Count });
    }

    /// <summary>
    /// Get market trend (price history) for a make/model.
    /// </summary>
    [HttpGet("market-price/{make}/{model}/trend")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMarketTrend(
        string make, string model,
        [FromQuery] int? year = null,
        [FromQuery] int monthsBack = 12,
        CancellationToken ct = default)
    {
        var trend = await _marketPriceService.GetMarketTrendAsync(make, model, year, monthsBack, ct);
        if (trend == null)
            return NotFound(new { error = "No trend data available" });

        return Ok(new { success = true, data = trend });
    }

    /// <summary>
    /// Get a price recommendation for listing a vehicle.
    /// </summary>
    [HttpPost("market-price/recommendation")]
    [Authorize]
    public async Task<IActionResult> GetPriceRecommendation(
        [FromBody] PriceRecommendationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Make) || string.IsNullOrWhiteSpace(request.Model))
            return BadRequest(new { error = "Make and Model are required" });

        var recommendation = await _marketPriceService.GetPriceRecommendationAsync(
            request.Make, request.Model, request.Year,
            request.MileageKm, request.Condition ?? "Bueno",
            request.Province, ct);

        if (recommendation == null)
            return NotFound(new { error = "Could not generate recommendation" });

        return Ok(new { success = true, data = recommendation });
    }

    // ══════════════════════════════════════════════════════════════
    //  NHTSA (Free API — VIN Decode, Recalls, Complaints)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Decode a VIN using NHTSA's free database.
    /// Returns make, model, year, body class, engine, transmission, etc.
    /// </summary>
    [HttpGet("nhtsa/decode/{vin}")]
    [AllowAnonymous]
    public async Task<IActionResult> NhtsaDecodeVin(string vin, CancellationToken ct)
    {
        if (_nhtsaService == null)
            return StatusCode(503, new { error = "NHTSA service is not enabled" });

        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
            return BadRequest(new { error = "VIN must be exactly 17 characters" });

        var result = await _nhtsaService.DecodeVinAsync(vin.ToUpper(), ct);
        if (result == null)
            return NotFound(new { error = "Could not decode VIN" });

        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Get all recalls for a specific vehicle from NHTSA (free).
    /// </summary>
    [HttpGet("nhtsa/recalls/{make}/{model}/{year}")]
    [AllowAnonymous]
    public async Task<IActionResult> NhtsaGetRecalls(
        string make, string model, int year, CancellationToken ct)
    {
        if (_nhtsaService == null)
            return StatusCode(503, new { error = "NHTSA service is not enabled" });

        var recalls = await _nhtsaService.GetRecallsAsync(make, model, year, ct);
        return Ok(new { success = true, data = recalls, count = recalls.Count });
    }

    /// <summary>
    /// Get safety complaints for a specific vehicle from NHTSA (free).
    /// </summary>
    [HttpGet("nhtsa/complaints/{make}/{model}/{year}")]
    [AllowAnonymous]
    public async Task<IActionResult> NhtsaGetComplaints(
        string make, string model, int year, CancellationToken ct)
    {
        if (_nhtsaService == null)
            return StatusCode(503, new { error = "NHTSA service is not enabled" });

        var complaints = await _nhtsaService.GetComplaintsAsync(make, model, year, ct);
        return Ok(new { success = true, data = complaints, count = complaints.Count });
    }

    /// <summary>
    /// Get all vehicle makes from NHTSA database (free).
    /// </summary>
    [HttpGet("nhtsa/makes")]
    [AllowAnonymous]
    public async Task<IActionResult> NhtsaGetMakes(CancellationToken ct)
    {
        if (_nhtsaService == null)
            return StatusCode(503, new { error = "NHTSA service is not enabled" });

        var makes = await _nhtsaService.GetAllMakesAsync(ct);
        return Ok(new { success = true, data = makes, count = makes.Count });
    }

    /// <summary>
    /// Get all models for a specific make from NHTSA database (free).
    /// </summary>
    [HttpGet("nhtsa/models/{make}")]
    [AllowAnonymous]
    public async Task<IActionResult> NhtsaGetModels(string make, CancellationToken ct)
    {
        if (_nhtsaService == null)
            return StatusCode(503, new { error = "NHTSA service is not enabled" });

        var models = await _nhtsaService.GetModelsForMakeAsync(make, ct);
        return Ok(new { success = true, data = models, count = models.Count });
    }

    // ══════════════════════════════════════════════════════════════
    //  PROVIDER STATUS (Health Check for integrations)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get the status of all vehicle data providers.
    /// Dynamically reflects the configured provider from appsettings.
    /// Useful for frontend to show which features are available.
    /// </summary>
    [HttpGet("providers/status")]
    [AllowAnonymous]
    public IActionResult GetProviderStatus()
    {
        var historyProvider = _configuration["ExternalApis:VehicleHistory:Provider"] ?? "Mock";
        var specsProvider = _configuration["ExternalApis:VehicleSpecs:Provider"] ?? "Mock";
        var priceProvider = _configuration["ExternalApis:MarketPrice:Provider"] ?? "Mock";
        var nhtsaEnabled = bool.TryParse(
            _configuration["ExternalApis:Nhtsa:Enabled"], out var ne) ? ne : true;

        return Ok(new
        {
            success = true,
            data = new
            {
                vehicleHistory = new
                {
                    provider = historyProvider,
                    status = "Active",
                    isMock = historyProvider.Equals("Mock", StringComparison.OrdinalIgnoreCase),
                    supportedProviders = new[] { "Mock", "VinAudit", "CARFAX" },
                    note = historyProvider == "Mock"
                        ? "Using simulated data. Set ExternalApis:VehicleHistory:Provider to switch."
                        : $"Using {historyProvider} API."
                },
                vehicleSpecs = new
                {
                    provider = specsProvider,
                    status = "Active",
                    isMock = specsProvider.Equals("Mock", StringComparison.OrdinalIgnoreCase),
                    supportedProviders = new[] { "Mock", "Edmunds" },
                    note = specsProvider == "Mock"
                        ? "Using simulated data. Set ExternalApis:VehicleSpecs:Provider to switch."
                        : $"Using {specsProvider} API."
                },
                marketPrice = new
                {
                    provider = priceProvider,
                    status = "Active",
                    isMock = priceProvider.Equals("Mock", StringComparison.OrdinalIgnoreCase),
                    supportedProviders = new[] { "Mock", "MarketCheck" },
                    note = priceProvider == "Mock"
                        ? "Using simulated DR-market data. Set ExternalApis:MarketPrice:Provider to switch."
                        : $"Using {priceProvider} API."
                },
                nhtsa = new
                {
                    provider = "NHTSA",
                    status = nhtsaEnabled ? "Active" : "Disabled",
                    isMock = false,
                    note = nhtsaEnabled
                        ? "Free NHTSA API — VIN decode, recalls, complaints. No API key needed."
                        : "NHTSA service is disabled. Set ExternalApis:Nhtsa:Enabled=true to enable."
                },
            }
        });
    }
}

// ── Request Models ──────────────────────────────────────────────

public record PriceRecommendationRequest(
    string Make,
    string Model,
    int Year,
    decimal MileageKm,
    string? Condition = "Bueno",
    string? Province = null
);
