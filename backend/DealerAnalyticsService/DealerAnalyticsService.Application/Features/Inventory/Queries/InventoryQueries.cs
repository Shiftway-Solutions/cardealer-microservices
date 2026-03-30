using DealerAnalyticsService.Application.DTOs;
using MediatR;

namespace DealerAnalyticsService.Application.Features.Inventory.Queries;

/// <summary>
/// Query para obtener estadísticas del inventario
/// </summary>
public record GetInventoryStatsQuery(
    Guid DealerId,
    DateTime? AsOfDate = null
) : IRequest<InventoryStatsDto>;

/// <summary>
/// Query para obtener análisis de antigüedad
/// </summary>
public record GetInventoryAgingQuery(
    Guid DealerId
) : IRequest<InventoryAgingDto>;

/// <summary>
/// Query para obtener rotación de inventario
/// </summary>
public record GetInventoryTurnoverQuery(
    Guid DealerId,
    DateTime FromDate,
    DateTime ToDate
) : IRequest<InventoryTurnoverDto>;

/// <summary>
/// Query para obtener performance por vehículo
/// </summary>
public record GetVehiclePerformanceQuery(
    Guid DealerId,
    int Limit = 10,
    string SortBy = "engagement", // engagement, views, contacts, daysOnMarket
    bool Ascending = false
) : IRequest<List<VehiclePerformanceDto>>;

/// <summary>
/// Query para obtener vehículos de bajo rendimiento
/// </summary>
public record GetLowPerformersQuery(
    Guid DealerId,
    int Limit = 5
) : IRequest<List<VehiclePerformanceDto>>;

// DTOs adicionales para inventario
public record InventoryStatsDto
{
    public Guid DealerId { get; init; }
    public DateTime AsOfDate { get; init; }

    // Counts
    public int TotalVehicles { get; init; }
    public int ActiveVehicles { get; init; }
    public int PendingVehicles { get; init; }
    public int SoldVehicles { get; init; }
    public int DraftVehicles { get; init; }
    public int TotalViews { get; init; }
    public int TotalContacts { get; init; }
    public int NewLeads { get; init; }

    // Values
    public decimal TotalValue { get; init; }
    public decimal AvgPrice { get; init; }
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public decimal TotalRevenue { get; init; }

    // Timing
    public double AvgDaysOnMarket { get; init; }
    public double MedianDaysOnMarket { get; init; }

    // Categories
    public List<CategoryBreakdownDto> ByCategory { get; init; } = new();
    public List<PriceRangeBreakdownDto> ByPriceRange { get; init; } = new();

    // Changes
    public int ListedThisWeek { get; init; }
    public int SoldThisWeek { get; init; }
    public double ListingTrend { get; init; } // % change vs previous period
}

public record CategoryBreakdownDto
{
    public string Category { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Value { get; init; }
    public double Percentage { get; init; }
}

public record PriceRangeBreakdownDto
{
    public string Range { get; init; } = string.Empty;
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public int Count { get; init; }
    public double Percentage { get; init; }
}

public record InventoryTurnoverDto
{
    public Guid DealerId { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }

    // Turnover Metrics
    public double TurnoverRate { get; init; }
    public int VehiclesSold { get; init; }
    public int AvgInventory { get; init; }
    public double AvgDaysToSell { get; init; }

    // By Category
    public List<TurnoverByCategoryDto> ByCategory { get; init; } = new();

    // Trend
    public List<TrendDataPointDto> TurnoverTrend { get; init; } = new();

    // Comparison
    public double MarketAvgTurnover { get; init; }
    public bool IsBetterThanMarket { get; init; }
}

public record TurnoverByCategoryDto
{
    public string Category { get; init; } = string.Empty;
    public double TurnoverRate { get; init; }
    public int Sold { get; init; }
    public double AvgDaysToSell { get; init; }
}
