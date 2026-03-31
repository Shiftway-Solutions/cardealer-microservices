using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VehiclesSaleService.Domain.Entities;
using VehiclesSaleService.Domain.Interfaces;
using VehiclesSaleService.Infrastructure.Messaging;
using VehiclesSaleService.Infrastructure.Persistence;
using CarDealer.Contracts.Events.Vehicle;
using CarDealer.Shared.Configuration;
using CarDealer.Shared.Caching.Interfaces;
using System.Security.Claims;
using System.Text.Json;
using Entities = VehiclesSaleService.Domain.Entities;
using Microsoft.AspNetCore.RateLimiting;
using VehiclesSaleService.Infrastructure.External;

namespace VehiclesSaleService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("VehiclesPolicy")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<VehiclesController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IConfigurationServiceClient _configClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly VehiclesSaleService.Application.Interfaces.IDealerVerificationClient _dealerVerificationClient;
    private readonly ICacheService _cache;
    private readonly INhtsaVehicleDataService? _nhtsaService;
    private readonly VehiclesSaleService.Application.Interfaces.IVehicleHistoryService? _vehicleHistoryService;
    private readonly VehiclesSaleService.Infrastructure.Services.SitemapRevalidationService? _sitemapRevalidation;

    public VehiclesController(
        IVehicleRepository vehicleRepository,
        ICategoryRepository categoryRepository,
        IEventPublisher eventPublisher,
        ILogger<VehiclesController> logger,
        ApplicationDbContext context,
        IConfigurationServiceClient configClient,
        IHttpClientFactory httpClientFactory,
        VehiclesSaleService.Application.Interfaces.IDealerVerificationClient dealerVerificationClient,
        ICacheService cache,
        INhtsaVehicleDataService? nhtsaService = null,
        VehiclesSaleService.Application.Interfaces.IVehicleHistoryService? vehicleHistoryService = null,
        VehiclesSaleService.Infrastructure.Services.SitemapRevalidationService? sitemapRevalidation = null)
    {
        _vehicleRepository = vehicleRepository;
        _categoryRepository = categoryRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _context = context;
        _configClient = configClient;
        _httpClientFactory = httpClientFactory;
        _dealerVerificationClient = dealerVerificationClient;
        _cache = cache;
        _nhtsaService = nhtsaService;
        _vehicleHistoryService = vehicleHistoryService;
        _sitemapRevalidation = sitemapRevalidation;
    }

    /// <summary>
    /// Get effective vehicle listing settings from ConfigurationService (admin panel).
    /// Used by frontend to enforce limits (max images, max price, KYC requirement, etc.)
    /// </summary>
    [HttpGet("settings")]
    [AllowAnonymous]
    public async Task<ActionResult> GetSettings()
    {
        var settings = new
        {
            MaxImagesPerListing = await _configClient.GetIntAsync("vehicles.max_images_per_listing", 30),
            ListingExpirationDays = await _configClient.GetIntAsync("vehicles.listing_expiration_days", 90),
            FeaturedDurationDays = await _configClient.GetIntAsync("vehicles.featured_duration_days", 30),
            MaxPriceDop = await _configClient.GetDecimalAsync("vehicles.max_price_dop", 100_000_000m),
            PaginationDefault = await _configClient.GetIntAsync("vehicles.pagination_default", 20),
            RequireKycToSell = await _configClient.IsEnabledAsync("vehicles.require_kyc_to_sell", defaultValue: true),
        };

        return Ok(settings);
    }

    /// <summary>
    /// Search vehicles with filters and pagination
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<VehicleSearchResult>> Search([FromQuery] VehicleSearchRequest request)
    {
        var parameters = new VehicleSearchParameters
        {
            SearchTerm = request.Search,
            CategoryId = request.CategoryId,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            Make = request.Make,
            Model = request.Model,
            MinYear = request.MinYear,
            MaxYear = request.MaxYear,
            MaxMileage = request.MaxMileage,
            VehicleType = request.VehicleType,
            BodyStyle = request.BodyStyle,
            FuelType = request.FuelType,
            Transmission = request.Transmission,
            DriveType = request.DriveType,
            Condition = request.Condition,
            ExteriorColor = request.ExteriorColor,
            State = request.State,
            City = request.City,
            ZipCode = request.ZipCode,
            IsCertified = request.IsCertified,
            HasCleanTitle = request.HasCleanTitle,
            SellerType = request.SellerType,
            MinSeats = request.MinSeats,
            Cylinders = request.Cylinders,
            InteriorColor = request.InteriorColor,
            Features = request.Features?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            Skip = Math.Max(0, (request.Page - 1)) * request.PageSize,  // Handle page=0 as page=1
            Take = request.PageSize,
            SortBy = request.SortBy ?? "CreatedAt",
            SortDescending = request.SortDescending ?? true
        };

        // Normalize page to at least 1 for response
        var normalizedPage = Math.Max(1, request.Page);

        try
        {
            var vehicles = await _vehicleRepository.SearchAsync(parameters);
            var totalCount = await _vehicleRepository.GetCountAsync(parameters);

            return Ok(new VehicleSearchResult
            {
                Vehicles = vehicles,
                TotalCount = totalCount,
                Page = normalizedPage,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            });
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42601")
        {
            // Full-text search syntax error — retry without SearchTerm
            _logger.LogWarning(ex, "Full-text search failed for term '{SearchTerm}', retrying without text search", request.Search);
            parameters.SearchTerm = null;

            var vehicles = await _vehicleRepository.SearchAsync(parameters);
            var totalCount = await _vehicleRepository.GetCountAsync(parameters);

            return Ok(new VehicleSearchResult
            {
                Vehicles = vehicles,
                TotalCount = totalCount,
                Page = normalizedPage,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            });
        }
    }

    /// <summary>
    /// Get vehicle by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<Vehicle>> GetById(Guid id)
    {
        var cacheKey = $"vehicle:detail:{id}";
        var cached = await _cache.GetAsync<Vehicle>(cacheKey);
        if (cached is not null)
            return Ok(cached);

        var vehicle = await _vehicleRepository.GetByIdAsync(id);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        await _cache.SetAsync(cacheKey, vehicle, ttlSeconds: 300); // 5 min
        return Ok(vehicle);
    }

    /// <summary>
    /// Internal endpoint: Mark vehicle as having broken images.
    /// Called by NotificationService when ImageUrlHealthScanJob detects >50% broken images.
    /// Sets HasBrokenImages=true so the buyer-facing "Fotos en actualización" banner shows.
    /// </summary>
    [HttpPatch("{id:guid}/broken-images")]
    [AllowAnonymous] // Internal service-to-service call (no user token available)
    public async Task<ActionResult> MarkBrokenImages(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        vehicle.HasBrokenImages = true;
        vehicle.BrokenImagesDetectedAt = DateTime.UtcNow;

        await _vehicleRepository.UpdateAsync(vehicle);

        // Invalidate cache so buyers see the banner immediately
        await _cache.RemoveAsync($"vehicle:detail:{id}");

        _logger.LogInformation(
            "🖼️ Vehicle {VehicleId} marked HasBrokenImages=true by health scan",
            id);

        return Ok(new { message = "Vehicle marked as having broken images", vehicleId = id });
    }

    /// <summary>
    /// Get vehicle by slug (format: {year}-{make}-{model}-{shortId})
    /// </summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<Vehicle>> GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest(new { message = "Slug is required" });

        // Extract the short ID from the slug (last 8 characters after the last dash)
        var lastDash = slug.LastIndexOf('-');
        if (lastDash < 0 || lastDash >= slug.Length - 1)
            return NotFound(new { message = "Vehicle not found" });

        var shortId = slug[(lastDash + 1)..];
        if (shortId.Length != 8)
            return NotFound(new { message = "Vehicle not found" });

        // SECURITY: Validate shortId is strictly hex to prevent LIKE wildcard injection (%, _)
        if (!System.Text.RegularExpressions.Regex.IsMatch(shortId, "^[0-9a-fA-F]{8}$"))
            return NotFound(new { message = "Vehicle not found" });

        // Search for vehicle whose ID starts with the short ID using SQL-translatable pattern
        var idPattern = $"{shortId}%";
        var vehicles = await _context.Vehicles
            .Include(v => v.Images)
            .Where(v => !v.IsDeleted && EF.Functions.Like(
                v.Id.ToString().Replace("-", ""), idPattern))
            .Take(10) // Limit results to prevent excessive loading
            .ToListAsync();

        // Verify the full slug matches to avoid collisions
        var vehicle = vehicles.FirstOrDefault(v => GenerateSlug(v) == slug);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        return Ok(vehicle);
    }

    /// <summary>
    /// Get vehicle by VIN
    /// </summary>
    [HttpGet("vin/{vin}")]
    [AllowAnonymous]
    public async Task<ActionResult<Vehicle>> GetByVIN(string vin)
    {
        var vehicle = await _vehicleRepository.GetByVINAsync(vin);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        return Ok(vehicle);
    }

    /// <summary>
    /// Check if a VIN already exists in the system.
    /// Fast endpoint for real-time validation while user types.
    /// </summary>
    [HttpGet("vin/{vin}/exists")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VinExistsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VinExistsResponse>> CheckVinExists(string vin)
    {
        if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
            return Ok(new VinExistsResponse { Exists = false });

        vin = vin.ToUpperInvariant().Trim();

        var vehicle = await _vehicleRepository.GetByVINAsync(vin);
        if (vehicle == null)
            return Ok(new VinExistsResponse { Exists = false });

        return Ok(new VinExistsResponse
        {
            Exists = true,
            VehicleId = vehicle.Id,
            Slug = GenerateSlug(vehicle),
            Status = vehicle.Status.ToString()
        });
    }

    /// <summary>
    /// Get featured vehicles
    /// </summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Vehicle>>> GetFeatured([FromQuery] int take = 10)
    {
        var cacheKey = $"vehicle:featured:{take}";
        var cached = await _cache.GetAsync<List<Vehicle>>(cacheKey);
        if (cached is not null)
            return Ok(cached);

        var vehicles = await _vehicleRepository.GetFeaturedAsync(take);
        var vehicleList = vehicles.ToList();
        await _cache.SetAsync(cacheKey, vehicleList, ttlSeconds: 600); // 10 min
        return Ok(vehicleList);
    }

    /// <summary>
    /// Get vehicles by dealer
    /// </summary>
    [HttpGet("dealer/{dealerId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetByDealer(
        Guid dealerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var (vehicles, totalCount) = await _vehicleRepository.GetByDealerAsync(dealerId, page, pageSize);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return Ok(new
        {
            Data = vehicles,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }

    /// <summary>
    /// Get vehicles by seller (user).
    /// AllowAnonymous: called pod-to-pod by UserService (no Bearer token).
    /// </summary>
    [HttpGet("seller/{sellerId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<SellerVehiclesResponse>> GetBySeller(
        Guid sellerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? status = null)
    {
        VehicleStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            if (Enum.TryParse<VehicleStatus>(status, true, out var statusEnum))
                statusFilter = statusEnum;
        }

        var (vehicles, totalCount) = await _vehicleRepository.GetBySellerAsync(sellerId, page, pageSize, statusFilter);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var pagedVehicles = vehicles.Select(v => new SellerVehicleDto
        {
            Id = v.Id,
            Title = v.Title,
            Slug = GenerateSlug(v),
            Price = v.Price,
            Currency = v.Currency,
            Status = v.Status.ToString(),
            MainImageUrl = v.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url,
            Year = v.Year,
            Make = v.Make,
            Model = v.Model,
            Mileage = v.Mileage,
            Transmission = v.Transmission.ToString(),
            FuelType = v.FuelType.ToSpanish(),
            Views = v.ViewCount,
            Favorites = v.FavoriteCount,
            CreatedAt = v.CreatedAt
        })
            .ToList();

        return Ok(new SellerVehiclesResponse
        {
            Data = pagedVehicles,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }

    /// <summary>
    /// Get seller vehicle statistics
    /// </summary>
    [HttpGet("seller/{sellerId:guid}/stats")]
    [AllowAnonymous]
    public async Task<ActionResult<SellerVehicleStats>> GetSellerStats(Guid sellerId)
    {
        var stats = await _vehicleRepository.GetSellerStatsAsync(sellerId);

        return Ok(new SellerVehicleStats
        {
            TotalListings = stats.TotalListings,
            ActiveListings = stats.ActiveListings,
            SoldListings = stats.SoldListings,
            PendingListings = stats.PendingListings,
            TotalViews = (int)stats.TotalViews,
            TotalFavorites = (int)stats.TotalFavorites
        });
    }

    /// <summary>
    /// Compare multiple vehicles by their IDs.
    /// Results are cached in Redis for 60 s keyed by sorted ID set.
    /// </summary>
    [HttpPost("compare")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Vehicle>>> Compare([FromBody] CompareVehiclesRequest request)
    {
        if (request.VehicleIds == null || !request.VehicleIds.Any())
            return BadRequest(new { message = "At least one vehicle ID is required" });

        const int defaultMaxCompare = 5;
        if (request.VehicleIds.Count > defaultMaxCompare)
            return BadRequest(new { message = $"Cannot compare more than {defaultMaxCompare} vehicles at once" });

        // Build a deterministic cache key from sorted IDs
        var sortedIds = request.VehicleIds.OrderBy(id => id).ToList();
        var cacheKey = $"vehiclessale:compare:{string.Join(",", sortedIds)}";

        var vehicles = await _cache.GetOrSetAsync<List<Vehicle>>(
            cacheKey,
            async () =>
            {
                return (await _vehicleRepository.GetByIdsAsync(sortedIds))
                    .Where(v => !v.IsDeleted && v.Status != VehicleStatus.Archived)
                    .ToList();
            },
            ttlSeconds: 60);

        if (vehicles == null || !vehicles.Any())
            return NotFound(new { message = "No vehicles found with the provided IDs" });

        return Ok(vehicles);
    }

    /// <summary>
    /// Get similar vehicles: same body type/segment, ±20% price, ±2 years, verified availability.
    /// Falls back gracefully: segment+price+year → segment+price → segment → same make.
    /// </summary>
    [HttpGet("{id:guid}/similar")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetSimilar(Guid id, [FromQuery] int limit = 6)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        // Clamp limit to 4–6 as per spec
        limit = Math.Clamp(limit, 4, 6);

        // Price range: ±20%
        var priceMargin = vehicle.Price * 0.20m;
        var minPrice = vehicle.Price - priceMargin;
        var maxPrice = vehicle.Price + priceMargin;

        // Year range: ±2
        var minYear = vehicle.Year - 2;
        var maxYear = vehicle.Year + 2;

        // ── Strategy 1: Same body type + price range + year range ──────────
        var parameters = new VehicleSearchParameters
        {
            BodyStyle = vehicle.BodyStyle,
            MinPrice = minPrice > 0 ? minPrice : 0,
            MaxPrice = maxPrice,
            MinYear = minYear,
            MaxYear = maxYear,
            Skip = 0,
            Take = limit + 5, // fetch extra to filter out current + ensure enough
            SortBy = "CreatedAt",
            SortDescending = true
        };

        var candidates = await _vehicleRepository.SearchAsync(parameters);
        var results = candidates
            .Where(v => v.Id != id && v.Status == VehicleStatus.Active)
            .Take(limit)
            .ToList();

        // ── Strategy 2: Relax year range if not enough results ──────────
        if (results.Count < limit)
        {
            parameters.MinYear = null;
            parameters.MaxYear = null;
            parameters.Take = limit + 5;
            candidates = await _vehicleRepository.SearchAsync(parameters);
            results = candidates
                .Where(v => v.Id != id && v.Status == VehicleStatus.Active)
                .Take(limit)
                .ToList();
        }

        // ── Strategy 3: Relax price range if still not enough ──────────
        if (results.Count < limit)
        {
            parameters.MinPrice = null;
            parameters.MaxPrice = null;
            parameters.Make = vehicle.Make; // same make at least
            parameters.Take = limit + 5;
            candidates = await _vehicleRepository.SearchAsync(parameters);
            results = candidates
                .Where(v => v.Id != id && v.Status == VehicleStatus.Active)
                .Take(limit)
                .ToList();
        }

        // ── Strategy 4: Same condition, any make/body, most recent ──────────
        if (results.Count < limit)
        {
            var fallbackParams = new VehicleSearchParameters
            {
                Condition = vehicle.Condition,
                MinPrice = minPrice > 0 ? minPrice : 0,
                MaxPrice = maxPrice,
                Skip = 0,
                Take = limit + 5,
                SortBy = "CreatedAt",
                SortDescending = true
            };
            candidates = await _vehicleRepository.SearchAsync(fallbackParams);
            var existingIds = results.Select(r => r.Id).ToHashSet();
            var extra = candidates
                .Where(v => v.Id != id && v.Status == VehicleStatus.Active && !existingIds.Contains(v.Id))
                .Take(limit - results.Count)
                .ToList();
            results.AddRange(extra);
        }

        var response = results.Select(v => new
        {
            v.Id,
            Slug = GenerateSlug(v),
            v.Title,
            v.Make,
            v.Model,
            v.Year,
            v.Trim,
            v.Price,
            v.Currency,
            v.Mileage,
            v.MileageUnit,
            v.Transmission,
            v.FuelType,
            v.City,
            v.State,
            v.Status,
            v.Condition,
            v.IsFeatured,
            v.ViewCount,
            v.FavoriteCount,
            v.CreatedAt,
            v.SellerId,
            v.SellerName,
            SellerType = v.SellerType.ToString(),
            v.SellerPhone,
            v.SellerEmail,
            v.SellerWhatsApp,
            v.SellerVerified,
            v.SellerRating,
            v.SellerReviewCount,
            v.SellerCity,
            v.SellerState,
            v.SellerLogoUrl,
            Images = v.Images.OrderBy(i => i.SortOrder).Select(i => new
            {
                i.Id,
                i.Url,
                i.ThumbnailUrl,
                i.SortOrder,
                i.IsPrimary
            })
        });

        return Ok(response);
    }

    /// <summary>
    /// Create a new vehicle listing
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateVehicleResult>> Create([FromBody] CreateVehicleRequest request)
    {
        if (request.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId.Value);
            if (category == null)
                return BadRequest(new { message = "Category not found" });
        }

        // Resolve sellerId: always prefer the authenticated user's ID from JWT (security: prevent
        // the frontend from impersonating another seller). Fall back to request body only when the
        // endpoint is called without a JWT (e.g. seeding / tests).
        var resolvedSellerId = Guid.Empty;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var claimsUserId))
        {
            resolvedSellerId = claimsUserId;
        }
        else if (request.SellerId.HasValue && request.SellerId.Value != Guid.Empty)
        {
            // Fallback: unauthenticated callers (seeding/admin import) may supply sellerId explicitly
            resolvedSellerId = request.SellerId.Value;
            _logger.LogWarning("Create vehicle called without valid JWT; using request.SellerId {SellerId}", resolvedSellerId);
        }

        if (resolvedSellerId == Guid.Empty)
        {
            _logger.LogWarning("Create vehicle: could not resolve SellerId from JWT or request — vehicle will have empty SellerId");
        }

        // Build title from make/model/year if not provided
        var title = string.IsNullOrWhiteSpace(request.Title)
            ? $"{request.Year} {request.Make} {request.Model}{(string.IsNullOrWhiteSpace(request.Trim) ? "" : " " + request.Trim)}"
            : request.Title;

        // Accept province as alias for state (frontend uses province)
        var state = request.State ?? request.Province;

        // Convert FeaturesJson: accept either a JSON string or build from Features list
        var featuresJson = request.FeaturesJson;
        if (string.IsNullOrWhiteSpace(featuresJson) && request.Features != null && request.Features.Count > 0)
        {
            featuresJson = System.Text.Json.JsonSerializer.Serialize(request.Features);
        }
        featuresJson ??= "[]";

        // Map bodyStyle: accept BodyStyle enum or BodyType string alias
        var bodyStyle = request.BodyStyle ?? MapBodyTypeToBodyStyle(request.BodyType);

        // Currency default to DOP for DR market
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "DOP" : request.Currency;

        // ═══════════════════════════════════════════════════════════════
        // FRAUD PREVENTION: VIN validation at creation time
        // ═══════════════════════════════════════════════════════════════
        string? normalizedVin = null;
        if (!string.IsNullOrWhiteSpace(request.VIN))
        {
            var vinResult = VehiclesSaleService.Application.Services.VinValidationService.ValidateFormat(request.VIN);
            if (!vinResult.IsValid)
            {
                return BadRequest(new { message = vinResult.Error, field = "VIN" });
            }
            normalizedVin = vinResult.NormalizedVin;

            // Check for duplicate VIN on active/pending listings
            var existingVehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VIN == normalizedVin && !v.IsDeleted);
            if (existingVehicle != null)
            {
                return Conflict(new
                {
                    code = "DUPLICATE_VEHICLE_VIN",
                    message = $"Ya existe un vehículo registrado con el VIN {normalizedVin}.",
                    existingVehicleId = existingVehicle.Id,
                    field = "VIN"
                });
            }
        }

        var vehicle = new Vehicle
        {
            Title = title,
            Description = request.Description ?? string.Empty,
            Price = request.Price,
            Currency = currency,
            Status = VehicleStatus.Draft,
            VIN = normalizedVin ?? request.VIN,
            Make = request.Make,
            Model = request.Model,
            Trim = request.Trim,
            Year = request.Year,
            Mileage = request.Mileage,
            MileageUnit = request.MileageUnit ?? MileageUnit.Kilometers,
            VehicleType = request.VehicleType,
            BodyStyle = bodyStyle ?? BodyStyle.Sedan,
            Doors = request.Doors ?? 4,
            Seats = request.Seats ?? 5,
            FuelType = request.FuelType ?? FuelType.Gasoline,
            Transmission = request.Transmission ?? TransmissionType.Automatic,
            DriveType = request.DriveType ?? Entities.DriveType.FWD,
            EngineSize = request.EngineSize,
            Cylinders = request.Cylinders,
            Horsepower = request.Horsepower,
            ExteriorColor = request.ExteriorColor,
            InteriorColor = request.InteriorColor,
            Condition = request.Condition,
            IsCertified = request.IsCertified ?? false,
            HasCleanTitle = request.HasCleanTitle ?? true,
            City = request.City,
            State = state,
            ZipCode = request.ZipCode,
            Country = request.Country ?? "DO",
            SellerId = resolvedSellerId,
            SellerName = request.SellerName ?? string.Empty,
            SellerPhone = request.SellerPhone,
            SellerEmail = request.SellerEmail,
            SellerWhatsApp = request.SellerWhatsApp,
            DealerId = request.DealerId ?? Guid.Empty,
            CategoryId = request.CategoryId,
            FeaturesJson = featuresJson,
            HomepageSections = request.HomepageSections ?? HomepageSection.None
        };

        // Add images — accept either List<string> (legacy) or List<VehicleImageDto> (new)
        if (request.ImageObjects != null && request.ImageObjects.Count > 0)
        {
            foreach (var img in request.ImageObjects.OrderBy(i => i.SortOrder ?? 0))
            {
                vehicle.Images.Add(new VehicleImage
                {
                    Url = img.Url,
                    ThumbnailUrl = img.ThumbnailUrl,
                    SortOrder = img.SortOrder ?? vehicle.Images.Count,
                    IsPrimary = img.IsPrimary ?? vehicle.Images.Count == 0
                });
            }
        }
        else if (request.Images != null)
        {
            var sortOrder = 0;
            foreach (var imageUrl in request.Images)
            {
                vehicle.Images.Add(new VehicleImage
                {
                    Url = imageUrl,
                    SortOrder = sortOrder,
                    IsPrimary = sortOrder == 0
                });
                sortOrder++;
            }
        }

        var created = await _vehicleRepository.CreateAsync(vehicle);

        _logger.LogInformation("Vehicle created: {VehicleId} - {Title}", created.Id, created.Title);

        // Publish VehicleCreatedEvent to RabbitMQ
        try
        {
            await _eventPublisher.PublishAsync(new VehicleCreatedEvent
            {
                VehicleId = created.Id,
                Make = created.Make,
                Model = created.Model,
                Year = created.Year,
                Price = created.Price,
                VIN = created.VIN ?? string.Empty,
                CreatedBy = created.SellerId,
                CreatedAt = created.CreatedAt
            });

            _logger.LogInformation("VehicleCreatedEvent published for VehicleId: {VehicleId}", created.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish VehicleCreatedEvent for VehicleId: {VehicleId}", created.Id);
            // Don't fail the request if event publishing fails
        }

        var slug = GenerateSlug(created);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, new CreateVehicleResult
        {
            Id = created.Id,
            Slug = slug,
            Status = created.Status,
            Message = "Vehicle created successfully. Call /publish to make it visible."
        });
    }

    /// <summary>
    /// Maps a frontend bodyType string to BodyStyle enum (case-insensitive)
    /// </summary>
    private static BodyStyle? MapBodyTypeToBodyStyle(string? bodyType)
    {
        if (string.IsNullOrWhiteSpace(bodyType)) return null;
        return bodyType.ToLowerInvariant() switch
        {
            "sedan" => BodyStyle.Sedan,
            "coupe" => BodyStyle.Coupe,
            "hatchback" => BodyStyle.Hatchback,
            "wagon" => BodyStyle.Wagon,
            "suv" => BodyStyle.SUV,
            "crossover" => BodyStyle.Crossover,
            "pickup" or "truck" => BodyStyle.Pickup,
            "van" => BodyStyle.Van,
            "minivan" => BodyStyle.Minivan,
            "convertible" => BodyStyle.Convertible,
            "sports" or "sportscar" or "sport" => BodyStyle.SportsCar,
            _ => null
        };
    }

    /// <summary>
    /// Update an existing vehicle
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Vehicle>> Update(Guid id, [FromBody] UpdateVehicleRequest request)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        // Ownership check: only the seller or Admin/Moderator can update
        if (!IsOwnerOrAdmin(vehicle))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para editar este vehículo." });

        // Track price change for alert notifications
        var oldPrice = vehicle.Price;
        var oldMileage = vehicle.Mileage;
        var oldStatus = vehicle.Status;

        // ═══════════════════════════════════════════════════════════════
        // FRAUD PREVENTION: Mileage rollback detection
        // ═══════════════════════════════════════════════════════════════
        if (request.Mileage.HasValue && request.Mileage.Value < vehicle.Mileage)
        {
            _logger.LogWarning(
                "MILEAGE ROLLBACK DETECTED: Vehicle {VehicleId} mileage decreased from {OldMileage} to {NewMileage} by seller {SellerId}",
                id, vehicle.Mileage, request.Mileage.Value, vehicle.SellerId);

            // Block the mileage decrease — require admin override
            if (!IsAdmin())
            {
                return BadRequest(new
                {
                    message = "No se puede reducir el kilometraje de un vehículo. Si el valor es incorrecto, contacte a soporte.",
                    field = "Mileage",
                    currentMileage = vehicle.Mileage,
                    attemptedMileage = request.Mileage.Value
                });
            }
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.Title)) vehicle.Title = request.Title;
        if (request.Description != null) vehicle.Description = request.Description;
        if (request.Price.HasValue) vehicle.Price = request.Price.Value;
        if (!string.IsNullOrEmpty(request.Currency)) vehicle.Currency = request.Currency;
        // Status cannot be changed via Update — use /publish, /approve, /reject, /unpublish, /sold
        if (request.VehicleType.HasValue) vehicle.VehicleType = request.VehicleType.Value;
        if (request.BodyStyle.HasValue) vehicle.BodyStyle = request.BodyStyle.Value;
        if (request.FuelType.HasValue) vehicle.FuelType = request.FuelType.Value;
        if (request.Transmission.HasValue) vehicle.Transmission = request.Transmission.Value;
        if (request.DriveType.HasValue) vehicle.DriveType = request.DriveType.Value;
        if (request.Mileage.HasValue) vehicle.Mileage = request.Mileage.Value;
        if (!string.IsNullOrEmpty(request.ExteriorColor)) vehicle.ExteriorColor = request.ExteriorColor;
        if (!string.IsNullOrEmpty(request.InteriorColor)) vehicle.InteriorColor = request.InteriorColor;
        if (request.Condition.HasValue) vehicle.Condition = request.Condition.Value;
        if (request.IsCertified.HasValue) vehicle.IsCertified = request.IsCertified.Value;
        if (request.HasCleanTitle.HasValue) vehicle.HasCleanTitle = request.HasCleanTitle.Value;
        if (!string.IsNullOrEmpty(request.City)) vehicle.City = request.City;
        if (!string.IsNullOrEmpty(request.State)) vehicle.State = request.State;
        if (!string.IsNullOrEmpty(request.ZipCode)) vehicle.ZipCode = request.ZipCode;

        // SECURITY FIX: Only Admin/Moderator can set IsFeatured and HomepageSections
        // Prevents sellers from self-featuring their vehicles
        if (User.IsInRole("Admin") || User.IsInRole("Moderator") || User.IsInRole("admin") || User.IsInRole("moderator"))
        {
            if (request.IsFeatured.HasValue) vehicle.IsFeatured = request.IsFeatured.Value;
            if (request.HomepageSections.HasValue) vehicle.HomepageSections = request.HomepageSections.Value;
        }

        // ═══════════════════════════════════════════════════════════════
        // FRAUD PREVENTION: Reset to PendingReview when critical fields
        // change on an already-approved listing (H-1/H-2)
        // ═══════════════════════════════════════════════════════════════
        bool criticalFieldChanged = false;
        if (oldStatus == VehicleStatus.Active || oldStatus == VehicleStatus.PendingReview)
        {
            criticalFieldChanged =
                (request.Price.HasValue && request.Price.Value != oldPrice) ||
                (request.Mileage.HasValue && request.Mileage.Value != oldMileage) ||
                (request.Condition.HasValue && request.Condition.Value != vehicle.Condition) ||
                (request.HasCleanTitle.HasValue && request.HasCleanTitle.Value != vehicle.HasCleanTitle);

            if (criticalFieldChanged && oldStatus == VehicleStatus.Active && !IsAdmin())
            {
                vehicle.Status = VehicleStatus.PendingReview;
                vehicle.SubmittedForReviewAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "Vehicle {VehicleId} reset to PendingReview due to critical field changes after approval",
                    id);
            }
        }

        vehicle.UpdatedAt = DateTime.UtcNow;

        await _vehicleRepository.UpdateAsync(vehicle);

        // ═══════════════════════════════════════════════════════════════
        // OKLA PLATFORM SCORE: Persist price change to VehiclePriceHistory
        // This creates the transparency trail visible to buyers and
        // accumulates switching cost for the dealer.
        // ═══════════════════════════════════════════════════════════════
        if (request.Price.HasValue && oldPrice != vehicle.Price)
        {
            var sellerId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var sid) ? sid : Guid.Empty;
            var priceHistoryEntry = new VehiclePriceHistory
            {
                Id = Guid.NewGuid(),
                DealerId = vehicle.DealerId,
                VehicleId = vehicle.Id,
                OldPrice = oldPrice,
                NewPrice = vehicle.Price,
                Currency = vehicle.Currency ?? "DOP",
                ChangedBy = sellerId,
                ChangeType = IsAdmin() ? PriceChangeType.AdminAdjustment : PriceChangeType.Manual,
                ChangedAt = DateTime.UtcNow
            };
            _context.VehiclePriceHistories.Add(priceHistoryEntry);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "OKLA Platform Score: Price change recorded for vehicle {VehicleId}: {OldPrice} → {NewPrice} ({ChangeType})",
                id, oldPrice, vehicle.Price, priceHistoryEntry.ChangeType);
        }
        await InvalidateVehicleCacheAsync(id);

        _logger.LogInformation("Vehicle updated: {VehicleId}", id);

        // Notify AlertService if price changed
        if (request.Price.HasValue && oldPrice != vehicle.Price)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("AlertService");
                    var slug = GenerateSlug(vehicle);
                    var notification = new
                    {
                        vehicleId = vehicle.Id,
                        vehicleTitle = vehicle.Title,
                        vehicleSlug = slug,
                        oldPrice,
                        newPrice = vehicle.Price,
                        currency = vehicle.Currency ?? "DOP",
                        updatedBy = Guid.Empty
                    };
                    var response = await client.PostAsJsonAsync("/api/pricealerts/check-price", notification);
                    _logger.LogInformation(
                        "Price change notification sent for vehicle {VehicleId}: {OldPrice} → {NewPrice}, AlertService responded {StatusCode}",
                        id, oldPrice, vehicle.Price, response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to notify AlertService about price change for vehicle {VehicleId}",
                        id);
                }
            });
        }

        return Ok(vehicle);
    }

    /// <summary>
    /// Delete a vehicle (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        // Ownership check: only the seller or Admin/Moderator can delete
        if (!IsOwnerOrAdmin(vehicle))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para eliminar este vehículo." });

        await _vehicleRepository.DeleteAsync(id);

        // Invalidate cache for this vehicle
        await InvalidateVehicleCacheAsync(id);

        _logger.LogInformation("Vehicle deleted: {VehicleId}", id);

        // ═══════════════════════════════════════════════════════════════
        // SITEMAP FIX: Trigger ISR revalidation + Google URL_DELETED
        // ═══════════════════════════════════════════════════════════════
        _ = Task.Run(async () =>
        {
            try
            {
                var slug = $"{vehicle.Make}-{vehicle.Model}-{vehicle.Year}-{vehicle.Id}".ToLowerInvariant();
                if (_sitemapRevalidation != null)
                    await _sitemapRevalidation.OnVehicleUnpublishedAsync(slug);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sitemap revalidation failed for deleted vehicle {VehicleId}", vehicle.Id);
            }
        });

        return NoContent();
    }

    // ========================================
    // OKLA PLATFORM SCORE — Buyer Transparency & Switching Cost
    // ========================================

    /// <summary>
    /// Get the price change history of a vehicle.
    /// Public endpoint — buyers can see the full price history for transparency.
    /// This is a KEY component of the OKLA Platform Score switching cost.
    /// </summary>
    [HttpGet("{id:guid}/price-history")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VehiclePriceHistoryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehiclePriceHistoryResponse>> GetPriceHistory(Guid id)
    {
        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        var priceHistory = await _context.VehiclePriceHistories
            .Where(ph => ph.VehicleId == id)
            .OrderByDescending(ph => ph.ChangedAt)
            .Select(ph => new PriceChangeRecord
            {
                OldPrice = ph.OldPrice,
                NewPrice = ph.NewPrice,
                PriceDifference = ph.NewPrice - ph.OldPrice,
                ChangePercentage = ph.OldPrice > 0
                    ? Math.Round((ph.NewPrice - ph.OldPrice) / ph.OldPrice * 100, 2) : 0,
                Currency = ph.Currency,
                ChangeType = ph.ChangeType.ToString(),
                ChangedAt = ph.ChangedAt,
                Reason = ph.Reason
            })
            .ToListAsync();

        var currentPrice = vehicle.Price;
        var originalPrice = priceHistory.LastOrDefault()?.OldPrice ?? vehicle.Price;
        var totalVariation = originalPrice > 0
            ? Math.Round((currentPrice - originalPrice) / originalPrice * 100, 2) : 0;

        return Ok(new VehiclePriceHistoryResponse
        {
            VehicleId = id,
            CurrentPrice = currentPrice,
            OriginalListingPrice = originalPrice,
            TotalPriceChanges = priceHistory.Count,
            TotalVariationPercent = totalVariation,
            PriceTrend = totalVariation < -5 ? "Decreasing" : totalVariation > 5 ? "Increasing" : "Stable",
            Currency = vehicle.Currency ?? "DOP",
            History = priceHistory
        });
    }

    /// <summary>
    /// Get the OKLA Platform Score for a vehicle.
    /// Public endpoint — shows buyers the accumulated engagement and transparency metrics.
    /// This score is unique to OKLA and creates switching cost for dealers.
    /// </summary>
    [HttpGet("{id:guid}/platform-score")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OklaPlatformScore), StatusCodes.Status200OK)]
    public async Task<ActionResult<OklaPlatformScore>> GetPlatformScore(Guid id)
    {
        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.Images)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        var priceHistoryCount = await _context.VehiclePriceHistories
            .Where(ph => ph.VehicleId == id)
            .CountAsync();

        var firstPrice = await _context.VehiclePriceHistories
            .Where(ph => ph.VehicleId == id)
            .OrderBy(ph => ph.ChangedAt)
            .Select(ph => (decimal?)ph.OldPrice)
            .FirstOrDefaultAsync();

        var daysOnPlatform = vehicle.PublishedAt.HasValue
            ? (int)(DateTime.UtcNow - vehicle.PublishedAt.Value).TotalDays : 0;

        var totalPriceVariation = firstPrice.HasValue && firstPrice.Value > 0
            ? Math.Round((vehicle.Price - firstPrice.Value) / firstPrice.Value * 100, 2) : 0;

        var score = new OklaPlatformScore
        {
            // D1: Antigüedad (25 pts max)
            DaysOnPlatform = daysOnPlatform,
            AntiquityPoints = daysOnPlatform switch
            {
                >= 31 => 25,
                >= 8 => 15,
                >= 1 => 5,
                _ => 0
            },

            // D2: Buyer Engagement (30 pts max)
            TotalViews = vehicle.ViewCount,
            TotalFavorites = vehicle.FavoriteCount,
            TotalInquiries = vehicle.InquiryCount,
            BuyerEngagementPoints = Math.Min(30,
                Math.Min(10, vehicle.ViewCount / 10) +    // max 10 pts for views (100+ views)
                Math.Min(10, vehicle.FavoriteCount * 2) +  // max 10 pts for favorites (5+ favs)
                Math.Min(10, vehicle.InquiryCount * 3)),   // max 10 pts for inquiries (4+ inquiries)

            // D3: Price Transparency (20 pts max)
            PriceChangeCount = priceHistoryCount,
            TotalPriceVariation = totalPriceVariation,
            PriceTrend = totalPriceVariation < -5 ? PriceTrend.Decreasing
                : totalPriceVariation > 5 ? PriceTrend.Increasing : PriceTrend.Stable,
            PriceTransparencyPoints = priceHistoryCount switch
            {
                0 => 10,   // No changes = stable = 10 pts base
                1 => 15,   // One change = some transparency
                >= 2 => 20, // Multiple changes = full transparency
                _ => 10
            },

            // D4: Seller Reputation (15 pts max)
            SellerRating = vehicle.SellerRating,
            SellerReviewCount = vehicle.SellerReviewCount,
            SellerVerified = vehicle.SellerVerified,
            SellerReputationPoints =
                (vehicle.SellerVerified ? 5 : 0) +
                (vehicle.SellerRating.HasValue ? Math.Min(5, (int)(vehicle.SellerRating.Value)) : 0) +
                (vehicle.SellerReviewCount.HasValue && vehicle.SellerReviewCount.Value >= 5 ? 5 : 0),

            // D5: Completeness (10 pts max)
            PhotoCount = vehicle.Images?.Count ?? 0,
            DescriptionLength = vehicle.Description?.Length ?? 0,
            HasVerifiedVin = !string.IsNullOrEmpty(vehicle.VIN),
            HasExternalHistory = !string.IsNullOrEmpty(vehicle.CarfaxReportUrl),
            CompletenessPoints =
                ((vehicle.Images?.Count ?? 0) >= 5 ? 3 : (vehicle.Images?.Count ?? 0) >= 3 ? 2 : 0) +
                ((vehicle.Description?.Length ?? 0) >= 100 ? 2 : (vehicle.Description?.Length ?? 0) >= 50 ? 1 : 0) +
                (!string.IsNullOrEmpty(vehicle.VIN) ? 3 : 0) +
                (!string.IsNullOrEmpty(vehicle.CarfaxReportUrl) ? 2 : 0),

            // Switching Cost
            SwitchingCost = new SwitchingCostSummary
            {
                DaysAccumulated = daysOnPlatform,
                ViewsAccumulated = vehicle.ViewCount,
                InquiriesAccumulated = vehicle.InquiryCount,
                FavoritesAccumulated = vehicle.FavoriteCount,
                PriceHistoryRecords = priceHistoryCount
            }
        };

        return Ok(score);
    }

    /// <summary>
    /// Get the switching cost preview for a vehicle before unpublish/delete.
    /// Dealer-only endpoint — shows what they'd lose if they remove this vehicle from OKLA.
    /// </summary>
    [HttpGet("{id:guid}/switching-cost")]
    [ProducesResponseType(typeof(SwitchingCostSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<SwitchingCostSummary>> GetSwitchingCost(Guid id)
    {
        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        if (!IsOwnerOrAdmin(vehicle))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso." });

        var priceHistoryCount = await _context.VehiclePriceHistories
            .Where(ph => ph.VehicleId == id)
            .CountAsync();

        var daysOnPlatform = vehicle.PublishedAt.HasValue
            ? (int)(DateTime.UtcNow - vehicle.PublishedAt.Value).TotalDays : 0;

        // Calculate platform score level for the warning
        var viewPts = Math.Min(10, vehicle.ViewCount / 10);
        var favPts = Math.Min(10, vehicle.FavoriteCount * 2);
        var inqPts = Math.Min(10, vehicle.InquiryCount * 3);
        var engagementPts = Math.Min(30, viewPts + favPts + inqPts);
        var antiquityPts = daysOnPlatform >= 31 ? 25 : daysOnPlatform >= 8 ? 15 : daysOnPlatform >= 1 ? 5 : 0;
        var totalScore = antiquityPts + engagementPts + 10 + 5; // base transparency + base seller

        var summary = new SwitchingCostSummary
        {
            DaysAccumulated = daysOnPlatform,
            ViewsAccumulated = vehicle.ViewCount,
            InquiriesAccumulated = vehicle.InquiryCount,
            FavoritesAccumulated = vehicle.FavoriteCount,
            PriceHistoryRecords = priceHistoryCount,
            CurrentPlatformScore = totalScore,
            CurrentLevel = totalScore >= 85 ? PlatformScoreLevel.Platinum
                : totalScore >= 65 ? PlatformScoreLevel.Gold
                : totalScore >= 45 ? PlatformScoreLevel.Silver
                : totalScore >= 25 ? PlatformScoreLevel.Bronze
                : PlatformScoreLevel.New
        };

        return Ok(summary);
    }

    // ========================================
    // AUTO-SUSPEND (called by ReportsService after ≥3 buyer reports)
    // ========================================

    /// <summary>
    /// Suspend a vehicle listing due to buyer reports.
    /// Internal endpoint — called by ReportsService when report count reaches threshold.
    /// Changes status to Suspended, records reason and timestamp.
    /// </summary>
    [HttpPatch("{id:guid}/suspend")]
    [AllowAnonymous] // Internal service-to-service call
    public async Task<IActionResult> Suspend(
        Guid id,
        [FromBody] SuspendVehicleRequest request)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        // Only suspend Active vehicles
        if (vehicle.Status != VehicleStatus.Active)
        {
            _logger.LogInformation(
                "Vehicle {VehicleId} is not Active (status: {Status}) — skipping suspend",
                id, vehicle.Status);
            return Ok(new { message = $"Vehicle is {vehicle.Status}, not Active. No action taken." });
        }

        vehicle.Status = VehicleStatus.Suspended;
        vehicle.SuspendedAt = DateTime.UtcNow;
        vehicle.SuspendedReason = request.Reason;
        vehicle.ReportCount = request.ReportCount;
        vehicle.ModerationNotes = $"[AUTO-SUSPEND {DateTime.UtcNow:yyyy-MM-dd HH:mm}] {request.Reason}";
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Invalidate cache
        await InvalidateVehicleCacheAsync(id);

        _logger.LogWarning(
            "🚨 Vehicle {VehicleId} AUTO-SUSPENDED — {ReportCount} reports. Reason: {Reason}",
            id, request.ReportCount, request.Reason);

        return Ok(new
        {
            message = "Listing suspended pending dealer review.",
            vehicleId = id,
            status = "Suspended",
            suspendedAt = vehicle.SuspendedAt,
            reportCount = vehicle.ReportCount
        });
    }

    // ========================================
    // PUBLISH / UNPUBLISH / SOLD / FEATURE / VIEWS
    // ========================================

    /// <summary>
    /// Publish a vehicle listing (Draft/Inactive -> Active)
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Vehicle must be in Draft or Inactive status
    /// - Minimum 3 images required
    /// - Title, price, make, model, year are required
    /// - At least one contact method (phone or email)
    /// </remarks>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(PublishVehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublishVehicleResponse>> Publish(Guid id, [FromBody] PublishVehicleRequest? request = null)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Images)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        // Ownership check: only the seller or Admin/Moderator can publish
        if (!IsOwnerOrAdmin(vehicle))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para publicar este vehículo." });

        // --- Dealer KYC gate ---
        // Dealers must complete KYC verification before publishing any listing.
        if (vehicle.SellerType == SellerType.Dealer)
        {
            var isVerified = await _dealerVerificationClient.IsDealerVerifiedAsync(vehicle.SellerId);
            if (!isVerified)
            {
                _logger.LogWarning(
                    "Dealer {SellerId} attempted to publish vehicle {VehicleId} without KYC verification",
                    vehicle.SellerId, id);
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Debes completar la verificación KYC antes de publicar vehículos.",
                    requiresKyc = true,
                    redirectUrl = "/cuenta/verificacion"
                });
            }
        }

        // Validate status - only Draft, Archived, or Rejected can be submitted for review
        if (vehicle.Status != VehicleStatus.Draft && vehicle.Status != VehicleStatus.Archived && vehicle.Status != VehicleStatus.Rejected && vehicle.Status != VehicleStatus.PendingMedia)
        {
            return BadRequest(new
            {
                message = $"Vehicle cannot be submitted from status '{vehicle.Status}'. Only Draft, Archived, Rejected, or PendingMedia vehicles can be submitted for review.",
                currentStatus = vehicle.Status.ToString()
            });
        }

        // Load dynamic config from ConfigurationService
        var maxPriceDop = await _configClient.GetDecimalAsync("vehicles.max_price_dop", 100_000_000m);
        var maxImages = await _configClient.GetIntAsync("vehicles.max_images_per_listing", 30);
        var listingExpirationDays = await _configClient.GetIntAsync("vehicles.listing_expiration_days", 90);

        // Validate required fields
        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(vehicle.Title) || vehicle.Title.Length < 10)
            validationErrors.Add("Title must be at least 10 characters");

        if (vehicle.Price <= 0)
            validationErrors.Add("Price must be greater than 0");

        if (vehicle.Price > maxPriceDop)
            validationErrors.Add($"Price cannot exceed {maxPriceDop:N0} DOP");

        if (string.IsNullOrWhiteSpace(vehicle.Make))
            validationErrors.Add("Make is required");

        if (string.IsNullOrWhiteSpace(vehicle.Model))
            validationErrors.Add("Model is required");

        if (vehicle.Year < 1900 || vehicle.Year > DateTime.UtcNow.Year + 2)
            validationErrors.Add($"Year must be between 1900 and {DateTime.UtcNow.Year + 2}");

        if (vehicle.Images.Count < 1)
            validationErrors.Add("At least 1 image is required (recommend 3+)");

        if (vehicle.Images.Count > maxImages)
            validationErrors.Add($"Maximum {maxImages} images allowed per listing");

        if (string.IsNullOrWhiteSpace(vehicle.SellerPhone) && string.IsNullOrWhiteSpace(vehicle.SellerEmail))
            validationErrors.Add("At least one contact method (phone or email) is required");

        // ═══════════════════════════════════════════════════════════════
        // FRAUD PREVENTION: VIN validation at publish time
        // Check digit errors → moderation warning (not hard block)
        // Format/character errors → hard block
        // Rationale: manual VIN entry in DR market has high transcription
        // error rate. Moderators verify the physical VIN on inspection.
        // ═══════════════════════════════════════════════════════════════
        if (!string.IsNullOrWhiteSpace(vehicle.VIN))
        {
            var vinResult = VehiclesSaleService.Application.Services.VinValidationService.Validate(vehicle.VIN);
            if (!vinResult.IsValid)
            {
                if (vinResult.IsChecksumFailure)
                {
                    // Checksum-only failures: flag in moderation notes for manual review
                    // but do NOT block publish — common transcription error in DR market
                    _logger.LogWarning(
                        "Vehicle {VehicleId} has VIN with invalid check digit: {VIN}. Flagged for moderator review.",
                        id, vehicle.VIN);
                    vehicle.ModerationNotes = (vehicle.ModerationNotes ?? string.Empty)
                        + $"\n[VIN CHECK] ⚠️ Dígito de verificación inválido (posición 9). Moderador debe verificar el VIN físico del vehículo.";
                }
                else
                {
                    // Format/character errors are hard blocks (clearly invalid VIN)
                    validationErrors.Add($"VIN inválido: {vinResult.Error}");
                }
            }
        }
        // VIN recommended warning (not blocking for now, but logged)
        if (string.IsNullOrWhiteSpace(vehicle.VIN))
        {
            _logger.LogWarning("Vehicle {VehicleId} submitted for review without VIN", id);
        }

        // ═══════════════════════════════════════════════════════════════
        // VIN CROSS-REFERENCE: Verify VIN data matches declared attributes
        // Calls NHTSA vPIC to decode the VIN and compares make/model/year/body
        // ═══════════════════════════════════════════════════════════════
        VehiclesSaleService.Application.Services.VinVerificationResult? vinVerification = null;
        if (!string.IsNullOrWhiteSpace(vehicle.VIN) && _nhtsaService != null)
        {
            try
            {
                var nhtsaResult = await _nhtsaService.DecodeVinAsync(vehicle.VIN);
                if (nhtsaResult != null && nhtsaResult.ErrorCode == "0")
                {
                    vinVerification = VehiclesSaleService.Application.Services.VinVerificationService.Verify(
                        nhtsaMake: nhtsaResult.Make,
                        nhtsaModel: nhtsaResult.Model,
                        nhtsaYear: nhtsaResult.ModelYear,
                        nhtsaBodyClass: nhtsaResult.BodyClass,
                        declaredMake: vehicle.Make ?? string.Empty,
                        declaredModel: vehicle.Model ?? string.Empty,
                        declaredYear: vehicle.Year,
                        declaredBodyType: vehicle.BodyStyle.ToString());

                    if (vinVerification.HasDiscrepancies)
                    {
                        _logger.LogWarning(
                            "VIN mismatch for vehicle {VehicleId}: {Discrepancies}",
                            id, string.Join("; ", vinVerification.Discrepancies.Select(d => d.Message)));

                        // Store discrepancy details in moderation notes for reviewer
                        vehicle.ModerationNotes = (vehicle.ModerationNotes ?? string.Empty)
                            + $"\n[VIN VERIFICACIÓN] {vinVerification.DealerMessage}";
                    }
                }
            }
            catch (Exception ex)
            {
                // NHTSA failure should NOT block publish — degrade gracefully
                _logger.LogWarning(ex, "NHTSA VIN verification failed for vehicle {VehicleId}. Proceeding without cross-reference.", id);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ODOMETER ROLLBACK DETECTION: Cross-reference declared mileage
        // with VinAudit/CARFAX historical odometer readings
        // ═══════════════════════════════════════════════════════════════
        bool odometerRollbackDetected = false;
        if (!string.IsNullOrWhiteSpace(vehicle.VIN) && _vehicleHistoryService != null && vehicle.Mileage > 0)
        {
            try
            {
                var historySummary = await _vehicleHistoryService.GetSummaryByVinAsync(vehicle.VIN);
                if (historySummary != null)
                {
                    vehicle.OdometerVerifiedAt = DateTime.UtcNow;
                    vehicle.HistoricalMileage = historySummary.LastReportedMileage;

                    // Check 1: VinAudit/CARFAX already flags rollback
                    if (historySummary.OdometerRollback)
                    {
                        odometerRollbackDetected = true;
                    }

                    // Check 2: Declared mileage < last reported mileage (>500 km tolerance)
                    if (historySummary.LastReportedMileage.HasValue
                        && vehicle.Mileage < (int)(historySummary.LastReportedMileage.Value - 500))
                    {
                        odometerRollbackDetected = true;
                        _logger.LogWarning(
                            "ODOMETER ROLLBACK detected for vehicle {VehicleId}: declared={Declared}km, historical={Historical}km (VIN={VIN})",
                            id, vehicle.Mileage, historySummary.LastReportedMileage.Value, vehicle.VIN);
                    }

                    if (odometerRollbackDetected)
                    {
                        vehicle.OdometerRollbackDetected = true;
                        vehicle.ModerationNotes = (vehicle.ModerationNotes ?? string.Empty)
                            + $"\n[ODÓMETRO] ⚠️ Posible rollback detectado: declarado={vehicle.Mileage}km, historial={historySummary.LastReportedMileage?.ToString("N0") ?? "N/A"}km. Requiere revisión manual.";
                    }
                }
            }
            catch (Exception ex)
            {
                // History service failure should NOT block publish
                _logger.LogWarning(ex, "Vehicle history odometer check failed for vehicle {VehicleId}. Proceeding without odometer verification.", id);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PHOTO MODERATION: Validate images for quality & content
        // Rejects: watermarks, stock photos, low resolution, documents
        // ═══════════════════════════════════════════════════════════════
        VehiclesSaleService.Application.Services.PhotoModerationResult? photoModeration = null;
        if (vehicle.Images.Count > 0)
        {
            photoModeration = VehiclesSaleService.Application.Services.PhotoModerationService.EvaluateAll(vehicle.Images);

            // Apply moderation results to each image entity
            foreach (var imgResult in photoModeration.ImageResults)
            {
                var img = vehicle.Images.FirstOrDefault(i => i.Id == imgResult.ImageId);
                if (img != null)
                {
                    img.ModerationStatus = imgResult.Status;
                    img.ModerationRejectionReason = imgResult.RejectionReason;
                    img.ModerationFlags = imgResult.Flags.Count > 0 ? string.Join(",", imgResult.Flags) : null;
                    img.ModeratedAt = DateTime.UtcNow;
                    img.ModeratedBy = "PhotoModerationService";
                }
            }

            if (photoModeration.HasRejections)
            {
                _logger.LogWarning(
                    "Photo moderation for vehicle {VehicleId}: {RejectedCount}/{TotalImages} photos rejected",
                    id, photoModeration.RejectedCount, photoModeration.TotalImages);

                // Add per-image rejection details as validation errors
                foreach (var rejected in photoModeration.ImageResults.Where(r => r.Status == Domain.Entities.ImageModerationStatus.Rejected))
                {
                    validationErrors.Add(rejected.DealerMessage ?? "Foto rechazada por moderación");
                }

                // Add moderation note for reviewer
                vehicle.ModerationNotes = (vehicle.ModerationNotes ?? string.Empty)
                    + $"\n[FOTOS] {photoModeration.RejectedCount}/{photoModeration.TotalImages} fotos rechazadas por moderación automática.";
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // CDN URL VERIFICATION: Validate images are accessible from public CDN
        // Uses HEAD requests in parallel to verify all image URLs within 3 seconds.
        // Detects permissions issues (403), missing files (404), server errors (500).
        // ═══════════════════════════════════════════════════════════════
        VehiclesSaleService.Application.Services.ImageUrlValidationResult? urlValidation = null;
        var imagesWithUrls = vehicle.Images
            .Where(img => !string.IsNullOrWhiteSpace(img.Url))
            .Select(img => (img.Id, img.Url))
            .ToList();

        if (imagesWithUrls.Count > 0)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ImageUrlValidation");
                httpClient.Timeout = TimeSpan.FromSeconds(5); // Per-request timeout

                urlValidation = await VehiclesSaleService.Application.Services.ImageUrlValidationService.ValidateAllAsync(
                    imagesWithUrls,
                    httpClient,
                    timeoutSeconds: 3,
                    cancellationToken: HttpContext.RequestAborted);

                if (!urlValidation.AllValid)
                {
                    _logger.LogWarning(
                        "CDN URL validation for vehicle {VehicleId}: {FailedCount}/{TotalChecked} URLs failed",
                        id, urlValidation.FailedCount, urlValidation.TotalChecked);

                    // Flag the vehicle for the buyer-facing "Fotos en actualización" banner
                    vehicle.HasBrokenImages = true;
                    vehicle.BrokenImagesDetectedAt = DateTime.UtcNow;

                    foreach (var failed in urlValidation.FailedImages)
                    {
                        validationErrors.Add($"La imagen no es accesible: {failed.Error} ({failed.Url})");
                    }

                    // If ALL images fail CDN verification, block listing in PendingMedia state
                    if (urlValidation.FailedCount == urlValidation.TotalChecked)
                    {
                        vehicle.Status = VehicleStatus.PendingMedia;
                        vehicle.UpdatedAt = DateTime.UtcNow;
                        vehicle.ModerationNotes = (vehicle.ModerationNotes ?? string.Empty)
                            + "\n[MEDIA] ⚠️ NINGUNA imagen es accesible desde el CDN. Listing bloqueado en estado PendingMedia.";

                        await _context.SaveChangesAsync();

                        _logger.LogWarning(
                            "Vehicle {VehicleId} set to PendingMedia: ALL {FailedCount} images failed CDN verification",
                            id, urlValidation.FailedCount);

                        return BadRequest(new
                        {
                            message = "Ninguna imagen del vehículo es accesible. Por favor, vuelve a subir las imágenes y reintenta la publicación.",
                            error = "ALL_IMAGES_INACCESSIBLE",
                            status = "PendingMedia",
                            failedImages = urlValidation.FailedImages.Select(f => new { f.Url, f.Error, f.HttpStatusCode }),
                            totalChecked = urlValidation.TotalChecked,
                            failedCount = urlValidation.FailedCount
                        });
                    }
                    else
                    {
                        vehicle.ModerationNotes = (vehicle.ModerationNotes ?? string.Empty)
                            + $"\n[MEDIA] {urlValidation.FailedCount}/{urlValidation.TotalChecked} imágenes no accesibles desde CDN.";
                    }
                }
            }
            catch (Exception ex)
            {
                // URL validation failure should NOT block publish — degrade gracefully
                _logger.LogWarning(ex, "CDN URL validation failed for vehicle {VehicleId}. Proceeding without URL verification.", id);
            }
        }

        if (validationErrors.Any())
        {
            return BadRequest(new
            {
                message = "Vehicle cannot be published due to validation errors",
                errors = validationErrors
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // DISCLAIMER VALIDATION — Ley 172-13 + TOS Compliance
        // The dealer/seller MUST explicitly confirm information accuracy
        // via a mandatory unchecked checkbox. This is a legal requirement.
        // ═══════════════════════════════════════════════════════════════
        if (request == null || !request.DisclaimerAccepted)
        {
            return BadRequest(new
            {
                message = "Debes confirmar que la información del vehículo es exacta y de tu propiedad antes de publicar.",
                error = "DISCLAIMER_NOT_ACCEPTED",
                requiresDisclaimer = true
            });
        }

        // Record disclaimer acceptance with timestamp and IP for legal audit trail
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            ?? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').First().Trim()
            ?? "Unknown";

        vehicle.DisclaimerAcceptedAt = DateTime.UtcNow;
        vehicle.DisclaimerAcceptedFromIp = clientIp;
        vehicle.DisclaimerTosVersion = request.TosVersion ?? "2026.1";

        _logger.LogInformation(
            "Disclaimer accepted for vehicle {VehicleId} by seller {SellerId} from IP {ClientIP} (TOS v{TosVersion})",
            id, vehicle.SellerId, clientIp, vehicle.DisclaimerTosVersion);

        // ═══════════════════════════════════════════════════════════════
        // FRAUD SCORING: Evaluate listing for fraud signals
        // ═══════════════════════════════════════════════════════════════
        var fraudEvaluation = VehiclesSaleService.Application.Services.ListingFraudDetector.Evaluate(vehicle);
        if (fraudEvaluation.FraudScore > 0)
        {
            _logger.LogInformation(
                "Fraud evaluation for vehicle {VehicleId}: Score={FraudScore}, Risk={RiskLevel}, Signals={SignalCount}",
                id, fraudEvaluation.FraudScore, fraudEvaluation.RiskLevel, fraudEvaluation.Signals.Count);
        }

        // Add VIN cross-reference fraud points if discrepancies found
        var vinFraudPoints = vinVerification?.FraudPointsAdded ?? 0;
        // Add odometer rollback fraud points (+30 for confirmed rollback)
        var odometerFraudPoints = odometerRollbackDetected ? 30 : 0;
        var totalFraudScore = Math.Min(fraudEvaluation.FraudScore + vinFraudPoints + odometerFraudPoints, 100);

        // Update status → PendingReview (requires staff approval before visible)
        vehicle.Status = VehicleStatus.PendingReview;
        vehicle.FraudScore = totalFraudScore;
        vehicle.SubmittedForReviewAt = DateTime.UtcNow;
        vehicle.UpdatedAt = DateTime.UtcNow;
        // Clear any previous rejection data if re-submitting
        vehicle.RejectedAt = null;
        vehicle.RejectedBy = null;
        vehicle.RejectionReason = null;

        // Set expiration based on dynamic config (will apply once approved)
        var expiresAt = request?.ExpiresAt ?? DateTime.UtcNow.AddDays(listingExpirationDays);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle submitted for review: {VehicleId} - {Title}", id, vehicle.Title);

        // Publish event
        try
        {
            await _eventPublisher.PublishAsync(new VehicleCreatedEvent
            {
                VehicleId = vehicle.Id,
                Make = vehicle.Make ?? string.Empty,
                Model = vehicle.Model ?? string.Empty,
                Year = vehicle.Year,
                Price = vehicle.Price,
                VIN = vehicle.VIN ?? string.Empty,
                CreatedBy = vehicle.SellerId,
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event for {VehicleId}", id);
        }

        // Build response message — include VIN discrepancy explanation if applicable
        var responseMessage = photoModeration?.HasRejections == true
            ? $"⚠️ {photoModeration.RejectedCount} foto(s) fueron rechazadas por moderación automática. Reemplaza las fotos señaladas y vuelve a publicar."
            : odometerRollbackDetected
            ? "⚠️ Se detectó una posible discrepancia en el odómetro. El kilometraje declarado es menor al registrado en el historial del vehículo. Tu listado será revisado manualmente y los compradores verán una alerta de verificación."
            : vinVerification?.HasDiscrepancies == true
            ? vinVerification.DealerMessage + " Tu listado será revisado manualmente."
            : "Tu vehículo ha sido enviado a revisión. Nuestro equipo lo revisará en las próximas 24 horas y recibirás una notificación cuando sea aprobado.";

        return Ok(new PublishVehicleResponse
        {
            Id = vehicle.Id,
            Status = vehicle.Status,
            PublishedAt = null,
            ExpiresAt = expiresAt,
            Message = responseMessage,
            VinDiscrepancies = vinVerification?.HasDiscrepancies == true
                ? vinVerification.Discrepancies.Select(d => new VinDiscrepancyDto
                {
                    Field = d.Field,
                    DeclaredValue = d.DeclaredValue,
                    VinValue = d.VinValue,
                    Severity = d.Severity.ToString()
                }).ToList()
                : null,
            PhotoModeration = photoModeration?.ImageResults
                .Where(r => r.Status == Domain.Entities.ImageModerationStatus.Rejected)
                .Select(r => new PhotoModerationDto
                {
                    ImageId = r.ImageId,
                    Status = r.Status.ToString(),
                    Flags = r.Flags,
                    RejectionReason = r.RejectionReason,
                    DealerMessage = r.DealerMessage
                }).ToList() is { Count: > 0 } rejectedPhotos ? rejectedPhotos : null
        });
    }

    /// <summary>
    /// Unpublish a vehicle listing (Active -> Archived)
    /// </summary>
    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(typeof(UnpublishVehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnpublishVehicleResponse>> Unpublish(Guid id, [FromBody] UnpublishVehicleRequest? request = null)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        // Ownership check: only the seller or Admin/Moderator can unpublish
        if (!IsOwnerOrAdmin(vehicle))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para despublicar este vehículo." });

        // Only Active or Reserved vehicles can be unpublished
        if (vehicle.Status != VehicleStatus.Active && vehicle.Status != VehicleStatus.Reserved)
        {
            return BadRequest(new
            {
                message = $"Vehicle cannot be unpublished from status '{vehicle.Status}'. Only Active or Reserved vehicles can be unpublished.",
                currentStatus = vehicle.Status.ToString()
            });
        }

        vehicle.Status = VehicleStatus.Archived;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════════
        // OKLA PLATFORM SCORE: Calculate switching cost on unpublish
        // Shows the dealer what accumulated value they're losing.
        // ═══════════════════════════════════════════════════════════════
        var priceHistoryCount = await _context.VehiclePriceHistories
            .Where(ph => ph.VehicleId == id)
            .CountAsync();

        var switchingCost = new SwitchingCostSummary
        {
            DaysAccumulated = vehicle.PublishedAt.HasValue
                ? (int)(DateTime.UtcNow - vehicle.PublishedAt.Value).TotalDays : 0,
            ViewsAccumulated = vehicle.ViewCount,
            InquiriesAccumulated = vehicle.InquiryCount,
            FavoritesAccumulated = vehicle.FavoriteCount,
            PriceHistoryRecords = priceHistoryCount
        };

        _logger.LogInformation(
            "Vehicle unpublished: {VehicleId}, Reason: {Reason}, SwitchingCost: {Days}d/{Views}v/{Inquiries}i",
            id, request?.Reason ?? "Not specified",
            switchingCost.DaysAccumulated, switchingCost.ViewsAccumulated, switchingCost.InquiriesAccumulated);

        // ═══════════════════════════════════════════════════════════════
        // SITEMAP FIX: Trigger ISR revalidation + Google URL_DELETED
        // ═══════════════════════════════════════════════════════════════
        _ = Task.Run(async () =>
        {
            try
            {
                var slug = $"{vehicle.Make}-{vehicle.Model}-{vehicle.Year}-{vehicle.Id}".ToLowerInvariant();
                if (_sitemapRevalidation != null)
                    await _sitemapRevalidation.OnVehicleUnpublishedAsync(slug);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sitemap revalidation failed for unpublished vehicle {VehicleId}", vehicle.Id);
            }
        });

        return Ok(new UnpublishVehicleResponse
        {
            Id = vehicle.Id,
            Status = vehicle.Status,
            UpdatedAt = vehicle.UpdatedAt,
            Message = "Vehicle unpublished successfully. It is no longer visible to buyers.",
            SwitchingCostWarning = switchingCost.HasSignificantCost ? switchingCost.WarningMessage : null,
            AccumulatedDays = switchingCost.DaysAccumulated,
            AccumulatedViews = switchingCost.ViewsAccumulated,
            AccumulatedInquiries = switchingCost.InquiriesAccumulated,
            AccumulatedFavorites = switchingCost.FavoritesAccumulated,
            PriceHistoryRecords = switchingCost.PriceHistoryRecords
        });
    }

    // ========================================
    // MODERATION: APPROVE / REJECT (Staff only)
    // ========================================

    /// <summary>
    /// Approve a vehicle listing (PendingReview -> Active).
    /// Only staff/admin should call this endpoint.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,Moderator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveVehicleRequest? request = null)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Images)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        if (vehicle.Status != VehicleStatus.PendingReview)
        {
            return BadRequest(new
            {
                message = $"Only vehicles in PendingReview can be approved. Current status: '{vehicle.Status}'.",
                currentStatus = vehicle.Status.ToString()
            });
        }

        // Load dynamic config for expiration
        var listingExpirationDays = await _configClient.GetIntAsync("vehicles.listing_expiration_days", 90);

        vehicle.Status = VehicleStatus.Active;
        vehicle.PublishedAt = DateTime.UtcNow;
        vehicle.ApprovedAt = DateTime.UtcNow;
        vehicle.ApprovedBy = request?.ModeratorId;
        vehicle.ModerationNotes = request?.Notes;
        vehicle.UpdatedAt = DateTime.UtcNow;

        var expiresAt = DateTime.UtcNow.AddDays(listingExpirationDays);

        await _context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════════
        // OKLA PLATFORM SCORE: Record initial listing price
        // This is the baseline for the price transparency timeline.
        // ═══════════════════════════════════════════════════════════════
        var initialPriceRecord = new VehiclePriceHistory
        {
            Id = Guid.NewGuid(),
            DealerId = vehicle.DealerId,
            VehicleId = vehicle.Id,
            OldPrice = 0, // First listing — no previous price
            NewPrice = vehicle.Price,
            Currency = vehicle.Currency ?? "DOP",
            ChangedBy = vehicle.SellerId,
            ChangeType = PriceChangeType.InitialListing,
            ChangedAt = DateTime.UtcNow
        };
        _context.VehiclePriceHistories.Add(initialPriceRecord);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "OKLA Platform Score: Initial price {Price} {Currency} recorded for vehicle {VehicleId}",
            vehicle.Price, vehicle.Currency, vehicle.Id);

        // ── Publish VehiclePublishedEvent para alertas y notificaciones ──
        try
        {
            var mainImage = vehicle.Images?.FirstOrDefault(i => i.IsPrimary) ?? vehicle.Images?.FirstOrDefault();
            await _eventPublisher.PublishAsync(new VehiclePublishedEvent
            {
                VehicleId = vehicle.Id,
                SellerId = vehicle.SellerId,
                DealerId = vehicle.DealerId == Guid.Empty ? null : vehicle.DealerId,
                SellerType = vehicle.SellerType.ToString(),
                Title = vehicle.Title,
                Price = vehicle.Price,
                Currency = vehicle.Currency ?? "DOP",
                IsFeatured = vehicle.IsFeatured,
                PublishedAt = vehicle.PublishedAt ?? DateTime.UtcNow,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                BodyType = vehicle.BodyStyle.ToString(),
                FuelType = vehicle.FuelType.ToSpanish(),
                Transmission = vehicle.Transmission.ToString(),
                City = vehicle.City,
                State = vehicle.State,
                Mileage = vehicle.Mileage,
                Condition = vehicle.Condition.ToString(),
                Slug = $"{vehicle.Make}-{vehicle.Model}-{vehicle.Year}-{vehicle.Id}".ToLowerInvariant(),
                ImageUrl = mainImage?.Url
            });

            _logger.LogInformation("VehiclePublishedEvent emitted for {VehicleId}", vehicle.Id);
        }
        catch (Exception ex)
        {
            // Event publishing should not block the approval flow
            _logger.LogWarning(ex, "Failed to publish VehiclePublishedEvent for {VehicleId}. Alert matching may be delayed.", vehicle.Id);
        }

        _logger.LogInformation("Vehicle approved: {VehicleId} by moderator {ModeratorId}", id, request?.ModeratorId);

        // ═══════════════════════════════════════════════════════════════
        // SITEMAP FIX: Trigger ISR revalidation + Google Indexing API
        // ═══════════════════════════════════════════════════════════════
        _ = Task.Run(async () =>
        {
            try
            {
                var slug = $"{vehicle.Make}-{vehicle.Model}-{vehicle.Year}-{vehicle.Id}".ToLowerInvariant();
                if (_sitemapRevalidation != null)
                    await _sitemapRevalidation.OnVehiclePublishedAsync(slug);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sitemap revalidation failed for approved vehicle {VehicleId}", vehicle.Id);
            }
        });

        return Ok(new
        {
            id = vehicle.Id,
            status = vehicle.Status.ToString(),
            publishedAt = vehicle.PublishedAt,
            expiresAt,
            message = "Vehicle approved and now visible to buyers."
        });
    }

    /// <summary>
    /// Reject a vehicle listing (PendingReview -> Rejected).
    /// Only staff/admin should call this endpoint.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,Moderator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectVehicleRequest request)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        if (vehicle.Status != VehicleStatus.PendingReview)
        {
            return BadRequest(new
            {
                message = $"Only vehicles in PendingReview can be rejected. Current status: '{vehicle.Status}'.",
                currentStatus = vehicle.Status.ToString()
            });
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "A rejection reason is required." });

        vehicle.Status = VehicleStatus.Rejected;
        vehicle.RejectedAt = DateTime.UtcNow;
        vehicle.RejectedBy = request.ModeratorId;
        vehicle.RejectionReason = request.Reason;
        vehicle.ModerationNotes = request.Notes;
        vehicle.RejectionCount++;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle rejected: {VehicleId} by moderator {ModeratorId}, reason: {Reason}",
            id, request.ModeratorId, request.Reason);

        return Ok(new
        {
            id = vehicle.Id,
            status = vehicle.Status.ToString(),
            rejectedAt = vehicle.RejectedAt,
            reason = vehicle.RejectionReason,
            rejectionCount = vehicle.RejectionCount,
            message = "Vehicle rejected. Seller will be notified."
        });
    }

    /// <summary>
    /// Get vehicles pending review (moderation queue).
    /// Returns only PendingReview vehicles, ordered by submission date (oldest first).
    /// Secured: requires Admin or Moderator role. Pod-to-pod calls must forward JWT.
    /// </summary>
    [HttpGet("moderation/queue")]
    [Authorize(Roles = "Admin,Moderator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModerationQueue([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _context.Vehicles
            .Include(v => v.Images)
            .Where(v => !v.IsDeleted && v.Status == VehicleStatus.PendingReview)
            .OrderBy(v => v.SubmittedForReviewAt ?? v.CreatedAt);

        var totalCount = await query.CountAsync();
        var vehicles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            vehicles = vehicles.Select(v => new
            {
                id = v.Id,
                title = v.Title,
                slug = GenerateSlug(v),
                make = v.Make,
                model = v.Model,
                year = v.Year,
                price = v.Price,
                currency = v.Currency,
                condition = v.Condition.ToString(),
                sellerName = v.SellerName,
                sellerType = v.SellerType.ToString(),
                imageUrl = v.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url,
                imageCount = v.Images.Count,
                submittedAt = v.SubmittedForReviewAt,
                rejectionCount = v.RejectionCount,
                city = v.City,
                state = v.State
            }),
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    /// <summary>
    /// Admin search: returns vehicles of ANY status (not just Active).
    /// Used by AdminService to populate the "Todos" tab in the admin panel.
    /// </summary>
    [HttpGet("admin/search")]
    [Authorize(Roles = "Admin,Moderator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AdminSearch(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? sellerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Vehicles
            .Include(v => v.Images)
            .Where(v => !v.IsDeleted);

        // Optional status filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<VehicleStatus>(status, ignoreCase: true, out var parsedStatus))
                query = query.Where(v => v.Status == parsedStatus);
        }

        // Optional seller filter
        if (!string.IsNullOrWhiteSpace(sellerId) && Guid.TryParse(sellerId, out var sellerGuid))
            query = query.Where(v => v.SellerId == sellerGuid);

        // Optional text search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(v =>
                v.Title.ToLower().Contains(term) ||
                v.Make.ToLower().Contains(term) ||
                v.Model.ToLower().Contains(term) ||
                (v.SellerName != null && v.SellerName.ToLower().Contains(term)));
        }

        query = query.OrderByDescending(v => v.CreatedAt);

        var totalCount = await query.CountAsync();
        var vehicles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            vehicles = vehicles.Select(v => new
            {
                id = v.Id,
                title = v.Title,
                slug = GenerateSlug(v),
                make = v.Make,
                model = v.Model,
                year = v.Year,
                price = v.Price,
                currency = v.Currency,
                status = v.Status.ToString(),
                isFeatured = v.IsFeatured,
                sellerId = v.SellerId,
                sellerName = v.SellerName,
                sellerType = v.SellerType.ToString(),
                viewCount = v.ViewCount,
                leadCount = v.InquiryCount,
                images = v.Images.OrderBy(i => i.SortOrder).Select(i => new
                {
                    url = i.Url,
                    isPrimary = i.IsPrimary
                }),
                createdAt = v.CreatedAt,
                publishedAt = v.PublishedAt,
                rejectionReason = v.RejectionReason,
                submittedAt = v.SubmittedForReviewAt,
                city = v.City,
                state = v.State
            }),
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    /// <summary>
    /// Mark a vehicle as sold
    /// </summary>
    [HttpPost("{id:guid}/sold")]
    [ProducesResponseType(typeof(MarkVehicleSoldResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarkVehicleSoldResponse>> MarkAsSold(Guid id, [FromBody] MarkVehicleSoldRequest? request = null)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        // Ownership check: only the seller or Admin/Moderator can mark as sold
        if (!IsOwnerOrAdmin(vehicle))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para marcar este vehículo como vendido." });

        // Only Active or Reserved vehicles can be marked as sold
        if (vehicle.Status != VehicleStatus.Active && vehicle.Status != VehicleStatus.Reserved)
        {
            return BadRequest(new
            {
                message = $"Vehicle cannot be marked as sold from status '{vehicle.Status}'. Only Active or Reserved vehicles can be sold.",
                currentStatus = vehicle.Status.ToString()
            });
        }

        var listedPrice = vehicle.Price;
        vehicle.Status = VehicleStatus.Sold;
        vehicle.SoldAt = DateTime.UtcNow;
        vehicle.UpdatedAt = DateTime.UtcNow;

        // Update price if sale price provided
        if (request?.SalePrice.HasValue == true)
        {
            vehicle.Price = request.SalePrice.Value;
        }

        // Create SaleTransaction for analytics and tracking
        var transaction = new SaleTransaction
        {
            VehicleId = vehicle.Id,
            SellerId = vehicle.SellerId,
            SellerType = vehicle.SellerType.ToString(),
            BuyerEmail = request?.BuyerEmail,
            ListedPrice = listedPrice,
            SalePrice = request?.SalePrice ?? vehicle.Price,
            Currency = "DOP",
            VehicleTitle = $"{vehicle.Make} {vehicle.Model} {vehicle.Year}",
            Vin = vehicle.VIN,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            ListedAt = vehicle.CreatedAt,
            SoldAt = vehicle.SoldAt!.Value,
            ConfidenceLevel = SaleConfidenceLevel.Medium,
            FraudScore = 100,
            Notes = request?.Notes,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
        };

        _context.SaleTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Publish VehicleSoldEvent for downstream services (DealerMgmt, Notifications, Reviews)
        try
        {
            var soldEvent = new VehicleSoldEvent
            {
                VehicleId = vehicle.Id,
                SellerId = vehicle.SellerId,
                SalePrice = request?.SalePrice ?? vehicle.Price,
                ListedPrice = listedPrice,
                SoldAt = vehicle.SoldAt!.Value,
                BuyerEmail = request?.BuyerEmail,
                VehicleTitle = $"{vehicle.Make} {vehicle.Model} {vehicle.Year}"
            };
            await _eventPublisher.PublishAsync(soldEvent);
            _logger.LogInformation("VehicleSoldEvent published for vehicle {VehicleId}, transaction {TransactionId}", id, transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish VehicleSoldEvent for vehicle {VehicleId} — sale still recorded", id);
        }

        _logger.LogInformation("Vehicle marked as sold: {VehicleId}, SalePrice: {SalePrice}, TransactionId: {TransactionId}",
            id, request?.SalePrice, transaction.Id);

        return Ok(new MarkVehicleSoldResponse
        {
            Id = vehicle.Id,
            Status = vehicle.Status,
            SoldAt = vehicle.SoldAt.Value,
            SalePrice = request?.SalePrice,
            Message = "Vehicle marked as sold successfully."
        });
    }

    /// <summary>
    /// Feature or unfeature a vehicle (Admin only)
    /// </summary>
    [HttpPost("{id:guid}/feature")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(FeatureVehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeatureVehicleResponse>> Feature(Guid id, [FromBody] FeatureVehicleRequest request)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        vehicle.IsFeatured = request.IsFeatured;
        vehicle.UpdatedAt = DateTime.UtcNow;

        // Update homepage sections if provided
        if (request.HomepageSections.HasValue)
        {
            vehicle.HomepageSections = request.HomepageSections.Value;
        }
        else if (request.IsFeatured && vehicle.HomepageSections == HomepageSection.None)
        {
            // Auto-add to Destacados if featuring and no sections set
            vehicle.HomepageSections = HomepageSection.Destacados;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle feature status changed: {VehicleId}, IsFeatured: {IsFeatured}, Sections: {Sections}",
            id, request.IsFeatured, vehicle.HomepageSections);

        return Ok(new FeatureVehicleResponse
        {
            Id = vehicle.Id,
            IsFeatured = vehicle.IsFeatured,
            HomepageSections = vehicle.HomepageSections,
            Message = request.IsFeatured
                ? "Vehicle featured successfully."
                : "Vehicle unfeatured successfully."
        });
    }

    /// <summary>
    /// Register a view for a vehicle
    /// </summary>
    [HttpPost("{id:guid}/views")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterViewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegisterViewResponse>> RegisterView(Guid id, [FromBody] RegisterViewRequest? request = null)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        // Increment view count
        vehicle.ViewCount++;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogDebug("View registered for vehicle: {VehicleId}, TotalViews: {ViewCount}, UserId: {UserId}",
            id, vehicle.ViewCount, request?.UserId);

        return Ok(new RegisterViewResponse
        {
            VehicleId = vehicle.Id,
            TotalViews = vehicle.ViewCount,
            Message = "View registered successfully."
        });
    }

    /// <summary>
    /// Add images to an existing vehicle
    /// </summary>
    [HttpPost("{id:guid}/images")]
    public async Task<ActionResult<List<VehicleImage>>> AddImages(Guid id, [FromBody] AddVehicleImagesRequest request)
    {
        // BUG-D003 fix: use AsNoTracking() so the Vehicle entity is NOT tracked and its
        // ConcurrencyStamp does not participate in SaveChangesAsync, preventing
        // DbUpdateConcurrencyException when only VehicleImage rows are being inserted.
        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        // Enforce max images per listing from ConfigurationService
        var maxImages = await _configClient.GetIntAsync("vehicles.max_images_per_listing", 30);
        var existingImageCount = await _context.VehicleImages.CountAsync(i => i.VehicleId == id);
        var totalAfterAdd = existingImageCount + request.Images.Count;
        if (totalAfterAdd > maxImages)
            return BadRequest(new { message = $"Cannot exceed {maxImages} images per listing. Current: {existingImageCount}, Adding: {request.Images.Count}" });

        var existingMaxOrder = await _context.VehicleImages
            .Where(i => i.VehicleId == id)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync() ?? -1;

        var addedImages = new List<VehicleImage>();

        foreach (var imageDto in request.Images)
        {
            existingMaxOrder++;
            var image = new VehicleImage
            {
                Id = Guid.NewGuid(),
                VehicleId = id,
                DealerId = vehicle.DealerId,
                Url = imageDto.Url,
                ThumbnailUrl = imageDto.ThumbnailUrl ?? imageDto.Url.Replace("/800/", "/200/").Replace("/600", "/150"),
                Caption = imageDto.Caption,
                ImageType = imageDto.ImageType ?? ImageType.Exterior,
                SortOrder = imageDto.SortOrder ?? existingMaxOrder,
                IsPrimary = imageDto.IsPrimary ?? (existingImageCount == 0 && existingMaxOrder == 0),
                MimeType = imageDto.MimeType ?? "image/jpeg",
                CreatedAt = DateTime.UtcNow
            };

            // Add directly to the DbSet to avoid Vehicle entity tracking
            _context.VehicleImages.Add(image);
            addedImages.Add(image);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Added {Count} images to vehicle {VehicleId}", addedImages.Count, id);

        // Invalidate vehicle cache so subsequent GET requests reflect the new images immediately
        await InvalidateVehicleCacheAsync(id);

        return Ok(addedImages);
    }

    /// <summary>
    /// Bulk add images to multiple vehicles (Admin only — for seeding)
    /// </summary>
    [HttpPost("bulk-images")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BulkAddImagesResponse>> BulkAddImages([FromBody] BulkAddVehicleImagesRequest request)
    {
        var vehicleIds = request.VehicleImages.Select(v => v.VehicleId).Distinct().ToList();
        var vehicles = await _context.Vehicles
            .Include(v => v.Images)
            .Where(v => vehicleIds.Contains(v.Id))
            .ToListAsync();

        var vehicleDict = vehicles.ToDictionary(v => v.Id);
        var totalAdded = 0;
        var errors = new List<string>();

        foreach (var vehicleImages in request.VehicleImages)
        {
            if (!vehicleDict.TryGetValue(vehicleImages.VehicleId, out var vehicle))
            {
                errors.Add($"Vehicle {vehicleImages.VehicleId} not found");
                continue;
            }

            var existingMaxOrder = vehicle.Images.Any() ? vehicle.Images.Max(i => i.SortOrder) : -1;

            foreach (var imageDto in vehicleImages.Images)
            {
                existingMaxOrder++;
                var image = new VehicleImage
                {
                    Id = Guid.NewGuid(),
                    VehicleId = vehicleImages.VehicleId,
                    DealerId = vehicle.DealerId,
                    Url = imageDto.Url,
                    ThumbnailUrl = imageDto.ThumbnailUrl ?? imageDto.Url.Replace("/800/", "/200/").Replace("/600", "/150"),
                    Caption = imageDto.Caption,
                    ImageType = imageDto.ImageType ?? ImageType.Exterior,
                    SortOrder = imageDto.SortOrder ?? existingMaxOrder,
                    IsPrimary = imageDto.IsPrimary ?? (!vehicle.Images.Any() && existingMaxOrder == 0),
                    MimeType = imageDto.MimeType ?? "image/jpeg",
                    CreatedAt = DateTime.UtcNow
                };

                vehicle.Images.Add(image);
                totalAdded++;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk added {Count} images to {VehicleCount} vehicles", totalAdded, vehicleIds.Count);

        return Ok(new BulkAddImagesResponse
        {
            TotalImagesAdded = totalAdded,
            VehiclesUpdated = vehicleIds.Count - errors.Count,
            Errors = errors
        });
    }

    /// <summary>
    /// Delete a specific image from a vehicle.
    /// Caller must be the vehicle's seller or an Admin.
    /// If the deleted image was the primary, the next image (by SortOrder) is promoted to primary.
    /// </summary>
    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    public async Task<ActionResult> DeleteImage(Guid id, Guid imageId)
    {
        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        // Authorization: caller must be the seller or an Admin
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin)
        {
            if (!Guid.TryParse(userIdClaim, out var callerId) || callerId != vehicle.SellerId)
                return Forbid();
        }

        var image = await _context.VehicleImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.VehicleId == id);

        if (image == null)
            return NotFound(new { message = "Image not found" });

        var wasPrimary = image.IsPrimary;
        _context.VehicleImages.Remove(image);
        await _context.SaveChangesAsync();

        // If the deleted image was primary, promote the next one
        if (wasPrimary)
        {
            var nextImage = await _context.VehicleImages
                .Where(i => i.VehicleId == id)
                .OrderBy(i => i.SortOrder)
                .FirstOrDefaultAsync();

            if (nextImage != null)
            {
                nextImage.IsPrimary = true;
                await _context.SaveChangesAsync();
            }
        }

        await InvalidateVehicleCacheAsync(id);

        _logger.LogInformation("Deleted image {ImageId} from vehicle {VehicleId}", imageId, id);
        return NoContent();
    }

    // Helper method to generate a URL-friendly slug from vehicle data
    private static string GenerateSlug(Vehicle vehicle)
    {
        // Generate slug like "2024-toyota-camry-{shortId}"
        var baseSlug = $"{vehicle.Year}-{vehicle.Make}-{vehicle.Model}"
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-");

        // Add short ID to ensure uniqueness
        var shortId = vehicle.Id.ToString("N")[..8];
        return $"{baseSlug}-{shortId}";
    }

    /// <summary>
    /// Invalidate all cache keys related to a vehicle after mutation (update/delete/publish/etc.)
    /// </summary>
    private async Task InvalidateVehicleCacheAsync(Guid vehicleId)
    {
        await _cache.RemoveAsync($"vehicle:detail:{vehicleId}");
        // Also invalidate featured list since the vehicle may have changed featured status
        await _cache.InvalidateByPatternAsync("vehicle:featured:*");
    }

    // ========================================
    // OWNERSHIP HELPERS
    // ========================================

    /// <summary>
    /// Extracts the authenticated user's ID from JWT claims.
    /// Returns null if no valid claim is found (should not happen on [Authorize] endpoints).
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value
                       ?? User.FindFirst("userId")?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            return userId;

        return null;
    }

    /// <summary>
    /// Checks whether the current user is the vehicle owner OR has Admin/Moderator role.
    /// Returns true if authorized, false otherwise.
    /// </summary>
    private bool IsOwnerOrAdmin(Vehicle vehicle)
    {
        // Admins/Moderators can operate on any vehicle
        if (User.IsInRole("Admin") || User.IsInRole("Moderator"))
            return true;

        var userId = GetCurrentUserId();
        return userId.HasValue && vehicle.SellerId == userId.Value;
    }

    /// <summary>
    /// Checks whether the current user has Admin or Moderator role.
    /// </summary>
    private bool IsAdmin()
    {
        return User.IsInRole("Admin") || User.IsInRole("Moderator")
            || User.IsInRole("admin") || User.IsInRole("moderator");
    }
}

#region Request/Response DTOs

public record VehicleSearchRequest
{
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? MinYear { get; init; }
    public int? MaxYear { get; init; }
    public int? MaxMileage { get; init; }
    public VehicleType? VehicleType { get; init; }
    public BodyStyle? BodyStyle { get; init; }
    public FuelType? FuelType { get; init; }
    public TransmissionType? Transmission { get; init; }
    public Entities.DriveType? DriveType { get; init; }
    public VehicleCondition? Condition { get; init; }
    public string? ExteriorColor { get; init; }
    public string? State { get; init; }
    public string? City { get; init; }
    public string? ZipCode { get; init; }
    public bool? IsCertified { get; init; }
    public bool? HasCleanTitle { get; init; }
    /// <summary>Filter by seller type: Seller (particular) or Dealer (concesionario)</summary>
    public SellerType? SellerType { get; init; }

    // Extended DR-market filters
    /// <summary>Minimum number of seats (5 = family SUV, 7 = 7-seater)</summary>
    public int? MinSeats { get; init; }
    /// <summary>Cylinders (3, 4, 6, 8)</summary>
    public int? Cylinders { get; init; }
    /// <summary>Interior color</summary>
    public string? InteriorColor { get; init; }
    /// <summary>Comma-separated list of required features (A/C, GPS, Sunroof...)</summary>
    public string? Features { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool? SortDescending { get; init; }
}

public record VehicleSearchResult
{
    public IEnumerable<Vehicle> Vehicles { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public record CreateVehicleRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string? Currency { get; init; }
    public string? VIN { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string? Trim { get; init; }
    public int Year { get; init; }
    public int Mileage { get; init; }
    public MileageUnit? MileageUnit { get; init; }
    public VehicleType VehicleType { get; init; }
    /// <summary>BodyStyle enum (accepts string enum values)</summary>
    public BodyStyle? BodyStyle { get; init; }
    /// <summary>Alias for BodyStyle used by frontend (sedan, suv, pickup, etc.)</summary>
    public string? BodyType { get; init; }
    public int? Doors { get; init; }
    public int? Seats { get; init; }
    public FuelType? FuelType { get; init; }
    public TransmissionType? Transmission { get; init; }
    public Entities.DriveType? DriveType { get; init; }
    public string? EngineSize { get; init; }
    public int? Cylinders { get; init; }
    public int? Horsepower { get; init; }
    public string? ExteriorColor { get; init; }
    public string? InteriorColor { get; init; }
    public VehicleCondition Condition { get; init; }
    public bool? IsCertified { get; init; }
    public bool? HasCleanTitle { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    /// <summary>Alias for State used by frontend</summary>
    public string? Province { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
    public Guid? SellerId { get; init; }
    public string? SellerName { get; init; }
    public string? SellerPhone { get; init; }
    public string? SellerEmail { get; init; }
    public string? SellerWhatsApp { get; init; }
    public Guid? DealerId { get; init; }
    public Guid? CategoryId { get; init; }
    /// <summary>Legacy: list of image URLs as strings</summary>
    public List<string>? Images { get; init; }
    /// <summary>New: list of image objects with url, order, isPrimary</summary>
    public List<CreateVehicleImageDto>? ImageObjects { get; init; }
    /// <summary>Features as JSON string (e.g. ["GPS","Sunroof"])</summary>
    public string? FeaturesJson { get; init; }
    /// <summary>Features as list (alternative to FeaturesJson)</summary>
    public List<string>? Features { get; init; }
    public HomepageSection? HomepageSections { get; init; }
}

public record CreateVehicleImageDto
{
    public required string Url { get; init; }
    public string? ThumbnailUrl { get; init; }
    public int? SortOrder { get; init; }
    public bool? IsPrimary { get; init; }
    public string? Alt { get; init; }
}

public record CreateVehicleResult
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public VehicleStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record UpdateVehicleRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public VehicleStatus? Status { get; init; }
    public VehicleType? VehicleType { get; init; }
    public BodyStyle? BodyStyle { get; init; }
    public FuelType? FuelType { get; init; }
    public TransmissionType? Transmission { get; init; }
    public Entities.DriveType? DriveType { get; init; }
    public int? Mileage { get; init; }
    public string? ExteriorColor { get; init; }
    public string? InteriorColor { get; init; }
    public VehicleCondition? Condition { get; init; }
    public bool? IsCertified { get; init; }
    public bool? HasCleanTitle { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public bool? IsFeatured { get; init; }

    /// <summary>
    /// Secciones del homepage donde mostrar este vehículo.
    /// Valores: None=0, Carousel=1, Sedanes=2, SUVs=4, Camionetas=8, Deportivos=16, Destacados=32, Lujo=64
    /// Se pueden combinar sumando los valores (ej: Carousel + Destacados = 33)
    /// </summary>
    public HomepageSection? HomepageSections { get; init; }
}

public record CompareVehiclesRequest
{
    public List<Guid> VehicleIds { get; init; } = new();
}

// Image DTOs for adding images to vehicles
public record VehicleImageDto
{
    public required string Url { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? Caption { get; init; }
    public ImageType? ImageType { get; init; }
    public int? SortOrder { get; init; }
    public bool? IsPrimary { get; init; }
    public string? MimeType { get; init; }
}

public record AddVehicleImagesRequest
{
    public List<VehicleImageDto> Images { get; init; } = new();
}

public record VehicleImagesEntry
{
    public Guid VehicleId { get; init; }
    public List<VehicleImageDto> Images { get; init; } = new();
}

public record BulkAddVehicleImagesRequest
{
    public List<VehicleImagesEntry> VehicleImages { get; init; } = new();
}

public record BulkAddImagesResponse
{
    public int TotalImagesAdded { get; init; }
    public int VehiclesUpdated { get; init; }
    public List<string> Errors { get; init; } = new();
}

// ========================================
// Publish/Unpublish/Sold/Feature/Views Requests
// ========================================

public record PublishVehicleRequest
{
    /// <summary>
    /// Optional: Set expiration date for the listing
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// REQUIRED — Dealer/seller must confirm information accuracy.
    /// This checkbox must NOT be pre-checked; it must be manually activated.
    /// Legal requirement per Ley 172-13 and OKLA Terms of Service.
    /// </summary>
    public bool DisclaimerAccepted { get; init; } = false;

    /// <summary>
    /// Version of the Terms of Service accepted (e.g., "2026.1").
    /// </summary>
    public string? TosVersion { get; init; }
}

public record PublishVehicleResponse
{
    public Guid Id { get; init; }
    public VehicleStatus Status { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string Message { get; init; } = string.Empty;
    /// <summary>
    /// VIN cross-reference discrepancies (null if no VIN or no discrepancies).
    /// Present when NHTSA-decoded data doesn't match dealer-declared attributes.
    /// </summary>
    public List<VinDiscrepancyDto>? VinDiscrepancies { get; init; }
    /// <summary>
    /// Photo moderation results (null if no images). Each rejected image
    /// includes a dealer-facing message explaining the rejection reason.
    /// </summary>
    public List<PhotoModerationDto>? PhotoModeration { get; init; }
}

public record PhotoModerationDto
{
    public Guid ImageId { get; init; }
    public string Status { get; init; } = string.Empty;
    public List<string> Flags { get; init; } = new();
    public string? RejectionReason { get; init; }
    public string? DealerMessage { get; init; }
}

public record VinDiscrepancyDto
{
    public string Field { get; init; } = string.Empty;
    public string DeclaredValue { get; init; } = string.Empty;
    public string VinValue { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
}

public record UnpublishVehicleRequest
{
    /// <summary>
    /// Optional: Reason for unpublishing
    /// </summary>
    public string? Reason { get; init; }
}

public record UnpublishVehicleResponse
{
    public Guid Id { get; init; }
    public VehicleStatus Status { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string Message { get; init; } = string.Empty;

    // ── OKLA Platform Score: Switching Cost Data ──
    /// <summary>Warning message showing what the dealer is losing (null if no significant history)</summary>
    public string? SwitchingCostWarning { get; init; }
    /// <summary>Days the vehicle was published on OKLA</summary>
    public int AccumulatedDays { get; init; }
    /// <summary>Total views accumulated</summary>
    public int AccumulatedViews { get; init; }
    /// <summary>Total inquiries accumulated</summary>
    public int AccumulatedInquiries { get; init; }
    /// <summary>Total favorites accumulated</summary>
    public int AccumulatedFavorites { get; init; }
    /// <summary>Number of price change records in history</summary>
    public int PriceHistoryRecords { get; init; }
}

public record MarkVehicleSoldRequest
{
    /// <summary>
    /// Optional: Final sale price
    /// </summary>
    public decimal? SalePrice { get; init; }

    /// <summary>
    /// Optional: Buyer email for sale confirmation tracking
    /// </summary>
    public string? BuyerEmail { get; init; }

    /// <summary>
    /// Optional: Buyer notes
    /// </summary>
    public string? Notes { get; init; }
}

public record MarkVehicleSoldResponse
{
    public Guid Id { get; init; }
    public VehicleStatus Status { get; init; }
    public DateTime SoldAt { get; init; }
    public decimal? SalePrice { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record FeatureVehicleRequest
{
    /// <summary>
    /// True to feature, false to unfeature
    /// </summary>
    public bool IsFeatured { get; init; }

    /// <summary>
    /// Optional: Homepage sections to display the vehicle in
    /// </summary>
    public HomepageSection? HomepageSections { get; init; }
}

public record FeatureVehicleResponse
{
    public Guid Id { get; init; }
    public bool IsFeatured { get; init; }
    public HomepageSection HomepageSections { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record RegisterViewRequest
{
    /// <summary>
    /// Optional: User ID if authenticated
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Optional: Session ID for anonymous users
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Optional: Referrer URL
    /// </summary>
    public string? Referrer { get; init; }
}

public record RegisterViewResponse
{
    public Guid VehicleId { get; init; }
    public int TotalViews { get; init; }
    public string Message { get; init; } = string.Empty;
}

// ========================================
// Seller Vehicles DTOs
// ========================================

public record SellerVehicleDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "DOP";
    public string Status { get; init; } = string.Empty;
    public string? MainImageUrl { get; init; }
    public int Year { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Mileage { get; init; }
    public string? Transmission { get; init; }
    public string? FuelType { get; init; }
    public int Views { get; init; }
    public int Favorites { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SellerVehiclesResponse
{
    public List<SellerVehicleDto> Data { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public record SellerVehicleStats
{
    public int TotalListings { get; init; }
    public int ActiveListings { get; init; }
    public int SoldListings { get; init; }
    public int PendingListings { get; init; }
    public int TotalViews { get; init; }
    public int TotalFavorites { get; init; }
}

// ========================================
// Moderation DTOs
// ========================================

public record ApproveVehicleRequest
{
    public Guid? ModeratorId { get; init; }
    public string? Notes { get; init; }
}

public record RejectVehicleRequest
{
    public Guid? ModeratorId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

/// <summary>
/// Request to suspend a vehicle listing due to buyer reports.
/// Called by ReportsService when report count reaches auto-suspend threshold.
/// </summary>
public record SuspendVehicleRequest
{
    public string Reason { get; init; } = string.Empty;
    public int ReportCount { get; init; }
}

#endregion

// ========================================
// DEALER SWITCHING COST CONTROLLER
// Shows dealer what they'd lose if they cancel their OKLA account.
// Reviews, badges, reputation, engagement — all non-portable.
// ========================================

[ApiController]
[Route("api/dealer")]
[Authorize]
public class DealerSwitchingCostController : ControllerBase
{
    private readonly VehiclesSaleService.Infrastructure.Persistence.ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DealerSwitchingCostController> _logger;

    public DealerSwitchingCostController(
        VehiclesSaleService.Infrastructure.Persistence.ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<DealerSwitchingCostController> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Get the complete switching cost summary for a dealer.
    /// Shows everything they'd lose by cancelling their OKLA account:
    /// reviews, badges, views, inquiries, favorites, sales, platform score.
    /// 
    /// Used in:
    /// - Dealer dashboard ("Your OKLA Value" widget)
    /// - Account cancellation confirmation screen
    /// - Retention emails and campaigns
    /// </summary>
    [HttpGet("{dealerId:guid}/switching-cost")]
    [ProducesResponseType(typeof(DealerSwitchingCostSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<DealerSwitchingCostSummary>> GetDealerSwitchingCost(Guid dealerId)
    {
        // Vehicle-level metrics aggregation
        var vehicleStats = await _context.Vehicles
            .Where(v => v.DealerId == dealerId && !v.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                ActiveVehicles = g.Count(v => v.Status == VehicleStatus.Active),
                TotalVehicles = g.Count(),
                TotalViews = g.Sum(v => v.ViewCount),
                TotalInquiries = g.Sum(v => v.InquiryCount),
                TotalFavorites = g.Sum(v => v.FavoriteCount),
                AvgSellerRating = g.Average(v => (decimal?)v.SellerRating) ?? 0,
                AvgSellerReviewCount = (int)(g.Average(v => (decimal?)v.SellerReviewCount) ?? 0)
            })
            .FirstOrDefaultAsync();

        var priceHistoryCount = await _context.VehiclePriceHistories
            .Where(ph => ph.DealerId == dealerId)
            .CountAsync();

        var leadCount = await _context.Leads
            .Where(l => l.DealerId == dealerId)
            .CountAsync();

        var salesCount = await _context.SaleTransactions
            .Where(s => s.SellerId == dealerId || s.SellerType == "Dealer")
            .CountAsync();

        // Try to get review data from ReviewService
        int totalReviews = 0;
        decimal averageRating = 0;
        int verifiedReviews = 0;
        int totalBadges = 0;
        var activeBadges = new List<string>();

        try
        {
            var reviewClient = _httpClientFactory.CreateClient("ReviewService");
            var summaryResponse = await reviewClient.GetAsync($"/api/reviews/seller/{dealerId}/summary");
            if (summaryResponse.IsSuccessStatusCode)
            {
                var summaryJson = await summaryResponse.Content.ReadFromJsonAsync<JsonElement>();
                totalReviews = summaryJson.TryGetProperty("totalReviews", out var tr) ? tr.GetInt32() : 0;
                averageRating = summaryJson.TryGetProperty("averageRating", out var ar) ? ar.GetDecimal() : 0;
                verifiedReviews = summaryJson.TryGetProperty("verifiedPurchaseReviews", out var vr) ? vr.GetInt32() : 0;
            }

            var badgesResponse = await reviewClient.GetAsync($"/api/reviews/seller/{dealerId}/badges");
            if (badgesResponse.IsSuccessStatusCode)
            {
                var badgesJson = await badgesResponse.Content.ReadFromJsonAsync<JsonElement>();
                if (badgesJson.ValueKind == JsonValueKind.Array)
                {
                    totalBadges = badgesJson.GetArrayLength();
                    foreach (var badge in badgesJson.EnumerateArray().Take(5))
                    {
                        if (badge.TryGetProperty("title", out var title))
                            activeBadges.Add(title.GetString() ?? "Badge");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch review data from ReviewService for dealer {DealerId}", dealerId);
            // Fallback to vehicle entity data
            totalReviews = vehicleStats?.AvgSellerReviewCount ?? 0;
            averageRating = vehicleStats?.AvgSellerRating ?? 0;
        }

        var summary = new DealerSwitchingCostSummary
        {
            DealerId = dealerId,
            TotalReviews = totalReviews,
            VerifiedPurchaseReviews = verifiedReviews,
            AverageRating = averageRating,
            TotalBadges = totalBadges,
            ActiveBadges = activeBadges,
            ActiveVehicles = vehicleStats?.ActiveVehicles ?? 0,
            TotalVehiclesEverPublished = vehicleStats?.TotalVehicles ?? 0,
            TotalViewsAcrossVehicles = vehicleStats?.TotalViews ?? 0,
            TotalInquiriesAcrossVehicles = vehicleStats?.TotalInquiries ?? 0,
            TotalFavoritesAcrossVehicles = vehicleStats?.TotalFavorites ?? 0,
            TotalPriceHistoryRecords = priceHistoryCount,
            TotalLeadsReceived = leadCount,
            TotalSalesCompleted = salesCount,
        };

        _logger.LogInformation(
            "Dealer {DealerId} switching cost queried: {Reviews} reviews, {Views} views, {Sales} sales, Retention={Level}",
            dealerId, summary.TotalReviews, summary.TotalViewsAcrossVehicles,
            summary.TotalSalesCompleted, summary.RetentionPriority);

        return Ok(summary);
    }
}

// ========================================
// SEO AUDIT FIX: Sitemap endpoint for dynamic sitemap generation
// ========================================

/// <summary>
/// Sitemap controller — public, no auth required.
/// Returns lightweight vehicle data for sitemap.xml generation.
/// 
/// SEO AUDIT: The frontend sitemap.ts was calling /api/vehicles/sitemap
/// but this endpoint didn't exist, resulting in zero dynamic vehicle URLs
/// in the sitemap. Google was only indexing static pages.
/// </summary>
[ApiController]
[Route("api/vehicles")]
public class VehicleSitemapController : ControllerBase
{
    private readonly VehiclesSaleService.Infrastructure.Persistence.ApplicationDbContext _context;
    private readonly ILogger<VehicleSitemapController> _logger;

    public VehicleSitemapController(
        VehiclesSaleService.Infrastructure.Persistence.ApplicationDbContext context,
        ILogger<VehicleSitemapController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all active vehicles for sitemap generation.
    /// Returns slug and updatedAt for each vehicle.
    /// No pagination — sitemaps need all URLs.
    /// Public endpoint — no auth required.
    /// </summary>
    [HttpGet("sitemap")]
    [AllowAnonymous]
    [ResponseCache(Duration = 300)] // SITEMAP FIX: Reduced from 3600→300s (5min) to match ISR revalidation cycle
    public async Task<IActionResult> GetSitemapData(CancellationToken ct)
    {
        try
        {
            var vehicles = await _context.Vehicles
                .Where(v => v.Status == Entities.VehicleStatus.Active)
                .OrderByDescending(v => v.UpdatedAt)
                .Select(v => new
                {
                    v.Id,
                    v.Year,
                    v.Make,
                    v.Model,
                    v.UpdatedAt,
                    v.CreatedAt,
                    v.IsFeatured,
                })
                .AsNoTracking()
                .ToListAsync(ct);

            var sitemapItems = vehicles.Select(v =>
            {
                var baseSlug = $"{v.Year}-{v.Make}-{v.Model}"
                    .ToLowerInvariant()
                    .Replace(" ", "-")
                    .Replace("--", "-");
                var shortId = v.Id.ToString("N")[..8];

                return new
                {
                    slug = $"{baseSlug}-{shortId}",
                    updatedAt = v.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    priority = v.IsFeatured ? 0.9 : 0.7,
                };
            });

            _logger.LogInformation("Sitemap endpoint served {Count} vehicle URLs", vehicles.Count);

            return Ok(new
            {
                items = sitemapItems,
                total = vehicles.Count,
                generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate sitemap data");
            return StatusCode(500, new { error = "Failed to generate sitemap data" });
        }
    }
}

// ========================================
// OKLA PLATFORM SCORE — DTOs
// ========================================

/// <summary>
/// Response for the price history endpoint — shows full price change timeline
/// </summary>
public record VehiclePriceHistoryResponse
{
    public Guid VehicleId { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal OriginalListingPrice { get; init; }
    public int TotalPriceChanges { get; init; }
    public decimal TotalVariationPercent { get; init; }
    public string PriceTrend { get; init; } = "Stable";
    public string Currency { get; init; } = "DOP";
    public List<PriceChangeRecord> History { get; init; } = new();
}

/// <summary>
/// Individual price change record visible to the buyer
/// </summary>
public record PriceChangeRecord
{
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public decimal PriceDifference { get; init; }
    public decimal ChangePercentage { get; init; }
    public string Currency { get; init; } = "DOP";
    public string ChangeType { get; init; } = "Manual";
    public DateTime ChangedAt { get; init; }
    public string? Reason { get; init; }
}
