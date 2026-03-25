using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VehiclesSaleService.Domain.Entities;
using VehiclesSaleService.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;

namespace VehiclesSaleService.Api.Controllers;

/// <summary>
/// Controller para gestionar las secciones del homepage y sus vehículos asignados.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
[EnableRateLimiting("VehiclesPolicy")]
public class HomepageSectionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomepageSectionsController> _logger;

    public HomepageSectionsController(ApplicationDbContext context, ILogger<HomepageSectionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ========================================
    // DTOs
    // ========================================

    public record HomepageSectionDto(
        Guid Id,
        string Name,
        string Slug,
        string? Description,
        int DisplayOrder,
        int MaxItems,
        bool IsActive,
        string? Icon,
        string AccentColor,
        string? ViewAllHref,
        string LayoutType,
        string? Subtitle,
        int VehicleCount
    );

    public record HomepageSectionWithVehiclesDto(
        Guid Id,
        string Name,
        string Slug,
        string? Description,
        int DisplayOrder,
        int MaxItems,
        bool IsActive,
        string? Icon,
        string AccentColor,
        string? ViewAllHref,
        string LayoutType,
        string? Subtitle,
        List<VehicleInSectionDto> Vehicles
    );

    public record VehicleInSectionDto(
        Guid Id,
        string Name,
        string? Make,
        string? Model,
        int? Year,
        decimal Price,
        int? Mileage,
        string? FuelType,
        string? Transmission,
        string? ExteriorColor,
        string? BodyStyle,
        string? ImageUrl,
        List<string> ImageUrls,
        int SortOrder,
        bool IsPinned
    );

    public record AssignVehicleToSectionRequest(
        Guid VehicleId,
        int SortOrder = 0,
        bool IsPinned = false,
        DateTime? StartDate = null,
        DateTime? EndDate = null
    );

    public record UpdateSectionRequest(
        string? Name,
        string? Description,
        int? DisplayOrder,
        int? MaxItems,
        bool? IsActive,
        string? Icon,
        string? AccentColor,
        string? ViewAllHref,
        SectionLayoutType? LayoutType,
        string? Subtitle
    );

    public record CreateSectionRequest(
        string Name,
        string Slug,
        string? Description,
        int DisplayOrder,
        int MaxItems,
        bool IsActive = true,
        string? Icon = null,
        string AccentColor = "blue",
        string? ViewAllHref = null,
        SectionLayoutType LayoutType = SectionLayoutType.Carousel,
        string? Subtitle = null
    );

    // ========================================
    // GET ENDPOINTS
    // ========================================

    /// <summary>
    /// Obtiene todas las secciones del homepage (sin vehículos).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<HomepageSectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<HomepageSectionDto>>> GetAllSections(
        [FromQuery] bool? activeOnly = true)
    {
        var query = _context.HomepageSectionConfigs.AsNoTracking();

        if (activeOnly == true)
        {
            query = query.Where(s => s.IsActive);
        }

        var sections = await query
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new HomepageSectionDto(
                s.Id,
                s.Name,
                s.Slug,
                s.Description,
                s.DisplayOrder,
                s.MaxItems,
                s.IsActive,
                s.Icon,
                s.AccentColor,
                s.ViewAllHref,
                s.LayoutType.ToString(),
                s.Subtitle,
                s.VehicleSections.Count
            ))
            .ToListAsync();

        return Ok(sections);
    }

    /// <summary>
    /// Obtiene una sección por slug con sus vehículos asignados.
    /// </summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HomepageSectionWithVehiclesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HomepageSectionWithVehiclesDto>> GetSectionBySlug(string slug)
    {
        var section = await _context.HomepageSectionConfigs
            .AsNoTracking()
            .Where(s => s.Slug == slug)
            .Select(s => new
            {
                Section = s,
                Vehicles = s.VehicleSections
                    .Where(vs => vs.StartDate == null || vs.StartDate <= DateTime.UtcNow)
                    .Where(vs => vs.EndDate == null || vs.EndDate >= DateTime.UtcNow)
                    .OrderByDescending(vs => vs.IsPinned)
                    .ThenBy(vs => vs.SortOrder)
                    .Take(s.MaxItems)
                    .Select(vs => new
                    {
                        vs.Vehicle,
                        vs.SortOrder,
                        vs.IsPinned
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (section == null)
        {
            return NotFound(new { error = $"Section '{slug}' not found" });
        }

        var vehicles = section.Vehicles.Select(v => new VehicleInSectionDto(
            v.Vehicle.Id,
            v.Vehicle.Title,
            v.Vehicle.Make,
            v.Vehicle.Model,
            v.Vehicle.Year,
            v.Vehicle.Price,
            v.Vehicle.Mileage,
            v.Vehicle.FuelType.ToString(),
            v.Vehicle.Transmission.ToString(),
            v.Vehicle.ExteriorColor,
            v.Vehicle.BodyStyle.ToString(),
            v.Vehicle.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url,
            v.Vehicle.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList(),
            v.SortOrder,
            v.IsPinned
        )).ToList();

        var result = new HomepageSectionWithVehiclesDto(
            section.Section.Id,
            section.Section.Name,
            section.Section.Slug,
            section.Section.Description,
            section.Section.DisplayOrder,
            section.Section.MaxItems,
            section.Section.IsActive,
            section.Section.Icon,
            section.Section.AccentColor,
            section.Section.ViewAllHref,
            section.Section.LayoutType.ToString(),
            section.Section.Subtitle,
            vehicles
        );

        return Ok(result);
    }

    /// <summary>
    /// Obtiene todas las secciones activas con sus vehículos para renderizar el homepage completo.
    /// </summary>
    [HttpGet("homepage")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<HomepageSectionWithVehiclesDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<HomepageSectionWithVehiclesDto>>> GetHomepageSections()
    {
        // Step 1: Load only section metadata (no vehicle data) to avoid loading all vehicles into memory
        var sections = await _context.HomepageSectionConfigs
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var result = new List<HomepageSectionWithVehiclesDto>();

        // Step 2: For each section, query vehicles with DB-level filtering, sorting, and Take
        // BUG FIX: Filter only Active vehicles to prevent Draft/Sold/Archived/Rejected from appearing
        foreach (var s in sections)
        {
            var vehicles = await _context.VehicleHomepageSections
                .AsNoTracking()
                .Where(vs => vs.HomepageSectionConfigId == s.Id)
                .Where(vs => vs.Vehicle.Status == VehicleStatus.Active && !vs.Vehicle.IsDeleted)
                // FIX FRONTEND-008: Exclude E2E test vehicles from homepage sections
                .Where(vs => vs.Vehicle.Title == null || !vs.Vehicle.Title.Contains("E2E"))
                .Where(vs => vs.StartDate == null || vs.StartDate <= now)
                .Where(vs => vs.EndDate == null || vs.EndDate >= now)
                .OrderByDescending(vs => vs.IsPinned)
                .ThenBy(vs => vs.SortOrder)
                .Take(s.MaxItems)
                .Select(vs => new VehicleInSectionDto(
                    vs.Vehicle.Id,
                    vs.Vehicle.Title,
                    vs.Vehicle.Make,
                    vs.Vehicle.Model,
                    vs.Vehicle.Year,
                    vs.Vehicle.Price,
                    vs.Vehicle.Mileage,
                    vs.Vehicle.FuelType.ToString(),
                    vs.Vehicle.Transmission.ToString(),
                    vs.Vehicle.ExteriorColor,
                    vs.Vehicle.BodyStyle.ToString(),
                    vs.Vehicle.Images.OrderBy(i => i.SortOrder).FirstOrDefault()!.Url,
                    vs.Vehicle.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList(),
                    vs.SortOrder,
                    vs.IsPinned
                ))
                .ToListAsync();

            result.Add(new HomepageSectionWithVehiclesDto(
                s.Id,
                s.Name,
                s.Slug,
                s.Description,
                s.DisplayOrder,
                s.MaxItems,
                s.IsActive,
                s.Icon,
                s.AccentColor,
                s.ViewAllHref,
                s.LayoutType.ToString(),
                s.Subtitle,
                vehicles
            ));
        }

        return Ok(result);
    }

    // ========================================
    // VEHICLE ASSIGNMENT ENDPOINTS
    // ========================================

    /// <summary>
    /// Asigna un vehículo a una sección del homepage.
    /// </summary>
    [HttpPost("{slug}/vehicles")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignVehicleToSection(
        string slug,
        [FromBody] AssignVehicleToSectionRequest request)
    {
        var section = await _context.HomepageSectionConfigs
            .FirstOrDefaultAsync(s => s.Slug == slug);

        if (section == null)
        {
            return NotFound(new { error = $"Section '{slug}' not found" });
        }

        // Ignore tenant filter for homepage sections
        var vehicle = await _context.Vehicles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId);
        if (vehicle == null)
        {
            return NotFound(new { error = $"Vehicle '{request.VehicleId}' not found" });
        }

        // Check if already assigned
        var existingAssignment = await _context.VehicleHomepageSections
            .FirstOrDefaultAsync(vhs =>
                vhs.VehicleId == request.VehicleId &&
                vhs.HomepageSectionConfigId == section.Id);

        if (existingAssignment != null)
        {
            return BadRequest(new { error = "Vehicle is already assigned to this section" });
        }

        var assignment = new VehicleHomepageSection
        {
            Id = Guid.NewGuid(),
            VehicleId = request.VehicleId,
            HomepageSectionConfigId = section.Id,
            SortOrder = request.SortOrder,
            IsPinned = request.IsPinned,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.VehicleHomepageSections.Add(assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Vehicle {VehicleId} assigned to section {Slug}",
            request.VehicleId, slug);

        return CreatedAtAction(
            nameof(GetSectionBySlug),
            new { slug },
            new { message = "Vehicle assigned to section successfully" });
    }

    /// <summary>
    /// Remueve un vehículo de una sección del homepage.
    /// </summary>
    [HttpDelete("{slug}/vehicles/{vehicleId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveVehicleFromSection(string slug, Guid vehicleId)
    {
        var section = await _context.HomepageSectionConfigs
            .FirstOrDefaultAsync(s => s.Slug == slug);

        if (section == null)
        {
            return NotFound(new { error = $"Section '{slug}' not found" });
        }

        var assignment = await _context.VehicleHomepageSections
            .FirstOrDefaultAsync(vhs =>
                vhs.VehicleId == vehicleId &&
                vhs.HomepageSectionConfigId == section.Id);

        if (assignment == null)
        {
            return NotFound(new { error = "Vehicle is not assigned to this section" });
        }

        _context.VehicleHomepageSections.Remove(assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Vehicle {VehicleId} removed from section {Slug}",
            vehicleId, slug);

        return NoContent();
    }

    /// <summary>
    /// Obtiene los vehículos asignados a una sección.
    /// </summary>
    [HttpGet("{slug}/vehicles")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<VehicleInSectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<VehicleInSectionDto>>> GetSectionVehicles(string slug)
    {
        var section = await _context.HomepageSectionConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug);

        if (section == null)
        {
            return NotFound(new { error = $"Section '{slug}' not found" });
        }

        var vehicles = await _context.VehicleHomepageSections
            .AsNoTracking()
            .Where(vhs => vhs.HomepageSectionConfigId == section.Id)
            // FIX FRONTEND-008: Exclude E2E test vehicles
            .Where(vhs => vhs.Vehicle.Title == null || !vhs.Vehicle.Title.Contains("E2E"))
            .OrderByDescending(vhs => vhs.IsPinned)
            .ThenBy(vhs => vhs.SortOrder)
            .Include(vhs => vhs.Vehicle)
                .ThenInclude(v => v.Images)
            .Select(vhs => new VehicleInSectionDto(
                vhs.Vehicle.Id,
                vhs.Vehicle.Title,
                vhs.Vehicle.Make,
                vhs.Vehicle.Model,
                vhs.Vehicle.Year,
                vhs.Vehicle.Price,
                vhs.Vehicle.Mileage,
                vhs.Vehicle.FuelType.ToString(),
                vhs.Vehicle.Transmission.ToString(),
                vhs.Vehicle.ExteriorColor,
                vhs.Vehicle.BodyStyle.ToString(),
                vhs.Vehicle.Images.OrderBy(i => i.SortOrder).FirstOrDefault()!.Url,
                vhs.Vehicle.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList(),
                vhs.SortOrder,
                vhs.IsPinned
            ))
            .ToListAsync();

        return Ok(vehicles);
    }

    // ========================================
    // SECTION CRUD ENDPOINTS
    // ========================================

    /// <summary>
    /// Crea una nueva sección del homepage.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(HomepageSectionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HomepageSectionDto>> CreateSection([FromBody] CreateSectionRequest request)
    {
        // Check if slug already exists
        var existingSlug = await _context.HomepageSectionConfigs
            .AnyAsync(s => s.Slug == request.Slug);

        if (existingSlug)
        {
            return BadRequest(new { error = $"Section with slug '{request.Slug}' already exists" });
        }

        var section = new HomepageSectionConfig
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder,
            MaxItems = request.MaxItems,
            IsActive = request.IsActive,
            Icon = request.Icon,
            AccentColor = request.AccentColor,
            ViewAllHref = request.ViewAllHref,
            LayoutType = request.LayoutType,
            Subtitle = request.Subtitle,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.HomepageSectionConfigs.Add(section);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created homepage section: {Name} ({Slug})", request.Name, request.Slug);

        var dto = new HomepageSectionDto(
            section.Id,
            section.Name,
            section.Slug,
            section.Description,
            section.DisplayOrder,
            section.MaxItems,
            section.IsActive,
            section.Icon,
            section.AccentColor,
            section.ViewAllHref,
            section.LayoutType.ToString(),
            section.Subtitle,
            0
        );

        return CreatedAtAction(nameof(GetSectionBySlug), new { slug = section.Slug }, dto);
    }

    /// <summary>
    /// Actualiza una sección del homepage.
    /// </summary>
    [HttpPut("{slug}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(HomepageSectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HomepageSectionDto>> UpdateSection(
        string slug,
        [FromBody] UpdateSectionRequest request)
    {
        var section = await _context.HomepageSectionConfigs
            .FirstOrDefaultAsync(s => s.Slug == slug);

        if (section == null)
        {
            return NotFound(new { error = $"Section '{slug}' not found" });
        }

        if (request.Name != null) section.Name = request.Name;
        if (request.Description != null) section.Description = request.Description;
        if (request.DisplayOrder.HasValue) section.DisplayOrder = request.DisplayOrder.Value;
        if (request.MaxItems.HasValue) section.MaxItems = request.MaxItems.Value;
        if (request.IsActive.HasValue) section.IsActive = request.IsActive.Value;
        if (request.Icon != null) section.Icon = request.Icon;
        if (request.AccentColor != null) section.AccentColor = request.AccentColor;
        if (request.ViewAllHref != null) section.ViewAllHref = request.ViewAllHref;
        if (request.LayoutType.HasValue) section.LayoutType = request.LayoutType.Value;
        if (request.Subtitle != null) section.Subtitle = request.Subtitle;

        section.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated homepage section: {Slug}", slug);

        var vehicleCount = await _context.VehicleHomepageSections
            .CountAsync(vhs => vhs.HomepageSectionConfigId == section.Id);

        var dto = new HomepageSectionDto(
            section.Id,
            section.Name,
            section.Slug,
            section.Description,
            section.DisplayOrder,
            section.MaxItems,
            section.IsActive,
            section.Icon,
            section.AccentColor,
            section.ViewAllHref,
            section.LayoutType.ToString(),
            section.Subtitle,
            vehicleCount
        );

        return Ok(dto);
    }

    /// <summary>
    /// Elimina una sección del homepage.
    /// </summary>
    [HttpDelete("{slug}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSection(string slug)
    {
        var section = await _context.HomepageSectionConfigs
            .FirstOrDefaultAsync(s => s.Slug == slug);

        if (section == null)
        {
            return NotFound(new { error = $"Section '{slug}' not found" });
        }

        _context.HomepageSectionConfigs.Remove(section);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted homepage section: {Slug}", slug);

        return NoContent();
    }

    // ========================================
    // BULK ASSIGNMENT ENDPOINT
    // ========================================

    public record BulkAssignVehiclesRequest(
        List<Guid> VehicleIds
    );

    /// <summary>
    /// Asigna múltiples vehículos a una sección en una sola operación.
    /// </summary>
    [HttpPost("{slug}/vehicles/bulk")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkAssignVehicles(
        string slug,
        [FromBody] BulkAssignVehiclesRequest request)
    {
        var section = await _context.HomepageSectionConfigs
            .FirstOrDefaultAsync(s => s.Slug == slug);

        if (section == null)
        {
            return NotFound(new { error = $"Section '{slug}' not found" });
        }

        // Get existing assignments
        var existingVehicleIds = await _context.VehicleHomepageSections
            .Where(vhs => vhs.HomepageSectionConfigId == section.Id)
            .Select(vhs => vhs.VehicleId)
            .ToListAsync();

        // Filter out already assigned vehicles
        var newVehicleIds = request.VehicleIds
            .Except(existingVehicleIds)
            .ToList();

        // Verify vehicles exist (ignore tenant filter for homepage sections)
        var validVehicleIds = await _context.Vehicles
            .IgnoreQueryFilters()
            .Where(v => newVehicleIds.Contains(v.Id))
            .Select(v => v.Id)
            .ToListAsync();

        var sortOrder = await _context.VehicleHomepageSections
            .Where(vhs => vhs.HomepageSectionConfigId == section.Id)
            .MaxAsync(vhs => (int?)vhs.SortOrder) ?? 0;

        var assignments = validVehicleIds.Select((vehicleId, index) => new VehicleHomepageSection
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HomepageSectionConfigId = section.Id,
            SortOrder = sortOrder + index + 1,
            IsPinned = false,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.VehicleHomepageSections.AddRange(assignments);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Bulk assigned {Count} vehicles to section {Slug}",
            assignments.Count, slug);

        return Ok(new
        {
            message = $"{assignments.Count} vehicles assigned to section",
            assignedCount = assignments.Count,
            skippedCount = request.VehicleIds.Count - assignments.Count
        });
    }
}
