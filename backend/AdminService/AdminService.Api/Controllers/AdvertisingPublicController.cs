// AdvertisingPublicController — public advertising API (campaigns, rotation, tracking)
// v2: CreateCampaignApiRequest includes VehicleTitle, VehicleSlug, VehicleImageUrl, VehiclePrice, etc.
using AdminService.Application.Interfaces;
using AdminService.Domain.Entities.Advertising;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Api.Controllers;

/// <summary>
/// Public advertising API controller.
/// Route prefix matches the Ocelot gateway upstream path (/api/advertising/*)
/// and the frontend service client (services/advertising.ts).
/// </summary>
[ApiController]
[Route("api/advertising")]
[Produces("application/json")]
public class AdvertisingPublicController : ControllerBase
{
    private readonly IAdvertisingService _advertisingService;
    private readonly ILogger<AdvertisingPublicController> _logger;

    public AdvertisingPublicController(
        IAdvertisingService advertisingService,
        ILogger<AdvertisingPublicController> logger)
    {
        _advertisingService = advertisingService;
        _logger = logger;
    }

    // ========================================
    // CAMPAIGNS
    // ========================================

    [HttpPost("campaigns")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignApiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OwnerId))
            return BadRequest(new { success = false, error = "OwnerId is required." });

        if (!Enum.TryParse<AdPlacementType>(request.PlacementType, out var placement))
            return BadRequest(new { success = false, error = "Invalid PlacementType. Use FeaturedSpot or PremiumSpot." });

        if (!Enum.TryParse<CampaignPricingModel>(request.PricingModel, out var pricing))
            return BadRequest(new { success = false, error = "Invalid PricingModel." });

        var dto = new CreateCampaignDto(
            Name: request.Name,
            OwnerId: request.OwnerId,
            OwnerType: request.OwnerType ?? "Individual",
            VehicleId: request.VehicleId,
            VehicleIds: request.VehicleIds,
            PlacementType: placement,
            PricingModel: pricing,
            TotalBudget: request.TotalBudget,
            DailyBudget: request.DailyBudget,
            BidAmount: request.BidAmount,
            StartDate: request.StartDate,
            EndDate: request.EndDate,
            VehicleTitle: request.VehicleTitle,
            VehicleSlug: request.VehicleSlug,
            VehicleImageUrl: request.VehicleImageUrl,
            VehiclePrice: request.VehiclePrice,
            VehicleCurrency: request.VehicleCurrency,
            VehicleLocation: request.VehicleLocation
        );

        var campaign = await _advertisingService.CreateCampaignAsync(dto);

        return StatusCode(StatusCodes.Status201Created, new
        {
            success = true,
            data = MapCampaign(campaign)
        });
    }

    [HttpGet("campaigns/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetCampaignById(Guid id)
    {
        var campaign = await _advertisingService.GetCampaignByIdAsync(id);
        if (campaign is null)
            return NotFound(new { success = false, error = "Campaign not found." });

        return Ok(new { success = true, data = MapCampaign(campaign) });
    }

    [HttpGet("campaigns/owner/{ownerId}")]
    [Authorize]
    public async Task<IActionResult> GetCampaignsByOwner(
        string ownerId,
        [FromQuery] string ownerType = "Individual",
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        CampaignStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CampaignStatus>(status, out var s))
            statusEnum = s;

        var (items, totalCount) = await _advertisingService.GetCampaignsByOwnerAsync(
            ownerId, ownerType, statusEnum, page, pageSize);

        return Ok(new
        {
            success = true,
            data = new
            {
                items = items.Select(MapCampaignSummary).ToList(),
                totalCount,
                page,
                pageSize
            }
        });
    }

    [HttpPost("campaigns/{id:guid}/pause")]
    [Authorize]
    public async Task<IActionResult> PauseCampaign(Guid id)
    {
        var result = await _advertisingService.PauseCampaignAsync(id);
        return result
            ? Ok(new { success = true })
            : NotFound(new { success = false, error = "Campaign not found or not active." });
    }

    [HttpPost("campaigns/{id:guid}/resume")]
    [Authorize]
    public async Task<IActionResult> ResumeCampaign(Guid id)
    {
        var result = await _advertisingService.ResumeCampaignAsync(id);
        return result
            ? Ok(new { success = true })
            : NotFound(new { success = false, error = "Campaign not found or not paused." });
    }

    [HttpPost("campaigns/{id:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelCampaign(Guid id)
    {
        var result = await _advertisingService.CancelCampaignAsync(id);
        return result
            ? Ok(new { success = true })
            : NotFound(new { success = false, error = "Campaign not found." });
    }

    // ========================================
    // TRACKING (anonymous — no auth required)
    // ========================================

    [HttpPost("tracking/impression")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordImpression([FromBody] TrackingApiRequest request)
    {
        await _advertisingService.RecordImpressionAsync(new TrackingEventDto(
            request.CampaignId ?? "", request.VehicleId ?? "",
            Enum.TryParse<AdPlacementType>(request.Section, out var s) ? s : AdPlacementType.FeaturedSpot,
            request.ViewerUserId, GetClientIp(), null));

        return Ok(new { success = true });
    }

    [HttpPost("tracking/click")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordClick([FromBody] ClickTrackingApiRequest request)
    {
        await _advertisingService.RecordClickAsync(new TrackingEventDto(
            request.CampaignId ?? "", request.VehicleId ?? "",
            Enum.TryParse<AdPlacementType>(request.Section, out var s) ? s : AdPlacementType.FeaturedSpot,
            request.ClickerUserId, GetClientIp(), request.DestinationUrl));

        return Ok(new { success = true });
    }

    // ========================================
    // ROTATION (anonymous GET, auth for config/refresh)
    // ========================================

    [HttpGet("rotation/{section}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHomepageRotation(string section)
    {
        if (!Enum.TryParse<AdPlacementType>(section, out var placement))
            return BadRequest(new { success = false, error = "Invalid section. Use FeaturedSpot or PremiumSpot." });

        var rotation = await _advertisingService.GetHomepageRotationAsync(placement);

        return Ok(new { success = true, data = rotation });
    }

    [HttpGet("rotation/config/{section}")]
    [Authorize]
    public async Task<IActionResult> GetRotationConfig(string section)
    {
        if (!Enum.TryParse<AdPlacementType>(section, out var placement))
            return BadRequest(new { success = false, error = "Invalid section." });

        var config = await _advertisingService.GetRotationConfigAsync(placement);

        return Ok(new { success = true, data = config is null ? null : MapRotationConfig(config) });
    }

    [HttpPut("rotation/config")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateRotationConfig([FromBody] UpdateRotationConfigApiRequest request)
    {
        if (!Enum.TryParse<AdPlacementType>(request.Section, out var placement))
            return BadRequest(new { success = false, error = "Invalid section." });

        RotationAlgorithmType? algo = null;
        if (request.Algorithm is not null && !Enum.TryParse(request.Algorithm, out RotationAlgorithmType a))
            return BadRequest(new { success = false, error = "Invalid algorithm." });
        else if (request.Algorithm is not null)
            algo = Enum.Parse<RotationAlgorithmType>(request.Algorithm);

        var config = await _advertisingService.UpdateRotationConfigAsync(new UpdateRotationConfigDto(
            placement, algo, request.MaxSlots, request.RotationIntervalMinutes,
            request.MinQualityScore, request.IsActive));

        return Ok(new { success = true, data = MapRotationConfig(config) });
    }

    [HttpPost("rotation/refresh")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> RefreshRotation([FromQuery] string? section = null)
    {
        AdPlacementType? placement = null;
        if (section is not null)
        {
            if (!Enum.TryParse<AdPlacementType>(section, out var p))
                return BadRequest(new { success = false, error = "Invalid section." });
            placement = p;
        }

        await _advertisingService.RefreshRotationAsync(placement);
        return Ok(new { success = true });
    }

    // ========================================
    // HOMEPAGE CONFIG
    // ========================================

    [HttpGet("homepage/categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories([FromQuery] bool includeHidden = false)
    {
        var categories = await _advertisingService.GetCategoriesAsync(includeHidden);
        return Ok(new { success = true, data = categories.Select(MapCategory).ToList() });
    }

    [HttpPut("homepage/categories")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateCategory([FromBody] UpdateCategoryApiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CategoryKey))
            return BadRequest(new { success = false, error = "CategoryKey is required." });

        var entity = await _advertisingService.UpdateCategoryAsync(new UpdateCategoryDto(
            request.CategoryKey, request.DisplayName, request.ImageUrl, request.Description,
            request.Href, request.AccentColor, request.DisplayOrder, request.IsActive, request.IsTrending));

        return Ok(new { success = true, data = MapCategory(entity) });
    }

    [HttpGet("homepage/brands")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrands([FromQuery] bool includeHidden = false)
    {
        var brands = await _advertisingService.GetBrandsAsync(includeHidden);
        return Ok(new { success = true, data = brands.Select(MapBrand).ToList() });
    }

    [HttpPut("homepage/brands")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateBrand([FromBody] UpdateBrandApiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BrandKey))
            return BadRequest(new { success = false, error = "BrandKey is required." });

        var entity = await _advertisingService.UpdateBrandAsync(new UpdateBrandDto(
            request.BrandKey, request.DisplayName, request.LogoUrl, request.VehicleCount,
            request.DisplayOrder, request.IsActive));

        return Ok(new { success = true, data = MapBrand(entity) });
    }

    // ========================================
    // REPORTS
    // ========================================

    [HttpGet("reports/campaign/{campaignId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetCampaignReport(Guid campaignId, [FromQuery] int daysBack = 30)
    {
        var report = await _advertisingService.GetCampaignReportAsync(campaignId, daysBack);
        return Ok(new { success = true, data = report });
    }

    [HttpGet("reports/platform")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetPlatformReport([FromQuery] int daysBack = 30)
    {
        var report = await _advertisingService.GetPlatformReportAsync(daysBack);
        return Ok(new { success = true, data = report });
    }

    [HttpGet("reports/owner/{ownerId}")]
    [Authorize]
    public async Task<IActionResult> GetOwnerReport(
        string ownerId,
        [FromQuery] string ownerType = "Individual",
        [FromQuery] int daysBack = 30)
    {
        var report = await _advertisingService.GetOwnerReportAsync(ownerId, ownerType, daysBack);
        return Ok(new { success = true, data = report });
    }

    // ========================================
    // PRICING (anonymous)
    // ========================================

    [HttpGet("reports/pricing/{placementType}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPricingEstimate(string placementType)
    {
        if (!Enum.TryParse<AdPlacementType>(placementType, out var placement))
            return BadRequest(new { success = false, error = "Invalid placementType." });

        var estimate = await _advertisingService.GetPricingEstimateAsync(placement);
        return Ok(new { success = true, data = estimate });
    }

    // ========================================
    // HELPERS
    // ========================================

    private string? GetClientIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static object MapCampaign(AdCampaignEntity c) => new
    {
        id = c.Id.ToString(),
        ownerId = c.OwnerId,
        ownerType = c.OwnerType,
        vehicleId = c.VehicleId,
        placementType = c.PlacementType.ToString(),
        pricingModel = c.PricingModel.ToString(),
        pricePerUnit = c.PricePerUnit,
        totalBudget = c.TotalBudget,
        remainingBudget = c.RemainingBudget,
        status = c.Status.ToString(),
        qualityScore = c.QualityScore,
        totalViews = c.TotalViews,
        totalClicks = c.TotalClicks,
        ctr = c.Ctr,
        startDate = c.StartDate.ToString("o"),
        endDate = c.EndDate.ToString("o"),
        createdAt = c.CreatedAt.ToString("o"),
        updatedAt = c.UpdatedAt.ToString("o"),
    };

    private static object MapCampaignSummary(AdCampaignEntity c) => new
    {
        id = c.Id.ToString(),
        vehicleId = c.VehicleId,
        placementType = c.PlacementType.ToString(),
        status = c.Status.ToString(),
        totalBudget = c.TotalBudget,
        remainingBudget = c.RemainingBudget,
        totalViews = c.TotalViews,
        totalClicks = c.TotalClicks,
        ctr = c.Ctr,
        startDate = c.StartDate.ToString("o"),
        endDate = c.EndDate.ToString("o"),
    };

    private static object MapRotationConfig(RotationConfigEntity c) => new
    {
        id = c.Id.ToString(),
        section = c.Section.ToString(),
        algorithm = c.Algorithm.ToString(),
        maxSlots = c.MaxSlots,
        rotationIntervalMinutes = c.RotationIntervalMinutes,
        minQualityScore = c.MinQualityScore,
        isActive = c.IsActive,
    };

    private static object MapCategory(CategoryImageConfigEntity c) => new
    {
        id = c.Id.ToString(),
        categoryKey = c.CategoryKey,
        displayName = c.DisplayName,
        imageUrl = c.ImageUrl,
        description = c.Description,
        href = c.Href,
        accentColor = c.AccentColor,
        displayOrder = c.DisplayOrder,
        isActive = c.IsActive,
        isTrending = c.IsTrending,
    };

    private static object MapBrand(BrandConfigEntity c) => new
    {
        id = c.Id.ToString(),
        brandKey = c.BrandKey,
        displayName = c.DisplayName,
        logoUrl = c.LogoUrl,
        vehicleCount = c.VehicleCount,
        displayOrder = c.DisplayOrder,
        isActive = c.IsActive,
    };
}

// ========================================
// API Request DTOs
// ========================================

public record CreateCampaignApiRequest
{
    public string? Name { get; init; }
    public string OwnerId { get; init; } = "";
    public string? OwnerType { get; init; }
    public string? VehicleId { get; init; }
    public string[]? VehicleIds { get; init; }
    public string PlacementType { get; init; } = "";
    public string PricingModel { get; init; } = "";
    public decimal TotalBudget { get; init; }
    public decimal? DailyBudget { get; init; }
    public decimal? BidAmount { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    // Vehicle metadata for rotation cache pre-population
    public string? VehicleTitle { get; init; }
    public string? VehicleSlug { get; init; }
    public string? VehicleImageUrl { get; init; }
    public decimal? VehiclePrice { get; init; }
    public string? VehicleCurrency { get; init; }
    public string? VehicleLocation { get; init; }
}

public record TrackingApiRequest
{
    public string? CampaignId { get; init; }
    public string? VehicleId { get; init; }
    public string? Section { get; init; }
    public string? ViewerUserId { get; init; }
    public string? ViewerIp { get; init; }
}

public record ClickTrackingApiRequest
{
    public string? CampaignId { get; init; }
    public string? VehicleId { get; init; }
    public string? Section { get; init; }
    public string? ClickerUserId { get; init; }
    public string? ClickerIp { get; init; }
    public string? DestinationUrl { get; init; }
}

public record UpdateRotationConfigApiRequest
{
    public string Section { get; init; } = "";
    public string? Algorithm { get; init; }
    public int? MaxSlots { get; init; }
    public int? RotationIntervalMinutes { get; init; }
    public double? MinQualityScore { get; init; }
    public bool? IsActive { get; init; }
}

public record UpdateCategoryApiRequest
{
    public string CategoryKey { get; init; } = "";
    public string? DisplayName { get; init; }
    public string? ImageUrl { get; init; }
    public string? Description { get; init; }
    public string? Href { get; init; }
    public string? AccentColor { get; init; }
    public int? DisplayOrder { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsTrending { get; init; }
}

public record UpdateBrandApiRequest
{
    public string BrandKey { get; init; } = "";
    public string? DisplayName { get; init; }
    public string? LogoUrl { get; init; }
    public int? VehicleCount { get; init; }
    public int? DisplayOrder { get; init; }
    public bool? IsActive { get; init; }
}
