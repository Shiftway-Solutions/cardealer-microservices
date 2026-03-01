using MediatR;

namespace AdminService.Application.UseCases.Analytics;

public record GetAnalyticsOverviewQuery(string Period = "7d") : IRequest<List<AnalyticsOverviewStat>>;
public record GetWeeklyDataQuery(string Period = "7d") : IRequest<List<WeeklyDataPoint>>;
public record GetTopVehicleSearchesQuery(int Limit = 5) : IRequest<List<TopVehicleSearch>>;
public record GetTrafficSourcesQuery(string Period = "7d") : IRequest<List<TrafficSource>>;
public record GetDeviceBreakdownQuery(string Period = "7d") : IRequest<List<DeviceBreakdown>>;
public record GetConversionRatesQuery(string Period = "7d") : IRequest<ConversionRates>;
public record GetRevenueByChannelQuery(string Period = "7d") : IRequest<List<RevenueByChannel>>;
public record GetPlatformAnalyticsQuery(string Period = "7d") : IRequest<PlatformAnalyticsResponse>;
