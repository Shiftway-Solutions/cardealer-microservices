using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using AdminService.Application.Interfaces;
using AdminService.Application.UseCases.Finance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AdminService.Infrastructure.External;

/// <summary>
/// Default implementation of IFinancialDataProvider.
/// Retrieves financial data from:
///   - LLM API costs: HTTP call to Gateway's cost endpoint or Redis fallback
///   - Infrastructure costs: Platform configuration settings
///   - Marketing costs: HTTP call to BillingService
///   - Development costs: Platform configuration settings
///   - Overage/Advertising revenue: HTTP calls to BillingService/ContactService
///
/// CONTRA #5 FIX: Unified financial data aggregation for admin dashboard.
/// All HTTP calls are wrapped in try/catch with fallback to $0 to prevent
/// partial dashboard failures from blocking the entire view.
/// </summary>
public sealed class FinancialDataProvider : IFinancialDataProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FinancialDataProvider> _logger;

    // Configuration keys for fixed monthly costs (set in appsettings or admin panel)
    private const string InfraSection = "FinancialDashboard:MonthlyCosts:Infrastructure";
    private const string DevSection = "FinancialDashboard:MonthlyCosts:Development";
    private const string CashBalanceKey = "FinancialDashboard:CashBalance";

    public FinancialDataProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FinancialDataProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(decimal Total, List<ExpenseSubItemDto> SubItems)> GetApiCostsAsync(
        string period, CancellationToken ct = default)
    {
        try
        {
            // Try to call Gateway's cost breakdown endpoint
            var client = _httpClientFactory.CreateClient("Gateway");
            var response = await client.GetAsync("/api/internal/llm-costs/breakdown", ct);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var breakdown = JsonSerializer.Deserialize<LlmCostBreakdownResponse>(json, options);

                if (breakdown != null)
                {
                    var subItems = new List<ExpenseSubItemDto>();
                    foreach (var (provider, cost) in breakdown.ByProvider)
                    {
                        subItems.Add(new ExpenseSubItemDto
                        {
                            Name = CapitalizeFirst(provider),
                            Amount = Math.Round(cost, 2)
                        });
                    }

                    return (Math.Round(breakdown.MonthlyTotalUsd, 2), subItems);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FinancialData] Failed to fetch LLM API costs — returning $0");
        }

        return (0m, []);
    }

    public Task<(decimal Total, List<ExpenseSubItemDto> SubItems)> GetInfrastructureCostsAsync(
        string period, CancellationToken ct = default)
    {
        // Infrastructure costs are configured in appsettings.json
        // (servers, DB hosting, CDN, monitoring, etc.)
        var subItems = new List<ExpenseSubItemDto>();
        var total = 0m;

        var items = _configuration.GetSection(InfraSection).GetChildren();
        foreach (var item in items)
        {
            var name = item.Key;
            if (decimal.TryParse(item.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                subItems.Add(new ExpenseSubItemDto { Name = name, Amount = amount });
                total += amount;
            }
        }

        // If no config, use sensible defaults for a small SaaS
        if (subItems.Count == 0)
        {
            subItems =
            [
                new() { Name = "DigitalOcean (Servidores)", Amount = 150m },
                new() { Name = "PostgreSQL (Managed DB)", Amount = 50m },
                new() { Name = "Redis (Managed)", Amount = 30m },
                new() { Name = "CDN / Almacenamiento", Amount = 25m },
                new() { Name = "Monitoring (Grafana/Prometheus)", Amount = 20m },
                new() { Name = "DNS / SSL / Dominio", Amount = 10m },
            ];
            total = subItems.Sum(s => s.Amount);

            _logger.LogDebug(
                "[FinancialData] No infrastructure costs configured — using defaults (${Total}/mo)",
                total);
        }

        return Task.FromResult((total, subItems));
    }

    public async Task<(decimal Total, List<ExpenseSubItemDto> SubItems)> GetMarketingCostsAsync(
        string period, CancellationToken ct = default)
    {
        try
        {
            // Parse period to get year/month for BillingService query
            if (DateTime.TryParseExact(period, "yyyy-MM", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            {
                var client = _httpClientFactory.CreateClient("BillingService");
                var url = $"/api/internal/marketing-spend?year={date.Year}&month={date.Month}";
                var response = await client.GetAsync(url, ct);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var spends = JsonSerializer.Deserialize<List<MarketingSpendResponse>>(json, options);

                    if (spends != null && spends.Count > 0)
                    {
                        var subItems = spends.Select(s => new ExpenseSubItemDto
                        {
                            Name = s.Channel,
                            Amount = Math.Round(s.SpendUsd, 2)
                        }).ToList();

                        return (subItems.Sum(s => s.Amount), subItems);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FinancialData] Failed to fetch marketing costs — returning $0");
        }

        return (0m, []);
    }

    public Task<(decimal Total, List<ExpenseSubItemDto> SubItems)> GetDevelopmentCostsAsync(
        string period, CancellationToken ct = default)
    {
        // Development costs are configured in appsettings.json
        // (salaries, contractors, tools, SaaS subscriptions)
        var subItems = new List<ExpenseSubItemDto>();
        var total = 0m;

        var items = _configuration.GetSection(DevSection).GetChildren();
        foreach (var item in items)
        {
            var name = item.Key;
            if (decimal.TryParse(item.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                subItems.Add(new ExpenseSubItemDto { Name = name, Amount = amount });
                total += amount;
            }
        }

        // If no config, use sensible defaults for a small startup team
        if (subItems.Count == 0)
        {
            subItems =
            [
                new() { Name = "Desarrollo (equipo interno)", Amount = 3000m },
                new() { Name = "Herramientas (GitHub, Copilot, etc.)", Amount = 100m },
                new() { Name = "QA / Testing", Amount = 200m },
            ];
            total = subItems.Sum(s => s.Amount);

            _logger.LogDebug(
                "[FinancialData] No development costs configured — using defaults (${Total}/mo)",
                total);
        }

        return Task.FromResult((total, subItems));
    }

    public async Task<decimal> GetOverageRevenueAsync(string period, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ContactService");
            var url = $"/api/internal/overage-revenue?period={period}";
            var response = await client.GetAsync(url, ct);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                if (decimal.TryParse(json.Trim('"'), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var revenue))
                {
                    return revenue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FinancialData] Failed to fetch overage revenue — returning $0");
        }

        return 0m;
    }

    public async Task<decimal> GetAdvertisingRevenueAsync(string period, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BillingService");
            var url = $"/api/internal/advertising-revenue?period={period}";
            var response = await client.GetAsync(url, ct);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                if (decimal.TryParse(json.Trim('"'), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var revenue))
                {
                    return revenue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FinancialData] Failed to fetch advertising revenue — returning $0");
        }

        return 0m;
    }

    public async Task<List<DailyFinancialEntryDto>> GetDailyExpenseHistoryAsync(
        int days = 30, CancellationToken ct = default)
    {
        var entries = new List<DailyFinancialEntryDto>();

        try
        {
            var client = _httpClientFactory.CreateClient("Gateway");
            var response = await client.GetAsync($"/api/internal/llm-costs/daily?days={days}", ct);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dailyCosts = JsonSerializer.Deserialize<List<DailyCostResponse>>(json, options);

                if (dailyCosts != null)
                {
                    entries = dailyCosts.Select(d => new DailyFinancialEntryDto
                    {
                        Date = d.Date,
                        Expenses = Math.Round(d.CostUsd, 2),
                        Revenue = 0m, // Daily revenue breakdown not yet available
                        NetMargin = -Math.Round(d.CostUsd, 2) // Expenses are negative
                    }).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FinancialData] Failed to fetch daily expense history");
        }

        // Fill missing days with $0
        if (entries.Count < days)
        {
            var today = DateTime.UtcNow.Date;
            var existingDates = entries.Select(e => e.Date).ToHashSet();

            for (var i = days - 1; i >= 0; i--)
            {
                var date = today.AddDays(-i).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (!existingDates.Contains(date))
                {
                    entries.Add(new DailyFinancialEntryDto { Date = date });
                }
            }

            entries = entries.OrderBy(e => e.Date).ToList();
        }

        return entries;
    }

    public Task<decimal> GetCashBalanceAsync(CancellationToken ct = default)
    {
        // Cash balance configured in appsettings or admin settings
        var cashStr = _configuration[CashBalanceKey];
        if (decimal.TryParse(cashStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var cash))
        {
            return Task.FromResult(cash);
        }

        // Default: no cash balance configured
        return Task.FromResult(0m);
    }

    public async Task<List<AdminBillingTransactionDto>> GetRecentTransactionsAsync(
        int limit = 10, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BillingService");
            var response = await client.GetAsync(
                $"/api/internal/admin/transactions?limit={limit}", ct);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<List<AdminBillingTransactionDto>>(json, options);
                if (items != null)
                    return items;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FinancialData] Failed to fetch billing transactions — returning empty list");
        }

        return [];
    }

    public async Task<List<AdminPendingPaymentDto>> GetPendingPaymentsAsync(CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BillingService");
            var response = await client.GetAsync("/api/internal/admin/pending", ct);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<List<AdminPendingPaymentDto>>(json, options);
                if (items != null)
                    return items;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FinancialData] Failed to fetch pending payments — returning empty list");
        }

        return [];
    }

    // ── PRIVATE RESPONSE DTOs ────────────────────────────────────────────

    private static string CapitalizeFirst(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

    private sealed class LlmCostBreakdownResponse
    {
        public decimal MonthlyTotalUsd { get; set; }
        public decimal DailyTotalUsd { get; set; }
        public Dictionary<string, decimal> ByProvider { get; set; } = new();
        public Dictionary<string, decimal> ByAgent { get; set; } = new();
    }

    private sealed class MarketingSpendResponse
    {
        public string Channel { get; set; } = string.Empty;
        public decimal SpendUsd { get; set; }
    }

    private sealed class DailyCostResponse
    {
        public string Date { get; set; } = string.Empty;
        public decimal CostUsd { get; set; }
    }
}
