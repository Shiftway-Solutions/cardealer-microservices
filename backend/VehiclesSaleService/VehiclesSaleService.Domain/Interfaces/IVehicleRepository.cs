using VehiclesSaleService.Domain.Entities;

namespace VehiclesSaleService.Domain.Interfaces;

/// <summary>
/// Result of seller stats aggregation (computed at database level).
/// </summary>
public record SellerVehicleStatsResult
{
    public int TotalListings { get; init; }
    public int ActiveListings { get; init; }
    public int SoldListings { get; init; }
    public int PendingListings { get; init; }
    public long TotalViews { get; init; }
    public long TotalFavorites { get; init; }
}

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id);
    Task<Vehicle?> GetByVINAsync(string vin);
    Task<IEnumerable<Vehicle>> GetAllAsync(int skip = 0, int take = 100);
    Task<IEnumerable<Vehicle>> SearchAsync(VehicleSearchParameters parameters);
    Task<(IEnumerable<Vehicle> Items, int TotalCount)> GetBySellerAsync(Guid sellerId, int page = 1, int pageSize = 12, VehicleStatus? status = null);
    Task<(IEnumerable<Vehicle> Items, int TotalCount)> GetByDealerAsync(Guid dealerId, int page = 1, int pageSize = 12);
    Task<SellerVehicleStatsResult> GetSellerStatsAsync(Guid sellerId);
    Task<IEnumerable<Vehicle>> GetFeaturedAsync(int take = 10);
    Task<IEnumerable<Vehicle>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<Vehicle> CreateAsync(Vehicle vehicle);
    Task UpdateAsync(Vehicle vehicle);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<int> GetCountAsync(VehicleSearchParameters? parameters = null);

    /// <summary>
    /// Returns active (non-featured, non-sold) vehicles published more than <paramref name="daysOld"/> days ago.
    /// Used by the listing inactivity upsell worker to suggest Featured Listing to dealers with stale inventory.
    /// </summary>
    Task<IEnumerable<Vehicle>> GetStaleActiveListingsAsync(int daysOld, int skip = 0, int take = 100);
}

public class VehicleSearchParameters
{
    public string? SearchTerm { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
    public int? MaxMileage { get; set; }
    public VehicleType? VehicleType { get; set; }
    public BodyStyle? BodyStyle { get; set; }
    public FuelType? FuelType { get; set; }
    public TransmissionType? Transmission { get; set; }
    public Entities.DriveType? DriveType { get; set; }
    public VehicleCondition? Condition { get; set; }
    public string? ExteriorColor { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
    public bool? IsCertified { get; set; }
    public bool? HasCleanTitle { get; set; }
    /// <summary>Seller type: Seller (particular) or Dealer (concesionario)</summary>
    public SellerType? SellerType { get; set; }

    // Extended DR-market filters
    /// <summary>Minimum passenger seats (e.g. 5 for family SUV, 7 for 7-seater)</summary>
    public int? MinSeats { get; set; }
    /// <summary>Number of cylinders (3, 4, 6, 8) — very common filter in DR market</summary>
    public int? Cylinders { get; set; }
    /// <summary>Interior color (negro, gris, beige, marrón, crema)</summary>
    public string? InteriorColor { get; set; }
    /// <summary>Vehicle features to filter on (A/C, GPS, Sunroof, etc.) — all must match</summary>
    public List<string>? Features { get; set; }

    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}
