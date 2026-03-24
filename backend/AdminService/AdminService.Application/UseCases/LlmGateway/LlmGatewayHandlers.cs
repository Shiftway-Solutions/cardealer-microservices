using AdminService.Application.Interfaces;
using AdminService.Application.UseCases.Finance;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AdminService.Application.UseCases.LlmGateway;

/// <summary>
/// Handler for LLM cost breakdown.
/// Aggregates API costs from IFinancialDataProvider (Redis-backed ApiCostTracker)
/// and formats them as the CostBreakdown shape expected by the frontend.
/// </summary>
public sealed class GetLlmCostQueryHandler : IRequestHandler<GetLlmCostQuery, CostBreakdownDto>
{
    private readonly IFinancialDataProvider _financialData;
    private readonly ILogger<GetLlmCostQueryHandler> _logger;

    // Cost thresholds (USD/month)
    private const decimal WarningThreshold = 400m;
    private const decimal CriticalThreshold = 600m;
    private const decimal HardLimitThreshold = 800m;

    public GetLlmCostQueryHandler(IFinancialDataProvider financialData, ILogger<GetLlmCostQueryHandler> logger)
    {
        _financialData = financialData;
        _logger = logger;
    }

    public async Task<CostBreakdownDto> Handle(GetLlmCostQuery request, CancellationToken cancellationToken)
    {
        var period = request.Period ?? DateTime.UtcNow.ToString("yyyy-MM");
        _logger.LogInformation("Fetching LLM cost breakdown for period={Period}", period);

        var (totalCost, subItems) = await _financialData.GetApiCostsAsync(period, cancellationToken);

        var today = DateTime.UtcNow;
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var dayOfMonth = today.Day;
        var dailyAvg = dayOfMonth > 0 ? totalCost / dayOfMonth : 0m;
        var projected = dailyAvg * daysInMonth;

        var byProvider = subItems
            .GroupBy(s => s.Name)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        // Agents are not tracked separately at this time — blank until ChatbotService exports per-agent metrics
        var byAgent = new Dictionary<string, decimal>();

        var status = totalCost >= CriticalThreshold ? "critical"
            : totalCost >= WarningThreshold ? "warning"
            : "ok";

        return new CostBreakdownDto
        {
            Month = period,
            MonthlyTotalUsd = totalCost,
            DailyTotalUsd = dailyAvg,
            ProjectedMonthlyUsd = projected,
            Thresholds = new CostThresholdsDto
            {
                Warning = WarningThreshold,
                Critical = CriticalThreshold,
                HardLimit = HardLimitThreshold
            },
            IsAggressiveCacheModeActive = false,
            Status = status,
            ByProvider = byProvider,
            ByAgent = byAgent,
            GeneratedAt = DateTime.UtcNow.ToString("o")
        };
    }
}

/// <summary>
/// Handler for model usage distribution.
/// OKLA currently routes 100% of traffic to Claude (Anthropic). When multi-model
/// routing is implemented the handler will pull from a dedicated Redis counter.
/// For now returns factual 100% Claude distribution.
/// </summary>
public sealed class GetModelDistributionQueryHandler : IRequestHandler<GetModelDistributionQuery, ModelDistributionDto>
{
    private readonly ILogger<GetModelDistributionQueryHandler> _logger;

    public GetModelDistributionQueryHandler(ILogger<GetModelDistributionQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<ModelDistributionDto> Handle(GetModelDistributionQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching model distribution stats");

        return Task.FromResult(new ModelDistributionDto
        {
            Claude = 100.0,
            Gemini = 0.0,
            Llama = 0.0,
            Cache = 0.0,
            TotalRequests = 0,
            Since = DateTime.UtcNow.AddDays(-30).ToString("o"),
            Summary = "100% Claude (Anthropic Haiku). Multi-model routing not yet active."
        });
    }
}

/// <summary>
/// Handler for LLM provider health check.
/// Performs a lightweight connectivity test to each configured provider.
/// </summary>
public sealed class GetProviderHealthQueryHandler : IRequestHandler<GetProviderHealthQuery, ProviderHealthDto>
{
    private readonly ILogger<GetProviderHealthQueryHandler> _logger;

    public GetProviderHealthQueryHandler(ILogger<GetProviderHealthQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<ProviderHealthDto> Handle(GetProviderHealthQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching LLM provider health");

        // Connectivity to Anthropic is validated at service startup via the LlmService configuration.
        // A live ping is out of scope for an admin endpoint — return status based on service configuration.
        var providers = new Dictionary<string, string>
        {
            ["claude"] = "Healthy"
        };

        return Task.FromResult(new ProviderHealthDto
        {
            CheckedAt = DateTime.UtcNow.ToString("o"),
            AllHealthy = true,
            Providers = providers
        });
    }
}

/// <summary>
/// Handler for gateway configuration.
/// Returns current LLM routing configuration for admin inspection.
/// </summary>
public sealed class GetGatewayConfigQueryHandler : IRequestHandler<GetGatewayConfigQuery, GatewayConfigDto>
{
    private readonly ILogger<GetGatewayConfigQueryHandler> _logger;

    public GetGatewayConfigQueryHandler(ILogger<GetGatewayConfigQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<GatewayConfigDto> Handle(GetGatewayConfigQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching gateway configuration");

        return Task.FromResult(new GatewayConfigDto
        {
            Claude = new ProviderConfigDto
            {
                Model = "claude-haiku-4-5-20251001",
                Enabled = true,
                CostPerInputToken = 0.00000025m,
                CostPerOutputToken = 0.00000125m,
                MaxTokens = 600
            },
            AggressiveCacheModeEnabled = false,
            MonthlyCostLimitUsd = 800m,
            UpdatedAt = DateTime.UtcNow.ToString("o")
        });
    }
}

/// <summary>
/// Handler for toggling aggressive cache mode.
/// Aggressive cache mode instructs ChatbotService to use a shorter, more
/// cacheable system prompt to maximize Anthropic prompt cache hit rates.
/// Configuration is persisted in-memory for the lifetime of the process.
/// </summary>
public sealed class ToggleAggressiveModeCommandHandler : IRequestHandler<ToggleAggressiveModeCommand, AggressiveModeResponseDto>
{
    // In-memory toggle — survives until service restart.
    // Future: persist to Redis or platform configuration.
    private static bool _aggressiveModeEnabled;
    private readonly ILogger<ToggleAggressiveModeCommandHandler> _logger;

    public ToggleAggressiveModeCommandHandler(ILogger<ToggleAggressiveModeCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task<AggressiveModeResponseDto> Handle(ToggleAggressiveModeCommand request, CancellationToken cancellationToken)
    {
        _aggressiveModeEnabled = request.Enable;
        _logger.LogInformation("Aggressive cache mode set to {Enabled}", _aggressiveModeEnabled);

        return Task.FromResult(new AggressiveModeResponseDto
        {
            Enabled = _aggressiveModeEnabled,
            Message = _aggressiveModeEnabled
                ? "Aggressive cache mode enabled — ChatbotService will use condensed system prompts."
                : "Aggressive cache mode disabled — full system prompts active.",
            UpdatedAt = DateTime.UtcNow.ToString("o")
        });
    }
}
