using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingService.Domain.Entities;
using BillingService.Application.DTOs;
using BillingService.Infrastructure.Persistence;
using CarDealer.Contracts.Enums;

namespace BillingService.Api.Controllers;

/// <summary>
/// Unified KPI Dashboard for OKLA Freemium v3 model.
/// Aggregates all critical business metrics: MRR, Churn, CAC, LTV, Conversion, Traffic.
/// KPI AUDIT: This was the #1 critical gap — KPIs were scattered across
/// RetentionController, AdminService, and DealerAnalyticsService with no unified view.
/// </summary>
[ApiController]
[Route("api/admin/kpis")]
[Authorize(Roles = "Admin")]
public class KpiDashboardController : ControllerBase
{
    private readonly BillingDbContext _dbContext;
    private readonly ILogger<KpiDashboardController> _logger;

    public KpiDashboardController(BillingDbContext dbContext, ILogger<KpiDashboardController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    /// <summary>
    /// Get the unified KPI dashboard — all critical metrics in one response.
    /// Designed for admin dashboard single-pane-of-glass view.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetUnifiedDashboard(
    [FromQuery] int monthsBack = 3, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var periodStart = now.AddMonths(-monthsBack);
        // ═══════════════════════════════════════════
        // 1. MRR (Monthly Recurring Revenue)
        // ═══════════════════════════════════════════
        var activeSubscriptions = await _dbContext.Subscriptions
        .Where(s => !s.IsDeleted && s.Status == SubscriptionStatus.Active)
        .ToListAsync(ct);
        var totalMrr = activeSubscriptions.Sum(s => s.PricePerCycle);
        var payingSubscriptions = activeSubscriptions.Where(s => s.Plan != SubscriptionPlan.Free).ToList();
        var mrrByPlan = activeSubscriptions
        .GroupBy(s => PlanConfiguration.GetDisplayName(s.Plan.ToString()))
        .Select(g => new
        {
            plan = g.Key,
            count = g.Count(),
            mrr = g.Sum(s => s.PricePerCycle),
            percentage = totalMrr > 0 ? Math.Round(g.Sum(s => s.PricePerCycle) / totalMrr * 100, 1) : 0
        })
        .OrderByDescending(x => x.mrr)
        .ToList();
        // MRR at risk
        var mrrAtRisk = await _dbContext.Subscriptions
        .Where(s => !s.IsDeleted && (s.Status == SubscriptionStatus.PastDue || s.Status == SubscriptionStatus.Suspended))
        .SumAsync(s => s.PricePerCycle, ct);
        // Expansion & Contraction MRR
        var expansionMrr = await _dbContext.SubscriptionChangeHistory
        .Where(h => h.ChangedAt >= periodStart && h.Direction == PlanChangeDirection.Upgrade)
        .SumAsync(h => h.NewPrice - h.OldPrice, ct);
        var contractionMrr = await _dbContext.SubscriptionChangeHistory
        .Where(h => h.ChangedAt >= periodStart && h.Direction == PlanChangeDirection.Downgrade)
        .SumAsync(h => h.OldPrice - h.NewPrice, ct);
        var churnedMrr = await _dbContext.SubscriptionChangeHistory
        .Where(h => h.ChangedAt >= periodStart && h.Direction == PlanChangeDirection.Cancel)
        .SumAsync(h => h.OldPrice, ct);
        // ═══════════════════════════════════════════
        // 2. CHURN
        // ═══════════════════════════════════════════
        var cancelledInPeriod = await _dbContext.Subscriptions
        .IgnoreQueryFilters()
        .Where(s => s.Status == SubscriptionStatus.Cancelled
        && s.CancelledAt >= periodStart
        && s.Plan != SubscriptionPlan.Free)
        .CountAsync(ct);
        var totalPayingCount = payingSubscriptions.Count;
        var monthlyChurnRate = (totalPayingCount + cancelledInPeriod) > 0
        ? Math.Round((decimal)cancelledInPeriod / (totalPayingCount + cancelledInPeriod) * 100 / monthsBack, 2)
        : 0;
        // Top cancellation reasons
        var topCancellationReasons = await _dbContext.SubscriptionChangeHistory
        .Where(h => h.ChangedAt >= periodStart && h.Direction == PlanChangeDirection.Cancel)
        .GroupBy(h => h.ReasonType)
        .Select(g => new
        {
            reason = g.Key.HasValue ? g.Key.Value.ToString() : "Unknown",
            count = g.Count(),
            mrrLost = g.Sum(h => h.OldPrice)
        })
        .OrderByDescending(x => x.count)
        .Take(5)
        .ToListAsync(ct);
        // ═══════════════════════════════════════════
        // 3. ARPU & LTV
        // ═══════════════════════════════════════════
        var arpu = totalPayingCount > 0
        ? Math.Round(payingSubscriptions.Sum(s => s.PricePerCycle) / totalPayingCount, 2)
        : 0;
        var monthlyChurnDecimal = monthlyChurnRate / 100;
        var avgLifetimeMonths = monthlyChurnDecimal > 0 ? Math.Round(1 / monthlyChurnDecimal, 1) : 0;
        var ltv = monthlyChurnDecimal > 0 ? Math.Round(arpu / monthlyChurnDecimal, 2) : 0;
        // ═══════════════════════════════════════════
        // 4. CAC (Customer Acquisition Cost)
        // ═══════════════════════════════════════════
        var acquisitions = await _dbContext.AcquisitionTrackings
        .Where(a => a.RegisteredAt >= periodStart)
        .ToListAsync(ct);
        var marketingSpend = await _dbContext.MarketingSpends
        .Where(m => (m.Year > periodStart.Year || (m.Year == periodStart.Year && m.Month >= periodStart.Month))
        && (m.Year < now.Year || (m.Year == now.Year && m.Month <= now.Month)))
        .ToListAsync(ct);
        var totalMarketingSpend = marketingSpend.Sum(m => m.SpendUsd);
        var totalNewDealers = acquisitions.Count;
        var paidConversions = acquisitions.Count(a => a.ConvertedToPaid);
        var cacBlended = totalNewDealers > 0
        ? Math.Round(totalMarketingSpend / totalNewDealers, 2)
        : 0;
        var cacPaidOnly = paidConversions > 0
        ? Math.Round(totalMarketingSpend / paidConversions, 2)
        : 0;
        // CAC by channel
        var cacByChannel = acquisitions
        .GroupBy(a => a.Channel.ToString())
        .Select(g =>
        {
            var channelSpend = marketingSpend
        .Where(m => m.Channel.ToString() == g.Key)
        .Sum(m => m.SpendUsd);
            var channelConversions = g.Count(a => a.ConvertedToPaid);
            return new
            {
                channel = g.Key,
                newDealers = g.Count(),
                paidConversions = channelConversions,
                totalSpend = channelSpend,
                cac = channelConversions > 0 ? Math.Round(channelSpend / channelConversions, 2) : 0,
                conversionRate = g.Count() > 0
        ? Math.Round((decimal)channelConversions / g.Count() * 100, 1)
        : 0
            };
        })
        .OrderBy(x => x.cac == 0 ? decimal.MaxValue : x.cac)
        .ToList();
        // ═══════════════════════════════════════════
        // 5. LTV:CAC RATIO (the golden metric)
        // ═══════════════════════════════════════════
        var ltvCacRatio = cacPaidOnly > 0 ? Math.Round(ltv / cacPaidOnly, 2) : 0;
        var ltvCacInterpretation = ltvCacRatio >= 3
        ? "Excellent — sustainable growth (target: ≥3:1)"
        : ltvCacRatio >= 2
        ? "Good — room for more aggressive growth"
        : ltvCacRatio >= 1
        ? "Warning — barely profitable per customer"
        : ltvCacRatio > 0
        ? "Critical — losing money on each customer"
        : "Insufficient data to calculate";
        // ═══════════════════════════════════════════
        // 6. CONVERSION FUNNEL
        // ═══════════════════════════════════════════
        var totalFreeSubscriptions = activeSubscriptions.Count(s => s.Plan == SubscriptionPlan.Free);
        var freeToPayingRate = (totalFreeSubscriptions + totalPayingCount) > 0
        ? Math.Round((decimal)totalPayingCount / (totalFreeSubscriptions + totalPayingCount) * 100, 2)
        : 0;
        // Trial conversion
        var trialsInPeriod = await _dbContext.Subscriptions
        .IgnoreQueryFilters()
        .Where(s => s.CreatedAt >= periodStart && s.TrialEndDate != null)
        .CountAsync(ct);
        var trialsConverted = await _dbContext.Subscriptions
        .IgnoreQueryFilters()
        .Where(s => s.CreatedAt >= periodStart
        && s.TrialEndDate != null
        && s.Status == SubscriptionStatus.Active
        && s.Plan != SubscriptionPlan.Free)
        .CountAsync(ct);
        var trialConversionRate = trialsInPeriod > 0
        ? Math.Round((decimal)trialsConverted / trialsInPeriod * 100, 2)
        : 0;
        // Avg days to conversion
        var avgDaysToConversion = acquisitions.Any(a => a.DaysToConversion.HasValue)
        ? Math.Round(acquisitions
        .Where(a => a.DaysToConversion.HasValue)
        .Average(a => (decimal)a.DaysToConversion!.Value), 1)
        : 0;
        // ═══════════════════════════════════════════
        // 7. NRR (Net Revenue Retention)
        // ═══════════════════════════════════════════
        var newSubscriptionsMrr = await _dbContext.Subscriptions
        .Where(s => !s.IsDeleted && s.CreatedAt >= periodStart && s.Status == SubscriptionStatus.Active)
        .SumAsync(s => s.PricePerCycle, ct);
        var startingMrr = totalMrr - newSubscriptionsMrr + churnedMrr;
        var nrr = startingMrr > 0
        ? Math.Round((startingMrr + expansionMrr - contractionMrr - churnedMrr) / startingMrr * 100, 2)
        : 0;
        // ═══════════════════════════════════════════
        // 8. FREEMIUM v3 PROJECTIONS vs ACTUAL
        // ═══════════════════════════════════════════
        // Target projections from the business model
        var projections = GetFreemiumV3Projections();
        var actualVsProjected = new
        {
            mrr = new { actual = totalMrr, projected = projections.TargetMrr, variance = totalMrr - projections.TargetMrr },
            churnRate = new { actual = monthlyChurnRate, projected = projections.TargetChurnRate, variance = monthlyChurnRate - projections.TargetChurnRate },
            arpu = new { actual = arpu, projected = projections.TargetArpu, variance = arpu - projections.TargetArpu },
            ltv = new { actual = ltv, projected = projections.TargetLtv, variance = ltv - projections.TargetLtv },
            cac = new { actual = cacPaidOnly, projected = projections.TargetCac, variance = cacPaidOnly - projections.TargetCac },
            ltvCacRatio = new { actual = ltvCacRatio, projected = projections.TargetLtvCacRatio, variance = ltvCacRatio - projections.TargetLtvCacRatio },
        };
        return Ok(new
        {
            success = true,
            data = new
            {
                period = new { from = periodStart, to = now, months = monthsBack },
                mrr = new
                {
                    total = totalMrr,
                    atRisk = mrrAtRisk,
                    expansion = expansionMrr,
                    contraction = contractionMrr,
                    churned = churnedMrr,
                    @new = newSubscriptionsMrr,
                    byPlan = mrrByPlan,
                    currency = "USD",
                },
                churn = new
                {
                    monthlyRate = monthlyChurnRate,
                    cancelledInPeriod,
                    topReasons = topCancellationReasons,
                },
                revenuePerUser = new
                {
                    arpu,
                    ltv,
                    avgLifetimeMonths,
                },
                acquisition = new
                {
                    cacBlended,
                    cacPaidOnly,
                    totalMarketingSpend,
                    totalNewDealers,
                    paidConversions,
                    byChannel = cacByChannel,
                },
                ltvVsCac = new
                {
                    ratio = ltvCacRatio,
                    interpretation = ltvCacInterpretation,
                    ltv,
                    cac = cacPaidOnly,
                },
                conversion = new
                {
                    freeToPayingRate,
                    trialConversionRate,
                    avgDaysToConversion,
                    totalFreeSubscriptions,
                    totalPayingSubscriptions = totalPayingCount,
                },
                nrr = new
                {
                    rate = nrr,
                    startingMrr = Math.Round(startingMrr, 2),
                    interpretation = nrr >= 100
        ? "Healthy — revenue from existing customers is growing"
        : nrr >= 90
        ? "Warning — slight revenue loss"
        : "Critical — significant revenue churn",
                },
                projections = actualVsProjected,
                calculatedAt = now,
            }
        });
    }
    /// <summary>
    /// Get CAC (Customer Acquisition Cost) detailed breakdown.
    /// </summary>
    [HttpGet("cac")]
    public async Task<IActionResult> GetCac(
    [FromQuery] int monthsBack = 3, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var periodStart = now.AddMonths(-monthsBack);
        var acquisitions = await _dbContext.AcquisitionTrackings
        .Where(a => a.RegisteredAt >= periodStart)
        .ToListAsync(ct);
        var marketingSpend = await _dbContext.MarketingSpends
        .Where(m => (m.Year > periodStart.Year || (m.Year == periodStart.Year && m.Month >= periodStart.Month))
        && (m.Year < now.Year || (m.Year == now.Year && m.Month <= now.Month)))
        .ToListAsync(ct);
        var totalSpend = marketingSpend.Sum(m => m.SpendUsd);
        var totalDealers = acquisitions.Count;
        var totalConverted = acquisitions.Count(a => a.ConvertedToPaid);
        // CAC trends by month
        var monthlyTrends = new List<object>();
        for (int i = monthsBack; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var monthAcquisitions = acquisitions.Where(a => a.RegisteredAt >= monthStart && a.RegisteredAt < monthEnd).ToList();
            var monthSpend = marketingSpend.Where(m => m.Year == monthStart.Year && m.Month == monthStart.Month).Sum(m => m.SpendUsd);
            var monthConversions = monthAcquisitions.Count(a => a.ConvertedToPaid);
            monthlyTrends.Add(new
            {
                month = monthStart.ToString("yyyy-MM"),
                spend = monthSpend,
                newDealers = monthAcquisitions.Count,
                conversions = monthConversions,
                cacBlended = monthAcquisitions.Count > 0 ? Math.Round(monthSpend / monthAcquisitions.Count, 2) : 0,
                cacPaidOnly = monthConversions > 0 ? Math.Round(monthSpend / monthConversions, 2) : 0,
            });
        }
        // By channel
        var byChannel = marketingSpend
        .GroupBy(m => m.Channel.ToString())
        .Select(g =>
        {
            var channelAcquisitions = acquisitions.Where(a => a.Channel.ToString() == g.Key).ToList();
            var channelConverted = channelAcquisitions.Count(a => a.ConvertedToPaid);
            var channelSpend = g.Sum(m => m.SpendUsd);
            return new
            {
                channel = g.Key,
                totalSpend = channelSpend,
                impressions = g.Sum(m => m.Impressions),
                clicks = g.Sum(m => m.Clicks),
                signups = channelAcquisitions.Count,
                paidConversions = channelConverted,
                cac = channelConverted > 0 ? Math.Round(channelSpend / channelConverted, 2) : 0,
                costPerSignup = channelAcquisitions.Count > 0
        ? Math.Round(channelSpend / channelAcquisitions.Count, 2) : 0,
                conversionRate = channelAcquisitions.Count > 0
        ? Math.Round((decimal)channelConverted / channelAcquisitions.Count * 100, 1) : 0,
                ctr = g.Sum(m => m.Impressions) > 0
        ? Math.Round((decimal)g.Sum(m => m.Clicks) / g.Sum(m => m.Impressions) * 100, 2) : 0,
            };
        })
        .OrderBy(x => x.cac == 0 ? decimal.MaxValue : x.cac)
        .ToList();
        // Avg days to conversion by channel
        var avgDaysToConversionByChannel = acquisitions
        .Where(a => a.ConvertedToPaid && a.DaysToConversion.HasValue)
        .GroupBy(a => a.Channel.ToString())
        .Select(g => new
        {
            channel = g.Key,
            avgDays = Math.Round(g.Average(a => (decimal)a.DaysToConversion!.Value), 1),
            minDays = g.Min(a => a.DaysToConversion!.Value),
            maxDays = g.Max(a => a.DaysToConversion!.Value),
        })
        .ToList();
        return Ok(new
        {
            success = true,
            data = new
            {
                summary = new
                {
                    cacBlended = totalDealers > 0 ? Math.Round(totalSpend / totalDealers, 2) : 0,
                    cacPaidOnly = totalConverted > 0 ? Math.Round(totalSpend / totalConverted, 2) : 0,
                    totalMarketingSpend = totalSpend,
                    totalNewDealers = totalDealers,
                    totalPaidConversions = totalConverted,
                    overallConversionRate = totalDealers > 0
        ? Math.Round((decimal)totalConverted / totalDealers * 100, 1) : 0,
                    currency = "USD",
                },
                monthlyTrends,
                byChannel,
                avgDaysToConversionByChannel,
                periodMonths = monthsBack,
                calculatedAt = now,
            }
        });
    }
    /// <summary>
    /// Record acquisition tracking for a new dealer.
    /// Called from AuthService via event or manually by admin.
    /// </summary>
    [HttpPost("acquisition")]
    public async Task<IActionResult> RecordAcquisition(
    [FromBody] RecordAcquisitionRequest request, CancellationToken ct)
    {
        // Check if dealer already has acquisition tracking
        var existing = await _dbContext.AcquisitionTrackings
        .FirstOrDefaultAsync(a => a.DealerId == request.DealerId, ct);
        if (existing != null)
            return Conflict(new { success = false, error = "Acquisition tracking already exists for this dealer." });
        var acquisition = new AcquisitionTracking(
        dealerId: request.DealerId,
        channel: request.Channel,
        registeredAt: request.RegisteredAt ?? DateTime.UtcNow,
        campaignId: request.CampaignId,
        campaignName: request.CampaignName,
        utmSource: request.UtmSource,
        utmMedium: request.UtmMedium,
        utmCampaign: request.UtmCampaign,
        utmContent: request.UtmContent,
        utmTerm: request.UtmTerm,
        acquisitionCostUsd: request.AcquisitionCostUsd,
        referredByDealerId: request.ReferredByDealerId,
        referralCode: request.ReferralCode,
        landingPage: request.LandingPage,
        country: request.Country);
        _dbContext.AcquisitionTrackings.Add(acquisition);
        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Acquisition tracked for dealer {DealerId} via {Channel}",
        request.DealerId, request.Channel);
        return Created($"/api/admin/kpis/acquisition/{acquisition.Id}", new { success = true, data = acquisition.Id });
    }
    /// <summary>
    /// Mark a dealer as converted to paid plan.
    /// Called when a dealer upgrades from Free to a paid plan.
    /// </summary>
    [HttpPost("acquisition/{dealerId:guid}/converted")]
    public async Task<IActionResult> MarkConverted(
    Guid dealerId, [FromBody] MarkConvertedRequest request, CancellationToken ct)
    {
        var acquisition = await _dbContext.AcquisitionTrackings
        .FirstOrDefaultAsync(a => a.DealerId == dealerId, ct);
        if (acquisition == null)
            return NotFound(new { success = false, error = $"No acquisition tracking for dealer {dealerId}." });
        if (acquisition.ConvertedToPaid)
            return Conflict(new { success = false, error = "Dealer already marked as converted." });
        acquisition.MarkConverted(request.Plan);
        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Dealer {DealerId} converted to {Plan}",
        dealerId, request.Plan);
        return Ok(new { success = true, data = new { dealerId, convertedToPlan = request.Plan.ToString(), acquisition.DaysToConversion } });
    }
    /// <summary>
    /// Record or update monthly marketing spend.
    /// </summary>
    [HttpPost("marketing-spend")]
    public async Task<IActionResult> RecordMarketingSpend(
    [FromBody] RecordMarketingSpendRequest request, CancellationToken ct)
    {
        var existing = await _dbContext.MarketingSpends
        .FirstOrDefaultAsync(m => m.Year == request.Year
        && m.Month == request.Month
        && m.Channel == request.Channel
        && m.CampaignId == request.CampaignId, ct);
        if (existing != null)
        {
            existing.UpdateMetrics(
            request.SpendUsd,
            request.Impressions,
            request.Clicks,
            request.Signups,
            request.PaidConversions);
            await _dbContext.SaveChangesAsync(ct);
            return Ok(new { success = true, data = existing.Id, updated = true });
        }
        var spend = new MarketingSpend(
        year: request.Year,
        month: request.Month,
        channel: request.Channel,
        spendUsd: request.SpendUsd,
        campaignId: request.CampaignId,
        campaignName: request.CampaignName,
        impressions: request.Impressions,
        clicks: request.Clicks,
        signups: request.Signups,
        paidConversions: request.PaidConversions,
        notes: request.Notes);
        _dbContext.MarketingSpends.Add(spend);
        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Marketing spend recorded: {Year}-{Month} {Channel} ${Spend}",
        request.Year, request.Month, request.Channel, request.SpendUsd);
        return Created($"/api/admin/kpis/marketing-spend/{spend.Id}", new { success = true, data = spend.Id });
    }
    /// <summary>
    /// Get LTV:CAC ratio trend over time.
    /// The golden metric for SaaS sustainability.
    /// </summary>
    [HttpGet("ltv-cac-trend")]
    public async Task<IActionResult> GetLtvCacTrend(
    [FromQuery] int monthsBack = 6, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var trends = new List<object>();
        for (int i = monthsBack; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            // LTV calculation for this month
            var activePayingAtMonth = await _dbContext.Subscriptions
            .Where(s => !s.IsDeleted
            && s.Status == SubscriptionStatus.Active
            && s.Plan != SubscriptionPlan.Free
            && s.CreatedAt < monthEnd)
            .ToListAsync(ct);
            var monthMrr = activePayingAtMonth.Sum(s => s.PricePerCycle);
            var monthPayingCount = activePayingAtMonth.Count;
            var monthArpu = monthPayingCount > 0 ? monthMrr / monthPayingCount : 0;
            var monthCancelled = await _dbContext.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.Status == SubscriptionStatus.Cancelled
            && s.CancelledAt >= monthStart && s.CancelledAt < monthEnd
            && s.Plan != SubscriptionPlan.Free)
            .CountAsync(ct);
            var monthChurnRate = (monthPayingCount + monthCancelled) > 0
            ? (decimal)monthCancelled / (monthPayingCount + monthCancelled)
            : 0;
            var monthLtv = monthChurnRate > 0 ? monthArpu / monthChurnRate : 0;
            // CAC for this month
            var monthSpend = await _dbContext.MarketingSpends
            .Where(m => m.Year == monthStart.Year && m.Month == monthStart.Month)
            .SumAsync(m => m.SpendUsd, ct);
            var monthConversions = await _dbContext.AcquisitionTrackings
            .Where(a => a.ConvertedToPaid
            && a.ConvertedAt >= monthStart && a.ConvertedAt < monthEnd)
            .CountAsync(ct);
            var monthCac = monthConversions > 0 ? monthSpend / monthConversions : 0;
            var monthRatio = monthCac > 0 ? Math.Round(monthLtv / monthCac, 2) : 0;
            trends.Add(new
            {
                month = monthStart.ToString("yyyy-MM"),
                ltv = Math.Round(monthLtv, 2),
                cac = Math.Round(monthCac, 2),
                ratio = monthRatio,
                arpu = Math.Round(monthArpu, 2),
                churnRate = Math.Round(monthChurnRate * 100, 2),
                mrr = Math.Round(monthMrr, 2),
                payingDealers = monthPayingCount,
            });
        }
        return Ok(new { success = true, data = trends, calculatedAt = now });
    }
    /// <summary>
    /// Get Freemium v3 health score — composite metric of model performance.
    /// Score from 0-100 based on weighted KPIs.
    /// </summary>
    [HttpGet("health-score")]
    public async Task<IActionResult> GetHealthScore(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var periodStart = now.AddMonths(-3);
        var projections = GetFreemiumV3Projections();
        // Calculate individual KPI scores (0-100 each)
        var activePayingSubs = await _dbContext.Subscriptions
        .Where(s => !s.IsDeleted && s.Status == SubscriptionStatus.Active && s.Plan != SubscriptionPlan.Free)
        .ToListAsync(ct);
        var totalMrr = activePayingSubs.Sum(s => s.PricePerCycle);
        var payingCount = activePayingSubs.Count;
        var arpu = payingCount > 0 ? activePayingSubs.Average(s => s.PricePerCycle) : 0;
        var cancelledInPeriod = await _dbContext.Subscriptions
        .IgnoreQueryFilters()
        .Where(s => s.Status == SubscriptionStatus.Cancelled
        && s.CancelledAt >= periodStart
        && s.Plan != SubscriptionPlan.Free)
        .CountAsync(ct);
        var churnRate = (payingCount + cancelledInPeriod) > 0
        ? (decimal)cancelledInPeriod / (payingCount + cancelledInPeriod) * 100 / 3
        : 0;
        // Score calculations (normalized to 0-100)
        var mrrScore = projections.TargetMrr > 0
        ? Math.Min(100, Math.Round(totalMrr / projections.TargetMrr * 100, 0)) : 0;
        var churnScore = projections.TargetChurnRate > 0
        ? Math.Min(100, Math.Round((projections.TargetChurnRate / Math.Max(churnRate, 0.01m)) * 100, 0)) : 0;
        // Higher ARPU is better
        var arpuScore = projections.TargetArpu > 0
        ? Math.Min(100, Math.Round((decimal)arpu / projections.TargetArpu * 100, 0)) : 0;
        // Weighted composite
        var weights = new { mrr = 0.30m, churn = 0.25m, arpu = 0.20m, conversion = 0.15m, engagement = 0.10m };
        var compositeScore = Math.Round(
        mrrScore * weights.mrr +
        churnScore * weights.churn +
        arpuScore * weights.arpu +
        50 * weights.conversion + // Placeholder until conversion tracking is fully wired
        50 * weights.engagement,  // Placeholder until engagement tracking is fully wired
        0);
        var grade = compositeScore >= 90 ? "A+" : compositeScore >= 80 ? "A" :
        compositeScore >= 70 ? "B" : compositeScore >= 60 ? "C" :
        compositeScore >= 50 ? "D" : "F";
        return Ok(new
        {
            success = true,
            data = new
            {
                overallScore = compositeScore,
                grade,
                breakdown = new
                {
                    mrr = new { score = mrrScore, weight = weights.mrr, actual = totalMrr, target = projections.TargetMrr },
                    churn = new { score = churnScore, weight = weights.churn, actual = churnRate, target = projections.TargetChurnRate },
                    arpu = new { score = arpuScore, weight = weights.arpu, actual = arpu, target = projections.TargetArpu },
                },
                recommendations = GenerateRecommendations(mrrScore, churnScore, arpuScore),
                calculatedAt = now,
            }
        });
    }
    // ═══════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Freemium v3 target projections for the current stage.
    /// Based on OKLA business model: 4 tiers, Dominican Republic market.
    /// </summary>
    private static FreemiumProjections GetFreemiumV3Projections()
    {
        return new FreemiumProjections
        {
            // Target: 100 paying dealers by Q2 2026
            // Mix: 40% Visible ($29), 35% Pro ($89), 25% Elite ($199)
            // Target MRR: 40*29 + 35*89 + 25*199 = 1,160 + 3,115 + 4,975 = $9,250
            TargetMrr = 9250m,
            // 5% monthly churn (early stage, aggressive target)
            TargetChurnRate = 5.0m,
            // $9,250 / 100 dealers
            TargetArpu = 92.50m,
            // $92.50 / 0.05 = $1,850
            TargetLtv = 1850m,
            // Target: $150 blended CAC for DR market
            TargetCac = 150m,
            // $1,850 / $150 = 12.3x
            TargetLtvCacRatio = 12.33m,
            TargetFreeToPayingRate = 15m,  // 15% of free dealers convert to paid
            TargetTrialConversionRate = 40m, // 40% trial-to-paid conversion
        };
    }
    private static List<string> GenerateRecommendations(decimal mrrScore, decimal churnScore, decimal arpuScore)
    {
        var recommendations = new List<string>();
        if (mrrScore < 50)
            recommendations.Add("🔴 MRR is significantly below target. Focus on dealer acquisition campaigns and upselling existing Free dealers.");
        if (mrrScore >= 50 && mrrScore < 80)
            recommendations.Add("🟡 MRR is growing but below target. Consider targeted campaigns for Pro/Elite plan upgrades.");
        if (churnScore < 50)
            recommendations.Add("🔴 Churn rate is too high. Implement proactive retention outreach for PastDue dealers and survey churned dealers.");
        if (churnScore >= 50 && churnScore < 80)
            recommendations.Add("🟡 Churn is manageable but needs attention. Analyze top cancellation reasons and address them.");
        if (arpuScore < 50)
            recommendations.Add("🔴 ARPU is below target. Most dealers may be on lower-tier plans. Demonstrate Pro/Elite value propositions.");
        if (arpuScore >= 50 && arpuScore < 80)
            recommendations.Add("🟡 ARPU is reasonable. Focus on upselling Pro to Elite with chatbot and analytics features.");
        if (recommendations.Count == 0)
            recommendations.Add("✅ All KPIs are healthy! Continue monitoring and explore expansion to other Caribbean markets.");
        return recommendations;
    }
    private class FreemiumProjections
    {
        public decimal TargetMrr { get; set; }
        public decimal TargetChurnRate { get; set; }
        public decimal TargetArpu { get; set; }
        public decimal TargetLtv { get; set; }
        public decimal TargetCac { get; set; }
        public decimal TargetLtvCacRatio { get; set; }
        public decimal TargetFreeToPayingRate { get; set; }
        public decimal TargetTrialConversionRate { get; set; }
    }
}
