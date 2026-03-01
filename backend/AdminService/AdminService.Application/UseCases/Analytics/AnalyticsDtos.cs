namespace AdminService.Application.UseCases.Analytics;

public record AnalyticsOverviewStat(
    string Label,
    string Value,
    string Change,
    string Trend,
    string Metric
);

public record WeeklyDataPoint(
    string Day,
    int Visits,
    int Signups,
    int Listings
);

public record TopVehicleSearch(
    string Make,
    string Model,
    int Searches,
    int Views,
    int Leads
);

public record TrafficSource(
    string Source,
    int Percentage,
    string Visits
);

public record DeviceBreakdown(
    string Device,
    int Percentage
);

public record ConversionRates(
    double VisitToSignup,
    double SignupToListing,
    double ViewToLead
);

public record RevenueByChannel(
    string Channel,
    int Amount,
    string Color
);

public record PlatformAnalyticsResponse(
    List<AnalyticsOverviewStat> Overview,
    List<WeeklyDataPoint> WeeklyData,
    List<TopVehicleSearch> TopVehicles,
    List<TrafficSource> TrafficSources,
    List<DeviceBreakdown> DeviceBreakdown,
    ConversionRates Conversions,
    List<RevenueByChannel> RevenueByChannel
);
