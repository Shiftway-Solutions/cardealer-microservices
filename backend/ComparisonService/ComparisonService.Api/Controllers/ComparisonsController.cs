using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ComparisonService.Domain.Entities;
using ComparisonService.Domain.Interfaces;
using System.Security.Claims;

namespace ComparisonService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComparisonsController : ControllerBase
{
    private readonly IComparisonRepository _comparisonRepository;
    private readonly ILogger<ComparisonsController> _logger;
    private readonly HttpClient _httpClient;

    public ComparisonsController(
        IComparisonRepository comparisonRepository,
        IHttpClientFactory httpClientFactory,
        ILogger<ComparisonsController> logger)
    {
        _comparisonRepository = comparisonRepository;
        _httpClient = httpClientFactory.CreateClient("VehiclesSaleService");
        _logger = logger;
    }

    /// <summary>
    /// Obtiene las comparaciones guardadas del usuario
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ComparisonDto>>> GetMyComparisons()
    {
        var userId = GetCurrentUserId();
        var comparisons = await _comparisonRepository.GetByUserIdAsync(userId);

        return Ok(comparisons.Select(c => MapToDto(c)));
    }

    /// <summary>
    /// Obtiene una comparación específica
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ComparisonDetailDto>> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        var comparison = await _comparisonRepository.GetByIdAsync(id);

        if (comparison == null)
            return NotFound();

        if (comparison.UserId != userId)
            return Forbid();

        // Fetch vehicles from VehiclesSaleService
        var vehicles = await FetchVehiclesAsync(comparison.VehicleIds);

        return Ok(new ComparisonDetailDto
        {
            Id = comparison.Id,
            Name = comparison.Name,
            VehicleIds = comparison.VehicleIds,
            Vehicles = vehicles,
            CreatedAt = comparison.CreatedAt,
            UpdatedAt = comparison.UpdatedAt,
            IsPublic = comparison.IsPublic,
            ShareToken = comparison.ShareToken,
            ShareUrl = comparison.ShareToken != null
                ? $"{Request.Scheme}://{Request.Host}/compare/{comparison.ShareToken}"
                : null
        });
    }

    /// <summary>
    /// Obtiene una comparación compartida (pública)
    /// </summary>
    [HttpGet("shared/{shareToken}")]
    [AllowAnonymous]
    public async Task<ActionResult<ComparisonDetailDto>> GetByShareToken(string shareToken)
    {
        var comparison = await _comparisonRepository.GetByShareTokenAsync(shareToken);

        if (comparison == null)
            return NotFound();

        var vehicles = await FetchVehiclesAsync(comparison.VehicleIds);

        return Ok(new ComparisonDetailDto
        {
            Id = comparison.Id,
            Name = comparison.Name,
            VehicleIds = comparison.VehicleIds,
            Vehicles = vehicles,
            CreatedAt = comparison.CreatedAt,
            IsPublic = comparison.IsPublic,
            ShareUrl = $"{Request.Scheme}://{Request.Host}/compare/{shareToken}"
        });
    }

    /// <summary>
    /// Crea una nueva comparación
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ComparisonDto>> Create([FromBody] CreateComparisonRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var comparison = new VehicleComparison(
                userId,
                request.Name,
                request.VehicleIds,
                request.IsPublic);

            await _comparisonRepository.CreateAsync(comparison);

            _logger.LogInformation(
                "User {UserId} created comparison {ComparisonId} with {Count} vehicles",
                userId, comparison.Id, comparison.VehicleIds.Count);

            return CreatedAtAction(
                nameof(GetById),
                new { id = comparison.Id },
                MapToDto(comparison));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza los vehículos de una comparación
    /// </summary>
    [HttpPut("{id:guid}/vehicles")]
    [Authorize]
    public async Task<ActionResult<ComparisonDto>> UpdateVehicles(
        Guid id,
        [FromBody] UpdateVehiclesRequest request)
    {
        var userId = GetCurrentUserId();
        var comparison = await _comparisonRepository.GetByIdAsync(id);

        if (comparison == null)
            return NotFound();

        if (comparison.UserId != userId)
            return Forbid();

        try
        {
            comparison.UpdateVehicles(request.VehicleIds);
            await _comparisonRepository.UpdateAsync(comparison);

            return Ok(MapToDto(comparison));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Renombra una comparación
    /// </summary>
    [HttpPut("{id:guid}/name")]
    [Authorize]
    public async Task<ActionResult<ComparisonDto>> Rename(
        Guid id,
        [FromBody] RenameRequest request)
    {
        var userId = GetCurrentUserId();
        var comparison = await _comparisonRepository.GetByIdAsync(id);

        if (comparison == null)
            return NotFound();

        if (comparison.UserId != userId)
            return Forbid();

        comparison.Rename(request.Name);
        await _comparisonRepository.UpdateAsync(comparison);

        return Ok(MapToDto(comparison));
    }

    /// <summary>
    /// Hace pública una comparación
    /// </summary>
    [HttpPost("{id:guid}/share")]
    [Authorize]
    public async Task<ActionResult<ShareResponse>> MakePublic(Guid id)
    {
        var userId = GetCurrentUserId();
        var comparison = await _comparisonRepository.GetByIdAsync(id);

        if (comparison == null)
            return NotFound();

        if (comparison.UserId != userId)
            return Forbid();

        comparison.MakePublic();
        await _comparisonRepository.UpdateAsync(comparison);

        return Ok(new ShareResponse
        {
            ShareToken = comparison.ShareToken!,
            ShareUrl = $"{Request.Scheme}://{Request.Host}/compare/{comparison.ShareToken}"
        });
    }

    /// <summary>
    /// Hace privada una comparación
    /// </summary>
    [HttpDelete("{id:guid}/share")]
    [Authorize]
    public async Task<IActionResult> MakePrivate(Guid id)
    {
        var userId = GetCurrentUserId();
        var comparison = await _comparisonRepository.GetByIdAsync(id);

        if (comparison == null)
            return NotFound();

        if (comparison.UserId != userId)
            return Forbid();

        comparison.MakePrivate();
        await _comparisonRepository.UpdateAsync(comparison);

        return NoContent();
    }

    /// <summary>
    /// Elimina una comparación
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var comparison = await _comparisonRepository.GetByIdAsync(id);

        if (comparison == null)
            return NotFound();

        if (comparison.UserId != userId)
            return Forbid();

        await _comparisonRepository.DeleteAsync(id);

        return NoContent();
    }

    // Helper methods
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        return userId;
    }

    private async Task<List<VehicleComparisonDto>> FetchVehiclesAsync(List<Guid> vehicleIds)
    {
        var vehicles = new List<VehicleComparisonDto>();

        foreach (var vehicleId in vehicleIds)
        {
            try
            {
                // Call VehiclesSaleService (base URL configured in Program.cs)
                var response = await _httpClient.GetAsync($"api/vehicles/{vehicleId}");

                if (response.IsSuccessStatusCode)
                {
                    var vehicle = await response.Content.ReadFromJsonAsync<VehicleComparisonDto>();
                    if (vehicle != null)
                    {
                        vehicles.Add(vehicle);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch vehicle {VehicleId}", vehicleId);
            }
        }

        return vehicles;
    }

    private static ComparisonDto MapToDto(VehicleComparison comparison)
    {
        return new ComparisonDto
        {
            Id = comparison.Id,
            Name = comparison.Name,
            VehicleIds = comparison.VehicleIds,
            VehicleCount = comparison.VehicleIds.Count,
            CreatedAt = comparison.CreatedAt,
            UpdatedAt = comparison.UpdatedAt ?? comparison.CreatedAt,
            IsPublic = comparison.IsPublic,
            HasShareLink = comparison.ShareToken != null
        };
    }
}

#region DTOs

public record CreateComparisonRequest
{
    public string Name { get; init; } = string.Empty;
    public List<Guid> VehicleIds { get; init; } = new();
    public bool IsPublic { get; init; } = false;
}

public record UpdateVehiclesRequest
{
    public List<Guid> VehicleIds { get; init; } = new();
}

public record RenameRequest
{
    public string Name { get; init; } = string.Empty;
}

public record ShareResponse
{
    public string ShareToken { get; init; } = string.Empty;
    public string ShareUrl { get; init; } = string.Empty;
}

public record ComparisonDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<Guid> VehicleIds { get; init; } = new();
    public int VehicleCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public bool IsPublic { get; init; }
    public bool HasShareLink { get; init; }
}

public record ComparisonDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<Guid> VehicleIds { get; init; } = new();
    public List<VehicleComparisonDto> Vehicles { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public bool IsPublic { get; init; }
    public string? ShareToken { get; init; }
    public string? ShareUrl { get; init; }
}

public record VehicleComparisonDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public int? Mileage { get; init; }
    public string? FuelType { get; init; }
    public string? Transmission { get; init; }
    public string? BodyStyle { get; init; }
    public string? Condition { get; init; }
    public string? PrimaryImageUrl { get; init; }
}

#endregion
