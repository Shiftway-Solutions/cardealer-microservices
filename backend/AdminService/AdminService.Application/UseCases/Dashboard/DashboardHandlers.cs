using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using AdminService.Domain.Interfaces;
using AdminService.Application.Interfaces;

namespace AdminService.Application.UseCases.Dashboard;

/// <summary>
/// Handler for getting dashboard statistics — aggregates from VehiclesSaleService,
/// DealerManagementService, and UserService concurrently.
/// KPI AUDIT FIX: Now computes real period-over-period deltas using daily snapshots.
/// </summary>
public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsResponse>
{
    private readonly IStatisticsRepository _statisticsRepository;
    private readonly IModerationRepository _moderationRepository;
    private readonly IDealerService _dealerService;
    private readonly IPlatformUserService _userService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<GetDashboardStatsQueryHandler> _logger;

    // Snapshot cache key pattern — stores daily snapshots for delta computation
    private const string SnapshotKeyPrefix = "dashboard_snapshot_";
    private static readonly TimeSpan SnapshotRetention = TimeSpan.FromDays(8);

    public GetDashboardStatsQueryHandler(
        IStatisticsRepository statisticsRepository,
        IModerationRepository moderationRepository,
        IDealerService dealerService,
        IPlatformUserService userService,
        IMemoryCache memoryCache,
        ILogger<GetDashboardStatsQueryHandler> logger)
    {
        _statisticsRepository = statisticsRepository;
        _moderationRepository = moderationRepository;
        _dealerService = dealerService;
        _userService = userService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<DashboardStatsResponse> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching dashboard statistics");

        // Run all service calls in parallel for performance
        var vehicleStatsTask = _statisticsRepository.GetPlatformStatsAsync();
        var dealerStatsTask = _dealerService.GetDealerStatsAsync(cancellationToken);
        var userStatsTask = _userService.GetUserStatsAsync(cancellationToken);
        var pendingReportsTask = _moderationRepository.GetPendingReportsAsync(100);

        await Task.WhenAll(vehicleStatsTask, dealerStatsTask, userStatsTask, pendingReportsTask);

        var platformStats = await vehicleStatsTask;
        var dealerStats = await dealerStatsTask;
        var userStats = await userStatsTask;
        var pendingReports = await pendingReportsTask;

        // Current snapshot values
        var currentSnapshot = new DashboardSnapshot
        {
            Mrr = dealerStats.TotalMrr,
            TotalUsers = userStats.Total,
            TotalVehicles = platformStats.TotalListings,
            TotalDealers = dealerStats.Total,
            CapturedAt = DateTime.UtcNow.Date
        };

        // Save today's snapshot (idempotent — same key per date)
        var todayKey = SnapshotKeyPrefix + DateTime.UtcNow.ToString("yyyy-MM-dd");
        _memoryCache.Set(todayKey, currentSnapshot, SnapshotRetention);

        // Compute 7-day deltas by looking up last week's snapshot
        var (mrrChange, usersChange, vehiclesChange, dealersChange) = ComputeDeltas(currentSnapshot);

        return new DashboardStatsResponse
        {
            TotalUsers = userStats.Total,
            TotalVehicles = platformStats.TotalListings,
            ActiveVehicles = platformStats.TotalListings - platformStats.PendingListings,
            TotalDealers = dealerStats.Total,
            ActiveDealers = dealerStats.Active,
            PendingApprovals = dealerStats.Pending,
            PendingVerifications = platformStats.PendingListings,
            TotalReports = pendingReports.Count(),
            OpenSupportTickets = platformStats.OpenTickets,
            Mrr = dealerStats.TotalMrr,
            MrrChange = mrrChange,
            UsersChange = usersChange,
            VehiclesChange = vehiclesChange,
            DealersChange = dealersChange
        };
    }

    /// <summary>
    /// Computes percentage deltas by comparing current values against the most recent
    /// cached snapshot from 1–7 days ago. Returns (0,0,0,0) if no prior snapshot exists.
    /// </summary>
    private (decimal mrr, decimal users, decimal vehicles, decimal dealers) ComputeDeltas(DashboardSnapshot current)
    {
        // Try to find the most recent previous snapshot (look back 1–7 days)
        for (var daysBack = 1; daysBack <= 7; daysBack++)
        {
            var dateKey = SnapshotKeyPrefix + DateTime.UtcNow.AddDays(-daysBack).ToString("yyyy-MM-dd");
            if (_memoryCache.TryGetValue<DashboardSnapshot>(dateKey, out var previous) && previous != null)
            {
                _logger.LogDebug("Computing dashboard deltas against snapshot from {DaysBack} day(s) ago", daysBack);
                return (
                    mrr: ComputePercentChange(previous.Mrr, current.Mrr),
                    users: ComputePercentChange(previous.TotalUsers, current.TotalUsers),
                    vehicles: ComputePercentChange(previous.TotalVehicles, current.TotalVehicles),
                    dealers: ComputePercentChange(previous.TotalDealers, current.TotalDealers)
                );
            }
        }

        _logger.LogInformation("No previous dashboard snapshot found — deltas will be 0 until next period");
        return (0, 0, 0, 0);
    }

    private static decimal ComputePercentChange(decimal previous, decimal current)
    {
        if (previous == 0) return current > 0 ? 100m : 0m;
        return Math.Round((current - previous) / previous * 100, 2);
    }
}

/// <summary>
/// Handler for getting recent dashboard activity
/// </summary>
public class GetDashboardActivityQueryHandler : IRequestHandler<GetDashboardActivityQuery, List<DashboardActivityResponse>>
{
    private readonly IAdminActionLogRepository _actionLogRepository;
    private readonly ILogger<GetDashboardActivityQueryHandler> _logger;

    public GetDashboardActivityQueryHandler(
        IAdminActionLogRepository actionLogRepository,
        ILogger<GetDashboardActivityQueryHandler> logger)
    {
        _actionLogRepository = actionLogRepository;
        _logger = logger;
    }

    public async Task<List<DashboardActivityResponse>> Handle(GetDashboardActivityQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching dashboard activity with limit: {Limit}", request.Limit);

        var recentActions = await _actionLogRepository.GetRecentAsync(request.Limit);

        return recentActions.Select(a => new DashboardActivityResponse
        {
            Id = a.Id.ToString(),
            Action = a.Action,
            Subject = a.Description ?? a.Action,
            SubjectType = MapTargetTypeToSubjectType(a.TargetType),
            SubjectId = a.TargetId ?? string.Empty,
            Timestamp = a.Timestamp,
            Metadata = string.IsNullOrEmpty(a.Metadata)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(a.Metadata)
        }).ToList();
    }

    private static string MapTargetTypeToSubjectType(string? targetType)
    {
        return targetType?.ToLowerInvariant() switch
        {
            "user" => "user",
            "dealer" => "dealer",
            "vehicle" or "listing" => "vehicle",
            "payment" or "transaction" => "payment",
            "report" => "report",
            _ => "user"
        };
    }
}

/// <summary>
/// Handler for getting pending actions for dashboard
/// </summary>
public class GetDashboardPendingQueryHandler : IRequestHandler<GetDashboardPendingQuery, List<DashboardPendingActionResponse>>
{
    private readonly IStatisticsRepository _statisticsRepository;
    private readonly IModerationRepository _moderationRepository;
    private readonly IDealerService _dealerService;
    private readonly ILogger<GetDashboardPendingQueryHandler> _logger;

    public GetDashboardPendingQueryHandler(
        IStatisticsRepository statisticsRepository,
        IModerationRepository moderationRepository,
        IDealerService dealerService,
        ILogger<GetDashboardPendingQueryHandler> logger)
    {
        _statisticsRepository = statisticsRepository;
        _moderationRepository = moderationRepository;
        _dealerService = dealerService;
        _logger = logger;
    }

    public async Task<List<DashboardPendingActionResponse>> Handle(GetDashboardPendingQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching dashboard pending actions");

        var vehicleStatsTask = _statisticsRepository.GetPlatformStatsAsync();
        var dealerStatsTask = _dealerService.GetDealerStatsAsync(cancellationToken);
        var pendingReportsTask = _moderationRepository.GetPendingReportsAsync(100);

        await Task.WhenAll(vehicleStatsTask, dealerStatsTask, pendingReportsTask);

        var platformStats = await vehicleStatsTask;
        var dealerStats = await dealerStatsTask;
        var pendingReports = await pendingReportsTask;

        var pendingActions = new List<DashboardPendingActionResponse>();

        // Pending vehicle moderation
        if (platformStats.PendingListings > 0)
        {
            pendingActions.Add(new DashboardPendingActionResponse
            {
                Type = "moderation",
                Title = "Vehículos pendientes de aprobación",
                Count = platformStats.PendingListings,
                Priority = platformStats.PendingListings > 10 ? "high" : "medium",
                Href = "/admin/moderation/vehicles"
            });
        }

        // Pending dealer verification
        if (dealerStats.Pending > 0)
        {
            pendingActions.Add(new DashboardPendingActionResponse
            {
                Type = "verification",
                Title = "Dealers pendientes de verificación",
                Count = dealerStats.Pending,
                Priority = dealerStats.Pending > 5 ? "high" : "medium",
                Href = "/admin/dealers?status=pending"
            });
        }

        // Pending reports
        var reportCount = pendingReports.Count();
        if (reportCount > 0)
        {
            pendingActions.Add(new DashboardPendingActionResponse
            {
                Type = "report",
                Title = "Reportes sin revisar",
                Count = reportCount,
                Priority = reportCount > 10 ? "high" : "medium",
                Href = "/admin/reports"
            });
        }

        // Open support tickets
        if (platformStats.OpenTickets > 0)
        {
            pendingActions.Add(new DashboardPendingActionResponse
            {
                Type = "support",
                Title = "Tickets de soporte abiertos",
                Count = platformStats.OpenTickets,
                Priority = platformStats.OpenTickets > 20 ? "high" : "low",
                Href = "/admin/support"
            });
        }

        return pendingActions;
    }
}
