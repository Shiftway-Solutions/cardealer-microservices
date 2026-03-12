using Microsoft.EntityFrameworkCore;
using DealerAnalyticsService.Domain.Entities;
using DealerAnalyticsService.Domain.Interfaces;

namespace DealerAnalyticsService.Infrastructure.Persistence.Repositories;

public class ConversionFunnelRepository : IConversionFunnelRepository
{
    private readonly DealerAnalyticsDbContext _context;

    public ConversionFunnelRepository(DealerAnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<ConversionFunnel?> GetFunnelAsync(Guid dealerId, DateTime date)
    {
        return await _context.ConversionFunnels
            .FirstOrDefaultAsync(x => x.DealerId == dealerId && x.Date.Date == date.Date);
    }

    public async Task<IEnumerable<ConversionFunnel>> GetFunnelRangeAsync(Guid dealerId, DateTime fromDate, DateTime toDate)
    {
        return await _context.ConversionFunnels
            .Where(x => x.DealerId == dealerId && x.Date >= fromDate && x.Date <= toDate)
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<ConversionFunnel> CreateOrUpdateFunnelAsync(ConversionFunnel funnel)
    {
        var existing = await GetFunnelAsync(funnel.DealerId, funnel.Date);

        if (existing == null)
        {
            funnel.Id = Guid.NewGuid();
            funnel.CreatedAt = DateTime.UtcNow;
            funnel.UpdatedAt = DateTime.UtcNow;

            _context.ConversionFunnels.Add(funnel);
        }
        else
        {
            existing.TotalViews = funnel.TotalViews;
            existing.TotalContacts = funnel.TotalContacts;
            existing.TestDriveRequests = funnel.TestDriveRequests;
            existing.ActualSales = funnel.ActualSales;
            existing.ViewToContactRate = funnel.ViewToContactRate;
            existing.ContactToTestDriveRate = funnel.ContactToTestDriveRate;
            existing.TestDriveToSaleRate = funnel.TestDriveToSaleRate;
            existing.OverallConversionRate = funnel.OverallConversionRate;
            existing.AverageTimeToSale = funnel.AverageTimeToSale;
            existing.UpdatedAt = DateTime.UtcNow;

            funnel = existing;
        }

        await _context.SaveChangesAsync();
        return funnel;
    }

    public async Task DeleteFunnelAsync(Guid dealerId, DateTime date)
    {
        var funnel = await GetFunnelAsync(dealerId, date);
        if (funnel != null)
        {
            _context.ConversionFunnels.Remove(funnel);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ConversionFunnel> CalculateFunnelMetricsAsync(Guid dealerId, DateTime fromDate, DateTime toDate)
    {
        // Obtener datos desde DealerAnalytics para calcular el funnel
        var analytics = await _context.DealerAnalytics
            .Where(x => x.DealerId == dealerId && x.Date >= fromDate && x.Date <= toDate)
            .ToListAsync();

        if (!analytics.Any())
        {
            return new ConversionFunnel
            {
                Id = Guid.NewGuid(),
                DealerId = dealerId,
                Date = toDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        var totalViews = analytics.Sum(x => x.TotalViews);
        var totalContacts = analytics.Sum(x => x.TotalContacts);
        var testDriveRequests = analytics.Sum(x => x.TestDriveRequests);
        var actualSales = analytics.Sum(x => x.ActualSales);

        var funnel = new ConversionFunnel
        {
            Id = Guid.NewGuid(),
            DealerId = dealerId,
            Date = toDate,
            TotalViews = totalViews,
            TotalContacts = totalContacts,
            TestDriveRequests = testDriveRequests,
            ActualSales = actualSales,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Calcular tasas de conversión
        funnel.ViewToContactRate = totalViews > 0 ? (decimal)totalContacts / totalViews * 100 : 0;
        funnel.ContactToTestDriveRate = totalContacts > 0 ? (decimal)testDriveRequests / totalContacts * 100 : 0;
        funnel.TestDriveToSaleRate = testDriveRequests > 0 ? (decimal)actualSales / testDriveRequests * 100 : 0;
        funnel.OverallConversionRate = totalViews > 0 ? (decimal)actualSales / totalViews * 100 : 0;

        // Estimar tiempo promedio a venta (simplificado)
        funnel.AverageTimeToSale = analytics.Average(x => x.AverageDaysOnMarket) * 0.7m; // Estimado

        return await CreateOrUpdateFunnelAsync(funnel);
    }

    public async Task<decimal> GetAverageConversionRateAsync(DateTime fromDate, DateTime toDate)
    {
        var funnels = await _context.ConversionFunnels
            .Where(x => x.Date >= fromDate && x.Date <= toDate)
            .ToListAsync();

        if (!funnels.Any()) return 0;

        return funnels.Average(x => x.OverallConversionRate);
    }
}
