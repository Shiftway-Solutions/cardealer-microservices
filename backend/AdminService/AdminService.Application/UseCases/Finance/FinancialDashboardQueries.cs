using MediatR;

namespace AdminService.Application.UseCases.Finance;

/// <summary>
/// Query to get the full financial dashboard for the admin internal view.
/// Aggregates expenses, revenue, margin, and runway projection.
/// </summary>
public record GetFinancialDashboardQuery(
    /// <summary>Period in YYYY-MM format. Defaults to current month.</summary>
    string? Period = null
) : IRequest<FinancialDashboardDto>;

/// <summary>
/// Query to get revenue breakdown by subscription plan tier.
/// Returns MRR, dealer count, and unit revenue per plan (libre/visible/pro/elite).
/// </summary>
public record GetRevenueByPlanQuery(string? Period = null) : IRequest<RevenueBreakdownDto>;

/// <summary>
/// Query to get recent billing transactions across all dealers.
/// </summary>
public record GetBillingTransactionsQuery(int Limit = 10) : IRequest<List<AdminBillingTransactionDto>>;

/// <summary>
/// Query to get pending / overdue payments across all dealers.
/// </summary>
public record GetPendingPaymentsQuery() : IRequest<List<AdminPendingPaymentDto>>;
