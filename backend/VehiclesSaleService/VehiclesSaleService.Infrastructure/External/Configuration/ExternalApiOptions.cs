namespace VehiclesSaleService.Infrastructure.External.Configuration;

/// <summary>
/// Configuration options for all external vehicle data APIs.
/// Maps to the "ExternalApis" section in appsettings.json / K8s secrets.
/// 
/// Example appsettings.json:
/// {
///   "ExternalApis": {
///     "VehicleHistory": { "Provider": "Mock", "ApiKey": "", "BaseUrl": "https://api.vinaudit.com/v1" },
///     "VehicleSpecs":   { "Provider": "Mock", "ApiKey": "", "BaseUrl": "https://api.edmunds.com" },
///     "MarketPrice":    { "Provider": "Mock", "ApiKey": "", "BaseUrl": "https://api.marketcheck.com/v2" },
///     "Nhtsa":          { "Enabled": true, "BaseUrl": "https://vpic.nhtsa.dot.gov/api" },
///     "Carfax":         { "Provider": "Mock", "ApiKey": "", "BaseUrl": "https://api.carfax.com" }
///   }
/// }
/// 
/// To switch to real APIs:
/// 1. Set Provider to "VinAudit" | "CARFAX" | "Edmunds" | "MarketCheck"
/// 2. Provide a valid ApiKey
/// 3. Optionally override BaseUrl
/// </summary>
public class ExternalApiOptions
{
    public const string SectionName = "ExternalApis";

    public VehicleHistoryOptions VehicleHistory { get; set; } = new();
    public VehicleSpecsOptions VehicleSpecs { get; set; } = new();
    public MarketPriceOptions MarketPrice { get; set; } = new();
    public NhtsaOptions Nhtsa { get; set; } = new();
    public CarfaxOptions Carfax { get; set; } = new();
}

public class VehicleHistoryOptions
{
    /// <summary>
    /// Provider to use: "Mock" | "VinAudit" | "CARFAX"
    /// </summary>
    public string Provider { get; set; } = "Mock";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.vinaudit.com/v1";

    /// <summary>
    /// If true, falls back to Mock when the real API is unavailable.
    /// Recommended for development/staging.
    /// </summary>
    public bool FallbackToMock { get; set; } = true;

    /// <summary>
    /// Timeout for HTTP requests to the provider API in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;
}

public class VehicleSpecsOptions
{
    /// <summary>
    /// Provider to use: "Mock" | "Edmunds"
    /// </summary>
    public string Provider { get; set; } = "Mock";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.edmunds.com";
    public bool FallbackToMock { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 15;
}

public class MarketPriceOptions
{
    /// <summary>
    /// Provider to use: "Mock" | "MarketCheck"
    /// </summary>
    public string Provider { get; set; } = "Mock";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.marketcheck.com/v2";
    public bool FallbackToMock { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 15;
}

public class NhtsaOptions
{
    /// <summary>
    /// NHTSA API is free and requires no API key.
    /// Set Enabled = false to disable.
    /// </summary>
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://vpic.nhtsa.dot.gov/api";
    public int TimeoutSeconds { get; set; } = 15;
}

public class CarfaxOptions
{
    public string Provider { get; set; } = "Mock";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.carfax.com";
    public string PartnerId { get; set; } = string.Empty;
    public bool FallbackToMock { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 15;
}
