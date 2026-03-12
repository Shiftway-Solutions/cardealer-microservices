namespace DealerAnalyticsService.Domain.Entities;

/// <summary>
/// Métricas de rendimiento de un vehículo específico
/// Permite identificar qué vehículos tienen mejor performance
/// </summary>
public class VehiclePerformance
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public Guid DealerId { get; set; }
    public DateTime Date { get; set; }

    // Vehicle Info (denormalized for performance)
    public string? VehicleTitle { get; set; }
    public string? VehicleMake { get; set; }
    public string? VehicleModel { get; set; }
    public int? VehicleYear { get; set; }
    public decimal? VehiclePrice { get; set; }
    public string? VehicleThumbnailUrl { get; set; }

    // Daily Metrics
    public int Views { get; set; }
    public int UniqueViews { get; set; }
    public int Contacts { get; set; }
    public int PhoneCalls { get; set; }
    public int WhatsAppClicks { get; set; }
    public int EmailInquiries { get; set; }
    public int Favorites { get; set; }
    public int ShareClicks { get; set; }

    // Search Performance
    public int SearchImpressions { get; set; }
    public int SearchClicks { get; set; }
    public int SearchPosition { get; set; } // Average position in search results

    // Engagement Duration
    public decimal AvgViewDurationSeconds { get; set; }
    public int PhotoGalleryViews { get; set; }
    public int FeatureExpansions { get; set; }

    // Calculated Rates
    public double ClickThroughRate => SearchImpressions > 0
        ? (double)SearchClicks / SearchImpressions * 100
        : 0;

    public double ContactRate => Views > 0
        ? (double)Contacts / Views * 100
        : 0;

    public double FavoriteRate => Views > 0
        ? (double)Favorites / Views * 100
        : 0;

    // Scoring
    public double EngagementScore { get; set; }
    public double PerformanceScore { get; set; }

    // Metadata
    public int DaysOnMarket { get; set; }
    public bool IsSold { get; set; }
    public DateTime? SoldDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Constructor
    public VehiclePerformance()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calcula el score de engagement basado en múltiples factores
    /// </summary>
    public void CalculateEngagementScore()
    {
        var viewWeight = 1.0;
        var contactWeight = 10.0;
        var favoriteWeight = 3.0;
        var shareWeight = 5.0;
        var galleryWeight = 0.5;

        EngagementScore =
            (Views * viewWeight) +
            (Contacts * contactWeight) +
            (Favorites * favoriteWeight) +
            (ShareClicks * shareWeight) +
            (PhotoGalleryViews * galleryWeight);
    }

    /// <summary>
    /// Calcula el performance score combinando engagement y conversión
    /// </summary>
    public void CalculatePerformanceScore()
    {
        CalculateEngagementScore();

        // Performance = Engagement * CTR * Contact Rate
        var ctrMultiplier = ClickThroughRate > 0 ? Math.Log10(ClickThroughRate + 1) : 0;
        var contactMultiplier = ContactRate > 0 ? Math.Log10(ContactRate + 1) : 0;

        PerformanceScore = EngagementScore * (1 + ctrMultiplier) * (1 + contactMultiplier);
    }
}
