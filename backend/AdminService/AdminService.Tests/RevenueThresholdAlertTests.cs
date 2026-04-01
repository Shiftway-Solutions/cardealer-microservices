using AdminService.Application.Interfaces;
using AdminService.Application.Services;
using AdminService.Application.UseCases.Dealers;
using AdminService.Application.UseCases.Finance;
using AdminService.Infrastructure.Services;
using CarDealer.Contracts.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AdminService.Tests;

/// <summary>
/// CONTRA #7 FIX: Revenue Threshold Alert Tests
///
/// Validates the RevenueProjectionService correctly:
///   1. Projects monthly revenue from daily accumulation
///   2. Compares against OPEX threshold ($2,215)
///   3. Fires alerts when projected revenue falls below OPEX
///   4. Applies early-month safety margin (before day 15)
///   5. Skips alerts before day 5 (volatile projections)
///   6. Generates actionable suggested actions
///   7. Calculates correct shortfall and required daily revenue
///   8. Handles API failures gracefully
///   9. Determines correct severity (Warning vs Critical)
/// </summary>
public class RevenueThresholdAlertTests
{
    private readonly Mock<IFinancialDataProvider> _financialDataMock;
    private readonly Mock<IDealerService> _dealerServiceMock;
    private readonly RevenueProjectionService _service;

    public RevenueThresholdAlertTests()
    {
        _financialDataMock = new Mock<IFinancialDataProvider>();
        _dealerServiceMock = new Mock<IDealerService>();
        var logger = NullLogger<RevenueProjectionService>.Instance;

        // Default: no additional revenue
        _financialDataMock
            .Setup(x => x.GetOverageRevenueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        _financialDataMock
            .Setup(x => x.GetAdvertisingRevenueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        _service = new RevenueProjectionService(
            _financialDataMock.Object,
            _dealerServiceMock.Object,
            logger);
    }

    // ════════════════════════════════════════════════════════════════════════
    // OPEX THRESHOLD CONSTANTS
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OpexThreshold_Is2215()
    {
        Assert.Equal(2215m, PlanFeatureLimits.MonthlyOpexThreshold);
    }

    [Fact]
    public void AlertCheckInterval_Is6Hours()
    {
        Assert.Equal(6, PlanFeatureLimits.RevenueAlertCheckIntervalHours);
    }

    [Fact]
    public void AlertMinDay_Is5()
    {
        Assert.Equal(5, PlanFeatureLimits.RevenueAlertMinDayOfMonth);
    }

    [Fact]
    public void SafetyMargin_Is1_15()
    {
        Assert.Equal(1.15m, PlanFeatureLimits.RevenueAlertEarlyMonthSafetyMargin);
    }

    // ════════════════════════════════════════════════════════════════════════
    // REVENUE PROJECTION LOGIC
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateCurrentMonth_CalculatesProjectedRevenue()
    {
        // Arrange: MRR from 10 Visible ($29) + 2 Elite ($199) = $688
        SetupDealerStats(libre: 50, visible: 10, pro: 0, elite: 2);

        var result = await _service.EvaluateCurrentMonthAsync();

        // MRR should be 10*29 + 2*199 = 290 + 398 = 688
        Assert.Equal(688m, result.CurrentMrr);
        Assert.True(result.ProjectedMonthlyRevenue > 0);
        Assert.Equal(PlanFeatureLimits.MonthlyOpexThreshold, result.OpexThreshold);
    }

    [Fact]
    public async Task EvaluateCurrentMonth_IncludesOverageAndAdRevenue()
    {
        // Arrange: MRR = $199, Overage = $50, Ads = $30
        SetupDealerStats(libre: 0, visible: 0, pro: 0, elite: 1);
        _financialDataMock
            .Setup(x => x.GetOverageRevenueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(50m);
        _financialDataMock
            .Setup(x => x.GetAdvertisingRevenueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(30m);

        var result = await _service.EvaluateCurrentMonthAsync();

        Assert.Equal(80m, result.AdditionalRevenue);
        Assert.Equal(199m + 80m, result.AccumulatedRevenue);
    }

    [Fact]
    public async Task EvaluateCurrentMonth_CalculatesShortfall()
    {
        // Arrange: Low revenue → below OPEX
        SetupDealerStats(libre: 100, visible: 1, pro: 0, elite: 0);
        // MRR = 1 * $29 = $29 → projected monthly = $29 * (daysInMonth/dayOfMonth)
        // At any day, $29 is way below $2,215

        var result = await _service.EvaluateCurrentMonthAsync();

        // Shortfall should be > $0 since $29 << $2,215
        Assert.True(result.Shortfall > 0);
        Assert.True(result.ShortfallPercent > 0);
    }

    [Fact]
    public async Task EvaluateCurrentMonth_CalculatesDailyRevenueRate()
    {
        SetupDealerStats(libre: 0, visible: 0, pro: 5, elite: 3);
        // MRR = 5*89 + 3*199 = 445 + 597 = 1042

        var result = await _service.EvaluateCurrentMonthAsync();

        var dayOfMonth = DateTime.UtcNow.Day;
        var expectedDailyRate = Math.Round(1042m / dayOfMonth, 2);
        Assert.Equal(expectedDailyRate, result.DailyRevenueRate);
    }

    [Fact]
    public async Task EvaluateCurrentMonth_CalculatesRequiredDailyRevenue()
    {
        SetupDealerStats(libre: 100, visible: 1, pro: 0, elite: 0);
        // MRR = $29, well below OPEX

        var result = await _service.EvaluateCurrentMonthAsync();

        if (result.DaysRemaining > 0)
        {
            Assert.True(result.RequiredDailyRevenue > 0);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // ALERT TRIGGER LOGIC
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateCurrentMonth_HighRevenue_NoAlert()
    {
        // Arrange: 50 Elite dealers → MRR = 50 * $199 = $9,950 → well above OPEX
        SetupDealerStats(libre: 0, visible: 0, pro: 0, elite: 50);

        var result = await _service.EvaluateCurrentMonthAsync();

        Assert.False(result.ShouldAlert);
        Assert.Equal("Info", result.Severity);
        Assert.Equal(0m, result.Shortfall);
    }

    [Fact]
    public async Task EvaluateCurrentMonth_LowRevenue_TriggersAlert()
    {
        // Arrange: Only 1 Visible dealer → MRR = $29 → way below OPEX
        SetupDealerStats(libre: 100, visible: 1, pro: 0, elite: 0);

        var result = await _service.EvaluateCurrentMonthAsync();

        var dayOfMonth = DateTime.UtcNow.Day;
        if (dayOfMonth >= PlanFeatureLimits.RevenueAlertMinDayOfMonth)
        {
            Assert.True(result.ShouldAlert);
            Assert.True(result.Severity == "Warning" || result.Severity == "Critical");
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // SEVERITY CLASSIFICATION
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateCurrentMonth_Below90Percent_CriticalSeverity()
    {
        // MRR = $29 → projected ~$29 → this is < 90% of $2,215 ($1,993.50)
        SetupDealerStats(libre: 100, visible: 1, pro: 0, elite: 0);

        var result = await _service.EvaluateCurrentMonthAsync();

        var dayOfMonth = DateTime.UtcNow.Day;
        if (dayOfMonth >= PlanFeatureLimits.RevenueAlertMinDayOfMonth)
        {
            Assert.Equal("Critical", result.Severity);
        }
    }

    [Fact]
    public async Task EvaluateCurrentMonth_HealthyRevenue_InfoSeverity()
    {
        // MRR = 50 * $199 = $9,950 → healthy
        SetupDealerStats(libre: 0, visible: 0, pro: 0, elite: 50);

        var result = await _service.EvaluateCurrentMonthAsync();

        Assert.Equal("Info", result.Severity);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SUGGESTED ACTIONS
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateCurrentMonth_NoAlert_SaysNoActionNeeded()
    {
        SetupDealerStats(libre: 0, visible: 0, pro: 0, elite: 50);

        var result = await _service.EvaluateCurrentMonthAsync();

        var dayOfMonth = DateTime.UtcNow.Day;
        if (dayOfMonth >= PlanFeatureLimits.RevenueAlertMinDayOfMonth)
        {
            Assert.Contains("No action needed", result.SuggestedAction);
        }
        else
        {
            // Before min day, service returns projection-not-available message
            Assert.Contains("Projection available after day", result.SuggestedAction);
        }
    }

    [Fact]
    public async Task EvaluateCurrentMonth_BelowThreshold_SuggestsConversions()
    {
        SetupDealerStats(libre: 100, visible: 5, pro: 2, elite: 1);
        // MRR = 5*29 + 2*89 + 1*199 = 145 + 178 + 199 = 522

        var result = await _service.EvaluateCurrentMonthAsync();

        var dayOfMonth = DateTime.UtcNow.Day;
        if (dayOfMonth >= PlanFeatureLimits.RevenueAlertMinDayOfMonth && result.ShouldAlert)
        {
            // Should suggest conversion options
            Assert.True(
                result.SuggestedAction.Contains("Convert") ||
                result.SuggestedAction.Contains("Upgrade") ||
                result.SuggestedAction.Contains("OPEX"));
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // DEALER COUNTS
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateCurrentMonth_ReturnsDealersByPlan()
    {
        SetupDealerStats(libre: 10, visible: 5, pro: 3, elite: 2);

        var result = await _service.EvaluateCurrentMonthAsync();

        Assert.Equal(10, result.DealersByPlan["libre"]);
        Assert.Equal(5, result.DealersByPlan["visible"]);
        Assert.Equal(3, result.DealersByPlan["pro"]);
        Assert.Equal(2, result.DealersByPlan["elite"]);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PERIOD HANDLING
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateCurrentMonth_SetsCurrentPeriod()
    {
        SetupDealerStats(libre: 0, visible: 0, pro: 0, elite: 0);

        var result = await _service.EvaluateCurrentMonthAsync();

        var expectedPeriod = DateTime.UtcNow.ToString("yyyy-MM");
        Assert.Equal(expectedPeriod, result.Period);
    }

    [Fact]
    public async Task EvaluateCurrentMonth_SetsCorrectDayInfo()
    {
        SetupDealerStats(libre: 0, visible: 0, pro: 0, elite: 0);

        var result = await _service.EvaluateCurrentMonthAsync();

        var now = DateTime.UtcNow;
        Assert.Equal(now.Day, result.DayOfMonth);
        Assert.Equal(DateTime.DaysInMonth(now.Year, now.Month), result.DaysInMonth);
        Assert.Equal(result.DaysInMonth - result.DayOfMonth, result.DaysRemaining);
    }

    // ════════════════════════════════════════════════════════════════════════
    // EDGE CASES & RESILIENCE
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateCurrentMonth_NoDealers_HandlesGracefully()
    {
        SetupDealerStats(libre: 0, visible: 0, pro: 0, elite: 0);

        var result = await _service.EvaluateCurrentMonthAsync();

        Assert.Equal(0m, result.CurrentMrr);
        Assert.Equal(0m, result.AccumulatedRevenue);
    }

    [Fact]
    public async Task EvaluateCurrentMonth_DealerServiceFails_ReturnsZeroMrr()
    {
        _dealerServiceMock
            .Setup(x => x.GetDealerStatsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _service.EvaluateCurrentMonthAsync();

        // Should not throw, should return zero MRR
        Assert.Equal(0m, result.CurrentMrr);
    }

    [Fact]
    public async Task EvaluateCurrentMonth_OverageServiceFails_StillCalculatesMrr()
    {
        SetupDealerStats(libre: 0, visible: 0, pro: 0, elite: 5);
        _financialDataMock
            .Setup(x => x.GetOverageRevenueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Timeout"));

        var result = await _service.EvaluateCurrentMonthAsync();

        // MRR should still be calculated even if overage fails
        Assert.Equal(5 * 199m, result.CurrentMrr);
        Assert.Equal(0m, result.AdditionalRevenue);
    }

    [Fact]
    public async Task EvaluateCurrentMonth_OnlyFreeDealers_ProjectsZeroRevenue()
    {
        SetupDealerStats(libre: 500, visible: 0, pro: 0, elite: 0);

        var result = await _service.EvaluateCurrentMonthAsync();

        Assert.Equal(0m, result.CurrentMrr);
        Assert.Equal(0m, result.AccumulatedRevenue);
        Assert.Equal(0m, result.ProjectedMonthlyRevenue);
    }

    // ════════════════════════════════════════════════════════════════════════
    // REVENUE EVENT DTO
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RevenueThresholdAlertEvent_HasCorrectEventType()
    {
        var evt = new CarDealer.Contracts.Events.Alert.RevenueThresholdAlertEvent();
        Assert.Equal("alert.revenue.threshold_breached", evt.EventType);
    }

    [Fact]
    public void RevenueThresholdAlertEvent_DefaultSeverityIsWarning()
    {
        var evt = new CarDealer.Contracts.Events.Alert.RevenueThresholdAlertEvent();
        Assert.Equal("Warning", evt.Severity);
    }

    [Fact]
    public void RevenueThresholdAlertEvent_SetsAllProperties()
    {
        var evt = new CarDealer.Contracts.Events.Alert.RevenueThresholdAlertEvent
        {
            Period = "2026-03",
            AccumulatedRevenue = 1500m,
            ProjectedMonthlyRevenue = 1800m,
            OpexThreshold = 2215m,
            Shortfall = 415m,
            ShortfallPercent = 0.1873m,
            DaysElapsed = 15,
            DaysRemaining = 16,
            DailyRevenueRate = 100m,
            RequiredDailyRevenue = 25.94m,
            CurrentMrr = 1400m,
            AdditionalRevenue = 100m,
            Severity = "Critical",
            SuggestedAction = "Convert 15 Free→Visible",
            DealersByPlan = new Dictionary<string, int>
            {
                ["libre"] = 100,
                ["visible"] = 5,
                ["pro"] = 3,
                ["elite"] = 2,
            },
        };

        Assert.Equal("2026-03", evt.Period);
        Assert.Equal(1500m, evt.AccumulatedRevenue);
        Assert.Equal(415m, evt.Shortfall);
        Assert.Equal("Critical", evt.Severity);
        Assert.Equal(4, evt.DealersByPlan.Count);
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════════

    private void SetupDealerStats(int libre, int visible, int pro, int elite)
    {
        var stats = new DealerStatsDto
        {
            Total = libre + visible + pro + elite,
            Active = visible + pro + elite,
            ByPlan = new DealerPlanBreakdown
            {
                Libre = libre,
                Visible = visible,
                Pro = pro,
                Elite = elite,
            },
            TotalMrr = visible * 29m + pro * 89m + elite * 199m,
        };
        _dealerServiceMock
            .Setup(x => x.GetDealerStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);
    }
}
