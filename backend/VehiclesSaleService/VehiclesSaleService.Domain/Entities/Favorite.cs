using CarDealer.Shared.MultiTenancy;

namespace VehiclesSaleService.Domain.Entities;

/// <summary>
/// Vehículo marcado como favorito por un usuario
/// </summary>
public class Favorite : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; } // Multi-tenant (siempre 0 para usuarios finales)

    // Relaciones
    public Guid UserId { get; set; }
    public Guid VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Opcional: Notas personales del usuario
    public string? Notes { get; set; }

    // Opcional: Notificar cambios de precio
    public bool NotifyPriceChange { get; set; } = false;
}
