using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Domain.Enums;
using DealerAnalyticsService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DealerAnalyticsService.Application.Features.Analytics.Queries;

/// <summary>
/// Handler para GetAnalyticsOverviewQuery
/// </summary>
public class GetAnalyticsOverviewQueryHandler : IRequestHandler<GetAnalyticsOverviewQuery, AnalyticsOverviewDto>
{
    private readonly IDealerSnapshotRepository _snapshotRepository;
    private readonly IVehiclePerformanceRepository _vehiclePerformanceRepository;
    private readonly ILeadFunnelRepository _funnelRepository;
    private readonly IDealerBenchmarkRepository _benchmarkRepository;
    private readonly IDealerAlertRepository _alertRepository;
    private readonly IInventoryAgingRepository _agingRepository;
    private readonly ILogger<GetAnalyticsOverviewQueryHandler> _logger;

    public GetAnalyticsOverviewQueryHandler(
        IDealerSnapshotRepository snapshotRepository,
        IVehiclePerformanceRepository vehiclePerformanceRepository,
        ILeadFunnelRepository funnelRepository,
        IDealerBenchmarkRepository benchmarkRepository,
        IDealerAlertRepository alertRepository,
        IInventoryAgingRepository agingRepository,
        ILogger<GetAnalyticsOverviewQueryHandler> logger)
    {
        _snapshotRepository = snapshotRepository;
        _vehiclePerformanceRepository = vehiclePerformanceRepository;
        _funnelRepository = funnelRepository;
        _benchmarkRepository = benchmarkRepository;
        _alertRepository = alertRepository;
        _agingRepository = agingRepository;
        _logger = logger;
    }

    public async Task<AnalyticsOverviewDto> Handle(GetAnalyticsOverviewQuery request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Getting analytics overview for dealer {DealerId} from {From} to {To}",
            request.DealerId, request.FromDate, request.ToDate);

        // Get current and previous snapshots for comparison
        var (currentSnapshot, previousSnapshot) = await _snapshotRepository.GetComparisonAsync(
            request.DealerId,
            request.ToDate,
            (int)(request.ToDate - request.FromDate).TotalDays,
            ct);

        // Get aggregated snapshot for the period
        var periodSnapshot = await _snapshotRepository.AggregateAsync(
            request.DealerId, request.FromDate, request.ToDate, ct);

        // Get top performing vehicles
        var topPerformers = await _vehiclePerformanceRepository.GetTopPerformersAsync(
            request.DealerId, 5, ct);

        // Get funnel metrics
        var funnel = await _funnelRepository.AggregateAsync(
            request.DealerId, request.FromDate, request.ToDate, ct);

        // Get benchmark
        var benchmark = await _benchmarkRepository.GetLatestAsync(request.DealerId, ct);

        // Get inventory aging
        var aging = await _agingRepository.GetLatestAsync(request.DealerId, ct);

        // Get active alerts
        var alerts = await _alertRepository.GetActiveAlertsAsync(request.DealerId, ct);
        var unreadCount = await _alertRepository.GetUnreadCountAsync(request.DealerId, ct);

        // Build KPIs with changes
        var kpis = BuildKpis(periodSnapshot, currentSnapshot, previousSnapshot);

        // Build comparison
        var comparison = BuildComparison(periodSnapshot, currentSnapshot, previousSnapshot);

        return new AnalyticsOverviewDto
        {
            DealerId = request.DealerId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Kpis = kpis,
            CurrentSnapshot = MapSnapshot(periodSnapshot),
            Comparison = comparison,
            TopPerformers = topPerformers.Select(MapVehiclePerformance).ToList(),
            Funnel = MapFunnel(funnel),
            Benchmark = benchmark != null ? MapBenchmark(benchmark) : CreateEmptyBenchmark(request.DealerId),
            InventoryAging = aging != null ? MapAging(aging) : CreateEmptyAging(request.DealerId),
            Alerts = new AlertsSummaryDto
            {
                TotalActive = alerts.Count(),
                TotalUnread = unreadCount,
                Alerts = alerts.Take(5).Select(MapAlert).ToList()
            },
            LastUpdated = DateTime.UtcNow
        };
    }

    private KpiSummaryDto BuildKpis(
        Domain.Entities.DealerSnapshot period,
        Domain.Entities.DealerSnapshot? current,
        Domain.Entities.DealerSnapshot? previous)
    {
        return new KpiSummaryDto
        {
            TotalViews = period?.TotalViews ?? 0,
            ViewsChange = CalculateChange(previous?.TotalViews ?? 0, current?.TotalViews ?? 0),
            TotalContacts = period?.TotalContacts ?? 0,
            ContactsChange = CalculateChange(previous?.TotalContacts ?? 0, current?.TotalContacts ?? 0),
            TotalLeads = period?.QualifiedLeads ?? 0,
            LeadsChange = CalculateChange(previous?.QualifiedLeads ?? 0, current?.QualifiedLeads ?? 0),
            TotalSales = period?.ConvertedLeads ?? 0,
            SalesChange = CalculateChange(previous?.ConvertedLeads ?? 0, current?.ConvertedLeads ?? 0),
            TotalRevenue = period?.TotalRevenue ?? 0,
            RevenueChange = CalculateChange((double)(previous?.TotalRevenue ?? 0), (double)(current?.TotalRevenue ?? 0)),
            ConversionRate = period?.LeadConversionRate ?? 0,
            ConversionChange = CalculateChange(previous?.LeadConversionRate ?? 0, current?.LeadConversionRate ?? 0),
            AvgResponseTime = period?.AvgResponseTimeMinutes ?? 0,
            ResponseTimeChange = CalculateChange(previous?.AvgResponseTimeMinutes ?? 0, current?.AvgResponseTimeMinutes ?? 0),
            ActiveListings = period?.ActiveVehicles ?? 0,
            InventoryValue = period?.TotalInventoryValue ?? 0
        };
    }

    private SnapshotComparisonDto BuildComparison(
        Domain.Entities.DealerSnapshot period,
        Domain.Entities.DealerSnapshot? current,
        Domain.Entities.DealerSnapshot? previous)
    {
        return new SnapshotComparisonDto
        {
            Current = MapSnapshot(current ?? period),
            Previous = previous != null ? MapSnapshot(previous) : null,
            ViewsChange = CalculateChange(previous?.TotalViews ?? 0, current?.TotalViews ?? 0),
            ContactsChange = CalculateChange(previous?.TotalContacts ?? 0, current?.TotalContacts ?? 0),
            LeadsChange = CalculateChange(previous?.QualifiedLeads ?? 0, current?.QualifiedLeads ?? 0),
            SalesChange = CalculateChange(previous?.ConvertedLeads ?? 0, current?.ConvertedLeads ?? 0),
            RevenueChange = CalculateChange((double)(previous?.TotalRevenue ?? 0), (double)(current?.TotalRevenue ?? 0)),
            ConversionRateChange = CalculateChange(previous?.LeadConversionRate ?? 0, current?.LeadConversionRate ?? 0),
            InventoryValueChange = CalculateChange((double)(previous?.TotalInventoryValue ?? 0), (double)(current?.TotalInventoryValue ?? 0))
        };
    }

    private static double CalculateChange(double previous, double current)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return ((current - previous) / previous) * 100;
    }

    private static double CalculateChange(int previous, int current)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return ((double)(current - previous) / previous) * 100;
    }

    private static DealerSnapshotDto MapSnapshot(Domain.Entities.DealerSnapshot snapshot)
    {
        return new DealerSnapshotDto
        {
            Id = snapshot.Id,
            DealerId = snapshot.DealerId,
            SnapshotDate = snapshot.SnapshotDate,
            TotalVehicles = snapshot.TotalVehicles,
            ActiveVehicles = snapshot.ActiveVehicles,
            SoldVehicles = snapshot.SoldVehicles,
            TotalInventoryValue = snapshot.TotalInventoryValue,
            AvgVehiclePrice = snapshot.AvgVehiclePrice,
            AvgDaysOnMarket = snapshot.AvgDaysOnMarket,
            VehiclesOver60Days = snapshot.VehiclesOver60Days,
            TotalViews = snapshot.TotalViews,
            UniqueViews = snapshot.UniqueViews,
            TotalContacts = snapshot.TotalContacts,
            TotalFavorites = snapshot.TotalFavorites,
            SearchImpressions = snapshot.SearchImpressions,
            NewLeads = snapshot.NewLeads,
            QualifiedLeads = snapshot.QualifiedLeads,
            ConvertedLeads = snapshot.ConvertedLeads,
            LeadConversionRate = snapshot.LeadConversionRate,
            TotalRevenue = snapshot.TotalRevenue,
            AvgTransactionValue = snapshot.AvgTransactionValue,
            ClickThroughRate = snapshot.ClickThroughRate,
            ContactRate = snapshot.ContactRate,
            FavoriteRate = snapshot.FavoriteRate,
            InventoryTurnoverRate = snapshot.InventoryTurnoverRate,
            AgingRate = snapshot.AgingRate
        };
    }

    private static VehiclePerformanceDto MapVehiclePerformance(Domain.Entities.VehiclePerformance perf)
    {
        return new VehiclePerformanceDto
        {
            Id = perf.Id,
            VehicleId = perf.VehicleId,
            DealerId = perf.DealerId,
            VehicleTitle = perf.VehicleTitle,
            VehicleMake = perf.VehicleMake,
            VehicleModel = perf.VehicleModel,
            VehicleYear = perf.VehicleYear,
            VehiclePrice = perf.VehiclePrice,
            VehicleThumbnailUrl = perf.VehicleThumbnailUrl,
            Views = perf.Views,
            UniqueViews = perf.UniqueViews,
            Contacts = perf.Contacts,
            Favorites = perf.Favorites,
            SearchImpressions = perf.SearchImpressions,
            SearchClicks = perf.SearchClicks,
            ClickThroughRate = perf.ClickThroughRate,
            ContactRate = perf.ContactRate,
            FavoriteRate = perf.FavoriteRate,
            EngagementScore = perf.EngagementScore,
            PerformanceScore = perf.PerformanceScore,
            DaysOnMarket = perf.DaysOnMarket,
            IsSold = perf.IsSold,
            PerformanceLabel = GetPerformanceLabel(perf.PerformanceScore)
        };
    }

    private static string GetPerformanceLabel(double score)
    {
        return score switch
        {
            >= 80 => "Top Performer",
            >= 50 => "Good",
            >= 30 => "Average",
            _ => "Needs Attention"
        };
    }

    private static LeadFunnelDto MapFunnel(Domain.Entities.LeadFunnelMetrics? funnel)
    {
        if (funnel == null)
        {
            return new LeadFunnelDto
            {
                Stages = new List<FunnelStageDto>
                {
                    new() { Name = "Impresiones", Value = 0, Color = "#93C5FD" },
                    new() { Name = "Vistas", Value = 0, Color = "#60A5FA" },
                    new() { Name = "Contactos", Value = 0, Color = "#3B82F6" },
                    new() { Name = "Calificados", Value = 0, Color = "#2563EB" },
                    new() { Name = "Negociación", Value = 0, Color = "#1D4ED8" },
                    new() { Name = "Ventas", Value = 0, Color = "#1E40AF" }
                }
            };
        }

        return new LeadFunnelDto
        {
            DealerId = funnel.DealerId,
            PeriodStart = funnel.PeriodStart,
            PeriodEnd = funnel.PeriodEnd,
            Impressions = funnel.Impressions,
            Views = funnel.Views,
            Contacts = funnel.Contacts,
            Qualified = funnel.Qualified,
            Negotiation = funnel.Negotiation,
            Converted = funnel.Converted,
            ImpressionsToViews = funnel.ImpressionsToViews,
            ViewsToContacts = funnel.ViewsToContacts,
            ContactsToQualified = funnel.ContactsToQualified,
            QualifiedToConverted = funnel.QualifiedToConverted,
            OverallConversion = funnel.OverallConversion,
            AttributedRevenue = funnel.AttributedRevenue,
            AvgDealValue = funnel.AvgDealValue,
            Stages = new List<FunnelStageDto>
            {
                new() { Name = "Impresiones", Value = funnel.Impressions, Color = "#93C5FD", Percentage = 100 },
                new() { Name = "Vistas", Value = funnel.Views, ConversionRate = funnel.ImpressionsToViews, Color = "#60A5FA" },
                new() { Name = "Contactos", Value = funnel.Contacts, ConversionRate = funnel.ViewsToContacts, Color = "#3B82F6" },
                new() { Name = "Calificados", Value = funnel.Qualified, ConversionRate = funnel.ContactsToQualified, Color = "#2563EB" },
                new() { Name = "Negociación", Value = funnel.Negotiation, ConversionRate = funnel.QualifiedToNegotiation, Color = "#1D4ED8" },
                new() { Name = "Ventas", Value = funnel.Converted, ConversionRate = funnel.NegotiationToConverted, Color = "#1E40AF" }
            }
        };
    }

    private static DealerBenchmarkDto MapBenchmark(Domain.Entities.DealerBenchmark benchmark)
    {
        return new DealerBenchmarkDto
        {
            DealerId = benchmark.DealerId,
            Date = benchmark.Date,
            AvgDaysOnMarket = benchmark.AvgDaysOnMarket,
            ConversionRate = benchmark.ConversionRate,
            AvgResponseTimeMinutes = benchmark.AvgResponseTimeMinutes,
            CustomerSatisfaction = benchmark.CustomerSatisfaction,
            ListingQualityScore = benchmark.ListingQualityScore,
            ActiveListings = benchmark.ActiveListings,
            MonthlySales = benchmark.MonthlySales,
            MarketComparison = new MarketComparisonDto
            {
                MarketAvgDaysOnMarket = benchmark.MarketAvgDaysOnMarket,
                MarketAvgConversionRate = benchmark.MarketAvgConversionRate,
                MarketAvgResponseTime = benchmark.MarketAvgResponseTime,
                MarketAvgSatisfaction = benchmark.MarketAvgSatisfaction,
                IsBetterDaysOnMarket = benchmark.IsBetterThanMarketDaysOnMarket,
                IsBetterConversion = benchmark.IsBetterThanMarketConversion,
                IsBetterResponseTime = benchmark.IsBetterThanMarketResponseTime,
                IsBetterSatisfaction = benchmark.IsBetterThanMarketSatisfaction
            },
            Rankings = new RankingsDto
            {
                OverallRank = benchmark.OverallRank,
                TotalDealers = benchmark.TotalDealers,
                DaysOnMarketPercentile = benchmark.DaysOnMarketPercentile,
                ConversionRatePercentile = benchmark.ConversionRatePercentile,
                ResponseTimePercentile = benchmark.ResponseTimePercentile,
                SatisfactionPercentile = benchmark.SatisfactionPercentile,
                EngagementPercentile = benchmark.EngagementPercentile
            },
            Tier = benchmark.TierName,
            TierColor = GetTierColor(benchmark.Tier),
            TierIcon = GetTierIcon(benchmark.Tier),
            Strengths = benchmark.Strengths,
            ImprovementAreas = benchmark.ImprovementAreas
        };
    }

    private static string GetTierColor(Domain.Entities.DealerTier tier)
    {
        return tier switch
        {
            Domain.Entities.DealerTier.Diamond => "#06B6D4",
            Domain.Entities.DealerTier.Platinum => "#8B5CF6",
            Domain.Entities.DealerTier.Gold => "#F59E0B",
            Domain.Entities.DealerTier.Silver => "#9CA3AF",
            _ => "#92400E"
        };
    }

    private static string GetTierIcon(Domain.Entities.DealerTier tier)
    {
        return tier switch
        {
            Domain.Entities.DealerTier.Diamond => "💎",
            Domain.Entities.DealerTier.Platinum => "🏆",
            Domain.Entities.DealerTier.Gold => "🥇",
            Domain.Entities.DealerTier.Silver => "🥈",
            _ => "🥉"
        };
    }

    private static DealerBenchmarkDto CreateEmptyBenchmark(Guid dealerId)
    {
        return new DealerBenchmarkDto
        {
            DealerId = dealerId,
            Date = DateTime.UtcNow,
            Tier = "Bronze",
            TierColor = "#92400E",
            TierIcon = "🥉",
            MarketComparison = new MarketComparisonDto(),
            Rankings = new RankingsDto { TotalDealers = 1, OverallRank = 1 },
            Strengths = new List<string>(),
            ImprovementAreas = new List<string> { "Complete más ventas para mejorar su ranking" }
        };
    }

    private static InventoryAgingDto MapAging(Domain.Entities.InventoryAging aging)
    {
        var buckets = aging.GetBuckets();
        var total = aging.TotalVehicles;

        return new InventoryAgingDto
        {
            DealerId = aging.DealerId,
            Date = aging.Date,
            TotalVehicles = total,
            TotalValue = aging.TotalValue,
            AverageDaysOnMarket = aging.AverageDaysOnMarket,
            MedianDaysOnMarket = aging.MedianDaysOnMarket,
            PercentFresh = aging.PercentFresh,
            PercentAging = aging.PercentAging,
            AtRiskCount = aging.AtRiskCount,
            AtRiskValue = aging.AtRiskValue,
            Buckets = buckets.Select(b => new AgingBucketDto
            {
                Label = b.Label,
                Count = b.Count,
                Value = b.Value,
                Color = b.Color,
                Percentage = total > 0 ? (double)b.Count / total * 100 : 0
            }).ToList()
        };
    }

    private static InventoryAgingDto CreateEmptyAging(Guid dealerId)
    {
        return new InventoryAgingDto
        {
            DealerId = dealerId,
            Date = DateTime.UtcNow,
            Buckets = new List<AgingBucketDto>
            {
                new() { Label = "0-15 días", Count = 0, Color = "#22C55E" },
                new() { Label = "16-30 días", Count = 0, Color = "#84CC16" },
                new() { Label = "31-45 días", Count = 0, Color = "#EAB308" },
                new() { Label = "46-60 días", Count = 0, Color = "#F97316" },
                new() { Label = "61-90 días", Count = 0, Color = "#EF4444" },
                new() { Label = "+90 días", Count = 0, Color = "#DC2626" }
            }
        };
    }

    private static DealerAlertDto MapAlert(Domain.Entities.DealerAlert alert)
    {
        return new DealerAlertDto
        {
            Id = alert.Id,
            DealerId = alert.DealerId,
            Type = alert.Type.ToString(),
            TypeLabel = GetAlertTypeLabel(alert.Type),
            Severity = alert.Severity.ToString(),
            SeverityColor = GetSeverityColor(alert.Severity),
            Status = alert.Status.ToString(),
            Title = alert.Title,
            Message = alert.Message,
            ActionUrl = alert.ActionUrl,
            ActionLabel = alert.ActionLabel,
            CurrentValue = alert.CurrentValue,
            ThresholdValue = alert.ThresholdValue,
            IsRead = alert.IsRead,
            IsDismissed = alert.IsDismissed,
            CreatedAt = alert.CreatedAt,
            ExpiresAt = alert.ExpiresAt,
            TimeAgo = GetTimeAgo(alert.CreatedAt)
        };
    }

    private static string GetAlertTypeLabel(DealerAlertType type)
    {
        return type switch
        {
            DealerAlertType.LowInventory => "Inventario Bajo",
            DealerAlertType.AgingInventory => "Vehículo Antiguo",
            DealerAlertType.ViewsDropping => "Vistas en Descenso",
            DealerAlertType.LeadResponseSlow => "Respuesta Lenta",
            DealerAlertType.ConversionDropping => "Conversión Baja",
            DealerAlertType.GoalAchieved => "Meta Alcanzada",
            _ => type.ToString()
        };
    }

    private static string GetSeverityColor(AlertSeverity severity)
    {
        return severity switch
        {
            AlertSeverity.Critical => "#DC2626",
            AlertSeverity.High => "#EA580C",
            AlertSeverity.Warning => "#F59E0B",
            AlertSeverity.Medium => "#3B82F6",
            AlertSeverity.Low => "#6B7280",
            _ => "#22C55E"
        };
    }

    private static string GetTimeAgo(DateTime date)
    {
        var diff = DateTime.UtcNow - date;

        if (diff.TotalMinutes < 1) return "Ahora";
        if (diff.TotalMinutes < 60) return $"Hace {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24) return $"Hace {(int)diff.TotalHours} h";
        if (diff.TotalDays < 7) return $"Hace {(int)diff.TotalDays} días";

        return date.ToString("dd/MM");
    }
}

/// <summary>
/// Handler para GetTrendsQuery - obtiene datos de tendencia para una métrica específica
/// </summary>
public class GetTrendsQueryHandler : IRequestHandler<GetTrendsQuery, List<TrendDataPointDto>>
{
    private readonly IDealerSnapshotRepository _snapshotRepository;
    private readonly ILogger<GetTrendsQueryHandler> _logger;

    public GetTrendsQueryHandler(
        IDealerSnapshotRepository snapshotRepository,
        ILogger<GetTrendsQueryHandler> logger)
    {
        _snapshotRepository = snapshotRepository;
        _logger = logger;
    }

    public async Task<List<TrendDataPointDto>> Handle(GetTrendsQuery request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Getting {MetricType} trends for dealer {DealerId} from {From} to {To}",
            request.MetricType, request.DealerId, request.FromDate, request.ToDate);

        try
        {
            // Map metric type to the snapshot field name
            var metricFieldName = request.MetricType.ToLower() switch
            {
                "views" => "TotalViews",
                "contacts" => "TotalContacts",
                "sales" => "ConvertedLeads",
                "revenue" => "TotalRevenue",
                "conversion" => "LeadConversionRate",
                "leads" => "QualifiedLeads",
                _ => "TotalViews"
            };

            var trendData = await _snapshotRepository.GetMetricTrendAsync(
                request.DealerId, metricFieldName, request.FromDate, request.ToDate, ct);

            var result = trendData
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new TrendDataPointDto
                {
                    Date = kvp.Key,
                    Value = kvp.Value,
                    Label = kvp.Key.ToString("dd/MM")
                })
                .ToList();

            // If no data found, return empty data points for the date range
            if (result.Count == 0)
            {
                var days = (int)(request.ToDate - request.FromDate).TotalDays;
                for (var i = 0; i <= days; i++)
                {
                    var date = request.FromDate.AddDays(i);
                    result.Add(new TrendDataPointDto
                    {
                        Date = date,
                        Value = 0,
                        Label = date.ToString("dd/MM")
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trends for dealer {DealerId}", request.DealerId);
            // Return empty trend data instead of throwing
            return new List<TrendDataPointDto>();
        }
    }
}
