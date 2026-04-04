using AdminService.Application.UseCases.Finance;

namespace AdminService.Application.Interfaces;

/// <summary>
/// Interface for retrieving financial data from external sources.
/// Abstracts HTTP calls to BillingService, LlmGateway cost endpoint, etc.
///
/// CONTRA #5 FIX: Enables unified financial dashboard by aggregating data
/// from multiple microservices into a single view.
/// </summary>
public interface IFinancialDataProvider
{
    /// <summary>
    /// Get LLM API costs for a period from the ApiCostTracker (Redis-backed).
    /// Returns total + per-provider breakdown.
    /// </summary>
    Task<(decimal Total, List<ExpenseSubItemDto> SubItems)> GetApiCostsAsync(
        string period, CancellationToken ct = default);

    /// <summary>
    /// Get infrastructure costs for a period.
    /// Sources: Platform configuration or BillingService.
    /// </summary>
    Task<(decimal Total, List<ExpenseSubItemDto> SubItems)> GetInfrastructureCostsAsync(
        string period, CancellationToken ct = default);

    /// <summary>
    /// Get marketing costs for a period from BillingService's MarketingSpend entity.
    /// </summary>
    Task<(decimal Total, List<ExpenseSubItemDto> SubItems)> GetMarketingCostsAsync(
        string period, CancellationToken ct = default);

    /// <summary>
    /// Get development costs for a period.
    /// Sources: Platform configuration (salaries, contractors, tools).
    /// </summary>
    Task<(decimal Total, List<ExpenseSubItemDto> SubItems)> GetDevelopmentCostsAsync(
        string period, CancellationToken ct = default);

    /// <summary>
    /// Get overage billing revenue for a period (conversations beyond plan limit at $0.08 each).
    /// </summary>
    Task<decimal> GetOverageRevenueAsync(string period, CancellationToken ct = default);

    /// <summary>
    /// Get advertising revenue for a period (dealer ad campaigns).
    /// </summary>
    Task<decimal> GetAdvertisingRevenueAsync(string period, CancellationToken ct = default);

    /// <summary>
    /// Get daily LLM API cost history for trending charts.
    /// </summary>
    Task<List<DailyFinancialEntryDto>> GetDailyExpenseHistoryAsync(
        int days = 30, CancellationToken ct = default);

    /// <summary>
    /// Get the configured cash balance for runway calculation.
    /// Sources: Platform configuration setting.
    /// </summary>
    Task<decimal> GetCashBalanceAsync(CancellationToken ct = default);

    /// <summary>
    /// Get recent billing transactions across all dealers.
    /// Sources: BillingService internal admin endpoint.
    /// Returns empty list on failure (graceful degradation).
    /// </summary>
    Task<List<AdminBillingTransactionDto>> GetRecentTransactionsAsync(int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Get pending / overdue payments across all dealers.
    /// Sources: BillingService internal admin endpoint.
    /// Returns empty list on failure (graceful degradation).
    /// </summary>
    Task<List<AdminPendingPaymentDto>> GetPendingPaymentsAsync(CancellationToken ct = default);
}
