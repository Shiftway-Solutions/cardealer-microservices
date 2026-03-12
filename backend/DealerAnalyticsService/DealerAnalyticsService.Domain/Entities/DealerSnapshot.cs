namespace DealerAnalyticsService.Domain.Entities;

/// <summary>
/// Snapshot diario del estado y métricas de un dealer
/// Generado automáticamente por el job de agregación diaria
/// </summary>
public class DealerSnapshot
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public DateTime SnapshotDate { get; set; }

    // Inventory Metrics
    public int TotalVehicles { get; set; }
    public int ActiveVehicles { get; set; }
    public int SoldVehicles { get; set; }
    public int PendingVehicles { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public decimal AvgVehiclePrice { get; set; }
    public double AvgDaysOnMarket { get; set; }
    public int VehiclesOver60Days { get; set; }

    // Engagement Metrics
    public int TotalViews { get; set; }
    public int UniqueViews { get; set; }
    public int TotalContacts { get; set; }
    public int PhoneCalls { get; set; }
    public int WhatsAppMessages { get; set; }
    public int EmailInquiries { get; set; }
    public int TotalFavorites { get; set; }
    public int SearchImpressions { get; set; }
    public int SearchClicks { get; set; }

    // Lead Metrics
    public int NewLeads { get; set; }
    public int QualifiedLeads { get; set; }
    public int HotLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public double LeadConversionRate { get; set; }
    public double AvgResponseTimeMinutes { get; set; }

    // Revenue Metrics
    public decimal TotalRevenue { get; set; }
    public decimal AvgTransactionValue { get; set; }
    public int TransactionCount { get; set; }

    // Calculated Rates
    public double ClickThroughRate => SearchImpressions > 0
        ? (double)SearchClicks / SearchImpressions * 100
        : 0;

    public double ContactRate => TotalViews > 0
        ? (double)TotalContacts / TotalViews * 100
        : 0;

    public double FavoriteRate => TotalViews > 0
        ? (double)TotalFavorites / TotalViews * 100
        : 0;

    public double InventoryTurnoverRate => TotalVehicles > 0
        ? (double)SoldVehicles / TotalVehicles * 100
        : 0;

    public double AgingRate => TotalVehicles > 0
        ? (double)VehiclesOver60Days / TotalVehicles * 100
        : 0;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Constructor
    public DealerSnapshot()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public static DealerSnapshot CreateEmpty(Guid dealerId, DateTime date)
    {
        return new DealerSnapshot
        {
            DealerId = dealerId,
            SnapshotDate = date.Date,
            CreatedAt = DateTime.UtcNow
        };
    }
}
