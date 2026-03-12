namespace DealerAnalyticsService.Domain.Entities;

public class DealerAnalytic
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public DateTime Date { get; set; }

    // Vista métricas
    public int TotalViews { get; set; }
    public int UniqueViews { get; set; }
    public decimal AverageViewDuration { get; set; }

    // Métricas de contacto
    public int TotalContacts { get; set; }
    public int PhoneCalls { get; set; }
    public int WhatsAppMessages { get; set; }
    public int EmailInquiries { get; set; }

    // Métricas de conversión
    public int TestDriveRequests { get; set; }
    public int ActualSales { get; set; }
    public decimal ConversionRate { get; set; }

    // Métricas financieras
    public decimal TotalRevenue { get; set; }
    public decimal AverageVehiclePrice { get; set; }
    public decimal RevenuePerView { get; set; }

    // Métricas de inventario
    public int ActiveListings { get; set; }
    public decimal AverageDaysOnMarket { get; set; }
    public int SoldVehicles { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
