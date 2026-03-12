namespace DealerAnalyticsService.Domain.Entities;

public class ConversionFunnel
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public DateTime Date { get; set; }

    // Etapa 1: Vistas
    public int TotalViews { get; set; }

    // Etapa 2: Interés (contactos)
    public int TotalContacts { get; set; }
    public decimal ViewToContactRate { get; set; }

    // Etapa 3: Consideración (test drives)
    public int TestDriveRequests { get; set; }
    public decimal ContactToTestDriveRate { get; set; }

    // Etapa 4: Compra
    public int ActualSales { get; set; }
    public decimal TestDriveToSaleRate { get; set; }

    // Métricas generales
    public decimal OverallConversionRate { get; set; }
    public decimal AverageTimeToSale { get; set; } // En días

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
