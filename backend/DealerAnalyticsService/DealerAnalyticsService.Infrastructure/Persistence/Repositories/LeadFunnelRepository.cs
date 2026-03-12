using DealerAnalyticsService.Domain.Entities;
using DealerAnalyticsService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DealerAnalyticsService.Infrastructure.Persistence.Repositories;

public class LeadFunnelRepository : ILeadFunnelRepository
{
    private readonly DealerAnalyticsDbContext _context;
    private readonly ILogger<LeadFunnelRepository> _logger;

    public LeadFunnelRepository(
        DealerAnalyticsDbContext context,
        ILogger<LeadFunnelRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LeadFunnelMetrics?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.LeadFunnelMetrics.FindAsync(new object[] { id }, ct);
    }

    public async Task<LeadFunnelMetrics?> GetByPeriodAsync(
        Guid dealerId, DateTime periodStart, DateTime periodEnd, string periodType, CancellationToken ct = default)
    {
        return await _context.LeadFunnelMetrics
            .Where(f => f.DealerId == dealerId &&
                        f.PeriodStart == periodStart &&
                        f.PeriodEnd == periodEnd &&
                        f.PeriodType == periodType)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<LeadFunnelMetrics>> GetHistoryAsync(
        Guid dealerId, int months, CancellationToken ct = default)
    {
        var fromDate = DateTime.UtcNow.AddMonths(-months);
        return await _context.LeadFunnelMetrics
            .Where(f => f.DealerId == dealerId && f.PeriodStart >= fromDate)
            .OrderBy(f => f.PeriodStart)
            .ToListAsync(ct);
    }

    public async Task<LeadFunnelMetrics> CreateAsync(LeadFunnelMetrics metrics, CancellationToken ct = default)
    {
        _context.LeadFunnelMetrics.Add(metrics);
        await _context.SaveChangesAsync(ct);
        return metrics;
    }

    public async Task<LeadFunnelMetrics> UpdateAsync(LeadFunnelMetrics metrics, CancellationToken ct = default)
    {
        metrics.UpdatedAt = DateTime.UtcNow;
        _context.LeadFunnelMetrics.Update(metrics);
        await _context.SaveChangesAsync(ct);
        return metrics;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var funnel = await GetByIdAsync(id, ct);
        if (funnel != null)
        {
            _context.LeadFunnelMetrics.Remove(funnel);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<LeadFunnelMetrics> AggregateAsync(
        Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var funnels = await _context.LeadFunnelMetrics
            .Where(f => f.DealerId == dealerId && f.PeriodStart >= fromDate && f.PeriodEnd <= toDate)
            .ToListAsync(ct);

        if (!funnels.Any())
        {
            return new LeadFunnelMetrics
            {
                DealerId = dealerId,
                PeriodStart = fromDate,
                PeriodEnd = toDate,
                PeriodType = "Aggregated"
            };
        }

        return new LeadFunnelMetrics
        {
            DealerId = dealerId,
            PeriodStart = fromDate,
            PeriodEnd = toDate,
            PeriodType = "Aggregated",
            Impressions = funnels.Sum(f => f.Impressions),
            Views = funnels.Sum(f => f.Views),
            Contacts = funnels.Sum(f => f.Contacts),
            Qualified = funnels.Sum(f => f.Qualified),
            Negotiation = funnels.Sum(f => f.Negotiation),
            Converted = funnels.Sum(f => f.Converted),
            HotLeads = funnels.Sum(f => f.HotLeads),
            WarmLeads = funnels.Sum(f => f.WarmLeads),
            ColdLeads = funnels.Sum(f => f.ColdLeads),
            PhoneContacts = funnels.Sum(f => f.PhoneContacts),
            WhatsAppContacts = funnels.Sum(f => f.WhatsAppContacts),
            EmailContacts = funnels.Sum(f => f.EmailContacts),
            ChatContacts = funnels.Sum(f => f.ChatContacts),
            AttributedRevenue = funnels.Sum(f => f.AttributedRevenue)
        };
    }

    public async Task<IEnumerable<LeadFunnelMetrics>> GetMonthlyTrendAsync(
        Guid dealerId, int months, CancellationToken ct = default)
    {
        var fromDate = DateTime.UtcNow.AddMonths(-months);
        return await _context.LeadFunnelMetrics
            .Where(f => f.DealerId == dealerId &&
                        f.PeriodStart >= fromDate &&
                        f.PeriodType == "Monthly")
            .OrderBy(f => f.PeriodStart)
            .ToListAsync(ct);
    }

    public async Task<(LeadFunnelMetrics? current, LeadFunnelMetrics? previous)> GetComparisonAsync(
        Guid dealerId, DateTime currentPeriodStart, DateTime currentPeriodEnd, CancellationToken ct = default)
    {
        var current = await AggregateAsync(dealerId, currentPeriodStart, currentPeriodEnd, ct);

        var periodDays = (int)(currentPeriodEnd - currentPeriodStart).TotalDays;
        var previousEnd = currentPeriodStart.AddDays(-1);
        var previousStart = previousEnd.AddDays(-periodDays);
        var previous = await AggregateAsync(dealerId, previousStart, previousEnd, ct);

        return (current, previous);
    }

    public async Task<List<FunnelStage>> GetFunnelStagesAsync(
        Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var aggregated = await AggregateAsync(dealerId, fromDate, toDate, ct);
        return FunnelStage.FromMetrics(aggregated);
    }

    public async Task<Dictionary<string, double>> GetConversionRatesAsync(
        Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var aggregated = await AggregateAsync(dealerId, fromDate, toDate, ct);

        return new Dictionary<string, double>
        {
            ["impressions_to_views"] = aggregated.ImpressionsToViews,
            ["views_to_contacts"] = aggregated.ViewsToContacts,
            ["contacts_to_qualified"] = aggregated.ContactsToQualified,
            ["qualified_to_negotiation"] = aggregated.QualifiedToNegotiation,
            ["negotiation_to_converted"] = aggregated.NegotiationToConverted,
            ["overall_conversion"] = aggregated.OverallConversion
        };
    }

    public async Task<Dictionary<string, int>> GetLeadsBySourceAsync(
        Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var aggregated = await AggregateAsync(dealerId, fromDate, toDate, ct);

        return new Dictionary<string, int>
        {
            ["phone"] = aggregated.PhoneContacts,
            ["whatsapp"] = aggregated.WhatsAppContacts,
            ["email"] = aggregated.EmailContacts,
            ["chat"] = aggregated.ChatContacts
        };
    }

    public async Task<Dictionary<string, double>> GetConversionBySourceAsync(
        Guid dealerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var aggregated = await AggregateAsync(dealerId, fromDate, toDate, ct);
        var totalContacts = aggregated.Contacts;

        if (totalContacts == 0)
        {
            return new Dictionary<string, double>
            {
                ["phone"] = 0,
                ["whatsapp"] = 0,
                ["email"] = 0,
                ["chat"] = 0
            };
        }

        // Estimated conversion rates by source
        var baseRate = aggregated.QualifiedToConverted;
        return new Dictionary<string, double>
        {
            ["phone"] = baseRate * 1.2,
            ["whatsapp"] = baseRate * 1.3,
            ["email"] = baseRate * 0.8,
            ["chat"] = baseRate * 0.9
        };
    }
}
