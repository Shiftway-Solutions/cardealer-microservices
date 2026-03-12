using Microsoft.EntityFrameworkCore;
using DealerAnalyticsService.Domain.Entities;
using DealerAnalyticsService.Domain.Interfaces;

namespace DealerAnalyticsService.Infrastructure.Persistence.Repositories;

public class DealerAnalyticsRepository : IDealerAnalyticsRepository
{
    private readonly DealerAnalyticsDbContext _context;

    public DealerAnalyticsRepository(DealerAnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<DealerAnalytic?> GetDealerAnalyticsAsync(Guid dealerId, DateTime date)
    {
        return await _context.DealerAnalytics
            .FirstOrDefaultAsync(x => x.DealerId == dealerId && x.Date.Date == date.Date);
    }

    public async Task<IEnumerable<DealerAnalytic>> GetDealerAnalyticsRangeAsync(Guid dealerId, DateTime fromDate, DateTime toDate)
    {
        return await _context.DealerAnalytics
            .Where(x => x.DealerId == dealerId && x.Date >= fromDate && x.Date <= toDate)
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<DealerAnalytic> CreateOrUpdateAnalyticsAsync(DealerAnalytic analytics)
    {
        var existing = await GetDealerAnalyticsAsync(analytics.DealerId, analytics.Date);

        if (existing == null)
        {
            analytics.Id = Guid.NewGuid();
            analytics.CreatedAt = DateTime.UtcNow;
            analytics.UpdatedAt = DateTime.UtcNow;

            _context.DealerAnalytics.Add(analytics);
        }
        else
        {
            // Actualizar valores existentes
            existing.TotalViews = analytics.TotalViews;
            existing.UniqueViews = analytics.UniqueViews;
            existing.AverageViewDuration = analytics.AverageViewDuration;
            existing.TotalContacts = analytics.TotalContacts;
            existing.PhoneCalls = analytics.PhoneCalls;
            existing.WhatsAppMessages = analytics.WhatsAppMessages;
            existing.EmailInquiries = analytics.EmailInquiries;
            existing.TestDriveRequests = analytics.TestDriveRequests;
            existing.ActualSales = analytics.ActualSales;
            existing.ConversionRate = analytics.ConversionRate;
            existing.TotalRevenue = analytics.TotalRevenue;
            existing.AverageVehiclePrice = analytics.AverageVehiclePrice;
            existing.RevenuePerView = analytics.RevenuePerView;
            existing.ActiveListings = analytics.ActiveListings;
            existing.AverageDaysOnMarket = analytics.AverageDaysOnMarket;
            existing.SoldVehicles = analytics.SoldVehicles;
            existing.UpdatedAt = DateTime.UtcNow;

            analytics = existing;
        }

        await _context.SaveChangesAsync();
        return analytics;
    }

    public async Task DeleteDealerAnalyticsAsync(Guid dealerId, DateTime date)
    {
        var analytics = await GetDealerAnalyticsAsync(dealerId, date);
        if (analytics != null)
        {
            _context.DealerAnalytics.Remove(analytics);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<DealerAnalytic> GetDealerAnalyticsSummaryAsync(Guid dealerId, DateTime fromDate, DateTime toDate)
    {
        var analytics = await GetDealerAnalyticsRangeAsync(dealerId, fromDate, toDate);

        if (!analytics.Any())
        {
            return new DealerAnalytic
            {
                Id = Guid.NewGuid(),
                DealerId = dealerId,
                Date = toDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        // Agregar todos los datos del rango de fechas
        var summary = new DealerAnalytic
        {
            Id = Guid.NewGuid(),
            DealerId = dealerId,
            Date = toDate,
            TotalViews = analytics.Sum(x => x.TotalViews),
            UniqueViews = analytics.Sum(x => x.UniqueViews),
            AverageViewDuration = analytics.Average(x => x.AverageViewDuration),
            TotalContacts = analytics.Sum(x => x.TotalContacts),
            PhoneCalls = analytics.Sum(x => x.PhoneCalls),
            WhatsAppMessages = analytics.Sum(x => x.WhatsAppMessages),
            EmailInquiries = analytics.Sum(x => x.EmailInquiries),
            TestDriveRequests = analytics.Sum(x => x.TestDriveRequests),
            ActualSales = analytics.Sum(x => x.ActualSales),
            TotalRevenue = analytics.Sum(x => x.TotalRevenue),
            AverageVehiclePrice = analytics.Average(x => x.AverageVehiclePrice),
            ActiveListings = analytics.LastOrDefault()?.ActiveListings ?? 0,
            AverageDaysOnMarket = analytics.Average(x => x.AverageDaysOnMarket),
            SoldVehicles = analytics.Sum(x => x.SoldVehicles),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Calcular métricas derivadas
        summary.ConversionRate = summary.TotalViews > 0 ?
            (decimal)summary.ActualSales / summary.TotalViews * 100 : 0;

        summary.RevenuePerView = summary.TotalViews > 0 ?
            summary.TotalRevenue / summary.TotalViews : 0;

        return summary;
    }

    public async Task<decimal> GetDealerConversionRateAsync(Guid dealerId, DateTime fromDate, DateTime toDate)
    {
        var summary = await GetDealerAnalyticsSummaryAsync(dealerId, fromDate, toDate);
        return summary.ConversionRate;
    }

    public async Task<decimal> GetDealerRevenueAsync(Guid dealerId, DateTime fromDate, DateTime toDate)
    {
        var summary = await GetDealerAnalyticsSummaryAsync(dealerId, fromDate, toDate);
        return summary.TotalRevenue;
    }
}
