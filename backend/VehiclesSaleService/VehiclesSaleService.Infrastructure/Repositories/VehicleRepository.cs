using Microsoft.EntityFrameworkCore;
using VehiclesSaleService.Domain.Entities;
using VehiclesSaleService.Domain.Interfaces;
using VehiclesSaleService.Infrastructure.Persistence;

namespace VehiclesSaleService.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly ApplicationDbContext _context;

    public VehicleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id)
    {
        return await _context.Vehicles
            .Include(v => v.Images)
            .Include(v => v.Category)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
    }

    public async Task<Vehicle?> GetByVINAsync(string vin)
    {
        return await _context.Vehicles
            .Include(v => v.Images)
            .Include(v => v.Category)
            .FirstOrDefaultAsync(v => v.VIN == vin && !v.IsDeleted);
    }

    public async Task<IEnumerable<Vehicle>> GetAllAsync(int skip = 0, int take = 100)
    {
        return await _context.Vehicles
            .Include(v => v.Images.Where(i => i.IsPrimary))
            .Include(v => v.Category)
            .Where(v => !v.IsDeleted)
            .OrderByDescending(v => v.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<Vehicle>> SearchAsync(VehicleSearchParameters p)
    {
        var query = BuildSearchQuery(p);

        // Premium/Featured boost: Premium vehicles (paid campaigns) appear first,
        // then Featured (admin-curated), then normal vehicles.
        // This is the core monetization lever for the Freemium model — sellers pay
        // for visibility via Premium placement and OKLA Coins.
        // Only apply boost when not using a specific user-requested sort.
        var now = DateTime.UtcNow;

        // Sorting with promotion boost
        query = p.SortBy?.ToLower() switch
        {
            "price" => p.SortDescending
                ? query.OrderByDescending(v => v.IsPremium && (v.FeaturedUntil == null || v.FeaturedUntil > now) ? 2 : v.IsFeatured ? 1 : 0)
                    .ThenByDescending(v => v.Price)
                : query.OrderByDescending(v => v.IsPremium && (v.FeaturedUntil == null || v.FeaturedUntil > now) ? 2 : v.IsFeatured ? 1 : 0)
                    .ThenBy(v => v.Price),
            "year" => p.SortDescending
                ? query.OrderByDescending(v => v.IsPremium && (v.FeaturedUntil == null || v.FeaturedUntil > now) ? 2 : v.IsFeatured ? 1 : 0)
                    .ThenByDescending(v => v.Year)
                : query.OrderByDescending(v => v.IsPremium && (v.FeaturedUntil == null || v.FeaturedUntil > now) ? 2 : v.IsFeatured ? 1 : 0)
                    .ThenBy(v => v.Year),
            "mileage" => p.SortDescending
                ? query.OrderByDescending(v => v.IsPremium && (v.FeaturedUntil == null || v.FeaturedUntil > now) ? 2 : v.IsFeatured ? 1 : 0)
                    .ThenByDescending(v => v.Mileage)
                : query.OrderByDescending(v => v.IsPremium && (v.FeaturedUntil == null || v.FeaturedUntil > now) ? 2 : v.IsFeatured ? 1 : 0)
                    .ThenBy(v => v.Mileage),
            "make" => p.SortDescending
                ? query.OrderByDescending(v => v.IsPremium && (v.FeaturedUntil == null || v.FeaturedUntil > now) ? 2 : v.IsFeatured ? 1 : 0)
                    .ThenByDescending(v => v.Make)
                : query.OrderByDescending(v => v.IsPremium && (v.FeaturedUntil == null || v.FeaturedUntil > now) ? 2 : v.IsFeatured ? 1 : 0)
                    .ThenBy(v => v.Make),
            // "PublishedAt" sort: no premium boost — shows truly newest vehicles first.
            // Used by the "Más reciente" filter so buyers can discover fresh listings regardless of paid tier.
            "publishedat" => p.SortDescending
                ? query.OrderByDescending(v => v.PublishedAt ?? v.CreatedAt)
                : query.OrderBy(v => v.PublishedAt ?? v.CreatedAt),
            _ => query.OrderByDescending(v => v.IsPremium && (v.FeaturedUntil == null || v.FeaturedUntil > now) ? 2 : v.IsFeatured ? 1 : 0)
                .ThenByDescending(v => v.CreatedAt)
        };

        return await query
            .Skip(p.Skip)
            .Take(p.Take)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Vehicle> Items, int TotalCount)> GetBySellerAsync(Guid sellerId, int page = 1, int pageSize = 12, VehicleStatus? status = null)
    {
        var query = _context.Vehicles
            .Include(v => v.Images.Where(i => i.IsPrimary))
            .Include(v => v.Category)
            .Where(v => v.SellerId == sellerId && !v.IsDeleted);

        if (status.HasValue)
            query = query.Where(v => v.Status == status.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Vehicle> Items, int TotalCount)> GetByDealerAsync(Guid dealerId, int page = 1, int pageSize = 12)
    {
        var query = _context.Vehicles
            .Include(v => v.Images.Where(i => i.IsPrimary))
            .Include(v => v.Category)
            .Where(v => v.DealerId == dealerId && !v.IsDeleted);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<SellerVehicleStatsResult> GetSellerStatsAsync(Guid sellerId)
    {
        var stats = await _context.Vehicles
            .Where(v => v.SellerId == sellerId && !v.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new SellerVehicleStatsResult
            {
                TotalListings = g.Count(),
                ActiveListings = g.Count(v => v.Status == VehicleStatus.Active),
                SoldListings = g.Count(v => v.Status == VehicleStatus.Sold),
                PendingListings = g.Count(v => v.Status == VehicleStatus.PendingReview),
                TotalViews = g.Sum(v => (long)v.ViewCount),
                TotalFavorites = g.Sum(v => (long)v.FavoriteCount)
            })
            .FirstOrDefaultAsync();

        return stats ?? new SellerVehicleStatsResult();
    }

    public async Task<IEnumerable<Vehicle>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Take(50).ToList(); // Safety cap to prevent unbounded queries
        return await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.Images.Where(i => i.IsPrimary)) // Only primary image for list views
            .Include(v => v.Category)
            .Where(v => idList.Contains(v.Id) && !v.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Vehicle>> GetFeaturedAsync(int take = 10)
    {
        return await _context.Vehicles
            .Include(v => v.Images.Where(i => i.IsPrimary))
            .Include(v => v.Category)
            .Where(v => !v.IsDeleted && v.IsFeatured && v.Status == VehicleStatus.Active)
            // FIX FRONTEND-008: Exclude E2E test vehicles from featured listings
            .Where(v => v.Title == null || !v.Title.Contains("E2E"))
            .OrderByDescending(v => v.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Vehicle> CreateAsync(Vehicle vehicle)
    {
        vehicle.Id = Guid.NewGuid();
        vehicle.CreatedAt = DateTime.UtcNow;
        vehicle.UpdatedAt = DateTime.UtcNow;

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        return vehicle;
    }

    public async Task UpdateAsync(Vehicle vehicle)
    {
        vehicle.UpdatedAt = DateTime.UtcNow;
        _context.Vehicles.Update(vehicle);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle != null)
        {
            vehicle.IsDeleted = true;
            vehicle.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Vehicles
            .AnyAsync(v => v.Id == id && !v.IsDeleted);
    }

    public async Task<int> GetCountAsync(VehicleSearchParameters? parameters = null)
    {
        if (parameters == null)
        {
            return await _context.Vehicles.CountAsync(v => !v.IsDeleted);
        }

        return await BuildSearchQuery(parameters).CountAsync();
    }

    private IQueryable<Vehicle> BuildSearchQuery(VehicleSearchParameters p)
    {
        var query = _context.Vehicles
            .Include(v => v.Images.Where(i => i.IsPrimary))
            .Include(v => v.Category)
            .Where(v => !v.IsDeleted && v.Status == VehicleStatus.Active)
            // FIX FRONTEND-008: Exclude E2E test vehicles from public listings
            .Where(v => v.Title == null || !v.Title.Contains("E2E"));

        // Full-text search using PostgreSQL PlainToTsQuery (safe for any text input)
        if (!string.IsNullOrWhiteSpace(p.SearchTerm))
        {
            var term = p.SearchTerm.Trim();

            // PlainToTsQuery safely splits text into words and ANDs them.
            // Uses 'simple' config: no stemming, works for Spanish + English brand names.
            query = query.Where(v =>
                EF.Functions.ToTsVector("simple", v.Title + " " + v.Make + " " + v.Model + " " + (v.Description ?? ""))
                .Matches(EF.Functions.PlainToTsQuery("simple", term)));
        }

        if (p.CategoryId.HasValue)
            query = query.Where(v => v.CategoryId == p.CategoryId.Value);

        if (p.MinPrice.HasValue)
            query = query.Where(v => v.Price >= p.MinPrice.Value);

        if (p.MaxPrice.HasValue)
            query = query.Where(v => v.Price <= p.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(p.Make))
            query = query.Where(v => v.Make.ToLower() == p.Make.ToLower());

        if (!string.IsNullOrWhiteSpace(p.Model))
            query = query.Where(v => v.Model.ToLower() == p.Model.ToLower());

        if (p.MinYear.HasValue)
            query = query.Where(v => v.Year >= p.MinYear.Value);

        if (p.MaxYear.HasValue)
            query = query.Where(v => v.Year <= p.MaxYear.Value);

        if (p.MaxMileage.HasValue)
            query = query.Where(v => v.Mileage <= p.MaxMileage.Value);

        if (p.VehicleType.HasValue)
            query = query.Where(v => v.VehicleType == p.VehicleType.Value);

        if (p.BodyStyle.HasValue)
            query = query.Where(v => v.BodyStyle == p.BodyStyle.Value);

        if (p.FuelType.HasValue)
            query = query.Where(v => v.FuelType == p.FuelType.Value);

        if (p.Transmission.HasValue)
            query = query.Where(v => v.Transmission == p.Transmission.Value);

        if (p.DriveType.HasValue)
            query = query.Where(v => v.DriveType == p.DriveType.Value);

        if (p.Condition.HasValue)
            query = query.Where(v => v.Condition == p.Condition.Value);

        if (!string.IsNullOrWhiteSpace(p.ExteriorColor))
            query = query.Where(v => v.ExteriorColor!.ToLower() == p.ExteriorColor.ToLower());

        if (!string.IsNullOrWhiteSpace(p.State))
            query = query.Where(v => v.State!.ToLower() == p.State.ToLower());

        if (!string.IsNullOrWhiteSpace(p.City))
            query = query.Where(v => v.City!.ToLower() == p.City.ToLower());

        if (!string.IsNullOrWhiteSpace(p.ZipCode))
            query = query.Where(v => v.ZipCode == p.ZipCode);

        if (p.IsCertified.HasValue)
            query = query.Where(v => v.IsCertified == p.IsCertified.Value);

        if (p.HasCleanTitle.HasValue)
            query = query.Where(v => v.HasCleanTitle == p.HasCleanTitle.Value);

        if (p.SellerType.HasValue)
            query = query.Where(v => v.SellerType == p.SellerType.Value);

        // Extended DR-market filters
        if (p.MinSeats.HasValue)
            query = query.Where(v => v.Seats >= p.MinSeats.Value);

        if (p.Cylinders.HasValue)
            query = query.Where(v => v.Cylinders == p.Cylinders.Value);

        if (!string.IsNullOrWhiteSpace(p.InteriorColor))
            query = query.Where(v => v.InteriorColor != null &&
                v.InteriorColor.ToLower() == p.InteriorColor.ToLower());

        if (p.Features != null && p.Features.Count > 0)
        {
            foreach (var feature in p.Features)
            {
                var f = feature.ToLower();
                query = query.Where(v => v.FeaturesJson != null &&
                    v.FeaturesJson.ToLower().Contains(f));
            }
        }

        return query;
    }

    public async Task<(IEnumerable<Vehicle> Items, int TotalCount)> GetPagedVehiclesAsync(int page, int pageSize, VehicleSearchParameters parameters)
    {
        var query = BuildSearchQuery(parameters);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<Vehicle>> GetStaleActiveListingsAsync(int daysOld, int skip = 0, int take = 100)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

        return await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.Images.Where(i => i.IsPrimary))
            .Where(v => !v.IsDeleted
                        && v.Status == VehicleStatus.Active
                        && !v.IsFeatured
                        && !v.IsPremium
                        && v.PublishedAt != null
                        && v.PublishedAt <= cutoffDate
                        && v.SoldAt == null)
            .OrderBy(v => v.PublishedAt) // Oldest first
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
}
