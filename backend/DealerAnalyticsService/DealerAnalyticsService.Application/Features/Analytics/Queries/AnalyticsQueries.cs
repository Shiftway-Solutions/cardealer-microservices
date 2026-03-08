using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DealerAnalyticsService.Application.Features.Analytics.Queries;

// ============================================
// Get Dashboard Analytics Query
// ============================================

public record GetDashboardAnalyticsQuery(
    Guid DealerId,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<AnalyticsDashboardDto>;

public class GetDashboardAnalyticsHandler : IRequestHandler<GetDashboardAnalyticsQuery, AnalyticsDashboardDto>
{
    private readonly IAnalyticsRepository _repository;

    public GetDashboardAnalyticsHandler(IAnalyticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<AnalyticsDashboardDto> Handle(GetDashboardAnalyticsQuery request, CancellationToken ct)
    {
        var dailySummaries = await _repository.GetDailySummariesAsync(
            request.DealerId,
            request.StartDate,
            request.EndDate,
            ct
        );

        // Calculate totals
        var totalViews = dailySummaries.Sum(s => s.TotalViews);
        var totalUniqueVisitors = dailySummaries.Sum(s => s.UniqueVisitors);
        var totalContacts = dailySummaries.Sum(s => s.TotalContacts);
        var totalConverted = dailySummaries.Sum(s => s.ConvertedInquiries);
        var avgDuration = dailySummaries.Average(s => s.AverageViewDurationSeconds);
        var totalBounces = dailySummaries.Sum(s => s.BounceCount);
        var totalEngaged = dailySummaries.Sum(s => s.EngagedVisits);

        // Calculate rates
        var contactConversionRate = totalViews > 0 ? (totalContacts / (double)totalViews) * 100 : 0;
        var inquiryConversionRate = totalContacts > 0 ? (totalConverted / (double)totalContacts) * 100 : 0;
        var bounceRate = totalViews > 0 ? (totalBounces / (double)totalViews) * 100 : 0;
        var engagementRate = totalViews > 0 ? (totalEngaged / (double)totalViews) * 100 : 0;

        // Summary
        var summary = new AnalyticsSummaryDto(
            TotalViews: totalViews,
            UniqueVisitors: totalUniqueVisitors,
            AverageViewDuration: avgDuration,
            TotalContacts: totalContacts,
            ContactConversionRate: Math.Round(contactConversionRate, 2),
            InquiryConversionRate: Math.Round(inquiryConversionRate, 2),
            BounceRate: Math.Round(bounceRate, 2),
            EngagementRate: Math.Round(engagementRate, 2),
            ComparedToLastPeriod: 0 // TODO: Calculate comparison
        );

        // Timeseries
        var viewsTrend = dailySummaries
            .OrderBy(s => s.Date)
            .Select(s => new TimeseriesDataPoint(
                s.Date,
                s.TotalViews,
                s.TotalContacts,
                s.UniqueVisitors
            ))
            .ToList();

        // Contact method breakdown
        var totalPhoneClicks = dailySummaries.Sum(s => s.PhoneClicks);
        var totalEmailClicks = dailySummaries.Sum(s => s.EmailClicks);
        var totalWhatsAppClicks = dailySummaries.Sum(s => s.WhatsAppClicks);
        var totalWebsiteClicks = dailySummaries.Sum(s => s.WebsiteClicks);
        var totalSocialMediaClicks = dailySummaries.Sum(s => s.SocialMediaClicks);

        var contactMethodBreakdown = new List<ContactMethodStats>
        {
            new(Domain.Entities.ContactType.Phone, "Teléfono", totalPhoneClicks, 
                totalContacts > 0 ? (totalPhoneClicks / (double)totalContacts) * 100 : 0, 0, 0),
            new(Domain.Entities.ContactType.Email, "Email", totalEmailClicks, 
                totalContacts > 0 ? (totalEmailClicks / (double)totalContacts) * 100 : 0, 0, 0),
            new(Domain.Entities.ContactType.WhatsApp, "WhatsApp", totalWhatsAppClicks, 
                totalContacts > 0 ? (totalWhatsAppClicks / (double)totalContacts) * 100 : 0, 0, 0),
            new(Domain.Entities.ContactType.Website, "Sitio Web", totalWebsiteClicks, 
                totalContacts > 0 ? (totalWebsiteClicks / (double)totalContacts) * 100 : 0, 0, 0),
            new(Domain.Entities.ContactType.SocialMedia, "Redes Sociales", totalSocialMediaClicks, 
                totalContacts > 0 ? (totalSocialMediaClicks / (double)totalContacts) * 100 : 0, 0, 0),
        };

        // Device breakdown
        var totalMobile = dailySummaries.Sum(s => s.MobileViews);
        var totalDesktop = dailySummaries.Sum(s => s.DesktopViews);
        var totalTablet = dailySummaries.Sum(s => s.TabletViews);

        var deviceBreakdown = new List<DeviceStats>
        {
            new("Mobile", totalMobile, totalViews > 0 ? (totalMobile / (double)totalViews) * 100 : 0),
            new("Desktop", totalDesktop, totalViews > 0 ? (totalDesktop / (double)totalViews) * 100 : 0),
            new("Tablet", totalTablet, totalViews > 0 ? (totalTablet / (double)totalViews) * 100 : 0),
        };

        // Top referrers
        var topReferrers = new List<TopReferrer>
        {
            new("Direct", dailySummaries.Sum(s => s.DirectTraffic), 0),
            new("Search Engines", dailySummaries.Sum(s => s.SearchEngineTraffic), 0),
            new("Social Media", dailySummaries.Sum(s => s.SocialMediaTraffic), 0),
        };

        // Live stats
        var liveViewers = await _repository.GetLiveViewersCountAsync(request.DealerId, 5, ct);
        var mostRecentView = await _repository.GetMostRecentViewAsync(request.DealerId, ct);
        
        var today = DateTime.UtcNow.Date;
        var todaySummary = dailySummaries.FirstOrDefault(s => s.Date.Date == today);

        var liveStats = new LiveStatsDto(
            CurrentViewers: liveViewers,
            MostRecentView: mostRecentView != null ? new ProfileViewDto(
                mostRecentView.Id,
                mostRecentView.ViewedAt,
                mostRecentView.DeviceType,
                mostRecentView.City,
                mostRecentView.Country,
                mostRecentView.DurationSeconds,
                mostRecentView.IsBounce()
            ) : null,
            ViewsToday: todaySummary?.TotalViews ?? 0,
            ContactsToday: todaySummary?.TotalContacts ?? 0
        );

        return new AnalyticsDashboardDto(
            summary,
            viewsTrend,
            contactMethodBreakdown,
            deviceBreakdown,
            topReferrers,
            liveStats
        );
    }
}

// ============================================
// Track Profile View Command
// ============================================

public record TrackProfileViewCommand(
    TrackProfileViewRequest Request
) : IRequest<ProfileViewDto>;

public class TrackProfileViewHandler : IRequestHandler<TrackProfileViewCommand, ProfileViewDto>
{
    private readonly IAnalyticsRepository _repository;
    private readonly ILogger<TrackProfileViewHandler> _logger;

    public TrackProfileViewHandler(IAnalyticsRepository repository, ILogger<TrackProfileViewHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ProfileViewDto> Handle(TrackProfileViewCommand request, CancellationToken ct)
    {
        var req = request.Request;

        // Parse device type from user agent
        var deviceType = ParseDeviceType(req.ViewerUserAgent);
        
        var view = new Domain.Entities.ProfileView
        {
            DealerId = req.DealerId,
            ViewerIpAddress = req.ViewerIpAddress,
            ViewerUserAgent = req.ViewerUserAgent,
            ViewerUserId = req.ViewerUserId,
            ReferrerUrl = req.ReferrerUrl,
            ViewedPage = req.ViewedPage,
            DurationSeconds = req.DurationSeconds,
            DeviceType = deviceType,
            Browser = ParseBrowser(req.ViewerUserAgent),
            OperatingSystem = ParseOS(req.ViewerUserAgent)
        };

        var created = await _repository.CreateProfileViewAsync(view, ct);

        // Update daily summary asynchronously (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                var summary = await _repository.GetOrCreateDailySummaryAsync(req.DealerId, DateTime.UtcNow.Date, ct);
                summary.TotalViews++;
                if (deviceType == "mobile") summary.MobileViews++;
                else if (deviceType == "desktop") summary.DesktopViews++;
                else if (deviceType == "tablet") summary.TabletViews++;
                
                if (view.IsBounce()) summary.BounceCount++;
                if (view.IsEngagedVisit()) summary.EngagedVisits++;

                summary.Touch();
                await _repository.UpdateDailySummaryAsync(summary, ct);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the request
                _logger.LogWarning(ex, "Error updating daily summary for dealer {DealerId}", req.DealerId);
            }
        }, ct);

        return new ProfileViewDto(
            created.Id,
            created.ViewedAt,
            created.DeviceType,
            created.City,
            created.Country,
            created.DurationSeconds,
            created.IsBounce()
        );
    }

    private string ParseDeviceType(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "desktop";
        
        var ua = userAgent.ToLower();
        if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone")) return "mobile";
        if (ua.Contains("tablet") || ua.Contains("ipad")) return "tablet";
        return "desktop";
    }

    private string? ParseBrowser(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return null;
        
        var ua = userAgent.ToLower();
        if (ua.Contains("chrome")) return "Chrome";
        if (ua.Contains("firefox")) return "Firefox";
        if (ua.Contains("safari")) return "Safari";
        if (ua.Contains("edge")) return "Edge";
        return "Other";
    }

    private string? ParseOS(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return null;
        
        var ua = userAgent.ToLower();
        if (ua.Contains("windows")) return "Windows";
        if (ua.Contains("mac")) return "macOS";
        if (ua.Contains("linux")) return "Linux";
        if (ua.Contains("android")) return "Android";
        if (ua.Contains("ios") || ua.Contains("iphone") || ua.Contains("ipad")) return "iOS";
        return "Other";
    }
}

// ============================================
// Track Contact Event Command
// ============================================

public record TrackContactEventCommand(
    TrackContactEventRequest Request
) : IRequest<ContactEventDto>;

public class TrackContactEventHandler : IRequestHandler<TrackContactEventCommand, ContactEventDto>
{
    private readonly IAnalyticsRepository _repository;
    private readonly ILogger<TrackContactEventHandler> _logger;

    public TrackContactEventHandler(IAnalyticsRepository repository, ILogger<TrackContactEventHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ContactEventDto> Handle(TrackContactEventCommand request, CancellationToken ct)
    {
        var req = request.Request;

        var contactEvent = new Domain.Entities.ContactEvent
        {
            DealerId = req.DealerId,
            ContactType = req.ContactType,
            ViewerIpAddress = req.ViewerIpAddress,
            ViewerUserId = req.ViewerUserId,
            ContactValue = req.ContactValue,
            VehicleId = req.VehicleId,
            Source = req.Source
        };

        var created = await _repository.CreateContactEventAsync(contactEvent, ct);

        // Update daily summary
        _ = Task.Run(async () =>
        {
            try
            {
                var summary = await _repository.GetOrCreateDailySummaryAsync(req.DealerId, DateTime.UtcNow.Date, ct);
                summary.TotalContacts++;
                
                switch (req.ContactType)
                {
                    case Domain.Entities.ContactType.Phone:
                        summary.PhoneClicks++;
                        break;
                    case Domain.Entities.ContactType.Email:
                        summary.EmailClicks++;
                        break;
                    case Domain.Entities.ContactType.WhatsApp:
                        summary.WhatsAppClicks++;
                        break;
                    case Domain.Entities.ContactType.Website:
                        summary.WebsiteClicks++;
                        break;
                    case Domain.Entities.ContactType.SocialMedia:
                        summary.SocialMediaClicks++;
                        break;
                }

                summary.Touch();
                await _repository.UpdateDailySummaryAsync(summary, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating daily summary for dealer {DealerId}", req.DealerId);
            }
        }, ct);

        return new ContactEventDto(
            created.Id,
            created.ClickedAt,
            created.ContactType,
            created.Source ?? "unknown",
            created.DeviceType,
            created.ConvertedToInquiry,
            created.GetTimeToConversion()
        );
    }
}
