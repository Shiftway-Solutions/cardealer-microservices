using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VehiclesSaleService.Domain.Entities;
using VehiclesSaleService.Domain.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace VehiclesSaleService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("VehiclesPolicy")]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ILogger<FavoritesController> _logger;

    public FavoritesController(
        IFavoriteRepository favoriteRepository,
        IVehicleRepository vehicleRepository,
        ILogger<FavoritesController> logger)
    {
        _favoriteRepository = favoriteRepository;
        _vehicleRepository = vehicleRepository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene los vehículos favoritos del usuario actual
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(FavoritesListResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FavoritesListResponseDto>> GetMyFavorites(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var totalCount = await _favoriteRepository.GetCountByUserIdAsync(userId);
        var favorites = await _favoriteRepository.GetByUserIdAsync(userId, page, pageSize);

        var items = favorites
            .Where(f => f.Vehicle != null)
            .Select(f => new FavoriteItemDto
            {
                Id = f.Id,
                VehicleId = f.VehicleId,
                UserId = f.UserId,
                CreatedAt = f.CreatedAt,
                Notes = f.Notes,
                NotifyOnPriceChange = f.NotifyPriceChange,
                Vehicle = MapToFavoriteVehicleDto(f.Vehicle!)
            })
            .ToList();

        return Ok(new FavoritesListResponseDto
        {
            Favorites = items,
            Total = totalCount
        });
    }

    /// <summary>
    /// Obtiene el conteo de favoritos del usuario actual
    /// </summary>
    [HttpGet("count")]
    [ProducesResponseType(typeof(FavoriteCountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FavoriteCountResponse>> GetCount()
    {
        var userId = GetCurrentUserId();
        var count = await _favoriteRepository.GetCountByUserIdAsync(userId);

        return Ok(new FavoriteCountResponse { Count = count });
    }

    /// <summary>
    /// Verifica si un vehículo está en favoritos
    /// </summary>
    [HttpGet("check/{vehicleId:guid}")]
    [ProducesResponseType(typeof(IsFavoriteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<IsFavoriteResponse>> Check(Guid vehicleId)
    {
        var userId = GetCurrentUserId();
        var isFavorite = await _favoriteRepository.IsFavoriteAsync(userId, vehicleId);

        return Ok(new IsFavoriteResponse { IsFavorite = isFavorite });
    }

    /// <summary>
    /// Añade un vehículo a favoritos
    /// </summary>
    [HttpPost("{vehicleId:guid}")]
    [ProducesResponseType(typeof(FavoriteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FavoriteResponse>> AddFavorite(
        Guid vehicleId,
        [FromBody] AddFavoriteRequest? request = null)
    {
        var userId = GetCurrentUserId();

        // Verificar que el vehículo existe
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId);
        if (vehicle == null)
            return NotFound(new { message = "Vehículo no encontrado" });

        // Verificar si ya existe
        var existing = await _favoriteRepository.GetByUserAndVehicleAsync(userId, vehicleId);
        if (existing != null)
            return BadRequest(new { message = "El vehículo ya está en favoritos" });

        // Crear favorito
        var favorite = new Favorite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VehicleId = vehicleId,
            CreatedAt = DateTime.UtcNow,
            Notes = request?.Notes,
            NotifyPriceChange = request?.NotifyPriceChange ?? false,
            DealerId = Guid.Empty // Usuario final
        };

        await _favoriteRepository.CreateAsync(favorite);

        _logger.LogInformation(
            "User {UserId} added vehicle {VehicleId} to favorites",
            userId, vehicleId);

        return CreatedAtAction(
            nameof(Check),
            new { vehicleId },
            new FavoriteResponse
            {
                Id = favorite.Id,
                UserId = favorite.UserId,
                VehicleId = favorite.VehicleId,
                CreatedAt = favorite.CreatedAt,
                Notes = favorite.Notes,
                NotifyPriceChange = favorite.NotifyPriceChange
            });
    }

    /// <summary>
    /// Elimina un vehículo de favoritos
    /// </summary>
    [HttpDelete("{vehicleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFavorite(Guid vehicleId)
    {
        var userId = GetCurrentUserId();

        var favorite = await _favoriteRepository.GetByUserAndVehicleAsync(userId, vehicleId);
        if (favorite == null)
            return NotFound(new { message = "Favorito no encontrado" });

        await _favoriteRepository.DeleteAsync(favorite.Id);

        _logger.LogInformation(
            "User {UserId} removed vehicle {VehicleId} from favorites",
            userId, vehicleId);

        return NoContent();
    }

    /// <summary>
    /// Actualiza las notas de un favorito
    /// </summary>
    [HttpPut("{vehicleId:guid}")]
    [ProducesResponseType(typeof(FavoriteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FavoriteResponse>> UpdateFavorite(
        Guid vehicleId,
        [FromBody] UpdateFavoriteRequest request)
    {
        var userId = GetCurrentUserId();

        var favorite = await _favoriteRepository.GetByUserAndVehicleAsync(userId, vehicleId);
        if (favorite == null)
            return NotFound(new { message = "Favorito no encontrado" });

        // Actualizar
        if (request.Notes != null)
            favorite.Notes = request.Notes;
        if (request.NotifyPriceChange.HasValue)
            favorite.NotifyPriceChange = request.NotifyPriceChange.Value;

        // Nota: En un repositorio completo tendríamos UpdateAsync,
        // pero como no está definido, recrearemos el objeto
        await _favoriteRepository.DeleteAsync(favorite.Id);
        await _favoriteRepository.CreateAsync(favorite);

        return Ok(new FavoriteResponse
        {
            Id = favorite.Id,
            UserId = favorite.UserId,
            VehicleId = favorite.VehicleId,
            CreatedAt = favorite.CreatedAt,
            Notes = favorite.Notes,
            NotifyPriceChange = favorite.NotifyPriceChange
        });
    }

    // Helper: Obtener UserId del token JWT
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value 
            ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Usuario no autenticado");
        }

        return userId;
    }

    // Helper: Mapear Favorite+Vehicle a FavoriteItemDto (lo que espera el frontend)
    private static FavoriteItemDto MapToFavoriteItemDto(Favorite favorite)
    {
        return new FavoriteItemDto
        {
            Id = favorite.Id,
            VehicleId = favorite.VehicleId,
            UserId = favorite.UserId,
            CreatedAt = favorite.CreatedAt,
            Notes = favorite.Notes,
            NotifyOnPriceChange = favorite.NotifyPriceChange,
            Vehicle = MapToFavoriteVehicleDto(favorite.Vehicle!)
        };
    }

    // Helper: Mapear Vehicle a FavoriteVehicleDto
    private static FavoriteVehicleDto MapToFavoriteVehicleDto(Vehicle vehicle)
    {
        return new FavoriteVehicleDto
        {
            Id = vehicle.Id,
            Slug = GenerateSlug(vehicle),
            Title = vehicle.Title,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            Price = vehicle.Price,
            Mileage = vehicle.Mileage,
            Transmission = vehicle.Transmission.ToString(),
            FuelType = vehicle.FuelType.ToString(),
            BodyType = vehicle.BodyStyle.ToString(),
            Location = FormatLocation(vehicle.City, vehicle.State),
            ImageUrl = vehicle.Images
                .OrderBy(i => i.SortOrder)
                .FirstOrDefault()?.Url ?? string.Empty,
            Status = vehicle.Status.ToString().ToLowerInvariant()
        };
    }

    // Helper: Mapear Vehicle a VehicleDto (usado por otros endpoints)
    private static VehicleDto MapToDto(Vehicle vehicle)
    {
        return new VehicleDto
        {
            Id = vehicle.Id,
            Title = vehicle.Title,
            Description = vehicle.Description,
            Price = vehicle.Price,
            Currency = vehicle.Currency,
            Status = vehicle.Status.ToString(),
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            Mileage = vehicle.Mileage,
            FuelType = vehicle.FuelType.ToString(),
            Transmission = vehicle.Transmission.ToString(),
            BodyStyle = vehicle.BodyStyle.ToString(),
            PrimaryImageUrl = vehicle.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url,
            SellerName = vehicle.SellerName,
            ViewCount = vehicle.ViewCount,
            FavoriteCount = vehicle.FavoriteCount,
            CreatedAt = vehicle.CreatedAt
        };
    }

    // Helper: Generar slug URL para un vehículo (igual que VehiclesController)
    private static string GenerateSlug(Vehicle vehicle)
    {
        var baseSlug = $"{vehicle.Year}-{vehicle.Make}-{vehicle.Model}"
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-");
        var shortId = vehicle.Id.ToString("N")[..8];
        return $"{baseSlug}-{shortId}";
    }

    // Helper: Formatear ubicación desde city/state
    private static string FormatLocation(string? city, string? state)
    {
        var parts = new[] { city, state }.Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }
}

#region DTOs

public record AddFavoriteRequest
{
    public string? Notes { get; init; }
    public bool NotifyPriceChange { get; init; } = false;
}

public record UpdateFavoriteRequest
{
    public string? Notes { get; init; }
    public bool? NotifyPriceChange { get; init; }
}

public record FavoriteResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid VehicleId { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Notes { get; init; }
    public bool NotifyPriceChange { get; init; }
}

/// <summary>Response de GET /api/favorites — compatible con FavoritesListResponse del frontend</summary>
public record FavoritesListResponseDto
{
    public IReadOnlyList<FavoriteItemDto> Favorites { get; init; } = [];
    public int Total { get; init; }
}

/// <summary>Ítem individual: metadata del favorito + datos del vehículo</summary>
public record FavoriteItemDto
{
    public Guid Id { get; init; }
    public Guid VehicleId { get; init; }
    public Guid UserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Notes { get; init; }
    /// <summary>notifyOnPriceChange (camelCase) — coincide con la interfaz FavoriteVehicle del frontend</summary>
    public bool NotifyOnPriceChange { get; init; }
    public FavoriteVehicleDto Vehicle { get; init; } = new();
}

/// <summary>Datos del vehículo dentro de un favorito — campos que necesita FavoriteVehicle.vehicle en el frontend</summary>
public record FavoriteVehicleDto
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public decimal Price { get; init; }
    public int Mileage { get; init; }
    public string Transmission { get; init; } = string.Empty;
    public string FuelType { get; init; } = string.Empty;
    public string BodyType { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    /// <summary>Siempre en minúsculas: "active", "sold", "pending", "removed"</summary>
    public string Status { get; init; } = "active";
}

public record FavoriteCountResponse
{
    public int Count { get; init; }
}

public record IsFavoriteResponse
{
    public bool IsFavorite { get; init; }
}

// VehicleDto usado por otros endpoints (check, etc.)
public record VehicleDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public string Status { get; init; } = string.Empty;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public int? Mileage { get; init; }
    public string FuelType { get; init; } = string.Empty;
    public string Transmission { get; init; } = string.Empty;
    public string BodyStyle { get; init; } = string.Empty;
    public string? PrimaryImageUrl { get; init; }
    public string SellerName { get; init; } = string.Empty;
    public int ViewCount { get; init; }
    public int FavoriteCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

#endregion
