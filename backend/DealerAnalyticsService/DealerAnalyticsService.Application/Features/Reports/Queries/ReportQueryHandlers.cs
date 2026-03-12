using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DealerAnalyticsService.Application.Features.Reports.Queries;

/// <summary>
/// Handler para GetDailyReportQuery
/// </summary>
public class GetDailyReportQueryHandler : IRequestHandler<GetDailyReportQuery, AnalyticsReportDto>
{
    private readonly IDealerSnapshotRepository _snapshotRepository;
    private readonly IVehiclePerformanceRepository _vehiclePerformanceRepository;
    private readonly ILeadFunnelRepository _funnelRepository;
    private readonly IDealerBenchmarkRepository _benchmarkRepository;
    private readonly IInventoryAgingRepository _agingRepository;
    private readonly ILogger<GetDailyReportQueryHandler> _logger;

    public GetDailyReportQueryHandler(
        IDealerSnapshotRepository snapshotRepository,
        IVehiclePerformanceRepository vehiclePerformanceRepository,
        ILeadFunnelRepository funnelRepository,
        IDealerBenchmarkRepository benchmarkRepository,
        IInventoryAgingRepository agingRepository,
        ILogger<GetDailyReportQueryHandler> logger)
    {
        _snapshotRepository = snapshotRepository;
        _vehiclePerformanceRepository = vehiclePerformanceRepository;
        _funnelRepository = funnelRepository;
        _benchmarkRepository = benchmarkRepository;
        _agingRepository = agingRepository;
        _logger = logger;
    }

    public async Task<AnalyticsReportDto> Handle(GetDailyReportQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Generating daily report for dealer {DealerId} on {Date}", request.DealerId, request.Date);

        var fromDate = request.Date.Date;
        var toDate = fromDate.AddDays(1).AddSeconds(-1);

        return await BuildReport(request.DealerId, "Daily", fromDate, toDate, ct);
    }

    private async Task<AnalyticsReportDto> BuildReport(Guid dealerId, string reportType, DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        return await ReportBuilder.BuildReportAsync(
            dealerId, reportType, fromDate, toDate,
            _snapshotRepository, _vehiclePerformanceRepository,
            _funnelRepository, _benchmarkRepository, _agingRepository,
            _logger, ct);
    }
}

/// <summary>
/// Handler para GetWeeklyReportQuery
/// </summary>
public class GetWeeklyReportQueryHandler : IRequestHandler<GetWeeklyReportQuery, AnalyticsReportDto>
{
    private readonly IDealerSnapshotRepository _snapshotRepository;
    private readonly IVehiclePerformanceRepository _vehiclePerformanceRepository;
    private readonly ILeadFunnelRepository _funnelRepository;
    private readonly IDealerBenchmarkRepository _benchmarkRepository;
    private readonly IInventoryAgingRepository _agingRepository;
    private readonly ILogger<GetWeeklyReportQueryHandler> _logger;

    public GetWeeklyReportQueryHandler(
        IDealerSnapshotRepository snapshotRepository,
        IVehiclePerformanceRepository vehiclePerformanceRepository,
        ILeadFunnelRepository funnelRepository,
        IDealerBenchmarkRepository benchmarkRepository,
        IInventoryAgingRepository agingRepository,
        ILogger<GetWeeklyReportQueryHandler> logger)
    {
        _snapshotRepository = snapshotRepository;
        _vehiclePerformanceRepository = vehiclePerformanceRepository;
        _funnelRepository = funnelRepository;
        _benchmarkRepository = benchmarkRepository;
        _agingRepository = agingRepository;
        _logger = logger;
    }

    public async Task<AnalyticsReportDto> Handle(GetWeeklyReportQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Generating weekly report for dealer {DealerId} starting {WeekStart}", request.DealerId, request.WeekStartDate);

        var fromDate = request.WeekStartDate.Date;
        var toDate = fromDate.AddDays(7).AddSeconds(-1);

        return await ReportBuilder.BuildReportAsync(
            request.DealerId, "Weekly", fromDate, toDate,
            _snapshotRepository, _vehiclePerformanceRepository,
            _funnelRepository, _benchmarkRepository, _agingRepository,
            _logger, ct);
    }
}

/// <summary>
/// Handler para GetMonthlyReportQuery
/// </summary>
public class GetMonthlyReportQueryHandler : IRequestHandler<GetMonthlyReportQuery, AnalyticsReportDto>
{
    private readonly IDealerSnapshotRepository _snapshotRepository;
    private readonly IVehiclePerformanceRepository _vehiclePerformanceRepository;
    private readonly ILeadFunnelRepository _funnelRepository;
    private readonly IDealerBenchmarkRepository _benchmarkRepository;
    private readonly IInventoryAgingRepository _agingRepository;
    private readonly ILogger<GetMonthlyReportQueryHandler> _logger;

    public GetMonthlyReportQueryHandler(
        IDealerSnapshotRepository snapshotRepository,
        IVehiclePerformanceRepository vehiclePerformanceRepository,
        ILeadFunnelRepository funnelRepository,
        IDealerBenchmarkRepository benchmarkRepository,
        IInventoryAgingRepository agingRepository,
        ILogger<GetMonthlyReportQueryHandler> logger)
    {
        _snapshotRepository = snapshotRepository;
        _vehiclePerformanceRepository = vehiclePerformanceRepository;
        _funnelRepository = funnelRepository;
        _benchmarkRepository = benchmarkRepository;
        _agingRepository = agingRepository;
        _logger = logger;
    }

    public async Task<AnalyticsReportDto> Handle(GetMonthlyReportQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Generating monthly report for dealer {DealerId} for {Year}-{Month}", request.DealerId, request.Year, request.Month);

        var fromDate = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = fromDate.AddMonths(1).AddSeconds(-1);

        return await ReportBuilder.BuildReportAsync(
            request.DealerId, "Monthly", fromDate, toDate,
            _snapshotRepository, _vehiclePerformanceRepository,
            _funnelRepository, _benchmarkRepository, _agingRepository,
            _logger, ct);
    }
}

/// <summary>
/// Handler para GetCustomReportQuery
/// </summary>
public class GetCustomReportQueryHandler : IRequestHandler<GetCustomReportQuery, AnalyticsReportDto>
{
    private readonly IDealerSnapshotRepository _snapshotRepository;
    private readonly IVehiclePerformanceRepository _vehiclePerformanceRepository;
    private readonly ILeadFunnelRepository _funnelRepository;
    private readonly IDealerBenchmarkRepository _benchmarkRepository;
    private readonly IInventoryAgingRepository _agingRepository;
    private readonly ILogger<GetCustomReportQueryHandler> _logger;

    public GetCustomReportQueryHandler(
        IDealerSnapshotRepository snapshotRepository,
        IVehiclePerformanceRepository vehiclePerformanceRepository,
        ILeadFunnelRepository funnelRepository,
        IDealerBenchmarkRepository benchmarkRepository,
        IInventoryAgingRepository agingRepository,
        ILogger<GetCustomReportQueryHandler> logger)
    {
        _snapshotRepository = snapshotRepository;
        _vehiclePerformanceRepository = vehiclePerformanceRepository;
        _funnelRepository = funnelRepository;
        _benchmarkRepository = benchmarkRepository;
        _agingRepository = agingRepository;
        _logger = logger;
    }

    public async Task<AnalyticsReportDto> Handle(GetCustomReportQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Generating custom report for dealer {DealerId} from {From} to {To}", request.DealerId, request.FromDate, request.ToDate);

        return await ReportBuilder.BuildReportAsync(
            request.DealerId, "Custom", request.FromDate, request.ToDate,
            _snapshotRepository, _vehiclePerformanceRepository,
            _funnelRepository, _benchmarkRepository, _agingRepository,
            _logger, ct);
    }
}

/// <summary>
/// Handler para ExportReportQuery
/// </summary>
public class ExportReportQueryHandler : IRequestHandler<ExportReportQuery, ReportExportDto>
{
    private readonly IDealerSnapshotRepository _snapshotRepository;
    private readonly IVehiclePerformanceRepository _vehiclePerformanceRepository;
    private readonly ILeadFunnelRepository _funnelRepository;
    private readonly IDealerBenchmarkRepository _benchmarkRepository;
    private readonly IInventoryAgingRepository _agingRepository;
    private readonly ILogger<ExportReportQueryHandler> _logger;

    public ExportReportQueryHandler(
        IDealerSnapshotRepository snapshotRepository,
        IVehiclePerformanceRepository vehiclePerformanceRepository,
        ILeadFunnelRepository funnelRepository,
        IDealerBenchmarkRepository benchmarkRepository,
        IInventoryAgingRepository agingRepository,
        ILogger<ExportReportQueryHandler> logger)
    {
        _snapshotRepository = snapshotRepository;
        _vehiclePerformanceRepository = vehiclePerformanceRepository;
        _funnelRepository = funnelRepository;
        _benchmarkRepository = benchmarkRepository;
        _agingRepository = agingRepository;
        _logger = logger;
    }

    public async Task<ReportExportDto> Handle(ExportReportQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Exporting report for dealer {DealerId} in {Format} format", request.DealerId, request.Format);

        var report = await ReportBuilder.BuildReportAsync(
            request.DealerId, "Export", request.FromDate, request.ToDate,
            _snapshotRepository, _vehiclePerformanceRepository,
            _funnelRepository, _benchmarkRepository, _agingRepository,
            _logger, ct);

        var (content, contentType, extension) = request.Format.ToLower() switch
        {
            "csv" => (GenerateCsvContent(report), "text/csv", "csv"),
            "excel" => (GenerateCsvContent(report), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "pdf" => (GenerateCsvContent(report), "application/pdf", "pdf"),
            _ => (GenerateCsvContent(report), "text/csv", "csv")
        };

        return new ReportExportDto
        {
            FileName = $"report_{request.DealerId:N}_{request.FromDate:yyyyMMdd}_{request.ToDate:yyyyMMdd}.{extension}",
            ContentType = contentType,
            Content = content,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static byte[] GenerateCsvContent(AnalyticsReportDto report)
    {
        var lines = new List<string>
        {
            "Metric,Value",
            $"Report Type,{report.ReportType}",
            $"From,{report.FromDate:yyyy-MM-dd}",
            $"To,{report.ToDate:yyyy-MM-dd}",
            $"Generated,{report.GeneratedAt:yyyy-MM-dd HH:mm:ss}",
            "",
            "KPI Summary",
            $"Total Views,{report.Summary.TotalViews}",
            $"Views Change %,{report.Summary.ViewsChange:F1}",
            $"Total Contacts,{report.Summary.TotalContacts}",
            $"Contacts Change %,{report.Summary.ContactsChange:F1}",
            $"Total Leads,{report.Summary.TotalLeads}",
            $"Total Sales,{report.Summary.TotalSales}",
            $"Total Revenue,{report.Summary.TotalRevenue:F2}",
            $"Conversion Rate %,{report.Summary.ConversionRate:F1}",
            $"Active Listings,{report.Summary.ActiveListings}",
            $"Inventory Value,{report.Summary.InventoryValue:F2}"
        };

        if (report.VehiclePerformance.Any())
        {
            lines.Add("");
            lines.Add("Vehicle Performance");
            lines.Add("Vehicle,Views,Contacts,Days on Market,Performance Score");
            foreach (var v in report.VehiclePerformance)
            {
                lines.Add($"{v.VehicleTitle ?? "N/A"},{v.Views},{v.Contacts},{v.DaysOnMarket},{v.PerformanceScore:F1}");
            }
        }

        var csv = string.Join("\n", lines);
        return System.Text.Encoding.UTF8.GetBytes(csv);
    }
}

/// <summary>
/// Shared report building logic used by all report handlers
/// </summary>
internal static class ReportBuilder
{
    public static async Task<AnalyticsReportDto> BuildReportAsync(
        Guid dealerId,
        string reportType,
        DateTime fromDate,
        DateTime toDate,
        IDealerSnapshotRepository snapshotRepository,
        IVehiclePerformanceRepository vehiclePerformanceRepository,
        ILeadFunnelRepository funnelRepository,
        IDealerBenchmarkRepository benchmarkRepository,
        IInventoryAgingRepository agingRepository,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            // Get aggregated snapshot for the period
            var periodSnapshot = await snapshotRepository.AggregateAsync(dealerId, fromDate, toDate, ct);

            // Get comparison with previous period
            var periodDays = (int)(toDate - fromDate).TotalDays;
            var previousFrom = fromDate.AddDays(-periodDays);
            var previousTo = fromDate.AddSeconds(-1);
            var previousSnapshot = await snapshotRepository.AggregateAsync(dealerId, previousFrom, previousTo, ct);

            // Get top performing vehicles
            var topVehicles = await vehiclePerformanceRepository.GetTopPerformersAsync(dealerId, 10, ct);

            // Get funnel metrics
            var funnel = await funnelRepository.AggregateAsync(dealerId, fromDate, toDate, ct);

            // Get benchmark
            var benchmark = await benchmarkRepository.GetLatestAsync(dealerId, ct);

            // Get inventory aging
            var aging = await agingRepository.GetLatestAsync(dealerId, ct);

            // Build KPIs
            var kpis = BuildKpis(periodSnapshot, previousSnapshot);

            // Generate insights based on data
            var insights = GenerateInsights(kpis, funnel, aging);
            var recommendations = GenerateRecommendations(kpis, funnel, aging);

            return new AnalyticsReportDto
            {
                DealerId = dealerId,
                DealerName = string.Empty,
                ReportType = reportType,
                FromDate = fromDate,
                ToDate = toDate,
                GeneratedAt = DateTime.UtcNow,
                Summary = kpis,
                VehiclePerformance = topVehicles.Select(MapVehiclePerformance).ToList(),
                Funnel = MapFunnel(funnel, dealerId, fromDate, toDate),
                Benchmark = benchmark != null ? MapBenchmark(benchmark) : CreateEmptyBenchmark(dealerId),
                InventoryAging = aging != null ? MapAging(aging) : CreateEmptyAging(dealerId),
                KeyInsights = insights,
                Recommendations = recommendations
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error building report data for dealer {DealerId}, returning empty report", dealerId);

            // Return a valid empty report instead of throwing
            return CreateEmptyReport(dealerId, reportType, fromDate, toDate);
        }
    }

    private static KpiSummaryDto BuildKpis(
        Domain.Entities.DealerSnapshot? current,
        Domain.Entities.DealerSnapshot? previous)
    {
        return new KpiSummaryDto
        {
            TotalViews = current?.TotalViews ?? 0,
            ViewsChange = CalculateChange(previous?.TotalViews ?? 0, current?.TotalViews ?? 0),
            TotalContacts = current?.TotalContacts ?? 0,
            ContactsChange = CalculateChange(previous?.TotalContacts ?? 0, current?.TotalContacts ?? 0),
            TotalLeads = current?.QualifiedLeads ?? 0,
            LeadsChange = CalculateChange(previous?.QualifiedLeads ?? 0, current?.QualifiedLeads ?? 0),
            TotalSales = current?.ConvertedLeads ?? 0,
            SalesChange = CalculateChange(previous?.ConvertedLeads ?? 0, current?.ConvertedLeads ?? 0),
            TotalRevenue = current?.TotalRevenue ?? 0,
            RevenueChange = CalculateChange((double)(previous?.TotalRevenue ?? 0), (double)(current?.TotalRevenue ?? 0)),
            ConversionRate = current?.LeadConversionRate ?? 0,
            ConversionChange = CalculateChange(previous?.LeadConversionRate ?? 0, current?.LeadConversionRate ?? 0),
            AvgResponseTime = current?.AvgResponseTimeMinutes ?? 0,
            ResponseTimeChange = CalculateChange(previous?.AvgResponseTimeMinutes ?? 0, current?.AvgResponseTimeMinutes ?? 0),
            ActiveListings = current?.ActiveVehicles ?? 0,
            InventoryValue = current?.TotalInventoryValue ?? 0
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

    private static LeadFunnelDto MapFunnel(Domain.Entities.LeadFunnelMetrics? funnel, Guid dealerId, DateTime fromDate, DateTime toDate)
    {
        if (funnel == null)
        {
            return new LeadFunnelDto
            {
                DealerId = dealerId,
                PeriodStart = fromDate,
                PeriodEnd = toDate,
                Stages = new List<FunnelStageDto>
                {
                    new() { Name = "Impresiones", Value = 0, Color = "#93C5FD", Percentage = 100 },
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

    private static AnalyticsReportDto CreateEmptyReport(Guid dealerId, string reportType, DateTime fromDate, DateTime toDate)
    {
        return new AnalyticsReportDto
        {
            DealerId = dealerId,
            DealerName = string.Empty,
            ReportType = reportType,
            FromDate = fromDate,
            ToDate = toDate,
            GeneratedAt = DateTime.UtcNow,
            Summary = new KpiSummaryDto(),
            VehiclePerformance = new List<VehiclePerformanceDto>(),
            Funnel = new LeadFunnelDto
            {
                DealerId = dealerId,
                PeriodStart = fromDate,
                PeriodEnd = toDate,
                Stages = new List<FunnelStageDto>
                {
                    new() { Name = "Impresiones", Value = 0, Color = "#93C5FD", Percentage = 100 },
                    new() { Name = "Vistas", Value = 0, Color = "#60A5FA" },
                    new() { Name = "Contactos", Value = 0, Color = "#3B82F6" },
                    new() { Name = "Calificados", Value = 0, Color = "#2563EB" },
                    new() { Name = "Negociación", Value = 0, Color = "#1D4ED8" },
                    new() { Name = "Ventas", Value = 0, Color = "#1E40AF" }
                }
            },
            Benchmark = CreateEmptyBenchmark(dealerId),
            InventoryAging = CreateEmptyAging(dealerId),
            KeyInsights = new List<string> { "No hay datos suficientes para generar insights en este período." },
            Recommendations = new List<string> { "Publique más vehículos para comenzar a ver métricas detalladas." }
        };
    }

    private static List<string> GenerateInsights(KpiSummaryDto kpis, Domain.Entities.LeadFunnelMetrics? funnel, Domain.Entities.InventoryAging? aging)
    {
        var insights = new List<string>();

        if (kpis.TotalViews > 0)
            insights.Add($"Tu inventario recibió {kpis.TotalViews} vistas en este período.");

        if (kpis.ViewsChange > 10)
            insights.Add($"Las vistas aumentaron un {kpis.ViewsChange:F0}% respecto al período anterior. ¡Buen trabajo!");
        else if (kpis.ViewsChange < -10)
            insights.Add($"Las vistas disminuyeron un {Math.Abs(kpis.ViewsChange):F0}% respecto al período anterior.");

        if (kpis.ConversionRate > 0)
            insights.Add($"Tu tasa de conversión es del {kpis.ConversionRate:F1}%.");

        if (kpis.TotalSales > 0)
            insights.Add($"Se completaron {kpis.TotalSales} ventas con un ingreso total de RD${kpis.TotalRevenue:N0}.");

        if (funnel != null && funnel.OverallConversion > 5)
            insights.Add($"Tu funnel de conversión general es de {funnel.OverallConversion:F1}%, por encima del promedio del mercado.");

        if (aging != null && aging.AtRiskCount > 0)
            insights.Add($"Tienes {aging.AtRiskCount} vehículos en riesgo de envejecimiento (más de 60 días en inventario).");

        if (insights.Count == 0)
            insights.Add("No hay datos suficientes para generar insights en este período.");

        return insights;
    }

    private static List<string> GenerateRecommendations(KpiSummaryDto kpis, Domain.Entities.LeadFunnelMetrics? funnel, Domain.Entities.InventoryAging? aging)
    {
        var recommendations = new List<string>();

        if (kpis.TotalViews == 0)
            recommendations.Add("Publique más vehículos con fotos de alta calidad para aumentar las vistas.");

        if (kpis.ConversionRate < 2 && kpis.TotalContacts > 0)
            recommendations.Add("Tu tasa de conversión es baja. Considera mejorar tus tiempos de respuesta a contactos.");

        if (kpis.AvgResponseTime > 60)
            recommendations.Add($"Tu tiempo promedio de respuesta es de {kpis.AvgResponseTime:F0} minutos. Intenta responder en menos de 30 minutos.");

        if (aging != null && aging.PercentAging > 30)
            recommendations.Add("Más del 30% de tu inventario tiene más de 45 días. Considera ajustar precios en vehículos antiguos.");

        if (funnel != null && funnel.ViewsToContacts < 5)
            recommendations.Add("Mejora las descripciones y fotos de tus vehículos para aumentar la tasa de contacto.");

        if (kpis.ActiveListings < 5)
            recommendations.Add("Aumenta tu inventario activo. Los dealers con más de 10 vehículos reciben 3x más contactos.");

        if (recommendations.Count == 0)
            recommendations.Add("¡Sigue así! Tus métricas están en buen camino.");

        return recommendations;
    }
}
