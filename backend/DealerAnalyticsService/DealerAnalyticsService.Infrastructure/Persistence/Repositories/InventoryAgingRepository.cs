using DealerAnalyticsService.Domain.Entities;
using DealerAnalyticsService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DealerAnalyticsService.Infrastructure.Persistence.Repositories;

public class InventoryAgingRepository : IInventoryAgingRepository
{
    private readonly DealerAnalyticsDbContext _context;
    private readonly ILogger<InventoryAgingRepository> _logger;

    public InventoryAgingRepository(
        DealerAnalyticsDbContext context,
        ILogger<InventoryAgingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InventoryAging?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.InventoryAgings.FindAsync(new object[] { id }, ct);
    }

    public async Task<InventoryAging?> GetByDateAsync(Guid dealerId, DateTime date, CancellationToken ct = default)
    {
        return await _context.InventoryAgings
            .Where(i => i.DealerId == dealerId && i.Date.Date == date.Date)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<InventoryAging>> GetHistoryAsync(Guid dealerId, int days, CancellationToken ct = default)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);
        return await _context.InventoryAgings
            .Where(i => i.DealerId == dealerId && i.Date >= fromDate)
            .OrderBy(i => i.Date)
            .ToListAsync(ct);
    }

    public async Task<InventoryAging> CreateAsync(InventoryAging aging, CancellationToken ct = default)
    {
        _context.InventoryAgings.Add(aging);
        await _context.SaveChangesAsync(ct);
        return aging;
    }

    public async Task<InventoryAging> UpdateAsync(InventoryAging aging, CancellationToken ct = default)
    {
        _context.InventoryAgings.Update(aging);
        await _context.SaveChangesAsync(ct);
        return aging;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var aging = await GetByIdAsync(id, ct);
        if (aging != null)
        {
            _context.InventoryAgings.Remove(aging);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<InventoryAging?> GetLatestAsync(Guid dealerId, CancellationToken ct = default)
    {
        return await _context.InventoryAgings
            .Where(i => i.DealerId == dealerId)
            .OrderByDescending(i => i.Date)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<AgingBucket>> GetAgingBucketsAsync(Guid dealerId, CancellationToken ct = default)
    {
        var latest = await GetLatestAsync(dealerId, ct);
        return latest?.GetBuckets() ?? new List<AgingBucket>();
    }

    public async Task<Dictionary<string, int>> GetAgingDistributionAsync(Guid dealerId, CancellationToken ct = default)
    {
        var latest = await GetLatestAsync(dealerId, ct);
        if (latest == null)
        {
            return new Dictionary<string, int>();
        }

        return new Dictionary<string, int>
        {
            ["0_to_15"] = latest.Vehicles0To15Days,
            ["16_to_30"] = latest.Vehicles16To30Days,
            ["31_to_45"] = latest.Vehicles31To45Days,
            ["46_to_60"] = latest.Vehicles46To60Days,
            ["61_to_90"] = latest.Vehicles61To90Days,
            ["over_90"] = latest.VehiclesOver90Days
        };
    }

    public async Task<InventoryAging> CalculateCurrentAsync(Guid dealerId, CancellationToken ct = default)
    {
        var latest = await GetLatestAsync(dealerId, ct);
        return latest ?? new InventoryAging { DealerId = dealerId, Date = DateTime.UtcNow };
    }

    public async Task<decimal> GetAtRiskValueAsync(Guid dealerId, int daysThreshold = 60, CancellationToken ct = default)
    {
        var latest = await GetLatestAsync(dealerId, ct);
        return latest?.AtRiskValue ?? 0;
    }

    public async Task<int> GetAtRiskCountAsync(Guid dealerId, int daysThreshold = 60, CancellationToken ct = default)
    {
        var latest = await GetLatestAsync(dealerId, ct);
        return latest?.AtRiskCount ?? 0;
    }

    public async Task<Dictionary<DateTime, double>> GetAverageDaysOnMarketTrendAsync(
        Guid dealerId, int days, CancellationToken ct = default)
    {
        var history = await GetHistoryAsync(dealerId, days, ct);
        return history.ToDictionary(i => i.Date, i => i.AverageDaysOnMarket);
    }
}
