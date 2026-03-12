using DealerAnalyticsService.Domain.Entities;
using DealerAnalyticsService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DealerAnalyticsService.Infrastructure.Persistence.Repositories;

public class DealerSnapshotRepository : IDealerSnapshotRepository
{
    private readonly DealerAnalyticsDbContext _context;
    private readonly ILogger<DealerSnapshotRepository> _logger;

    public DealerSnapshotRepository(
        DealerAnalyticsDbContext context,
        ILogger<DealerSnapshotRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DealerSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.DealerSnapshots.FindAsync(new object[] { id }, ct);
    }

    public async Task<DealerSnapshot?> GetLatestAsync(Guid dealerId, CancellationToken ct = default)
    {
        return await _context.DealerSnapshots
            .Where(s => s.DealerId == dealerId)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<DealerSnapshot?> GetByDateAsync(Guid dealerId, DateTime date, CancellationToken ct = default)
    {
        return await _context.DealerSnapshots
            .Where(s => s.DealerId == dealerId && s.SnapshotDate.Date == date.Date)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<DealerSnapshot>> GetRangeAsync(
        Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        return await _context.DealerSnapshots
            .Where(s => s.DealerId == dealerId &&
                        s.SnapshotDate >= fromDate.Date &&
                        s.SnapshotDate <= toDate.Date)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync(ct);
    }

    public async Task<DealerSnapshot> CreateAsync(DealerSnapshot snapshot, CancellationToken ct = default)
    {
        _context.DealerSnapshots.Add(snapshot);
        await _context.SaveChangesAsync(ct);
        return snapshot;
    }

    public async Task<DealerSnapshot> UpdateAsync(DealerSnapshot snapshot, CancellationToken ct = default)
    {
        snapshot.UpdatedAt = DateTime.UtcNow;
        _context.DealerSnapshots.Update(snapshot);
        await _context.SaveChangesAsync(ct);
        return snapshot;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var snapshot = await GetByIdAsync(id, ct);
        if (snapshot != null)
        {
            _context.DealerSnapshots.Remove(snapshot);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task BulkInsertAsync(IEnumerable<DealerSnapshot> snapshots, CancellationToken ct = default)
    {
        await _context.DealerSnapshots.AddRangeAsync(snapshots, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<DealerSnapshot> AggregateAsync(
        Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var snapshots = await GetRangeAsync(dealerId, fromDate, toDate, ct);
        var list = snapshots.ToList();

        if (!list.Any())
        {
            return DealerSnapshot.CreateEmpty(dealerId, toDate);
        }

        return new DealerSnapshot
        {
            DealerId = dealerId,
            SnapshotDate = toDate.Date,
            TotalVehicles = list.LastOrDefault()?.TotalVehicles ?? 0,
            ActiveVehicles = list.LastOrDefault()?.ActiveVehicles ?? 0,
            SoldVehicles = list.Sum(s => s.SoldVehicles),
            TotalInventoryValue = list.LastOrDefault()?.TotalInventoryValue ?? 0,
            AvgVehiclePrice = list.Average(s => s.AvgVehiclePrice),
            AvgDaysOnMarket = list.Average(s => s.AvgDaysOnMarket),
            VehiclesOver60Days = list.LastOrDefault()?.VehiclesOver60Days ?? 0,
            TotalViews = list.Sum(s => s.TotalViews),
            UniqueViews = list.Sum(s => s.UniqueViews),
            TotalContacts = list.Sum(s => s.TotalContacts),
            PhoneCalls = list.Sum(s => s.PhoneCalls),
            WhatsAppMessages = list.Sum(s => s.WhatsAppMessages),
            EmailInquiries = list.Sum(s => s.EmailInquiries),
            TotalFavorites = list.Sum(s => s.TotalFavorites),
            SearchImpressions = list.Sum(s => s.SearchImpressions),
            SearchClicks = list.Sum(s => s.SearchClicks),
            NewLeads = list.Sum(s => s.NewLeads),
            QualifiedLeads = list.Sum(s => s.QualifiedLeads),
            HotLeads = list.Sum(s => s.HotLeads),
            ConvertedLeads = list.Sum(s => s.ConvertedLeads),
            LeadConversionRate = list.Average(s => s.LeadConversionRate),
            AvgResponseTimeMinutes = list.Average(s => s.AvgResponseTimeMinutes),
            TotalRevenue = list.Sum(s => s.TotalRevenue),
            AvgTransactionValue = list.Where(s => s.AvgTransactionValue > 0).DefaultIfEmpty().Average(s => s?.AvgTransactionValue ?? 0),
            TransactionCount = list.Sum(s => s.TransactionCount),
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<(DealerSnapshot? current, DealerSnapshot? previous)> GetComparisonAsync(
        Guid dealerId, DateTime currentDate, int compareDays, CancellationToken ct = default)
    {
        var current = await GetLatestAsync(dealerId, ct);

        var previousDate = currentDate.AddDays(-compareDays);
        var previous = await GetByDateAsync(dealerId, previousDate, ct);

        // Si no hay snapshot exacto, buscar el más cercano
        if (previous == null)
        {
            previous = await _context.DealerSnapshots
                .Where(s => s.DealerId == dealerId && s.SnapshotDate <= previousDate)
                .OrderByDescending(s => s.SnapshotDate)
                .FirstOrDefaultAsync(ct);
        }

        return (current, previous);
    }

    public async Task<double> GetAverageMetricAsync(
        Guid dealerId, string metricName, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var snapshots = await GetRangeAsync(dealerId, fromDate, toDate, ct);
        var list = snapshots.ToList();

        if (!list.Any()) return 0;

        return metricName.ToLower() switch
        {
            "views" => list.Average(s => s.TotalViews),
            "contacts" => list.Average(s => s.TotalContacts),
            "leads" => list.Average(s => s.QualifiedLeads),
            "sales" => list.Average(s => s.ConvertedLeads),
            "revenue" => (double)list.Average(s => s.TotalRevenue),
            "conversion" => list.Average(s => s.LeadConversionRate),
            "daysonmarket" => list.Average(s => s.AvgDaysOnMarket),
            _ => 0
        };
    }

    public async Task<Dictionary<DateTime, double>> GetMetricTrendAsync(
        Guid dealerId, string metricName, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var snapshots = await GetRangeAsync(dealerId, fromDate, toDate, ct);

        return snapshots.ToDictionary(
            s => s.SnapshotDate,
            s => metricName.ToLower() switch
            {
                "views" => (double)s.TotalViews,
                "contacts" => (double)s.TotalContacts,
                "leads" => (double)s.QualifiedLeads,
                "sales" => (double)s.ConvertedLeads,
                "revenue" => (double)s.TotalRevenue,
                "conversion" => s.LeadConversionRate,
                "daysonmarket" => s.AvgDaysOnMarket,
                _ => 0
            }
        );
    }
}
