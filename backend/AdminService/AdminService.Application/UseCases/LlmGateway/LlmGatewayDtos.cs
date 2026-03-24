using MediatR;

namespace AdminService.Application.UseCases.LlmGateway;

// ═══════════════════════════════════════════════════════════════════
// LLM GATEWAY ADMIN DTOs — match TypeScript interfaces in llm-costs.ts
// ═══════════════════════════════════════════════════════════════════

public sealed class CostThresholdsDto
{
    public decimal Warning { get; init; } = 400m;
    public decimal Critical { get; init; } = 600m;
    public decimal HardLimit { get; init; } = 800m;
}

public sealed class CostBreakdownDto
{
    public string Month { get; init; } = string.Empty;
    public decimal MonthlyTotalUsd { get; init; }
    public decimal DailyTotalUsd { get; init; }
    public decimal ProjectedMonthlyUsd { get; init; }
    public CostThresholdsDto Thresholds { get; init; } = new();
    public bool IsAggressiveCacheModeActive { get; init; }
    public string Status { get; init; } = "ok"; // ok | warning | critical
    public Dictionary<string, decimal> ByProvider { get; init; } = new();
    public Dictionary<string, decimal> ByAgent { get; init; } = new();
    public string GeneratedAt { get; init; } = string.Empty;
}

public sealed class ModelDistributionDto
{
    public double Claude { get; init; }
    public double Gemini { get; init; }
    public double Llama { get; init; }
    public double Cache { get; init; }
    public int TotalRequests { get; init; }
    public string Since { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
}

public sealed class ProviderHealthDto
{
    public string CheckedAt { get; init; } = string.Empty;
    public bool AllHealthy { get; init; }
    public Dictionary<string, string> Providers { get; init; } = new();
}

public sealed class ProviderConfigDto
{
    public string Model { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public decimal CostPerInputToken { get; init; }
    public decimal CostPerOutputToken { get; init; }
    public int MaxTokens { get; init; }
}

public sealed class GatewayConfigDto
{
    public ProviderConfigDto Claude { get; init; } = new();
    public bool AggressiveCacheModeEnabled { get; init; }
    public decimal MonthlyCostLimitUsd { get; init; }
    public string UpdatedAt { get; init; } = string.Empty;
}

public sealed class AggressiveModeResponseDto
{
    public bool Enabled { get; init; }
    public string Message { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}

// ═══════════════════════════════════════════════════════════════════
// QUERIES & COMMANDS
// ═══════════════════════════════════════════════════════════════════

public record GetLlmCostQuery(string? Period = null) : IRequest<CostBreakdownDto>;

public record GetModelDistributionQuery() : IRequest<ModelDistributionDto>;

public record GetProviderHealthQuery() : IRequest<ProviderHealthDto>;

public record GetGatewayConfigQuery() : IRequest<GatewayConfigDto>;

public record ToggleAggressiveModeCommand(bool Enable) : IRequest<AggressiveModeResponseDto>;
