using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DealerAnalyticsService.Application.Features.Inventory.Queries;

public class GetInventoryStatsQueryHandler : IRequestHandler<GetInventoryStatsQuery, InventoryStatsDto>
{
    private readonly IDealerSnapshotRepository _snapshotRepository;
    private readonly IInventoryAgingRepository _agingRepository;
    private readonly IVehiclePerformanceRepository _vehiclePerformanceRepository;
    private readonly ILogger<GetInventoryStatsQueryHandler> _logger;

    public GetInventoryStatsQueryHandler(
        IDealerSnapshotRepository snapshotRepository,
        IInventoryAgingRepository agingRepository,
        IVehiclePerformanceRepository vehiclePerformanceRepository,
        ILogger<GetInventoryStatsQueryHandler> logger)
    {
        _snapshotRepository = snapshotRepository;
        _agingRepository = agingRepository;
        _vehiclePerformanceRepository = vehiclePerformanceRepository;
        _logger = logger;
    }

    public async Task<InventoryStatsDto> Handle(GetInventoryStatsQuery request, CancellationToken ct)
    {
        var asOfDate = (request.AsOfDate ?? DateTime.UtcNow).Date;
        var fromDate = asOfDate.AddDays(-30);
        var previousFromDate = fromDate.AddDays(-30);
        var previousToDate = fromDate.AddDays(-1);

        _logger.LogInformation(
            "Getting inventory stats for dealer {DealerId} as of {AsOfDate}",
            request.DealerId,
            asOfDate);

        var currentSnapshot = await _snapshotRepository.GetLatestAsync(request.DealerId, ct)
            ?? Domain.Entities.DealerSnapshot.CreateEmpty(request.DealerId, asOfDate);
        var periodSnapshot = await _snapshotRepository.AggregateAsync(request.DealerId, fromDate, asOfDate, ct);
        var previousSnapshot = await _snapshotRepository.AggregateAsync(request.DealerId, previousFromDate, previousToDate, ct);
        var aging = await _agingRepository.GetLatestAsync(request.DealerId, ct);
        var vehicleHistory = await _vehiclePerformanceRepository.GetByDealerAsync(request.DealerId, fromDate, asOfDate, ct);

        var latestVehicles = InventoryAnalyticsMapper.GetLatestVehicleEntries(vehicleHistory);
        var currentInventoryVehicles = latestVehicles.Where(v => !v.IsSold).ToList();
        var pricedVehicles = currentInventoryVehicles.Where(v => v.VehiclePrice.HasValue).ToList();

        var totalVehicles = currentSnapshot.TotalVehicles > 0 ? currentSnapshot.TotalVehicles : latestVehicles.Count;
        var activeVehicles = currentSnapshot.ActiveVehicles > 0
            ? currentSnapshot.ActiveVehicles
            : currentInventoryVehicles.Count;
        var totalValue = currentSnapshot.TotalInventoryValue > 0
            ? currentSnapshot.TotalInventoryValue
            : pricedVehicles.Sum(v => v.VehiclePrice ?? 0m);
        var avgPrice = currentSnapshot.AvgVehiclePrice > 0
            ? currentSnapshot.AvgVehiclePrice
            : InventoryAnalyticsMapper.CalculateAveragePrice(pricedVehicles);
        var minPrice = pricedVehicles.Count > 0 ? pricedVehicles.Min(v => v.VehiclePrice ?? 0m) : 0m;
        var maxPrice = pricedVehicles.Count > 0 ? pricedVehicles.Max(v => v.VehiclePrice ?? 0m) : 0m;
        var avgDaysOnMarket = aging?.AverageDaysOnMarket > 0
            ? aging.AverageDaysOnMarket
            : (currentSnapshot.AvgDaysOnMarket > 0 ? currentSnapshot.AvgDaysOnMarket : InventoryAnalyticsMapper.CalculateAverageDaysOnMarket(currentInventoryVehicles));
        var medianDaysOnMarket = aging?.MedianDaysOnMarket > 0
            ? aging.MedianDaysOnMarket
            : InventoryAnalyticsMapper.CalculateMedianDaysOnMarket(currentInventoryVehicles);

        return new InventoryStatsDto
        {
            DealerId = request.DealerId,
            AsOfDate = asOfDate,
            TotalVehicles = totalVehicles,
            ActiveVehicles = activeVehicles,
            PendingVehicles = currentSnapshot.PendingVehicles,
            SoldVehicles = periodSnapshot.SoldVehicles,
            DraftVehicles = 0,
            TotalViews = periodSnapshot.TotalViews,
            TotalContacts = periodSnapshot.TotalContacts,
            NewLeads = periodSnapshot.NewLeads,
            TotalValue = totalValue,
            AvgPrice = avgPrice,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            TotalRevenue = periodSnapshot.TotalRevenue,
            AvgDaysOnMarket = avgDaysOnMarket,
            MedianDaysOnMarket = medianDaysOnMarket,
            ByCategory = InventoryAnalyticsMapper.BuildCategoryBreakdown(currentInventoryVehicles, activeVehicles),
            ByPriceRange = InventoryAnalyticsMapper.BuildPriceRangeBreakdown(currentInventoryVehicles, activeVehicles),
            ListedThisWeek = currentInventoryVehicles.Count(v => v.DaysOnMarket <= 7),
            SoldThisWeek = latestVehicles.Count(v => v.IsSold && v.SoldDate.HasValue && v.SoldDate.Value.Date >= asOfDate.AddDays(-6) && v.SoldDate.Value.Date <= asOfDate),
            ListingTrend = InventoryAnalyticsMapper.CalculateChange(previousSnapshot.TotalVehicles, totalVehicles)
        };
    }
}

public class GetInventoryAgingQueryHandler : IRequestHandler<GetInventoryAgingQuery, InventoryAgingDto>
{
    private readonly IInventoryAgingRepository _agingRepository;
    private readonly ILogger<GetInventoryAgingQueryHandler> _logger;

    public GetInventoryAgingQueryHandler(
        IInventoryAgingRepository agingRepository,
        ILogger<GetInventoryAgingQueryHandler> logger)
    {
        _agingRepository = agingRepository;
        _logger = logger;
    }

    public async Task<InventoryAgingDto> Handle(GetInventoryAgingQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Getting inventory aging for dealer {DealerId}", request.DealerId);

        var aging = await _agingRepository.GetLatestAsync(request.DealerId, ct);
        return aging != null
            ? InventoryAnalyticsMapper.MapAging(aging)
            : InventoryAnalyticsMapper.CreateEmptyAging(request.DealerId);
    }
}

public class GetInventoryTurnoverQueryHandler : IRequestHandler<GetInventoryTurnoverQuery, InventoryTurnoverDto>
{
    private readonly IDealerSnapshotRepository _snapshotRepository;
    private readonly IVehiclePerformanceRepository _vehiclePerformanceRepository;
    private readonly ILogger<GetInventoryTurnoverQueryHandler> _logger;

    public GetInventoryTurnoverQueryHandler(
        IDealerSnapshotRepository snapshotRepository,
        IVehiclePerformanceRepository vehiclePerformanceRepository,
        ILogger<GetInventoryTurnoverQueryHandler> logger)
    {
        _snapshotRepository = snapshotRepository;
        _vehiclePerformanceRepository = vehiclePerformanceRepository;
        _logger = logger;
    }

    public async Task<InventoryTurnoverDto> Handle(GetInventoryTurnoverQuery request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Getting inventory turnover for dealer {DealerId} from {FromDate} to {ToDate}",
            request.DealerId,
            request.FromDate,
            request.ToDate);

        var snapshots = (await _snapshotRepository.GetRangeAsync(request.DealerId, request.FromDate, request.ToDate, ct))
            .OrderBy(s => s.SnapshotDate)
            .ToList();
        var vehicleHistory = await _vehiclePerformanceRepository.GetByDealerAsync(request.DealerId, request.FromDate, request.ToDate, ct);
        var latestVehicles = InventoryAnalyticsMapper.GetLatestVehicleEntries(vehicleHistory);
        var soldVehicles = latestVehicles
            .Where(v => v.IsSold && v.SoldDate.HasValue && v.SoldDate.Value >= request.FromDate && v.SoldDate.Value <= request.ToDate)
            .ToList();

        var avgInventory = snapshots.Count > 0
            ? (int)Math.Round(snapshots.Average(s => s.TotalVehicles))
            : latestVehicles.Count;
        var turnoverRate = avgInventory > 0 ? soldVehicles.Count / (double)avgInventory * 100 : 0;
        var avgDaysToSell = soldVehicles.Count > 0 ? soldVehicles.Average(v => v.DaysOnMarket) : 0;

        return new InventoryTurnoverDto
        {
            DealerId = request.DealerId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            TurnoverRate = turnoverRate,
            VehiclesSold = soldVehicles.Count,
            AvgInventory = avgInventory,
            AvgDaysToSell = avgDaysToSell,
            ByCategory = InventoryAnalyticsMapper.BuildTurnoverByCategory(latestVehicles, soldVehicles),
            TurnoverTrend = snapshots.Select(s => new TrendDataPointDto
            {
                Date = s.SnapshotDate,
                Value = s.InventoryTurnoverRate,
                Label = s.SnapshotDate.ToString("dd/MM")
            }).ToList(),
            MarketAvgTurnover = 0,
            IsBetterThanMarket = false
        };
    }
}

public class GetVehiclePerformanceQueryHandler : IRequestHandler<GetVehiclePerformanceQuery, List<VehiclePerformanceDto>>
{
    private readonly IVehiclePerformanceRepository _vehiclePerformanceRepository;
    private readonly ILogger<GetVehiclePerformanceQueryHandler> _logger;

    public GetVehiclePerformanceQueryHandler(
        IVehiclePerformanceRepository vehiclePerformanceRepository,
        ILogger<GetVehiclePerformanceQueryHandler> logger)
    {
        _vehiclePerformanceRepository = vehiclePerformanceRepository;
        _logger = logger;
    }

    public async Task<List<VehiclePerformanceDto>> Handle(GetVehiclePerformanceQuery request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Getting vehicle performance for dealer {DealerId} ordered by {SortBy}",
            request.DealerId,
            request.SortBy);

        var toDate = DateTime.UtcNow;
        var fromDate = toDate.AddDays(-30);
        var vehicleHistory = await _vehiclePerformanceRepository.GetByDealerAsync(request.DealerId, fromDate, toDate, ct);
        var latestVehicles = InventoryAnalyticsMapper.GetLatestVehicleEntries(vehicleHistory);
        var mapped = latestVehicles.Select(InventoryAnalyticsMapper.MapVehiclePerformance).ToList();

        var ordered = InventoryAnalyticsMapper.OrderVehiclePerformance(mapped, request.SortBy, request.Ascending);
        var limit = request.Limit <= 0 ? 10 : Math.Min(request.Limit, 100);

        return ordered
            .Take(limit)
            .Select((item, index) => item with { Rank = index + 1 })
            .ToList();
    }
}

public class GetLowPerformersQueryHandler : IRequestHandler<GetLowPerformersQuery, List<VehiclePerformanceDto>>
{
    private readonly IVehiclePerformanceRepository _vehiclePerformanceRepository;
    private readonly ILogger<GetLowPerformersQueryHandler> _logger;

    public GetLowPerformersQueryHandler(
        IVehiclePerformanceRepository vehiclePerformanceRepository,
        ILogger<GetLowPerformersQueryHandler> logger)
    {
        _vehiclePerformanceRepository = vehiclePerformanceRepository;
        _logger = logger;
    }

    public async Task<List<VehiclePerformanceDto>> Handle(GetLowPerformersQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Getting low performers for dealer {DealerId}", request.DealerId);

        var limit = request.Limit <= 0 ? 5 : Math.Min(request.Limit, 50);
        var lowPerformers = await _vehiclePerformanceRepository.GetLowPerformersAsync(request.DealerId, limit, ct);

        return lowPerformers
            .Select(InventoryAnalyticsMapper.MapVehiclePerformance)
            .Take(limit)
            .Select((item, index) => item with { Rank = index + 1 })
            .ToList();
    }
}

internal static class InventoryAnalyticsMapper
{
    private static readonly (string Label, decimal Min, decimal Max)[] PriceRanges =
    {
        ("Menos de RD$500k", 0m, 500000m),
        ("RD$500k - RD$1M", 500000m, 1000000m),
        ("RD$1M - RD$2M", 1000000m, 2000000m),
        ("RD$2M - RD$3M", 2000000m, 3000000m),
        ("Mas de RD$3M", 3000000m, decimal.MaxValue)
    };

    public static List<Domain.Entities.VehiclePerformance> GetLatestVehicleEntries(IEnumerable<Domain.Entities.VehiclePerformance> history)
    {
        return history
            .GroupBy(v => v.VehicleId)
            .Select(group => group.OrderByDescending(v => v.Date).First())
            .ToList();
    }

    public static decimal CalculateAveragePrice(IEnumerable<Domain.Entities.VehiclePerformance> vehicles)
    {
        var pricedVehicles = vehicles.Where(v => v.VehiclePrice.HasValue).ToList();
        return pricedVehicles.Count > 0 ? pricedVehicles.Average(v => v.VehiclePrice ?? 0m) : 0m;
    }

    public static double CalculateAverageDaysOnMarket(IEnumerable<Domain.Entities.VehiclePerformance> vehicles)
    {
        var list = vehicles.ToList();
        return list.Count > 0 ? list.Average(v => v.DaysOnMarket) : 0;
    }

    public static double CalculateMedianDaysOnMarket(IEnumerable<Domain.Entities.VehiclePerformance> vehicles)
    {
        var ordered = vehicles.Select(v => v.DaysOnMarket).OrderBy(days => days).ToList();
        if (ordered.Count == 0)
        {
            return 0;
        }

        var middle = ordered.Count / 2;
        return ordered.Count % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2.0
            : ordered[middle];
    }

    public static List<CategoryBreakdownDto> BuildCategoryBreakdown(
        IEnumerable<Domain.Entities.VehiclePerformance> vehicles,
        int totalVehicles)
    {
        var vehicleList = vehicles.ToList();
        if (vehicleList.Count == 0)
        {
            return new List<CategoryBreakdownDto>();
        }

        var denominator = totalVehicles > 0 ? totalVehicles : vehicleList.Count;

        return vehicleList
            .GroupBy(v => NormalizeCategory(v.VehicleMake))
            .Select(group => new CategoryBreakdownDto
            {
                Category = group.Key,
                Count = group.Count(),
                Value = group.Sum(v => v.VehiclePrice ?? 0m),
                Percentage = denominator > 0 ? group.Count() / (double)denominator * 100 : 0
            })
            .OrderByDescending(item => item.Count)
            .ToList();
    }

    public static List<PriceRangeBreakdownDto> BuildPriceRangeBreakdown(
        IEnumerable<Domain.Entities.VehiclePerformance> vehicles,
        int totalVehicles)
    {
        var pricedVehicles = vehicles.Where(v => v.VehiclePrice.HasValue).ToList();
        if (pricedVehicles.Count == 0)
        {
            return new List<PriceRangeBreakdownDto>();
        }

        var denominator = totalVehicles > 0 ? totalVehicles : pricedVehicles.Count;

        return PriceRanges
            .Select(range =>
            {
                var count = pricedVehicles.Count(v =>
                {
                    var price = v.VehiclePrice ?? 0m;
                    return price >= range.Min && price < range.Max;
                });

                return new PriceRangeBreakdownDto
                {
                    Range = range.Label,
                    MinPrice = range.Min,
                    MaxPrice = range.Max == decimal.MaxValue ? 0m : range.Max,
                    Count = count,
                    Percentage = denominator > 0 ? count / (double)denominator * 100 : 0
                };
            })
            .Where(item => item.Count > 0)
            .ToList();
    }

    public static List<TurnoverByCategoryDto> BuildTurnoverByCategory(
        IEnumerable<Domain.Entities.VehiclePerformance> latestVehicles,
        IEnumerable<Domain.Entities.VehiclePerformance> soldVehicles)
    {
        var latestByCategory = latestVehicles.GroupBy(v => NormalizeCategory(v.VehicleMake)).ToList();
        var soldLookup = soldVehicles
            .GroupBy(v => NormalizeCategory(v.VehicleMake))
            .ToDictionary(group => group.Key, group => group.ToList());

        return latestByCategory
            .Select(group =>
            {
                soldLookup.TryGetValue(group.Key, out var soldGroup);
                soldGroup ??= new List<Domain.Entities.VehiclePerformance>();

                return new TurnoverByCategoryDto
                {
                    Category = group.Key,
                    Sold = soldGroup.Count,
                    TurnoverRate = group.Count() > 0 ? soldGroup.Count / (double)group.Count() * 100 : 0,
                    AvgDaysToSell = soldGroup.Count > 0 ? soldGroup.Average(v => v.DaysOnMarket) : 0
                };
            })
            .OrderByDescending(item => item.TurnoverRate)
            .ToList();
    }

    public static InventoryAgingDto MapAging(Domain.Entities.InventoryAging aging)
    {
        var buckets = aging.GetBuckets();
        var total = aging.TotalVehicles;

        return new InventoryAgingDto
        {
            DealerId = aging.DealerId,
            Date = aging.Date,
            TotalVehicles = total,
            TotalValue = aging.TotalValue,
            AverageDaysOnMarket = aging.AverageDaysOnMarket,
            MedianDaysOnMarket = aging.MedianDaysOnMarket,
            PercentFresh = aging.PercentFresh,
            PercentAging = aging.PercentAging,
            AtRiskCount = aging.AtRiskCount,
            AtRiskValue = aging.AtRiskValue,
            Buckets = buckets.Select(bucket => new AgingBucketDto
            {
                Label = bucket.Label,
                Count = bucket.Count,
                Value = bucket.Value,
                Color = bucket.Color,
                Percentage = total > 0 ? bucket.Count / (double)total * 100 : 0
            }).ToList()
        };
    }

    public static InventoryAgingDto CreateEmptyAging(Guid dealerId)
    {
        return new InventoryAgingDto
        {
            DealerId = dealerId,
            Date = DateTime.UtcNow,
            Buckets = new List<AgingBucketDto>
            {
                new() { Label = "0-15 dias", Count = 0, Color = "#22C55E" },
                new() { Label = "16-30 dias", Count = 0, Color = "#84CC16" },
                new() { Label = "31-45 dias", Count = 0, Color = "#EAB308" },
                new() { Label = "46-60 dias", Count = 0, Color = "#F97316" },
                new() { Label = "61-90 dias", Count = 0, Color = "#EF4444" },
                new() { Label = "+90 dias", Count = 0, Color = "#DC2626" }
            }
        };
    }

    public static VehiclePerformanceDto MapVehiclePerformance(Domain.Entities.VehiclePerformance performance)
    {
        return new VehiclePerformanceDto
        {
            Id = performance.Id,
            VehicleId = performance.VehicleId,
            DealerId = performance.DealerId,
            VehicleTitle = performance.VehicleTitle,
            VehicleMake = performance.VehicleMake,
            VehicleModel = performance.VehicleModel,
            VehicleYear = performance.VehicleYear,
            VehiclePrice = performance.VehiclePrice,
            VehicleThumbnailUrl = performance.VehicleThumbnailUrl,
            Views = performance.Views,
            UniqueViews = performance.UniqueViews,
            Contacts = performance.Contacts,
            Favorites = performance.Favorites,
            SearchImpressions = performance.SearchImpressions,
            SearchClicks = performance.SearchClicks,
            ClickThroughRate = performance.ClickThroughRate,
            ContactRate = performance.ContactRate,
            FavoriteRate = performance.FavoriteRate,
            EngagementScore = performance.EngagementScore,
            PerformanceScore = performance.PerformanceScore,
            DaysOnMarket = performance.DaysOnMarket,
            IsSold = performance.IsSold,
            PerformanceLabel = GetPerformanceLabel(performance.PerformanceScore)
        };
    }

    public static IEnumerable<VehiclePerformanceDto> OrderVehiclePerformance(
        IEnumerable<VehiclePerformanceDto> vehicles,
        string sortBy,
        bool ascending)
    {
        var normalizedSort = (sortBy ?? string.Empty).Trim().ToLowerInvariant();

        return (normalizedSort, ascending) switch
        {
            ("views", true) => vehicles.OrderBy(v => v.Views),
            ("views", false) => vehicles.OrderByDescending(v => v.Views),
            ("contacts", true) => vehicles.OrderBy(v => v.Contacts),
            ("contacts", false) => vehicles.OrderByDescending(v => v.Contacts),
            ("daysonmarket", true) => vehicles.OrderBy(v => v.DaysOnMarket),
            ("days_on_market", true) => vehicles.OrderBy(v => v.DaysOnMarket),
            ("daysOnMarket", true) => vehicles.OrderBy(v => v.DaysOnMarket),
            ("daysonmarket", false) => vehicles.OrderByDescending(v => v.DaysOnMarket),
            ("days_on_market", false) => vehicles.OrderByDescending(v => v.DaysOnMarket),
            ("daysOnMarket", false) => vehicles.OrderByDescending(v => v.DaysOnMarket),
            ("performance", true) => vehicles.OrderBy(v => v.PerformanceScore),
            ("performance", false) => vehicles.OrderByDescending(v => v.PerformanceScore),
            (_, true) => vehicles.OrderBy(v => v.EngagementScore),
            _ => vehicles.OrderByDescending(v => v.EngagementScore)
        };
    }

    public static double CalculateChange(int previous, int current)
    {
        if (previous == 0)
        {
            return current > 0 ? 100 : 0;
        }

        return (current - previous) / (double)previous * 100;
    }

    private static string NormalizeCategory(string? category)
    {
        return string.IsNullOrWhiteSpace(category) ? "Sin categoria" : category.Trim();
    }

    private static string GetPerformanceLabel(double score)
    {
        return score switch
        {
            >= 80 => "Top Performer",
            >= 50 => "Good",
            >= 30 => "Average",
            _ => "Needs Attention"
        };
    }
}