namespace DealerAnalyticsService.Domain.Entities;

/// <summary>
/// Tracks individual profile views for dealers
/// </summary>
public class ProfileView
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public DateTime ViewedAt { get; set; }
    public string? ViewerIpAddress { get; set; }
    public string? ViewerUserAgent { get; set; }
    public Guid? ViewerUserId { get; set; } // If authenticated
    public string? ReferrerUrl { get; set; }
    public string? ViewedPage { get; set; } // "profile", "vehicles", etc.
    public int DurationSeconds { get; set; } // Time spent on page
    public string? DeviceType { get; set; } // "mobile", "tablet", "desktop"
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }

    public ProfileView()
    {
        Id = Guid.NewGuid();
        ViewedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if view is from same user/IP within 30 minutes (to avoid counting refreshes)
    /// </summary>
    public bool IsDuplicateView(string ipAddress, int withinMinutes = 30)
    {
        if (ViewerIpAddress != ipAddress) return false;

        var timeDiff = DateTime.UtcNow - ViewedAt;
        return timeDiff.TotalMinutes < withinMinutes;
    }

    /// <summary>
    /// Determines if this is a bounce (left within 10 seconds)
    /// </summary>
    public bool IsBounce()
    {
        return DurationSeconds < 10;
    }

    /// <summary>
    /// Checks if this is an engaged visit (> 2 minutes)
    /// </summary>
    public bool IsEngagedVisit()
    {
        return DurationSeconds >= 120;
    }
}
