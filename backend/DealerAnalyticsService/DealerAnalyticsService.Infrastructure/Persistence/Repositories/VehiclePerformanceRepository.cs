using DealerAnalyticsService.Domain.Entities;
using DealerAnalyticsService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DealerAnalyticsService.Infrastructure.Persistence.Repositories;

public class VehiclePerformanceRepository : IVehiclePerformanceRepository
{
    private readonly DealerAnalyticsDbContext _context;
    private readonly ILogger<VehiclePerformanceRepository> _logger;

    public VehiclePerformanceRepository(
        DealerAnalyticsDbContext context,
        ILogger<VehiclePerformanceRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<VehiclePerformance?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.VehiclePerformances.FindAsync(new object[] { id }, ct);
    }

    public async Task<VehiclePerformance?> GetByVehicleAndDateAsync(
        Guid vehicleId, DateTime date, CancellationToken ct = default)
    {
        return await _context.VehiclePerformances
            .Where(v => v.VehicleId == vehicleId && v.Date.Date == date.Date)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<VehiclePerformance>> GetByDealerAsync(
        Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        return await _context.VehiclePerformances
            .Where(v => v.DealerId == dealerId &&
                        v.Date >= fromDate.Date &&
                        v.Date <= toDate.Date)
            .OrderByDescending(v => v.PerformanceScore)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<VehiclePerformance>> GetByVehicleAsync(
        Guid vehicleId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        return await _context.VehiclePerformances
            .Where(v => v.VehicleId == vehicleId &&
                        v.Date >= fromDate.Date &&
                        v.Date <= toDate.Date)
            .OrderBy(v => v.Date)
            .ToListAsync(ct);
    }

    public async Task<VehiclePerformance> CreateAsync(VehiclePerformance performance, CancellationToken ct = default)
    {
        performance.CalculatePerformanceScore();
        _context.VehiclePerformances.Add(performance);
        await _context.SaveChangesAsync(ct);
        return performance;
    }

    public async Task<VehiclePerformance> UpdateAsync(VehiclePerformance performance, CancellationToken ct = default)
    {
        performance.UpdatedAt = DateTime.UtcNow;
        performance.CalculatePerformanceScore();
        _context.VehiclePerformances.Update(performance);
        await _context.SaveChangesAsync(ct);
        return performance;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var performance = await GetByIdAsync(id, ct);
        if (performance != null)
        {
            _context.VehiclePerformances.Remove(performance);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<IEnumerable<VehiclePerformance>> GetTopPerformersAsync(
        Guid dealerId, int limit, CancellationToken ct = default)
    {
        // Get latest performance for each vehicle
        var latestDate = await _context.VehiclePerformances
            .Where(v => v.DealerId == dealerId)
            .MaxAsync(v => (DateTime?)v.Date, ct) ?? DateTime.UtcNow.AddDays(-1);

        return await _context.VehiclePerformances
            .Where(v => v.DealerId == dealerId && v.Date.Date == latestDate.Date && !v.IsSold)
            .OrderByDescending(v => v.PerformanceScore)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<VehiclePerformance>> GetTopByViewsAsync(
        Guid dealerId, int limit, DateTime? fromDate = null, CancellationToken ct = default)
    {
        var query = _context.VehiclePerformances.Where(v => v.DealerId == dealerId && !v.IsSold);

        if (fromDate.HasValue)
            query = query.Where(v => v.Date >= fromDate.Value);

        return await query
            .GroupBy(v => v.VehicleId)
            .Select(g => new { VehicleId = g.Key, TotalViews = g.Sum(v => v.Views), Latest = g.OrderByDescending(v => v.Date).First() })
            .OrderByDescending(x => x.TotalViews)
            .Take(limit)
            .Select(x => x.Latest)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<VehiclePerformance>> GetTopByContactsAsync(
        Guid dealerId, int limit, DateTime? fromDate = null, CancellationToken ct = default)
    {
        var query = _context.VehiclePerformances.Where(v => v.DealerId == dealerId && !v.IsSold);

        if (fromDate.HasValue)
            query = query.Where(v => v.Date >= fromDate.Value);

        return await query
            .GroupBy(v => v.VehicleId)
            .Select(g => new { VehicleId = g.Key, TotalContacts = g.Sum(v => v.Contacts), Latest = g.OrderByDescending(v => v.Date).First() })
            .OrderByDescending(x => x.TotalContacts)
            .Take(limit)
            .Select(x => x.Latest)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<VehiclePerformance>> GetTopByEngagementAsync(
        Guid dealerId, int limit, DateTime? fromDate = null, CancellationToken ct = default)
    {
        var query = _context.VehiclePerformances.Where(v => v.DealerId == dealerId && !v.IsSold);

        if (fromDate.HasValue)
            query = query.Where(v => v.Date >= fromDate.Value);

        return await query
            .GroupBy(v => v.VehicleId)
            .Select(g => new { VehicleId = g.Key, AvgEngagement = g.Average(v => v.EngagementScore), Latest = g.OrderByDescending(v => v.Date).First() })
            .OrderByDescending(x => x.AvgEngagement)
            .Take(limit)
            .Select(x => x.Latest)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<VehiclePerformance>> GetLowPerformersAsync(
        Guid dealerId, int limit, CancellationToken ct = default)
    {
        var latestDate = await _context.VehiclePerformances
            .Where(v => v.DealerId == dealerId)
            .MaxAsync(v => (DateTime?)v.Date, ct) ?? DateTime.UtcNow.AddDays(-1);

        return await _context.VehiclePerformances
            .Where(v => v.DealerId == dealerId &&
                        v.Date.Date == latestDate.Date &&
                        !v.IsSold &&
                        v.DaysOnMarket > 30) // At least 30 days old
            .OrderBy(v => v.PerformanceScore)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<VehiclePerformance> AggregateByVehicleAsync(
        Guid vehicleId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var performances = await GetByVehicleAsync(vehicleId, fromDate, toDate, ct);
        var list = performances.ToList();

        if (!list.Any())
        {
            return new VehiclePerformance { VehicleId = vehicleId };
        }

        var latest = list.Last();

        return new VehiclePerformance
        {
            VehicleId = vehicleId,
            DealerId = latest.DealerId,
            VehicleTitle = latest.VehicleTitle,
            VehicleMake = latest.VehicleMake,
            VehicleModel = latest.VehicleModel,
            VehicleYear = latest.VehicleYear,
            VehiclePrice = latest.VehiclePrice,
            VehicleThumbnailUrl = latest.VehicleThumbnailUrl,
            Date = toDate.Date,
            Views = list.Sum(p => p.Views),
            UniqueViews = list.Sum(p => p.UniqueViews),
            Contacts = list.Sum(p => p.Contacts),
            PhoneCalls = list.Sum(p => p.PhoneCalls),
            WhatsAppClicks = list.Sum(p => p.WhatsAppClicks),
            EmailInquiries = list.Sum(p => p.EmailInquiries),
            Favorites = list.Sum(p => p.Favorites),
            ShareClicks = list.Sum(p => p.ShareClicks),
            SearchImpressions = list.Sum(p => p.SearchImpressions),
            SearchClicks = list.Sum(p => p.SearchClicks),
            AvgViewDurationSeconds = list.Average(p => p.AvgViewDurationSeconds),
            PhotoGalleryViews = list.Sum(p => p.PhotoGalleryViews),
            DaysOnMarket = latest.DaysOnMarket,
            IsSold = latest.IsSold,
            SoldDate = latest.SoldDate,
            EngagementScore = list.Average(p => p.EngagementScore),
            PerformanceScore = list.Average(p => p.PerformanceScore)
        };
    }

    public async Task<Dictionary<Guid, VehiclePerformance>> AggregateByDealerAsync(
        Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var performances = await GetByDealerAsync(dealerId, fromDate, toDate, ct);

        return performances
            .GroupBy(p => p.VehicleId)
            .ToDictionary(
                g => g.Key,
                g => new VehiclePerformance
                {
                    VehicleId = g.Key,
                    DealerId = dealerId,
                    Views = g.Sum(p => p.Views),
                    Contacts = g.Sum(p => p.Contacts),
                    Favorites = g.Sum(p => p.Favorites),
                    PerformanceScore = g.Average(p => p.PerformanceScore)
                }
            );
    }

    public async Task<double> GetAverageViewsPerVehicleAsync(
        Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var performances = await GetByDealerAsync(dealerId, fromDate, toDate, ct);
        var grouped = performances.GroupBy(p => p.VehicleId).ToList();

        if (!grouped.Any()) return 0;

        return grouped.Average(g => g.Sum(p => p.Views));
    }

    public async Task<double> GetAverageContactRateAsync(
        Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var performances = await GetByDealerAsync(dealerId, fromDate, toDate, ct);
        var list = performances.ToList();

        if (!list.Any()) return 0;

        var totalViews = list.Sum(p => p.Views);
        var totalContacts = list.Sum(p => p.Contacts);

        return totalViews > 0 ? (double)totalContacts / totalViews * 100 : 0;
    }
}
