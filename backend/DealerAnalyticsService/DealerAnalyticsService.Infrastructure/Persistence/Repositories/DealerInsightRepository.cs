using Microsoft.EntityFrameworkCore;
using DealerAnalyticsService.Domain.Entities;
using DealerAnalyticsService.Domain.Interfaces;

namespace DealerAnalyticsService.Infrastructure.Persistence.Repositories;

public class DealerInsightRepository : IDealerInsightRepository
{
    private readonly DealerAnalyticsDbContext _context;

    public DealerInsightRepository(DealerAnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<DealerInsight?> GetInsightAsync(Guid id)
    {
        return await _context.DealerInsights.FindAsync(id);
    }

    public async Task<IEnumerable<DealerInsight>> GetDealerInsightsAsync(Guid dealerId, bool onlyUnread = false)
    {
        var query = _context.DealerInsights.Where(x => x.DealerId == dealerId);

        if (onlyUnread)
        {
            query = query.Where(x => !x.IsRead);
        }

        return await query
            .Where(x => x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DealerInsight>> GetInsightsByTypeAsync(Guid dealerId, InsightType type)
    {
        return await _context.DealerInsights
            .Where(x => x.DealerId == dealerId && x.Type == type)
            .Where(x => x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DealerInsight>> GetInsightsByPriorityAsync(Guid dealerId, InsightPriority priority)
    {
        return await _context.DealerInsights
            .Where(x => x.DealerId == dealerId && x.Priority == priority)
            .Where(x => x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<DealerInsight> CreateInsightAsync(DealerInsight insight)
    {
        insight.Id = Guid.NewGuid();
        insight.CreatedAt = DateTime.UtcNow;
        insight.UpdatedAt = DateTime.UtcNow;

        _context.DealerInsights.Add(insight);
        await _context.SaveChangesAsync();

        return insight;
    }

    public async Task<DealerInsight> UpdateInsightAsync(DealerInsight insight)
    {
        var existing = await GetInsightAsync(insight.Id);
        if (existing == null)
            throw new ArgumentException($"Insight with ID {insight.Id} not found");

        existing.Title = insight.Title;
        existing.Description = insight.Description;
        existing.ActionRecommendation = insight.ActionRecommendation;
        existing.SupportingData = insight.SupportingData;
        existing.PotentialImpact = insight.PotentialImpact;
        existing.Confidence = insight.Confidence;
        existing.IsRead = insight.IsRead;
        existing.IsActedUpon = insight.IsActedUpon;
        existing.ActionDate = insight.ActionDate;
        existing.ExpiresAt = insight.ExpiresAt;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteInsightAsync(Guid id)
    {
        var insight = await GetInsightAsync(id);
        if (insight != null)
        {
            _context.DealerInsights.Remove(insight);
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkInsightsAsReadAsync(Guid dealerId, IEnumerable<Guid> insightIds)
    {
        var insights = await _context.DealerInsights
            .Where(x => x.DealerId == dealerId && insightIds.Contains(x.Id))
            .ToListAsync();

        foreach (var insight in insights)
        {
            insight.IsRead = true;
            insight.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task MarkInsightAsActedUponAsync(Guid insightId, DateTime actionDate)
    {
        var insight = await GetInsightAsync(insightId);
        if (insight != null)
        {
            insight.IsActedUpon = true;
            insight.ActionDate = actionDate;
            insight.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteExpiredInsightsAsync()
    {
        var expiredInsights = await _context.DealerInsights
            .Where(x => x.ExpiresAt != null && x.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        if (expiredInsights.Any())
        {
            _context.DealerInsights.RemoveRange(expiredInsights);
            await _context.SaveChangesAsync();
        }
    }
}
