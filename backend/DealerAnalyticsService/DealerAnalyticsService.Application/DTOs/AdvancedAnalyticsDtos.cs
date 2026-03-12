namespace DealerAnalyticsService.Application.DTOs;

/// <summary>
/// DTO para snapshot de dealer
/// </summary>
public record DealerSnapshotDto
{
    public Guid Id { get; init; }
    public Guid DealerId { get; init; }
    public DateTime SnapshotDate { get; init; }

    // Inventory Metrics
    public int TotalVehicles { get; init; }
    public int ActiveVehicles { get; init; }
    public int SoldVehicles { get; init; }
    public decimal TotalInventoryValue { get; init; }
    public decimal AvgVehiclePrice { get; init; }
    public double AvgDaysOnMarket { get; init; }
    public int VehiclesOver60Days { get; init; }

    // Engagement Metrics
    public int TotalViews { get; init; }
    public int UniqueViews { get; init; }
    public int TotalContacts { get; init; }
    public int TotalFavorites { get; init; }
    public int SearchImpressions { get; init; }

    // Lead Metrics
    public int NewLeads { get; init; }
    public int QualifiedLeads { get; init; }
    public int ConvertedLeads { get; init; }
    public double LeadConversionRate { get; init; }

    // Revenue Metrics
    public decimal TotalRevenue { get; init; }
    public decimal AvgTransactionValue { get; init; }

    // Calculated Rates
    public double ClickThroughRate { get; init; }
    public double ContactRate { get; init; }
    public double FavoriteRate { get; init; }
    public double InventoryTurnoverRate { get; init; }
    public double AgingRate { get; init; }
}

/// <summary>
/// DTO para comparación de snapshots con período anterior
/// </summary>
public record SnapshotComparisonDto
{
    public DealerSnapshotDto Current { get; init; } = null!;
    public DealerSnapshotDto? Previous { get; init; }

    // Cambios porcentuales
    public double ViewsChange { get; init; }
    public double ContactsChange { get; init; }
    public double LeadsChange { get; init; }
    public double SalesChange { get; init; }
    public double RevenueChange { get; init; }
    public double ConversionRateChange { get; init; }
    public double InventoryValueChange { get; init; }
}

/// <summary>
/// DTO para performance de vehículo
/// </summary>
public record VehiclePerformanceDto
{
    public Guid Id { get; init; }
    public Guid VehicleId { get; init; }
    public Guid DealerId { get; init; }

    // Vehicle Info
    public string? VehicleTitle { get; init; }
    public string? VehicleMake { get; init; }
    public string? VehicleModel { get; init; }
    public int? VehicleYear { get; init; }
    public decimal? VehiclePrice { get; init; }
    public string? VehicleThumbnailUrl { get; init; }

    // Metrics
    public int Views { get; init; }
    public int UniqueViews { get; init; }
    public int Contacts { get; init; }
    public int Favorites { get; init; }
    public int SearchImpressions { get; init; }
    public int SearchClicks { get; init; }

    // Rates
    public double ClickThroughRate { get; init; }
    public double ContactRate { get; init; }
    public double FavoriteRate { get; init; }

    // Scores
    public double EngagementScore { get; init; }
    public double PerformanceScore { get; init; }

    // Status
    public int DaysOnMarket { get; init; }
    public bool IsSold { get; init; }

    // Ranking
    public int? Rank { get; init; }
    public string? PerformanceLabel { get; init; } // "Top Performer", "Average", "Needs Attention"
}

/// <summary>
/// DTO para métricas del funnel de leads
/// </summary>
public record LeadFunnelDto
{
    public Guid DealerId { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }

    // Funnel Stages
    public int Impressions { get; init; }
    public int Views { get; init; }
    public int Contacts { get; init; }
    public int Qualified { get; init; }
    public int Negotiation { get; init; }
    public int Converted { get; init; }

    // Conversion Rates
    public double ImpressionsToViews { get; init; }
    public double ViewsToContacts { get; init; }
    public double ContactsToQualified { get; init; }
    public double QualifiedToConverted { get; init; }
    public double OverallConversion { get; init; }

    // Visualization data
    public List<FunnelStageDto> Stages { get; init; } = new();

    // Revenue
    public decimal AttributedRevenue { get; init; }
    public decimal AvgDealValue { get; init; }

    // Comparison with previous period
    public double? ConversionChange { get; init; }
}

public record FunnelStageDto
{
    public string Name { get; init; } = string.Empty;
    public int Value { get; init; }
    public double ConversionRate { get; init; }
    public double DropOffRate { get; init; }
    public string Color { get; init; } = "#3B82F6";
    public double Percentage { get; init; }
}

/// <summary>
/// DTO para benchmark de dealer
/// </summary>
public record DealerBenchmarkDto
{
    public Guid DealerId { get; init; }
    public DateTime Date { get; init; }

    // Dealer's Metrics
    public double AvgDaysOnMarket { get; init; }
    public double ConversionRate { get; init; }
    public double AvgResponseTimeMinutes { get; init; }
    public double CustomerSatisfaction { get; init; }
    public double ListingQualityScore { get; init; }
    public int ActiveListings { get; init; }
    public int MonthlySales { get; init; }

    // Market Comparison
    public MarketComparisonDto MarketComparison { get; init; } = null!;

    // Rankings
    public RankingsDto Rankings { get; init; } = null!;

    // Tier
    public string Tier { get; init; } = string.Empty;
    public string TierColor { get; init; } = string.Empty;
    public string TierIcon { get; init; } = string.Empty;

    // Analysis
    public List<string> Strengths { get; init; } = new();
    public List<string> ImprovementAreas { get; init; } = new();
}

public record MarketComparisonDto
{
    public double MarketAvgDaysOnMarket { get; init; }
    public double MarketAvgConversionRate { get; init; }
    public double MarketAvgResponseTime { get; init; }
    public double MarketAvgSatisfaction { get; init; }

    public bool IsBetterDaysOnMarket { get; init; }
    public bool IsBetterConversion { get; init; }
    public bool IsBetterResponseTime { get; init; }
    public bool IsBetterSatisfaction { get; init; }
}

public record RankingsDto
{
    public int OverallRank { get; init; }
    public int TotalDealers { get; init; }
    public int DaysOnMarketPercentile { get; init; }
    public int ConversionRatePercentile { get; init; }
    public int ResponseTimePercentile { get; init; }
    public int SatisfactionPercentile { get; init; }
    public int EngagementPercentile { get; init; }
}

/// <summary>
/// DTO para alertas de dealer
/// </summary>
public record DealerAlertDto
{
    public Guid Id { get; init; }
    public Guid DealerId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string TypeLabel { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string SeverityColor { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
    public string? ActionLabel { get; init; }

    public double? CurrentValue { get; init; }
    public double? ThresholdValue { get; init; }

    public bool IsRead { get; init; }
    public bool IsDismissed { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string TimeAgo { get; init; } = string.Empty;
}

/// <summary>
/// DTO para resumen de alertas
/// </summary>
public record AlertsSummaryDto
{
    public int TotalActive { get; init; }
    public int TotalUnread { get; init; }
    public int CriticalCount { get; init; }
    public int HighCount { get; init; }
    public int MediumCount { get; init; }
    public int LowCount { get; init; }
    public List<DealerAlertDto> Alerts { get; init; } = new();
}

/// <summary>
/// DTO para análisis de antigüedad del inventario
/// </summary>
public record InventoryAgingDto
{
    public Guid DealerId { get; init; }
    public DateTime Date { get; init; }

    // Age Buckets
    public List<AgingBucketDto> Buckets { get; init; } = new();

    // Summary
    public int TotalVehicles { get; init; }
    public decimal TotalValue { get; init; }
    public double AverageDaysOnMarket { get; init; }
    public double MedianDaysOnMarket { get; init; }

    // Risk Analysis
    public double PercentFresh { get; init; }
    public double PercentAging { get; init; }
    public int AtRiskCount { get; init; }
    public decimal AtRiskValue { get; init; }
}

public record AgingBucketDto
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Value { get; init; }
    public string Color { get; init; } = "#3B82F6";
    public double Percentage { get; init; }
}

/// <summary>
/// DTO para el overview completo del dashboard
/// </summary>
public record AnalyticsOverviewDto
{
    public Guid DealerId { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }

    // KPIs
    public KpiSummaryDto Kpis { get; init; } = null!;

    // Snapshots
    public DealerSnapshotDto CurrentSnapshot { get; init; } = null!;
    public SnapshotComparisonDto Comparison { get; init; } = null!;

    // Charts Data
    public List<TrendDataPointDto> ViewsTrend { get; init; } = new();
    public List<TrendDataPointDto> ContactsTrend { get; init; } = new();
    public List<TrendDataPointDto> SalesTrend { get; init; } = new();
    public List<TrendDataPointDto> RevenueTrend { get; init; } = new();

    // Top Performers
    public List<VehiclePerformanceDto> TopPerformers { get; init; } = new();

    // Funnel
    public LeadFunnelDto Funnel { get; init; } = null!;

    // Benchmark
    public DealerBenchmarkDto Benchmark { get; init; } = null!;

    // Inventory
    public InventoryAgingDto InventoryAging { get; init; } = null!;

    // Alerts
    public AlertsSummaryDto Alerts { get; init; } = null!;

    public DateTime LastUpdated { get; init; }
}

public record KpiSummaryDto
{
    public int TotalViews { get; init; }
    public double ViewsChange { get; init; }

    public int TotalContacts { get; init; }
    public double ContactsChange { get; init; }

    public int TotalLeads { get; init; }
    public double LeadsChange { get; init; }

    public int TotalSales { get; init; }
    public double SalesChange { get; init; }

    public decimal TotalRevenue { get; init; }
    public double RevenueChange { get; init; }

    public double ConversionRate { get; init; }
    public double ConversionChange { get; init; }

    public double AvgResponseTime { get; init; }
    public double ResponseTimeChange { get; init; }

    public int ActiveListings { get; init; }
    public decimal InventoryValue { get; init; }
}

public record TrendDataPointDto
{
    public DateTime Date { get; init; }
    public double Value { get; init; }
    public string Label { get; init; } = string.Empty;
}

/// <summary>
/// DTO para reportes exportables
/// </summary>
public record AnalyticsReportDto
{
    public Guid DealerId { get; init; }
    public string DealerName { get; init; } = string.Empty;
    public string ReportType { get; init; } = string.Empty; // Daily, Weekly, Monthly
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public DateTime GeneratedAt { get; init; }

    // Summary
    public KpiSummaryDto Summary { get; init; } = null!;

    // Details
    public List<VehiclePerformanceDto> VehiclePerformance { get; init; } = new();
    public LeadFunnelDto Funnel { get; init; } = null!;
    public DealerBenchmarkDto Benchmark { get; init; } = null!;
    public InventoryAgingDto InventoryAging { get; init; } = null!;

    // Insights
    public List<string> KeyInsights { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
}
