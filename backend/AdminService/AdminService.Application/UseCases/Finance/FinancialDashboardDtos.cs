namespace AdminService.Application.UseCases.Finance;

// ═══════════════════════════════════════════════════════════════════════════════
// FINANCIAL DASHBOARD DTOs — CONTRA #5 FIX
//
// Internal admin dashboard showing:
//   1. Expenses by category (API, infrastructure, marketing, development)
//   2. Revenue by plan and source
//   3. Real-time net margin
//   4. Runway projection in months based on 30-day burn rate
//
// KPI AUDIT: No unified financial dashboard existed — revenue in AdminService,
// costs in LlmGateway/Redis, marketing in BillingService. Now unified here.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Full financial dashboard response with expenses, revenue, margin, and runway.
/// Designed for the admin internal view at /admin/financiero.
/// </summary>
public sealed class FinancialDashboardDto
{
    /// <summary>Month being reported (YYYY-MM).</summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>Currency code (always USD for OKLA).</summary>
    public string Currency { get; set; } = "USD";

    // ── EXPENSES ─────────────────────────────────────────────────────────

    /// <summary>Total expenses for the period.</summary>
    public decimal TotalExpenses { get; set; }

    /// <summary>Expense breakdown by category.</summary>
    public ExpenseBreakdownDto Expenses { get; set; } = new();

    // ── REVENUE ──────────────────────────────────────────────────────────

    /// <summary>Total revenue for the period (MRR + overage + other).</summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>Revenue breakdown by plan and source.</summary>
    public RevenueBreakdownDto Revenue { get; set; } = new();

    // ── MARGIN ───────────────────────────────────────────────────────────

    /// <summary>Net margin = TotalRevenue - TotalExpenses.</summary>
    public decimal NetMargin { get; set; }

    /// <summary>Net margin as percentage of revenue. Negative = burning cash.</summary>
    public decimal NetMarginPercent { get; set; }

    /// <summary>Whether the business is profitable this month.</summary>
    public bool IsProfitable { get; set; }

    // ── BURN RATE & RUNWAY ───────────────────────────────────────────────

    /// <summary>Average daily burn rate over the last 30 days (USD/day).</summary>
    public decimal DailyBurnRate { get; set; }

    /// <summary>Monthly burn rate = DailyBurnRate × 30.</summary>
    public decimal MonthlyBurnRate { get; set; }

    /// <summary>
    /// Runway in months = CashBalance / MonthlyBurnRate.
    /// -1 means infinite (profitable, not burning cash).
    /// 0 means no cash balance configured.
    /// </summary>
    public decimal RunwayMonths { get; set; }

    /// <summary>Current cash balance (configurable, defaults to $0 if not set).</summary>
    public decimal CashBalance { get; set; }

    // ── TRENDS ───────────────────────────────────────────────────────────

    /// <summary>Projected monthly expenses based on current daily rate.</summary>
    public decimal ProjectedMonthlyExpenses { get; set; }

    /// <summary>Projected monthly revenue based on current subscriptions.</summary>
    public decimal ProjectedMonthlyRevenue { get; set; }

    /// <summary>Daily expense history for the last 30 days (for chart).</summary>
    public List<DailyFinancialEntryDto> DailyHistory { get; set; } = [];

    /// <summary>When this dashboard was generated.</summary>
    public DateTimeOffset GeneratedAt { get; set; }
}

/// <summary>
/// Expense breakdown by category.
/// </summary>
public sealed class ExpenseBreakdownDto
{
    /// <summary>LLM API costs (Claude, Gemini, etc.) — from ApiCostTracker.</summary>
    public ExpenseCategoryDto Api { get; set; } = new() { Category = "API (LLM)" };

    /// <summary>Infrastructure costs (DigitalOcean, AWS, CDN, DB hosting).</summary>
    public ExpenseCategoryDto Infrastructure { get; set; } = new() { Category = "Infraestructura" };

    /// <summary>Marketing costs (Google Ads, Facebook, influencers).</summary>
    public ExpenseCategoryDto Marketing { get; set; } = new() { Category = "Marketing" };

    /// <summary>Development costs (salaries, contractors, tools).</summary>
    public ExpenseCategoryDto Development { get; set; } = new() { Category = "Desarrollo" };

    /// <summary>All categories as a list for iteration.</summary>
    public List<ExpenseCategoryDto> AsList() => [Api, Infrastructure, Marketing, Development];
}

/// <summary>
/// A single expense category with amount and percentage of total.
/// </summary>
public sealed class ExpenseCategoryDto
{
    /// <summary>Category display name (e.g., "API (LLM)", "Infraestructura").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Total amount for this category in the period (USD).</summary>
    public decimal Amount { get; set; }

    /// <summary>Percentage of total expenses.</summary>
    public decimal PercentOfTotal { get; set; }

    /// <summary>Sub-items breakdown (e.g., per-provider for API, per-service for infra).</summary>
    public List<ExpenseSubItemDto> SubItems { get; set; } = [];

    /// <summary>Chart color hex code.</summary>
    public string Color { get; set; } = "#6b7280";
}

/// <summary>
/// Sub-item within an expense category (e.g., Claude cost within API).
/// </summary>
public sealed class ExpenseSubItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// Revenue breakdown by plan and source.
/// </summary>
public sealed class RevenueBreakdownDto
{
    /// <summary>Monthly Recurring Revenue from plan subscriptions.</summary>
    public decimal Mrr { get; set; }

    /// <summary>MRR change vs previous month (percentage).</summary>
    public decimal MrrChangePercent { get; set; }

    /// <summary>Revenue per plan tier.</summary>
    public List<PlanRevenueDto> ByPlan { get; set; } = [];

    /// <summary>Revenue by source (subscriptions, overage, advertising, coins).</summary>
    public List<RevenueSourceDto> BySources { get; set; } = [];
}

/// <summary>
/// Revenue from a specific plan tier.
/// </summary>
public sealed class PlanRevenueDto
{
    /// <summary>Plan key (libre, visible, pro, elite).</summary>
    public string PlanKey { get; set; } = string.Empty;

    /// <summary>Plan display name.</summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>Number of active dealers on this plan.</summary>
    public int DealerCount { get; set; }

    /// <summary>Monthly price per dealer (USD).</summary>
    public decimal PricePerMonth { get; set; }

    /// <summary>Total MRR from this plan = DealerCount × PricePerMonth.</summary>
    public decimal TotalMrr { get; set; }

    /// <summary>Percentage of total MRR.</summary>
    public decimal PercentOfMrr { get; set; }

    /// <summary>Chart color hex code.</summary>
    public string Color { get; set; } = "#6b7280";
}

/// <summary>
/// Revenue from a specific source.
/// </summary>
public sealed class RevenueSourceDto
{
    /// <summary>Source name (e.g., "Suscripciones", "Overage", "Publicidad").</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Total amount from this source (USD).</summary>
    public decimal Amount { get; set; }

    /// <summary>Percentage of total revenue.</summary>
    public decimal PercentOfTotal { get; set; }

    /// <summary>Chart color hex code.</summary>
    public string Color { get; set; } = "#6b7280";
}

/// <summary>
/// Daily financial entry for trending charts.
/// </summary>
public sealed class DailyFinancialEntryDto
{
    /// <summary>Date (YYYY-MM-DD).</summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>Total expenses for the day.</summary>
    public decimal Expenses { get; set; }

    /// <summary>Total revenue for the day.</summary>
    public decimal Revenue { get; set; }

    /// <summary>Net margin for the day.</summary>
    public decimal NetMargin { get; set; }
}

// ===========================================================================
// BILLING TRANSACTIONS & PENDING PAYMENTS
// ===========================================================================

/// <summary>
/// A billing transaction visible to platform admins.
/// Corresponds to a payment processed by any dealer.
/// </summary>
public sealed class AdminBillingTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string DealerName { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    /// <summary>completed | pending | failed | refunded</summary>
    public string Status { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
}

/// <summary>
/// A pending / overdue payment visible to platform admins.
/// </summary>
public sealed class AdminPendingPaymentDto
{
    public string Id { get; set; } = string.Empty;
    public string DealerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string DueDate { get; set; } = string.Empty;
    public int DaysOverdue { get; set; }
}
