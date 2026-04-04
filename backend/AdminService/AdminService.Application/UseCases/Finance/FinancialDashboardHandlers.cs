using System.Globalization;
using AdminService.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AdminService.Application.UseCases.Finance;

/// <summary>
/// Handler for the financial dashboard query.
/// Aggregates expenses from multiple sources, calculates revenue from subscriptions,
/// computes net margin, and projects runway based on 30-day burn rate.
///
/// CONTRA #5 FIX: Unified financial dashboard — previously data was fragmented
/// across AdminService (MRR), BillingService (KPIs), LlmGateway (API costs).
///
/// Data sources:
///   Expenses:
///     - API (LLM): ApiCostTracker via HTTP / Redis
///     - Infrastructure: Platform configuration (DigitalOcean, AWS, CDN)
///     - Marketing: BillingService MarketingSpend entity
///     - Development: Platform configuration (salaries, contractors)
///   Revenue:
///     - Subscriptions: DealerStatsDto with plan breakdown × plan pricing
///     - Overage: ContactService conversation overage at $0.08/each
///     - Advertising: AdminService advertising campaigns
///   Margin:
///     - Net Margin = Total Revenue - Total Expenses
///   Runway:
///     - Monthly Burn = 30-day rolling average of daily net loss
///     - Runway = Cash Balance / Monthly Burn
/// </summary>
public sealed class GetFinancialDashboardQueryHandler
    : IRequestHandler<GetFinancialDashboardQuery, FinancialDashboardDto>
{
    private readonly IDealerService _dealerService;
    private readonly IFinancialDataProvider _financialData;
    private readonly ILogger<GetFinancialDashboardQueryHandler> _logger;

    // Plan pricing (from PlanConfiguration.cs — hardcoded here to avoid cross-project dependency)
    private static readonly Dictionary<string, (string Name, decimal Price, string Color)> PlanPricing = new()
    {
        ["libre"] = ("Libre", 0m, "#9ca3af"),
        ["visible"] = ("Visible", 29m, "#3b82f6"),
        ["pro"] = ("Pro", 89m, "#8b5cf6"),
        ["elite"] = ("Elite", 199m, "#f59e0b"),
    };

    public GetFinancialDashboardQueryHandler(
        IDealerService dealerService,
        IFinancialDataProvider financialData,
        ILogger<GetFinancialDashboardQueryHandler> logger)
    {
        _dealerService = dealerService;
        _financialData = financialData;
        _logger = logger;
    }

    public async Task<FinancialDashboardDto> Handle(
        GetFinancialDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var period = request.Period
            ?? DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        _logger.LogInformation("[FinancialDashboard] Generating dashboard for period={Period}", period);

        // ── PARALLEL DATA FETCH ──────────────────────────────────────────
        var dealerStatsTask = _dealerService.GetDealerStatsAsync(cancellationToken);
        var apiCostsTask = _financialData.GetApiCostsAsync(period, cancellationToken);
        var infraCostsTask = _financialData.GetInfrastructureCostsAsync(period, cancellationToken);
        var marketingCostsTask = _financialData.GetMarketingCostsAsync(period, cancellationToken);
        var devCostsTask = _financialData.GetDevelopmentCostsAsync(period, cancellationToken);
        var overageRevenueTask = _financialData.GetOverageRevenueAsync(period, cancellationToken);
        var adRevenueTask = _financialData.GetAdvertisingRevenueAsync(period, cancellationToken);
        var dailyHistoryTask = _financialData.GetDailyExpenseHistoryAsync(30, cancellationToken);
        var cashBalanceTask = _financialData.GetCashBalanceAsync(cancellationToken);

        await Task.WhenAll(
            dealerStatsTask, apiCostsTask, infraCostsTask, marketingCostsTask,
            devCostsTask, overageRevenueTask, adRevenueTask, dailyHistoryTask,
            cashBalanceTask);

        var dealerStats = await dealerStatsTask;
        var apiCosts = await apiCostsTask;
        var infraCosts = await infraCostsTask;
        var marketingCosts = await marketingCostsTask;
        var devCosts = await devCostsTask;
        var overageRevenue = await overageRevenueTask;
        var adRevenue = await adRevenueTask;
        var dailyHistory = await dailyHistoryTask;
        var cashBalance = await cashBalanceTask;

        // ── EXPENSES ─────────────────────────────────────────────────────
        var totalExpenses = apiCosts.Total + infraCosts.Total + marketingCosts.Total + devCosts.Total;

        var expenses = new ExpenseBreakdownDto
        {
            Api = BuildCategory("API (LLM)", apiCosts.Total, totalExpenses, apiCosts.SubItems, "#ef4444"),
            Infrastructure = BuildCategory("Infraestructura", infraCosts.Total, totalExpenses, infraCosts.SubItems, "#3b82f6"),
            Marketing = BuildCategory("Marketing", marketingCosts.Total, totalExpenses, marketingCosts.SubItems, "#22c55e"),
            Development = BuildCategory("Desarrollo", devCosts.Total, totalExpenses, devCosts.SubItems, "#8b5cf6"),
        };

        // ── REVENUE ──────────────────────────────────────────────────────
        var byPlan = BuildPlanRevenue(dealerStats);
        var mrr = byPlan.Sum(p => p.TotalMrr);
        var subscriptionRevenue = mrr;
        var totalRevenue = subscriptionRevenue + overageRevenue + adRevenue;

        var revenue = new RevenueBreakdownDto
        {
            Mrr = mrr,
            MrrChangePercent = dealerStats?.TotalMrr > 0 && mrr > 0
                ? Math.Round((mrr - dealerStats.TotalMrr) / dealerStats.TotalMrr * 100, 1)
                : 0m,
            ByPlan = byPlan,
            BySources = BuildRevenueSources(subscriptionRevenue, overageRevenue, adRevenue, totalRevenue),
        };

        // ── NET MARGIN ───────────────────────────────────────────────────
        var netMargin = totalRevenue - totalExpenses;
        var netMarginPercent = totalRevenue > 0
            ? Math.Round(netMargin / totalRevenue * 100, 1)
            : 0m;

        // ── BURN RATE & RUNWAY ───────────────────────────────────────────
        var dayOfMonth = DateTime.UtcNow.Day;
        var daysInMonth = DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month);

        // Daily burn = average daily net loss (expenses - revenue) over elapsed days
        var dailyBurnRate = dayOfMonth > 0
            ? Math.Max(0, (totalExpenses - totalRevenue) / dayOfMonth)
            : 0m;

        var monthlyBurnRate = dailyBurnRate * 30;

        // Runway = cash / monthly burn. If profitable, runway is infinite (-1).
        decimal runwayMonths;
        if (netMargin >= 0)
            runwayMonths = -1m; // Profitable — infinite runway
        else if (cashBalance <= 0 || monthlyBurnRate <= 0)
            runwayMonths = 0m; // No cash or no burn data
        else
            runwayMonths = Math.Round(cashBalance / monthlyBurnRate, 1);

        // ── PROJECTIONS ──────────────────────────────────────────────────
        var projectedExpenses = dayOfMonth > 0
            ? Math.Round(totalExpenses / dayOfMonth * daysInMonth, 2)
            : totalExpenses;
        var projectedRevenue = mrr; // MRR is already a monthly projection

        var dashboard = new FinancialDashboardDto
        {
            Period = period,
            Currency = "USD",
            TotalExpenses = Math.Round(totalExpenses, 2),
            Expenses = expenses,
            TotalRevenue = Math.Round(totalRevenue, 2),
            Revenue = revenue,
            NetMargin = Math.Round(netMargin, 2),
            NetMarginPercent = netMarginPercent,
            IsProfitable = netMargin >= 0,
            DailyBurnRate = Math.Round(dailyBurnRate, 2),
            MonthlyBurnRate = Math.Round(monthlyBurnRate, 2),
            RunwayMonths = runwayMonths,
            CashBalance = cashBalance,
            ProjectedMonthlyExpenses = projectedExpenses,
            ProjectedMonthlyRevenue = projectedRevenue,
            DailyHistory = dailyHistory,
            GeneratedAt = DateTimeOffset.UtcNow,
        };

        _logger.LogInformation(
            "[FinancialDashboard] Period={Period} Revenue=${Revenue:F2} Expenses=${Expenses:F2} " +
            "NetMargin=${Margin:F2} ({MarginPct}%) BurnRate=${Burn:F2}/day Runway={Runway}mo",
            period, totalRevenue, totalExpenses, netMargin, netMarginPercent,
            dailyBurnRate, runwayMonths);

        return dashboard;
    }

    // ── HELPERS ──────────────────────────────────────────────────────────

    private static ExpenseCategoryDto BuildCategory(
        string name, decimal amount, decimal total,
        List<ExpenseSubItemDto> subItems, string color)
    {
        return new ExpenseCategoryDto
        {
            Category = name,
            Amount = Math.Round(amount, 2),
            PercentOfTotal = total > 0 ? Math.Round(amount / total * 100, 1) : 0m,
            SubItems = subItems,
            Color = color,
        };
    }

    private static List<PlanRevenueDto> BuildPlanRevenue(
        AdminService.Application.UseCases.Dealers.DealerStatsDto? stats)
    {
        if (stats?.ByPlan == null)
            return [];

        var plans = new List<PlanRevenueDto>();
        var planCounts = new Dictionary<string, int>
        {
            ["libre"] = stats.ByPlan.Libre,
            ["visible"] = stats.ByPlan.Visible,
            ["pro"] = stats.ByPlan.Pro,
            ["elite"] = stats.ByPlan.Elite,
        };

        var totalMrr = 0m;
        foreach (var (key, count) in planCounts)
        {
            if (!PlanPricing.TryGetValue(key, out var pricing)) continue;
            totalMrr += count * pricing.Price;
        }

        foreach (var (key, count) in planCounts)
        {
            if (!PlanPricing.TryGetValue(key, out var pricing)) continue;
            var planMrr = count * pricing.Price;

            plans.Add(new PlanRevenueDto
            {
                PlanKey = key,
                PlanName = pricing.Name,
                DealerCount = count,
                PricePerMonth = pricing.Price,
                TotalMrr = planMrr,
                PercentOfMrr = totalMrr > 0 ? Math.Round(planMrr / totalMrr * 100, 1) : 0m,
                Color = pricing.Color,
            });
        }

        return plans;
    }

    private static List<RevenueSourceDto> BuildRevenueSources(
        decimal subscriptions, decimal overage, decimal advertising, decimal total)
    {
        return
        [
            new()
            {
                Source = "Suscripciones",
                Amount = Math.Round(subscriptions, 2),
                PercentOfTotal = total > 0 ? Math.Round(subscriptions / total * 100, 1) : 0m,
                Color = "#3b82f6",
            },
            new()
            {
                Source = "Overage (conversaciones)",
                Amount = Math.Round(overage, 2),
                PercentOfTotal = total > 0 ? Math.Round(overage / total * 100, 1) : 0m,
                Color = "#f59e0b",
            },
            new()
            {
                Source = "Publicidad",
                Amount = Math.Round(advertising, 2),
                PercentOfTotal = total > 0 ? Math.Round(advertising / total * 100, 1) : 0m,
                Color = "#22c55e",
            },
        ];
    }
}

/// <summary>
/// Handler for revenue breakdown by subscription plan.
/// Reuses IDealerService to get the plan-tier breakdown and compute MRR per plan.
/// </summary>
public sealed class GetRevenueByPlanQueryHandler : IRequestHandler<GetRevenueByPlanQuery, RevenueBreakdownDto>
{
    private readonly IDealerService _dealerService;
    private readonly ILogger<GetRevenueByPlanQueryHandler> _logger;

    private static readonly Dictionary<string, (string Name, decimal Price, string Color)> PlanPricing = new()
    {
        ["libre"] = ("Libre", 0m, "#9ca3af"),
        ["visible"] = ("Visible", 29m, "#3b82f6"),
        ["pro"] = ("Pro", 89m, "#8b5cf6"),
        ["elite"] = ("Elite", 199m, "#f59e0b"),
    };

    public GetRevenueByPlanQueryHandler(IDealerService dealerService, ILogger<GetRevenueByPlanQueryHandler> logger)
    {
        _dealerService = dealerService;
        _logger = logger;
    }

    public async Task<RevenueBreakdownDto> Handle(GetRevenueByPlanQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching revenue by plan breakdown");

        var dealerStats = await _dealerService.GetDealerStatsAsync(cancellationToken);

        var planCounts = new Dictionary<string, int>
        {
            ["libre"] = dealerStats.ByPlan?.Libre ?? 0,
            ["visible"] = dealerStats.ByPlan?.Visible ?? 0,
            ["pro"] = dealerStats.ByPlan?.Pro ?? 0,
            ["elite"] = dealerStats.ByPlan?.Elite ?? 0,
        };

        var byPlan = new List<PlanRevenueDto>();
        var totalMrr = 0m;

        foreach (var (key, count) in planCounts)
        {
            if (!PlanPricing.TryGetValue(key, out var pricing)) continue;
            totalMrr += count * pricing.Price;
        }

        foreach (var (key, count) in planCounts)
        {
            if (!PlanPricing.TryGetValue(key, out var pricing)) continue;
            var planMrr = count * pricing.Price;
            byPlan.Add(new PlanRevenueDto
            {
                PlanKey = key,
                PlanName = pricing.Name,
                DealerCount = count,
                PricePerMonth = pricing.Price,
                TotalMrr = planMrr,
                PercentOfMrr = totalMrr > 0 ? Math.Round(planMrr / totalMrr * 100, 1) : 0m,
                Color = pricing.Color,
            });
        }

        return new RevenueBreakdownDto
        {
            Mrr = totalMrr,
            MrrChangePercent = 0m,
            ByPlan = byPlan,
            BySources = [],
        };
    }
}

/// <summary>
/// Handler for recent billing transactions query.
/// Delegates to IFinancialDataProvider which calls BillingService internal admin endpoint.
/// Returns empty list on failure (graceful degradation — same pattern as marketing-spend).
/// </summary>
public sealed class GetBillingTransactionsQueryHandler
    : IRequestHandler<GetBillingTransactionsQuery, List<AdminBillingTransactionDto>>
{
    private readonly IFinancialDataProvider _financialData;
    private readonly ILogger<GetBillingTransactionsQueryHandler> _logger;

    public GetBillingTransactionsQueryHandler(
        IFinancialDataProvider financialData,
        ILogger<GetBillingTransactionsQueryHandler> logger)
    {
        _financialData = financialData;
        _logger = logger;
    }

    public async Task<List<AdminBillingTransactionDto>> Handle(
        GetBillingTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin requested recent billing transactions (limit={Limit})", request.Limit);
        return await _financialData.GetRecentTransactionsAsync(request.Limit, cancellationToken);
    }
}

/// <summary>
/// Handler for pending payments query.
/// Delegates to IFinancialDataProvider which calls BillingService internal admin endpoint.
/// Returns empty list on failure (graceful degradation).
/// </summary>
public sealed class GetPendingPaymentsQueryHandler
    : IRequestHandler<GetPendingPaymentsQuery, List<AdminPendingPaymentDto>>
{
    private readonly IFinancialDataProvider _financialData;
    private readonly ILogger<GetPendingPaymentsQueryHandler> _logger;

    public GetPendingPaymentsQueryHandler(
        IFinancialDataProvider financialData,
        ILogger<GetPendingPaymentsQueryHandler> logger)
    {
        _financialData = financialData;
        _logger = logger;
    }

    public async Task<List<AdminPendingPaymentDto>> Handle(
        GetPendingPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin requested pending payments");
        return await _financialData.GetPendingPaymentsAsync(cancellationToken);
    }
}
