namespace DealerAnalyticsService.Domain.Entities;

/// <summary>
/// Análisis de antigüedad del inventario
/// Muestra cuánto tiempo llevan los vehículos en el mercado
/// </summary>
public class InventoryAging
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public DateTime Date { get; set; }

    // Age Buckets (días en el mercado)
    public int Vehicles0To15Days { get; set; }
    public int Vehicles16To30Days { get; set; }
    public int Vehicles31To45Days { get; set; }
    public int Vehicles46To60Days { get; set; }
    public int Vehicles61To90Days { get; set; }
    public int VehiclesOver90Days { get; set; }

    // Values by bucket
    public decimal Value0To15Days { get; set; }
    public decimal Value16To30Days { get; set; }
    public decimal Value31To45Days { get; set; }
    public decimal Value46To60Days { get; set; }
    public decimal Value61To90Days { get; set; }
    public decimal ValueOver90Days { get; set; }

    // Totals
    public int TotalVehicles =>
        Vehicles0To15Days +
        Vehicles16To30Days +
        Vehicles31To45Days +
        Vehicles46To60Days +
        Vehicles61To90Days +
        VehiclesOver90Days;

    public decimal TotalValue =>
        Value0To15Days +
        Value16To30Days +
        Value31To45Days +
        Value46To60Days +
        Value61To90Days +
        ValueOver90Days;

    // Percentages
    public double PercentFresh => TotalVehicles > 0
        ? (double)(Vehicles0To15Days + Vehicles16To30Days) / TotalVehicles * 100
        : 0;

    public double PercentAging => TotalVehicles > 0
        ? (double)(Vehicles46To60Days + Vehicles61To90Days + VehiclesOver90Days) / TotalVehicles * 100
        : 0;

    // Average days calculation
    public double AverageDaysOnMarket { get; set; }
    public double MedianDaysOnMarket { get; set; }

    // Risk Metrics
    public decimal AtRiskValue => Value61To90Days + ValueOver90Days;
    public int AtRiskCount => Vehicles61To90Days + VehiclesOver90Days;

    public DateTime CreatedAt { get; set; }

    // Constructor
    public InventoryAging()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtiene los buckets como lista para gráficos
    /// </summary>
    public List<AgingBucket> GetBuckets()
    {
        return new List<AgingBucket>
        {
            new() { Label = "0-15 días", Count = Vehicles0To15Days, Value = Value0To15Days, Color = "#22C55E" },
            new() { Label = "16-30 días", Count = Vehicles16To30Days, Value = Value16To30Days, Color = "#84CC16" },
            new() { Label = "31-45 días", Count = Vehicles31To45Days, Value = Value31To45Days, Color = "#EAB308" },
            new() { Label = "46-60 días", Count = Vehicles46To60Days, Value = Value46To60Days, Color = "#F97316" },
            new() { Label = "61-90 días", Count = Vehicles61To90Days, Value = Value61To90Days, Color = "#EF4444" },
            new() { Label = "+90 días", Count = VehiclesOver90Days, Value = ValueOver90Days, Color = "#DC2626" }
        };
    }
}

public class AgingBucket
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Value { get; set; }
    public string Color { get; set; } = "#3B82F6";
    public double Percentage { get; set; }
}
