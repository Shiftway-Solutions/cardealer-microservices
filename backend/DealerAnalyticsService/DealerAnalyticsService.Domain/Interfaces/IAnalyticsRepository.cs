using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Domain.Interfaces;

public interface IAnalyticsRepository
{
    // Profile Views
    Task<ProfileView> CreateProfileViewAsync(ProfileView view, CancellationToken ct = default);
    Task<List<ProfileView>> GetProfileViewsAsync(Guid dealerId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<int> GetTotalViewsAsync(Guid dealerId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<int> GetUniqueVisitorsAsync(Guid dealerId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<double> GetAverageViewDurationAsync(Guid dealerId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<Dictionary<string, int>> GetViewsByDeviceTypeAsync(Guid dealerId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<Dictionary<DateTime, int>> GetViewsTimeseriesAsync(Guid dealerId, DateTime startDate, DateTime endDate, CancellationToken ct = default);

    // Contact Events
    Task<ContactEvent> CreateContactEventAsync(ContactEvent contactEvent, CancellationToken ct = default);
    Task<List<ContactEvent>> GetContactEventsAsync(Guid dealerId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<int> GetTotalContactsAsync(Guid dealerId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<Dictionary<ContactType, int>> GetContactsByTypeAsync(Guid dealerId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<double> GetContactConversionRateAsync(Guid dealerId, DateTime startDate, DateTime endDate, CancellationToken ct = default);

    // Daily Summaries
    Task<DailyAnalyticsSummary> GetOrCreateDailySummaryAsync(Guid dealerId, DateTime date, CancellationToken ct = default);
    Task<DailyAnalyticsSummary> UpdateDailySummaryAsync(DailyAnalyticsSummary summary, CancellationToken ct = default);
    Task<List<DailyAnalyticsSummary>> GetDailySummariesAsync(Guid dealerId, DateTime startDate, DateTime endDate, CancellationToken ct = default);

    // Top Performers
    Task<List<(Guid DealerId, int Views)>> GetTopDealersByViewsAsync(int count, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<List<(Guid DealerId, double ConversionRate)>> GetTopDealersByConversionAsync(int count, DateTime startDate, DateTime endDate, CancellationToken ct = default);

    // Real-time Stats
    Task<int> GetLiveViewersCountAsync(Guid dealerId, int withinMinutes = 5, CancellationToken ct = default);
    Task<ProfileView?> GetMostRecentViewAsync(Guid dealerId, CancellationToken ct = default);
}
