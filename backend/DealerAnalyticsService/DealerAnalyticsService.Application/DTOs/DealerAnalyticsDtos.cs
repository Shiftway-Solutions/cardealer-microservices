namespace DealerAnalyticsService.Application.DTOs;

public class DealerAnalyticsDto
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

public class ConversionFunnelDto
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public DateTime Date { get; set; }

    public int TotalViews { get; set; }
    public int TotalContacts { get; set; }
    public int TestDriveRequests { get; set; }
    public int ActualSales { get; set; }

    public decimal ViewToContactRate { get; set; }
    public decimal ContactToTestDriveRate { get; set; }
    public decimal TestDriveToSaleRate { get; set; }
    public decimal OverallConversionRate { get; set; }

    public decimal AverageTimeToSale { get; set; }
}

public class MarketBenchmarkDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string VehicleCategory { get; set; } = string.Empty;
    public string PriceRange { get; set; } = string.Empty;

    public decimal MarketAveragePrice { get; set; }
    public decimal MarketAverageDaysOnMarket { get; set; }
    public decimal MarketAverageViews { get; set; }
    public decimal MarketConversionRate { get; set; }

    public decimal PricePercentile25 { get; set; }
    public decimal PricePercentile50 { get; set; }
    public decimal PricePercentile75 { get; set; }

    public int TotalDealersInSample { get; set; }
    public int TotalVehiclesInSample { get; set; }
}

public class DealerInsightDto
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionRecommendation { get; set; } = string.Empty;

    public decimal PotentialImpact { get; set; }
    public decimal Confidence { get; set; }

    public bool IsRead { get; set; }
    public bool IsActedUpon { get; set; }
    public DateTime? ActionDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class DashboardSummaryDto
{
    public Guid DealerId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public DealerAnalyticsDto Analytics { get; set; } = new();
    public ConversionFunnelDto ConversionFunnel { get; set; } = new();
    public List<MarketBenchmarkDto> Benchmarks { get; set; } = new();
    public List<DealerInsightDto> TopInsights { get; set; } = new();

    // Comparaciones con período anterior
    public decimal ViewsGrowth { get; set; }
    public decimal ContactsGrowth { get; set; }
    public decimal SalesGrowth { get; set; }
    public decimal RevenueGrowth { get; set; }
}
