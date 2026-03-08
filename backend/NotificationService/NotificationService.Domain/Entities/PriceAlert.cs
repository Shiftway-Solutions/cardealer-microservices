namespace NotificationService.Domain.Entities;

/// <summary>
/// Price alert entity — users set alerts to be notified when a vehicle's price drops.
/// </summary>
public class PriceAlert
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid VehicleId { get; set; }
    public string VehicleTitle { get; set; } = string.Empty;
    public string? VehicleImageUrl { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal TargetPrice { get; set; }
    public decimal? PriceDropPercentage { get; set; }
    public bool IsActive { get; set; } = true;
    public bool NotifyByEmail { get; set; } = true;
    public bool NotifyByPush { get; set; } = true;
    public bool NotifyBySms { get; set; }
    public int TriggeredCount { get; set; }
    public DateTime? LastNotifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public PriceAlert()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public static PriceAlert Create(
        Guid userId,
        Guid vehicleId,
        string vehicleTitle,
        decimal currentPrice,
        decimal targetPrice,
        decimal? priceDropPercentage = null,
        bool notifyByEmail = true,
        bool notifyByPush = true,
        bool notifyBySms = false)
    {
        return new PriceAlert
        {
            UserId = userId,
            VehicleId = vehicleId,
            VehicleTitle = vehicleTitle,
            CurrentPrice = currentPrice,
            TargetPrice = targetPrice,
            PriceDropPercentage = priceDropPercentage,
            NotifyByEmail = notifyByEmail,
            NotifyByPush = notifyByPush,
            NotifyBySms = notifyBySms
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkTriggered()
    {
        TriggeredCount++;
        LastNotifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(decimal newCurrentPrice)
    {
        CurrentPrice = newCurrentPrice;
        UpdatedAt = DateTime.UtcNow;
    }
}
