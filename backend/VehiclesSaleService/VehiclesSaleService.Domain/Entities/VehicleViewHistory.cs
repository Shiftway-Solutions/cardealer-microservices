using CarDealer.Shared.MultiTenancy;

namespace VehiclesSaleService.Domain.Entities;

/// <summary>
/// Historial de vehículos vistos por un usuario
/// </summary>
public class VehicleViewHistory : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DealerId { get; set; } // Multi-tenant (always 0 for end users)

    // Relations
    public Guid UserId { get; set; }
    public Guid VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    // Metadata
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
}
