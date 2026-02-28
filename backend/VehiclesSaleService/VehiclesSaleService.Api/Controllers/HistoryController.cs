using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VehiclesSaleService.Domain.Entities;
using VehiclesSaleService.Infrastructure.Persistence;

namespace VehiclesSaleService.Api.Controllers;

/// <summary>
/// Historial de vehículos vistos por el usuario
/// Rutas: GET/POST/DELETE /api/history/views
/// </summary>
[ApiController]
[Route("api/history/views")]
[Authorize]
public class HistoryController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<HistoryController> _logger;
    private const int MaxHistoryItems = 100;
    private const int DefaultDays = 30;

    public HistoryController(ApplicationDbContext db, ILogger<HistoryController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el historial de vehículos vistos por el usuario actual
    /// GET /api/history/views
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ViewingHistoryResponse>> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int days = DefaultDays)
    {
        var userId = GetCurrentUserId();
        var since = DateTime.UtcNow.AddDays(-days);

        // Get history with vehicle data (most recent first, deduplicated by vehicleId)
        var query = _db.VehicleViewHistories
            .Where(h => h.UserId == userId && h.ViewedAt >= since)
            .Include(h => h.Vehicle)
                .ThenInclude(v => v!.Images)
            .OrderByDescending(h => h.ViewedAt)
            .AsNoTracking();

        var total = await query.CountAsync();

        var historyItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Get favorite vehicleIds for the user (to set isFavorite)
        var vehicleIds = historyItems.Select(h => h.VehicleId).ToHashSet();
        var favoriteVehicleIds = await _db.Favorites
            .Where(f => f.UserId == userId && vehicleIds.Contains(f.VehicleId))
            .Select(f => f.VehicleId)
            .ToHashSetAsync();

        var oldestDate = historyItems.Count > 0
            ? historyItems.Min(h => h.ViewedAt)
            : (DateTime?)null;

        var totalFavorites = historyItems.Count(h => favoriteVehicleIds.Contains(h.VehicleId));

        var items = historyItems
            .Where(h => h.Vehicle != null)
            .Select(h => new ViewedVehicleDto
            {
                Id = h.Id,
                VehicleId = h.VehicleId,
                ViewedAt = h.ViewedAt,
                IsFavorite = favoriteVehicleIds.Contains(h.VehicleId),
                Vehicle = MapVehicle(h.Vehicle!)
            })
            .ToList();

        return Ok(new ViewingHistoryResponse
        {
            Items = items,
            Total = total,
            TotalFavorites = totalFavorites,
            OldestDate = oldestDate?.ToString("O")
        });
    }

    /// <summary>
    /// Registra que el usuario vio un vehículo
    /// POST /api/history/views/{vehicleId}
    /// </summary>
    [HttpPost("{vehicleId:guid}")]
    public async Task<ActionResult> RecordView(Guid vehicleId)
    {
        var userId = GetCurrentUserId();

        // Check vehicle exists
        var vehicleExists = await _db.Vehicles.AnyAsync(v => v.Id == vehicleId);
        if (!vehicleExists)
            return NotFound();

        // Upsert: update ViewedAt if already exists, otherwise insert
        var existing = await _db.VehicleViewHistories
            .FirstOrDefaultAsync(h => h.UserId == userId && h.VehicleId == vehicleId);

        if (existing != null)
        {
            existing.ViewedAt = DateTime.UtcNow;
        }
        else
        {
            _db.VehicleViewHistories.Add(new VehicleViewHistory
            {
                UserId = userId,
                VehicleId = vehicleId,
                ViewedAt = DateTime.UtcNow
            });

            // Trim old history entries to keep max items
            var historyCount = await _db.VehicleViewHistories
                .CountAsync(h => h.UserId == userId);

            if (historyCount > MaxHistoryItems)
            {
                var oldest = await _db.VehicleViewHistories
                    .Where(h => h.UserId == userId)
                    .OrderBy(h => h.ViewedAt)
                    .Take(historyCount - MaxHistoryItems)
                    .ToListAsync();

                _db.VehicleViewHistories.RemoveRange(oldest);
            }
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Elimina un vehículo del historial
    /// DELETE /api/history/views/{vehicleId}
    /// </summary>
    [HttpDelete("{vehicleId:guid}")]
    public async Task<ActionResult> RemoveFromHistory(Guid vehicleId)
    {
        var userId = GetCurrentUserId();

        var items = await _db.VehicleViewHistories
            .Where(h => h.UserId == userId && h.VehicleId == vehicleId)
            .ToListAsync();

        if (items.Count == 0)
            return NotFound();

        _db.VehicleViewHistories.RemoveRange(items);
        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Limpia todo el historial del usuario
    /// DELETE /api/history/views
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult> ClearHistory()
    {
        var userId = GetCurrentUserId();

        var items = await _db.VehicleViewHistories
            .Where(h => h.UserId == userId)
            .ToListAsync();

        _db.VehicleViewHistories.RemoveRange(items);
        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Sincroniza historial local (de sesión no autenticada) con el servidor
    /// POST /api/history/views/sync
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult> SyncHistory([FromBody] SyncHistoryRequest request)
    {
        if (request?.Items == null || request.Items.Count == 0)
            return Ok();

        var userId = GetCurrentUserId();

        foreach (var item in request.Items)
        {
            if (!Guid.TryParse(item.VehicleId, out var vehicleId)) continue;

            var existing = await _db.VehicleViewHistories
                .FirstOrDefaultAsync(h => h.UserId == userId && h.VehicleId == vehicleId);

            if (existing != null)
            {
                // Keep the most recent timestamp
                if (item.ViewedAt > existing.ViewedAt)
                    existing.ViewedAt = item.ViewedAt;
            }
            else
            {
                _db.VehicleViewHistories.Add(new VehicleViewHistory
                {
                    UserId = userId,
                    VehicleId = vehicleId,
                    ViewedAt = item.ViewedAt
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Usuario no autenticado");

        return userId;
    }

    private static string GenerateSlug(Vehicle vehicle)
    {
        var make = vehicle.Make?.ToLower().Replace(" ", "-") ?? "auto";
        var model = vehicle.Model?.ToLower().Replace(" ", "-") ?? "modelo";
        var baseSlug = $"{vehicle.Year}-{make}-{model}";
        var shortId = vehicle.Id.ToString("N")[..8];
        return $"{baseSlug}-{shortId}";
    }

    private static HistoryVehicleDto MapVehicle(Vehicle v)
    {
        var primaryImage = v.Images
            .FirstOrDefault(i => i.IsPrimary)?.Url
            ?? v.Images.FirstOrDefault()?.Url;

        return new HistoryVehicleDto
        {
            Id = v.Id,
            Slug = GenerateSlug(v),
            Title = v.Title,
            Make = v.Make,
            Model = v.Model,
            Year = v.Year,
            Price = (double)v.Price,
            Mileage = v.Mileage,
            Location = v.City ?? v.SellerCity ?? "República Dominicana",
            ImageUrl = primaryImage,
            DealerName = v.SellerName,
            Status = v.Status.ToString().ToLower()
        };
    }
}

// =============================================================================
// DTOs
// =============================================================================

public record ViewingHistoryResponse
{
    public IReadOnlyList<ViewedVehicleDto> Items { get; init; } = [];
    public int Total { get; init; }
    public int TotalFavorites { get; init; }
    public string? OldestDate { get; init; }
}

public record ViewedVehicleDto
{
    public Guid Id { get; init; }
    public Guid VehicleId { get; init; }
    public DateTime ViewedAt { get; init; }
    public bool IsFavorite { get; init; }
    public HistoryVehicleDto Vehicle { get; init; } = new();
}

public record HistoryVehicleDto
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public double Price { get; init; }
    public int Mileage { get; init; }
    public string Location { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string DealerName { get; init; } = string.Empty;
    public string Status { get; init; } = "active";
}

public record SyncHistoryRequest
{
    public List<SyncHistoryItem> Items { get; init; } = [];
}

public record SyncHistoryItem
{
    public string VehicleId { get; init; } = string.Empty;
    public DateTime ViewedAt { get; init; }
}
