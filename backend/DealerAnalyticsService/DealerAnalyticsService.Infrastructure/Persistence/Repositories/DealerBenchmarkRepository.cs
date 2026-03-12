using DealerAnalyticsService.Domain.Entities;
using DealerAnalyticsService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DealerAnalyticsService.Infrastructure.Persistence.Repositories;

public class DealerBenchmarkRepository : IDealerBenchmarkRepository
{
    private readonly DealerAnalyticsDbContext _context;
    private readonly ILogger<DealerBenchmarkRepository> _logger;

    public DealerBenchmarkRepository(
        DealerAnalyticsDbContext context,
        ILogger<DealerBenchmarkRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DealerBenchmark?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.DealerBenchmarks.FindAsync(new object[] { id }, ct);
    }

    public async Task<DealerBenchmark?> GetLatestAsync(Guid dealerId, CancellationToken ct = default)
    {
        return await _context.DealerBenchmarks
            .Where(b => b.DealerId == dealerId)
            .OrderByDescending(b => b.Date)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<DealerBenchmark?> GetByDateAsync(Guid dealerId, DateTime date, CancellationToken ct = default)
    {
        return await _context.DealerBenchmarks
            .Where(b => b.DealerId == dealerId && b.Date.Date == date.Date)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<DealerBenchmark>> GetHistoryAsync(Guid dealerId, int months, CancellationToken ct = default)
    {
        var fromDate = DateTime.UtcNow.AddMonths(-months);
        return await _context.DealerBenchmarks
            .Where(b => b.DealerId == dealerId && b.Date >= fromDate)
            .OrderBy(b => b.Date)
            .ToListAsync(ct);
    }

    public async Task<DealerBenchmark> CreateAsync(DealerBenchmark benchmark, CancellationToken ct = default)
    {
        benchmark.CalculateTier();
        _context.DealerBenchmarks.Add(benchmark);
        await _context.SaveChangesAsync(ct);
        return benchmark;
    }

    public async Task<DealerBenchmark> UpdateAsync(DealerBenchmark benchmark, CancellationToken ct = default)
    {
        benchmark.UpdatedAt = DateTime.UtcNow;
        benchmark.CalculateTier();
        _context.DealerBenchmarks.Update(benchmark);
        await _context.SaveChangesAsync(ct);
        return benchmark;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var benchmark = await GetByIdAsync(id, ct);
        if (benchmark != null)
        {
            _context.DealerBenchmarks.Remove(benchmark);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<IEnumerable<DealerBenchmark>> GetTopDealersAsync(int limit, string? period = null, CancellationToken ct = default)
    {
        var latestDate = await _context.DealerBenchmarks.MaxAsync(b => (DateTime?)b.Date, ct)
                         ?? DateTime.UtcNow.Date;

        var query = _context.DealerBenchmarks
            .Where(b => b.Date.Date == latestDate.Date);

        if (!string.IsNullOrEmpty(period))
        {
            query = query.Where(b => b.Period == period);
        }

        return await query
            .OrderByDescending(b => b.ConversionRate)
            .ThenByDescending(b => b.CustomerSatisfaction)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<int> GetDealerRankAsync(Guid dealerId, string? period = null, CancellationToken ct = default)
    {
        var latest = await GetLatestAsync(dealerId, ct);
        return latest?.OverallRank ?? 0;
    }

    public async Task<IEnumerable<DealerBenchmark>> GetRankingsAsync(int page, int pageSize, string? sortBy = null, CancellationToken ct = default)
    {
        var latestDate = await _context.DealerBenchmarks.MaxAsync(b => (DateTime?)b.Date, ct)
                         ?? DateTime.UtcNow.Date;

        var query = _context.DealerBenchmarks
            .Where(b => b.Date.Date == latestDate.Date);

        query = sortBy?.ToLower() switch
        {
            "conversion" => query.OrderByDescending(b => b.ConversionRate),
            "satisfaction" => query.OrderByDescending(b => b.CustomerSatisfaction),
            "response" => query.OrderBy(b => b.AvgResponseTimeMinutes),
            "days" => query.OrderBy(b => b.AvgDaysOnMarket),
            _ => query.OrderBy(b => b.OverallRank)
        };

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<DealerBenchmark> GetMarketAveragesAsync(DateTime date, CancellationToken ct = default)
    {
        var benchmarks = await _context.DealerBenchmarks
            .Where(b => b.Date.Date == date.Date)
            .ToListAsync(ct);

        if (!benchmarks.Any())
        {
            return new DealerBenchmark
            {
                DealerId = Guid.Empty,
                Date = date,
                Period = "Market"
            };
        }

        return new DealerBenchmark
        {
            DealerId = Guid.Empty,
            Date = date,
            Period = "Market",
            AvgDaysOnMarket = benchmarks.Average(b => b.AvgDaysOnMarket),
            ConversionRate = benchmarks.Average(b => b.ConversionRate),
            AvgResponseTimeMinutes = benchmarks.Average(b => b.AvgResponseTimeMinutes),
            CustomerSatisfaction = benchmarks.Average(b => b.CustomerSatisfaction),
            ListingQualityScore = benchmarks.Average(b => b.ListingQualityScore),
            ViewsPerListing = benchmarks.Average(b => b.ViewsPerListing),
            ContactsPerListing = benchmarks.Average(b => b.ContactsPerListing),
            TotalDealers = benchmarks.Count
        };
    }

    public async Task<Dictionary<string, double>> GetMarketMetricsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var benchmarks = await _context.DealerBenchmarks
            .Where(b => b.Date >= fromDate.Date && b.Date <= toDate.Date)
            .ToListAsync(ct);

        if (!benchmarks.Any())
        {
            return new Dictionary<string, double>();
        }

        return new Dictionary<string, double>
        {
            ["avg_days_on_market"] = benchmarks.Average(b => b.AvgDaysOnMarket),
            ["avg_conversion_rate"] = benchmarks.Average(b => b.ConversionRate),
            ["avg_response_time"] = benchmarks.Average(b => b.AvgResponseTimeMinutes),
            ["avg_satisfaction"] = benchmarks.Average(b => b.CustomerSatisfaction),
            ["avg_listing_quality"] = benchmarks.Average(b => b.ListingQualityScore),
            ["avg_views_per_listing"] = benchmarks.Average(b => b.ViewsPerListing),
            ["avg_contacts_per_listing"] = benchmarks.Average(b => b.ContactsPerListing)
        };
    }

    public async Task<Dictionary<DealerTier, int>> GetDealersByTierAsync(CancellationToken ct = default)
    {
        var latestDate = await _context.DealerBenchmarks.MaxAsync(b => (DateTime?)b.Date, ct)
                         ?? DateTime.UtcNow.Date;

        return await _context.DealerBenchmarks
            .Where(b => b.Date.Date == latestDate.Date)
            .GroupBy(b => b.Tier)
            .Select(g => new { Tier = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Tier, x => x.Count, ct);
    }

    public async Task<DealerTier> CalculateDealerTierAsync(Guid dealerId, CancellationToken ct = default)
    {
        var latest = await GetLatestAsync(dealerId, ct);
        return latest?.Tier ?? DealerTier.Bronze;
    }

    public async Task BulkUpdateRankingsAsync(DateTime date, CancellationToken ct = default)
    {
        var benchmarks = await _context.DealerBenchmarks
            .Where(b => b.Date.Date == date.Date)
            .ToListAsync(ct);

        var totalDealers = benchmarks.Count;

        // Rank by conversion rate descending
        var ordered = benchmarks.OrderByDescending(b => b.ConversionRate).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].OverallRank = i + 1;
            ordered[i].TotalDealers = totalDealers;
            ordered[i].ConversionRatePercentile = (int)((double)(totalDealers - i) / totalDealers * 100);
        }

        // Rank by response time ascending (lower is better)
        ordered = benchmarks.OrderBy(b => b.AvgResponseTimeMinutes).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].ResponseTimePercentile = (int)((double)(totalDealers - i) / totalDealers * 100);
        }

        // Rank by satisfaction descending
        ordered = benchmarks.OrderByDescending(b => b.CustomerSatisfaction).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].SatisfactionPercentile = (int)((double)(totalDealers - i) / totalDealers * 100);
        }

        // Update tiers
        foreach (var benchmark in benchmarks)
        {
            benchmark.CalculateTier();
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Updated rankings for {Count} dealers on {Date}", totalDealers, date);
    }

    public async Task CalculateAllBenchmarksAsync(DateTime date, CancellationToken ct = default)
    {
        // Get all dealers with data
        var dealerIds = await _context.DealerBenchmarks
            .Where(b => b.Date.Date == date.Date)
            .Select(b => b.DealerId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var dealerId in dealerIds)
        {
            var benchmark = await GetByDateAsync(dealerId, date, ct);
            if (benchmark != null)
            {
                benchmark.CalculateTier();
                await UpdateAsync(benchmark, ct);
            }
        }

        await BulkUpdateRankingsAsync(date, ct);
        _logger.LogInformation("Calculated benchmarks for {Count} dealers on {Date}", dealerIds.Count, date);
    }
}
