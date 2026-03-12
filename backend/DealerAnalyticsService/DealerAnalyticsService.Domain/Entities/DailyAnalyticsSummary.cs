namespace DealerAnalyticsService.Domain.Entities;

/// <summary>
/// Daily aggregated analytics for dealers (for performance optimization)
/// </summary>
public class DailyAnalyticsSummary
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public DateTime Date { get; set; } // Date only (00:00:00)

    // Profile Views
    public int TotalViews { get; set; }
    public int UniqueVisitors { get; set; }
    public int MobileViews { get; set; }
    public int DesktopViews { get; set; }
    public int TabletViews { get; set; }
    public int BounceCount { get; set; }
    public int EngagedVisits { get; set; }
    public double AverageViewDurationSeconds { get; set; }

    // Contact Events
    public int TotalContacts { get; set; }
    public int PhoneClicks { get; set; }
    public int EmailClicks { get; set; }
    public int WhatsAppClicks { get; set; }
    public int WebsiteClicks { get; set; }
    public int SocialMediaClicks { get; set; }
    public int ConvertedInquiries { get; set; }

    // Location-specific
    public int TopLocationViews { get; set; }
    public Guid? TopLocationId { get; set; }

    // Referrers
    public string? TopReferrer { get; set; }
    public int DirectTraffic { get; set; }
    public int SearchEngineTraffic { get; set; }
    public int SocialMediaTraffic { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public DailyAnalyticsSummary()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates bounce rate (%)
    /// </summary>
    public double GetBounceRate()
    {
        if (TotalViews == 0) return 0;
        return (BounceCount / (double)TotalViews) * 100;
    }

    /// <summary>
    /// Calculates engagement rate (%)
    /// </summary>
    public double GetEngagementRate()
    {
        if (TotalViews == 0) return 0;
        return (EngagedVisits / (double)TotalViews) * 100;
    }

    /// <summary>
    /// Calculates contact conversion rate (%)
    /// </summary>
    public double GetContactConversionRate()
    {
        if (TotalViews == 0) return 0;
        return (TotalContacts / (double)TotalViews) * 100;
    }

    /// <summary>
    /// Calculates inquiry conversion rate (%)
    /// </summary>
    public double GetInquiryConversionRate()
    {
        if (TotalContacts == 0) return 0;
        return (ConvertedInquiries / (double)TotalContacts) * 100;
    }

    /// <summary>
    /// Gets most popular contact method
    /// </summary>
    public ContactType GetTopContactMethod()
    {
        var max = Math.Max(PhoneClicks, Math.Max(EmailClicks, Math.Max(WhatsAppClicks, WebsiteClicks)));

        if (max == WhatsAppClicks) return ContactType.WhatsApp;
        if (max == PhoneClicks) return ContactType.Phone;
        if (max == EmailClicks) return ContactType.Email;
        if (max == WebsiteClicks) return ContactType.Website;

        return ContactType.Phone; // Default
    }

    /// <summary>
    /// Checks if analytics are from today
    /// </summary>
    public bool IsToday()
    {
        return Date.Date == DateTime.UtcNow.Date;
    }

    /// <summary>
    /// Updates the UpdatedAt timestamp
    /// </summary>
    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
