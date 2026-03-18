using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Api.Controllers;

/// <summary>
/// Public-facing endpoint for platform configurations.
/// Serves the route /api/configurations/category/{category} that the frontend
/// SiteConfigProvider calls on every page load (unauthenticated).
/// 
/// The admin-only CRUD operations remain on ConfigurationsController
/// at /api/admin/configurations.
/// </summary>
[ApiController]
[Route("api/configurations")]
[Produces("application/json")]
public class PublicConfigurationsController : ControllerBase
{
    private static readonly Dictionary<string, List<ConfigKeyValue>> _configsByCategory = new()
    {
        ["general"] = new()
        {
            new("platform.name", "OKLA"),
            new("platform.url", "https://okla.com.do"),
            new("platform.currency", "DOP"),
            new("platform.country", "DO"),
            new("platform.locale", "es-DO"),
            new("platform.contact_email", "info@okla.com.do"),
            new("platform.whatsapp_number", "+18092001234"),
            new("platform.timezone", "America/Santo_Domingo"),
        },
        ["billing"] = new()
        {
            new("listing.price", "29"),
            new("listing.currency", "DOP"),
        },
        ["media"] = new()
        {
            new("media.max_upload_size_mb", "100"),
            new("media.max_images_per_vehicle", "12"),
        },
    };

    /// <summary>
    /// Get configurations by category (public, no auth required).
    /// Used by SiteConfigProvider on every page load.
    /// </summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetByCategory(string category, [FromQuery] string? environment = null)
    {
        if (_configsByCategory.TryGetValue(category.ToLowerInvariant(), out var configs))
            return Ok(configs);

        // Return empty array for unknown categories (frontend handles gracefully)
        return Ok(Array.Empty<ConfigKeyValue>());
    }
}

/// <summary>DTO matching frontend expectation: { key, value }</summary>
public record ConfigKeyValue(string Key, string Value);

// =============================================================================
// PUBLIC PRICING CONTROLLER
// =============================================================================

/// <summary>
/// Serves /api/public/pricing — the canonical platform pricing endpoint.
///
/// The Next.js BFF (/api/pricing) proxies to this endpoint via the Gateway.
/// All subscription plan prices shown to users come from here.
/// Admin can update pricing via PUT /api/admin/pricing (requires Admin role).
///
/// Architecture:
///   Frontend upgrade/suscripcion pages → usePlatformPricing hook
///     → Next.js /api/pricing route
///       → Gateway → AdminService /api/public/pricing (this controller)
/// </summary>
[ApiController]
[Produces("application/json")]
public class PublicPricingController : ControllerBase
{
    private readonly ILogger<PublicPricingController> _logger;

    // In-memory store. For full persistence, wire to a DB or Redis.
    // The admin PUT /api/admin/pricing updates this.
    private static PlatformPricingDto _pricing = DefaultPricing();

    public PublicPricingController(ILogger<PublicPricingController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// GET /api/public/pricing — unauthenticated, called by the Next.js BFF proxy.
    /// Returns the full platform pricing catalog (plans, boosts, commission rates).
    /// </summary>
    [HttpGet("api/public/pricing")]
    [ProducesResponseType(typeof(PlatformPricingDto), StatusCodes.Status200OK)]
    public IActionResult GetPublicPricing()
    {
        _logger.LogDebug("Public pricing request from {Host}", Request.Host);
        return Ok(_pricing);
    }

    /// <summary>
    /// GET /api/public/pricing/providers — returns payment provider config (unauthenticated).
    /// </summary>
    [HttpGet("api/public/pricing/providers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetPricingProviders()
    {
        return Ok(new
        {
            paypal = new { enabled = true, currency = "USD" },
            azul = new { enabled = true, currency = "DOP" },
            fygaro = new { enabled = false, currency = "DOP" },
        });
    }

    /// <summary>
    /// PUT /api/admin/pricing — admin-only, updates the live platform pricing.
    /// Called by the admin /admin/planes page "Guardar Todo" action.
    /// </summary>
    [HttpPut("api/admin/pricing")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PlatformPricingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult UpdatePricing([FromBody] PlatformPricingDto request)
    {
        if (request is null)
            return BadRequest(new { error = "Pricing body is required." });

        _pricing = request;
        _logger.LogInformation(
            "Platform pricing updated by admin. DealerVisible={V} DealerPro={P} SellerEstandar={SE} SellerVerificado={SV}",
            request.DealerVisible, request.DealerPro, request.SellerEstandar, request.SellerVerificado);

        return Ok(_pricing);
    }

    private static PlatformPricingDto DefaultPricing() => new()
    {
        BasicListing = 0,
        FeaturedListing = 1499,
        PremiumListing = 2999,
        SellerPremiumPrice = 1699,
        IndividualListingPrice = 1699,
        // Dealer plans (DOP/month) — LIBRE/VISIBLE/STARTER/PRO/ÉLITE/ENTERPRISE
        DealerLibre = 0,
        DealerVisible = 1699,
        DealerStarter = 3499,
        DealerPro = 5799,
        DealerElite = 20299,
        DealerEnterprise = 34999,
        // Seller plans — LIBRE/ESTÁNDAR($9.99/listing)/VERIFICADO($34.99/mes)
        SellerGratis = 0,
        SellerEstandar = 579,       // RD$9.99 ≈ DOP 579
        SellerVerificado = 1999,    // RD$34.99/mes ≈ DOP 1999
        // Legacy compat fields
        SellerPremium = 579,
        SellerProPlan = 1999,
        // Boosts
        BoostBasicPrice = 499,
        BoostBasicDays = 3,
        BoostProPrice = 999,
        BoostProDays = 7,
        BoostPremiumPrice = 1999,
        BoostPremiumDays = 14,
        // Durations
        BasicListingDays = 30,
        IndividualListingDays = 45,
        // Photo limits per plan
        FreeMaxPhotos = 5,
        VisibleMaxPhotos = 10,
        StarterMaxPhotos = 12,
        ProMaxPhotos = 15,
        EliteMaxPhotos = 20,
        EnterpriseMaxPhotos = 20,
        // Commission & taxes
        PlatformCommission = 2.5m,
        ItbisPercentage = 18m,
        Currency = "DOP",
        // Early Bird
        EarlyBirdDiscount = 25,
        EarlyBirdDeadline = "2026-12-31",
        EarlyBirdFreeMonths = 2,
        // Trial
        StripeTrialDays = 14,
    };
}

// =============================================================================
// DTO
// =============================================================================

/// <summary>
/// Platform pricing catalog. Uses PascalCase; ASP.NET Core serializes to camelCase
/// by default (matching the PlatformPricing TypeScript interface in the frontend).
/// </summary>
public class PlatformPricingDto
{
    public int BasicListing { get; set; }
    public int FeaturedListing { get; set; }
    public int PremiumListing { get; set; }
    public int SellerPremiumPrice { get; set; }
    public int IndividualListingPrice { get; set; }
    // Dealer plans (DOP/month)
    public int DealerLibre { get; set; }
    public int DealerVisible { get; set; }
    public int DealerStarter { get; set; }
    public int DealerPro { get; set; }
    public int DealerElite { get; set; }
    public int DealerEnterprise { get; set; }
    // Seller plans
    public int SellerGratis { get; set; }
    public int SellerEstandar { get; set; }     // Estándar: $9.99/listing
    public int SellerVerificado { get; set; }   // Verificado: $34.99/mes
    // Legacy compat fields
    public int SellerPremium { get; set; }
    public int SellerProPlan { get; set; }
    // Boosts
    public int BoostBasicPrice { get; set; }
    public int BoostBasicDays { get; set; }
    public int BoostProPrice { get; set; }
    public int BoostProDays { get; set; }
    public int BoostPremiumPrice { get; set; }
    public int BoostPremiumDays { get; set; }
    // Durations
    public int BasicListingDays { get; set; }
    public int IndividualListingDays { get; set; }
    // Photo limits
    public int FreeMaxPhotos { get; set; }
    public int VisibleMaxPhotos { get; set; }
    public int StarterMaxPhotos { get; set; }
    public int ProMaxPhotos { get; set; }
    public int EliteMaxPhotos { get; set; }
    public int EnterpriseMaxPhotos { get; set; }
    // Commission & taxes
    public decimal PlatformCommission { get; set; }
    public decimal ItbisPercentage { get; set; }
    public string Currency { get; set; } = "DOP";
    // Early Bird
    public int EarlyBirdDiscount { get; set; }
    public string EarlyBirdDeadline { get; set; } = "2026-12-31";
    public int EarlyBirdFreeMonths { get; set; }
    // Trial
    public int StripeTrialDays { get; set; }
}
