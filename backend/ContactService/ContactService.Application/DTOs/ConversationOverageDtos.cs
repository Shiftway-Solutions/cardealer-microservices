namespace ContactService.Application.DTOs;

/// <summary>
/// DTO for a single overage conversation detail line.
/// Used in the downloadable overage report.
///
/// CONTRA #5 / OVERAGE BILLING FIX
/// </summary>
public record ConversationOverageDetailDto
{
    public Guid Id { get; init; }
    public Guid ContactRequestId { get; init; }
    public Guid BuyerId { get; init; }
    public Guid? VehicleId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public int ConversationNumber { get; init; }
    public decimal UnitCost { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}

/// <summary>
/// Summary DTO for the overage report header.
/// </summary>
public record ConversationOverageReportDto
{
    public Guid DealerId { get; init; }
    public string BillingPeriod { get; init; } = string.Empty;
    public string DealerPlan { get; init; } = string.Empty;
    public int IncludedLimit { get; init; }
    public int TotalConversations { get; init; }
    public int OverageCount { get; init; }
    public decimal UnitCost { get; init; }
    public decimal TotalOverageCost { get; init; }
    public string Currency { get; init; } = "USD";
    public List<ConversationOverageDetailDto> Details { get; init; } = new();
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Monthly conversation usage summary for the current billing period.
/// Used by the dealer dashboard to display usage vs. plan limit.
/// </summary>
public record MonthlyConversationUsageDto
{
    /// <summary>Total conversations initiated this calendar month.</summary>
    public int CurrentCount { get; init; }

    /// <summary>Plan limit for conversations per month. -1 = unlimited.</summary>
    public int MaxAllowed { get; init; }

    /// <summary>Conversations remaining before the plan limit is reached. -1 = unlimited.</summary>
    public int Remaining { get; init; }

    /// <summary>Usage as a percentage of the plan limit (0–100+). 0 for unlimited plans.</summary>
    public double UsagePercent { get; init; }

    /// <summary>Number of conversations beyond the plan limit (overage).</summary>
    public int OverageCount { get; init; }

    /// <summary>Current billing period in YYYY-MM format.</summary>
    public string BillingPeriod { get; init; } = string.Empty;
}
