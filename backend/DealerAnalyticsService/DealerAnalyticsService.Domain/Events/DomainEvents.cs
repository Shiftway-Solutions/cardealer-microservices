namespace DealerAnalyticsService.Domain.Events;

/// <summary>
/// Base interface for all domain events
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    string EventType { get; }
}

/// <summary>
/// Event published when a daily dealer snapshot is created
/// </summary>
public record DealerSnapshotCreatedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string EventType => "dealer.analytics.snapshot.created";

    public Guid DealerId { get; init; }
    public Guid SnapshotId { get; init; }
    public DateOnly Date { get; init; }
    public int TotalInventory { get; init; }
    public int ActiveListings { get; init; }
    public int TotalViews { get; init; }
    public int TotalContacts { get; init; }
    public decimal ConversionRate { get; init; }
}

/// <summary>
/// Event published when a dealer alert is triggered
/// </summary>
public record DealerAlertTriggeredEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string EventType => "dealer.analytics.alert.triggered";

    public Guid DealerId { get; init; }
    public Guid AlertId { get; init; }
    public string AlertType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool RequiresNotification { get; init; }
}

/// <summary>
/// Event published when a dealer's benchmark/tier changes
/// </summary>
public record DealerTierChangedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string EventType => "dealer.analytics.tier.changed";

    public Guid DealerId { get; init; }
    public string PreviousTier { get; init; } = string.Empty;
    public string NewTier { get; init; } = string.Empty;
    public int NewRanking { get; init; }
    public decimal NewPerformanceScore { get; init; }
}

/// <summary>
/// Event published when a lead funnel analysis is completed
/// </summary>
public record LeadFunnelAnalyzedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string EventType => "dealer.analytics.funnel.analyzed";

    public Guid DealerId { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public int Impressions { get; init; }
    public int Views { get; init; }
    public int Contacts { get; init; }
    public int Conversions { get; init; }
    public decimal OverallConversionRate { get; init; }
}

/// <summary>
/// Event published when inventory aging analysis detects at-risk vehicles
/// </summary>
public record InventoryAgingAnalyzedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string EventType => "dealer.analytics.inventory.aging.analyzed";

    public Guid DealerId { get; init; }
    public int TotalVehicles { get; init; }
    public int AtRiskVehicles { get; init; }
    public decimal AtRiskValue { get; init; }
    public decimal AverageDaysOnMarket { get; init; }
}

/// <summary>
/// Event published when a report is generated
/// </summary>
public record AnalyticsReportGeneratedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string EventType => "dealer.analytics.report.generated";

    public Guid DealerId { get; init; }
    public string ReportType { get; init; } = string.Empty; // daily, weekly, monthly
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public string? DownloadUrl { get; init; }
}
