using MediatR;
using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Domain.Interfaces;
using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Application.Features.Dashboard.Queries;

public record GetDashboardSummaryQuery(Guid DealerId, DateTime FromDate, DateTime ToDate) : IRequest<DashboardSummaryDto>;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IDealerAnalyticsRepository _analyticsRepository;
    private readonly IConversionFunnelRepository _funnelRepository;
    private readonly IMarketBenchmarkRepository _benchmarkRepository;
    private readonly IDealerInsightRepository _insightRepository;

    public GetDashboardSummaryQueryHandler(
        IDealerAnalyticsRepository analyticsRepository,
        IConversionFunnelRepository funnelRepository,
        IMarketBenchmarkRepository benchmarkRepository,
        IDealerInsightRepository insightRepository)
    {
        _analyticsRepository = analyticsRepository;
        _funnelRepository = funnelRepository;
        _benchmarkRepository = benchmarkRepository;
        _insightRepository = insightRepository;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        // Obtener analytics actuales
        var analytics = await _analyticsRepository.GetDealerAnalyticsSummaryAsync(
            request.DealerId, request.FromDate, request.ToDate);

        // Obtener funnel de conversión
        var funnel = await _funnelRepository.CalculateFunnelMetricsAsync(
            request.DealerId, request.FromDate, request.ToDate);

        // Obtener benchmarks del mercado
        var benchmarks = await _benchmarkRepository.GetBenchmarksAsync(DateTime.UtcNow);

        // Obtener top insights
        var insights = await _insightRepository.GetDealerInsightsAsync(request.DealerId, onlyUnread: true);
        var topInsights = insights.OrderByDescending(i => i.Priority)
                                 .ThenByDescending(i => i.PotentialImpact)
                                 .Take(5)
                                 .ToList();

        // Calcular comparaciones con período anterior
        var previousFromDate = request.FromDate.AddDays(-(request.ToDate - request.FromDate).Days);
        var previousAnalytics = await _analyticsRepository.GetDealerAnalyticsSummaryAsync(
            request.DealerId, previousFromDate, request.FromDate);

        return new DashboardSummaryDto
        {
            DealerId = request.DealerId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Analytics = MapAnalyticsToDto(analytics),
            ConversionFunnel = MapFunnelToDto(funnel),
            Benchmarks = benchmarks.Select(MapBenchmarkToDto).ToList(),
            TopInsights = topInsights.Select(MapInsightToDto).ToList(),
            ViewsGrowth = CalculateGrowth(analytics.TotalViews, previousAnalytics.TotalViews),
            ContactsGrowth = CalculateGrowth(analytics.TotalContacts, previousAnalytics.TotalContacts),
            SalesGrowth = CalculateGrowth(analytics.ActualSales, previousAnalytics.ActualSales),
            RevenueGrowth = CalculateGrowth(analytics.TotalRevenue, previousAnalytics.TotalRevenue)
        };
    }

    private static decimal CalculateGrowth(decimal current, decimal previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return Math.Round(((current - previous) / previous) * 100, 2);
    }

    private static int CalculateGrowth(int current, int previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return (int)Math.Round(((decimal)(current - previous) / previous) * 100);
    }

    private static DealerAnalyticsDto MapAnalyticsToDto(DealerAnalytic analytics)
    {
        return new DealerAnalyticsDto
        {
            Id = analytics.Id,
            DealerId = analytics.DealerId,
            Date = analytics.Date,
            TotalViews = analytics.TotalViews,
            UniqueViews = analytics.UniqueViews,
            AverageViewDuration = analytics.AverageViewDuration,
            TotalContacts = analytics.TotalContacts,
            PhoneCalls = analytics.PhoneCalls,
            WhatsAppMessages = analytics.WhatsAppMessages,
            EmailInquiries = analytics.EmailInquiries,
            TestDriveRequests = analytics.TestDriveRequests,
            ActualSales = analytics.ActualSales,
            ConversionRate = analytics.ConversionRate,
            TotalRevenue = analytics.TotalRevenue,
            AverageVehiclePrice = analytics.AverageVehiclePrice,
            RevenuePerView = analytics.RevenuePerView,
            ActiveListings = analytics.ActiveListings,
            AverageDaysOnMarket = analytics.AverageDaysOnMarket,
            SoldVehicles = analytics.SoldVehicles,
            CreatedAt = analytics.CreatedAt,
            UpdatedAt = analytics.UpdatedAt
        };
    }

    private static ConversionFunnelDto MapFunnelToDto(ConversionFunnel funnel)
    {
        return new ConversionFunnelDto
        {
            Id = funnel.Id,
            DealerId = funnel.DealerId,
            Date = funnel.Date,
            TotalViews = funnel.TotalViews,
            TotalContacts = funnel.TotalContacts,
            TestDriveRequests = funnel.TestDriveRequests,
            ActualSales = funnel.ActualSales,
            ViewToContactRate = funnel.ViewToContactRate,
            ContactToTestDriveRate = funnel.ContactToTestDriveRate,
            TestDriveToSaleRate = funnel.TestDriveToSaleRate,
            OverallConversionRate = funnel.OverallConversionRate,
            AverageTimeToSale = funnel.AverageTimeToSale
        };
    }

    private static MarketBenchmarkDto MapBenchmarkToDto(MarketBenchmark benchmark)
    {
        return new MarketBenchmarkDto
        {
            Id = benchmark.Id,
            Date = benchmark.Date,
            VehicleCategory = benchmark.VehicleCategory,
            PriceRange = benchmark.PriceRange,
            MarketAveragePrice = benchmark.MarketAveragePrice,
            MarketAverageDaysOnMarket = benchmark.MarketAverageDaysOnMarket,
            MarketAverageViews = benchmark.MarketAverageViews,
            MarketConversionRate = benchmark.MarketConversionRate,
            PricePercentile25 = benchmark.PricePercentile25,
            PricePercentile50 = benchmark.PricePercentile50,
            PricePercentile75 = benchmark.PricePercentile75,
            TotalDealersInSample = benchmark.TotalDealersInSample,
            TotalVehiclesInSample = benchmark.TotalVehiclesInSample
        };
    }

    private static DealerInsightDto MapInsightToDto(DealerInsight insight)
    {
        return new DealerInsightDto
        {
            Id = insight.Id,
            DealerId = insight.DealerId,
            Type = insight.Type.ToString(),
            Priority = insight.Priority.ToString(),
            Title = insight.Title,
            Description = insight.Description,
            ActionRecommendation = insight.ActionRecommendation,
            PotentialImpact = insight.PotentialImpact,
            Confidence = insight.Confidence,
            IsRead = insight.IsRead,
            IsActedUpon = insight.IsActedUpon,
            ActionDate = insight.ActionDate,
            CreatedAt = insight.CreatedAt,
            UpdatedAt = insight.UpdatedAt,
            ExpiresAt = insight.ExpiresAt
        };
    }
}
