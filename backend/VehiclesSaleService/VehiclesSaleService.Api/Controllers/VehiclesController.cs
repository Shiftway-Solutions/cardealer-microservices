using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VehiclesSaleService.Domain.Entities;
using VehiclesSaleService.Domain.Interfaces;
using VehiclesSaleService.Infrastructure.Messaging;
using VehiclesSaleService.Infrastructure.Persistence;
using CarDealer.Contracts.Events.Vehicle;
using CarDealer.Shared.Configuration;
using Entities = VehiclesSaleService.Domain.Entities;

namespace VehiclesSaleService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<VehiclesController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IConfigurationServiceClient _configClient;

    public VehiclesController(
        IVehicleRepository vehicleRepository,
        ICategoryRepository categoryRepository,
        IEventPublisher eventPublisher,
        ILogger<VehiclesController> logger,
        ApplicationDbContext context,
        IConfigurationServiceClient configClient)
    {
        _vehicleRepository = vehicleRepository;
        _categoryRepository = categoryRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _context = context;
        _configClient = configClient;
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
            Skip = Math.Max(0, (request.Page - 1)) * request.PageSize,  // Handle page=0 as page=1
            Take = request.PageSize,
            SortBy = request.SortBy ?? "CreatedAt",
            SortDescending = request.SortDescending ?? true
        };

        // Normalize page to at least 1 for response
        var normalizedPage = Math.Max(1, request.Page);

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

    /// <summary>
    /// Get vehicle by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<Vehicle>> GetById(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        return Ok(vehicle);
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

        // Search for vehicle whose ID starts with the short ID
        var vehicles = await _context.Vehicles
            .Include(v => v.Images)
            .Where(v => !v.IsDeleted && v.Id.ToString().Replace("-", "").StartsWith(shortId))
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
        var vehicles = await _vehicleRepository.GetFeaturedAsync(take);
        return Ok(vehicles);
    }

    /// <summary>
    /// Get vehicles by dealer
    /// </summary>
    [HttpGet("dealer/{dealerId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Vehicle>>> GetByDealer(Guid dealerId)
    {
        var vehicles = await _vehicleRepository.GetByDealerAsync(dealerId);
        return Ok(vehicles);
    }

    /// <summary>
    /// Get vehicles by seller (user)
    /// </summary>
    [HttpGet("seller/{sellerId:guid}")]
    public async Task<ActionResult<SellerVehiclesResponse>> GetBySeller(
        Guid sellerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? status = null)
    {
        var vehicles = await _vehicleRepository.GetBySellerAsync(sellerId);
        
        // Filter by status if provided
        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            if (Enum.TryParse<VehicleStatus>(status, true, out var statusEnum))
            {
                vehicles = vehicles.Where(v => v.Status == statusEnum);
            }
        }
        
        // Filter out deleted vehicles
        vehicles = vehicles.Where(v => !v.IsDeleted);
        
        var totalCount = vehicles.Count();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        
        var pagedVehicles = vehicles
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new SellerVehicleDto
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
                FuelType = v.FuelType.ToString(),
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
    public async Task<ActionResult<SellerVehicleStats>> GetSellerStats(Guid sellerId)
    {
        var vehicles = await _vehicleRepository.GetBySellerAsync(sellerId);
        var activeVehicles = vehicles.Where(v => !v.IsDeleted).ToList();
        
        return Ok(new SellerVehicleStats
        {
            TotalListings = activeVehicles.Count,
            ActiveListings = activeVehicles.Count(v => v.Status == VehicleStatus.Active),
            SoldListings = activeVehicles.Count(v => v.Status == VehicleStatus.Sold),
            PendingListings = activeVehicles.Count(v => v.Status == VehicleStatus.PendingReview),
            TotalViews = activeVehicles.Sum(v => v.ViewCount),
            TotalFavorites = activeVehicles.Sum(v => v.FavoriteCount)
        });
    }

    /// <summary>
    /// Compare multiple vehicles by their IDs
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

        var vehicles = new List<Vehicle>();
        foreach (var id in request.VehicleIds)
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id);
            if (vehicle != null && !vehicle.IsDeleted && vehicle.Status != VehicleStatus.Archived)
            {
                vehicles.Add(vehicle);
            }
        }

        if (!vehicles.Any())
            return NotFound(new { message = "No vehicles found with the provided IDs" });

        return Ok(vehicles);
    }

    /// <summary>
    /// Create a new vehicle listing
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Vehicle>> Create([FromBody] CreateVehicleRequest request)
    {
        if (request.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId.Value);
            if (category == null)
                return BadRequest(new { message = "Category not found" });
        }

        var vehicle = new Vehicle
        {
            Title = request.Title,
            Description = request.Description ?? string.Empty,
            Price = request.Price,
            Currency = request.Currency ?? "USD",
            Status = VehicleStatus.Draft,
            VIN = request.VIN,
            Make = request.Make,
            Model = request.Model,
            Trim = request.Trim,
            Year = request.Year,
            Mileage = request.Mileage,
            MileageUnit = request.MileageUnit ?? MileageUnit.Miles,
            VehicleType = request.VehicleType,
            BodyStyle = request.BodyStyle ?? BodyStyle.Sedan,
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
            State = request.State,
            ZipCode = request.ZipCode,
            Country = request.Country ?? "USA",
            SellerId = request.SellerId ?? Guid.Empty,
            SellerName = request.SellerName ?? string.Empty,
            SellerPhone = request.SellerPhone,
            SellerEmail = request.SellerEmail,
            SellerWhatsApp = request.SellerWhatsApp,
            DealerId = request.DealerId ?? Guid.Empty,
            CategoryId = request.CategoryId,
            FeaturesJson = request.FeaturesJson ?? "[]",
            HomepageSections = request.HomepageSections ?? HomepageSection.None
        };

        // Add images
        if (request.Images != null)
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

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
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

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.Title)) vehicle.Title = request.Title;
        if (request.Description != null) vehicle.Description = request.Description;
        if (request.Price.HasValue) vehicle.Price = request.Price.Value;
        if (!string.IsNullOrEmpty(request.Currency)) vehicle.Currency = request.Currency;
        if (request.Status.HasValue) vehicle.Status = request.Status.Value;
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
        if (request.IsFeatured.HasValue) vehicle.IsFeatured = request.IsFeatured.Value;
        if (request.HomepageSections.HasValue) vehicle.HomepageSections = request.HomepageSections.Value;

        vehicle.UpdatedAt = DateTime.UtcNow;

        await _vehicleRepository.UpdateAsync(vehicle);

        _logger.LogInformation("Vehicle updated: {VehicleId}", id);

        return Ok(vehicle);
    }

    /// <summary>
    /// Delete a vehicle (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var exists = await _vehicleRepository.ExistsAsync(id);
        if (!exists)
            return NotFound(new { message = "Vehicle not found" });

        await _vehicleRepository.DeleteAsync(id);

        _logger.LogInformation("Vehicle deleted: {VehicleId}", id);

        return NoContent();
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

        // Validate status - only Draft or Inactive can be published
        if (vehicle.Status != VehicleStatus.Draft && vehicle.Status != VehicleStatus.Archived)
        {
            return BadRequest(new { 
                message = $"Vehicle cannot be published from status '{vehicle.Status}'. Only Draft or Archived vehicles can be published.",
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

        if (validationErrors.Any())
        {
            return BadRequest(new { 
                message = "Vehicle cannot be published due to validation errors",
                errors = validationErrors 
            });
        }

        // Update status and timestamps
        vehicle.Status = VehicleStatus.Active;
        vehicle.PublishedAt = DateTime.UtcNow;
        vehicle.UpdatedAt = DateTime.UtcNow;

        // Set expiration based on dynamic config
        var expiresAt = request?.ExpiresAt ?? DateTime.UtcNow.AddDays(listingExpirationDays);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle published: {VehicleId} - {Title}, expires: {ExpiresAt}", id, vehicle.Title, expiresAt);

        // Publish event
        try
        {
            await _eventPublisher.PublishAsync(new VehicleCreatedEvent
            {
                VehicleId = vehicle.Id,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Price = vehicle.Price,
                VIN = vehicle.VIN ?? string.Empty,
                CreatedBy = vehicle.SellerId,
                CreatedAt = vehicle.PublishedAt.Value
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish VehiclePublishedEvent for {VehicleId}", id);
        }

        return Ok(new PublishVehicleResponse
        {
            Id = vehicle.Id,
            Status = vehicle.Status,
            PublishedAt = vehicle.PublishedAt.Value,
            ExpiresAt = expiresAt,
            Message = $"Vehicle published successfully. It is now visible to buyers. Expires: {expiresAt:yyyy-MM-dd}"
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

        // Only Active or Reserved vehicles can be unpublished
        if (vehicle.Status != VehicleStatus.Active && vehicle.Status != VehicleStatus.Reserved)
        {
            return BadRequest(new { 
                message = $"Vehicle cannot be unpublished from status '{vehicle.Status}'. Only Active or Reserved vehicles can be unpublished.",
                currentStatus = vehicle.Status.ToString()
            });
        }

        vehicle.Status = VehicleStatus.Archived;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle unpublished: {VehicleId}, Reason: {Reason}", id, request?.Reason ?? "Not specified");

        return Ok(new UnpublishVehicleResponse
        {
            Id = vehicle.Id,
            Status = vehicle.Status,
            UpdatedAt = vehicle.UpdatedAt,
            Message = "Vehicle unpublished successfully. It is no longer visible to buyers."
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

        // Only Active or Reserved vehicles can be marked as sold
        if (vehicle.Status != VehicleStatus.Active && vehicle.Status != VehicleStatus.Reserved)
        {
            return BadRequest(new { 
                message = $"Vehicle cannot be marked as sold from status '{vehicle.Status}'. Only Active or Reserved vehicles can be sold.",
                currentStatus = vehicle.Status.ToString()
            });
        }

        vehicle.Status = VehicleStatus.Sold;
        vehicle.SoldAt = DateTime.UtcNow;
        vehicle.UpdatedAt = DateTime.UtcNow;

        // Update price if sale price provided
        if (request?.SalePrice.HasValue == true)
        {
            vehicle.Price = request.SalePrice.Value;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle marked as sold: {VehicleId}, SalePrice: {SalePrice}", id, request?.SalePrice);

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

        return Ok(addedImages);
    }

    /// <summary>
    /// Bulk add images to multiple vehicles (for seeding)
    /// </summary>
    [HttpPost("bulk-images")]
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
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string? Currency { get; init; }
    public string VIN { get; init; } = string.Empty;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string? Trim { get; init; } // LE, SE, XLE, Sport
    public int Year { get; init; }
    public int Mileage { get; init; }
    public MileageUnit? MileageUnit { get; init; }
    public VehicleType VehicleType { get; init; }
    public BodyStyle? BodyStyle { get; init; }
    public int? Doors { get; init; } // Número de puertas (default: 4)
    public int? Seats { get; init; } // Número de asientos (default: 5)
    public FuelType? FuelType { get; init; } // Opcional - VIN no siempre tiene datos
    public TransmissionType? Transmission { get; init; } // Opcional - VIN no siempre tiene datos
    public Entities.DriveType? DriveType { get; init; } // Opcional - VIN no siempre tiene datos
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
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
    public Guid? SellerId { get; init; }
    public string? SellerName { get; init; }
    public string? SellerPhone { get; init; }
    public string? SellerEmail { get; init; }
    public string? SellerWhatsApp { get; init; }
    public Guid? DealerId { get; init; }
    public Guid? CategoryId { get; init; }
    public List<string>? Images { get; init; }
    public string? FeaturesJson { get; init; } // JSON array de características

    /// <summary>
    /// Secciones del homepage donde mostrar este vehículo.
    /// Valores: None=0, Carousel=1, Sedanes=2, SUVs=4, Camionetas=8, Deportivos=16, Destacados=32, Lujo=64
    /// Se pueden combinar sumando los valores (ej: Carousel + Destacados = 33)
    /// </summary>
    public HomepageSection? HomepageSections { get; init; }
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
}

public record PublishVehicleResponse
{
    public Guid Id { get; init; }
    public VehicleStatus Status { get; init; }
    public DateTime PublishedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string Message { get; init; } = string.Empty;
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
}

public record MarkVehicleSoldRequest
{
    /// <summary>
    /// Optional: Final sale price
    /// </summary>
    public decimal? SalePrice { get; init; }
    
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

#endregion
