namespace AdminService.Application.UseCases.Dashboard;

/// <summary>
/// Dashboard statistics response matching frontend DashboardStats interface
/// </summary>
public class DashboardStatsResponse
{
    public int TotalUsers { get; set; }
    public int TotalVehicles { get; set; }
    public int ActiveVehicles { get; set; }
    public int TotalDealers { get; set; }
    public int ActiveDealers { get; set; }
    public int PendingApprovals { get; set; }
    public int PendingVerifications { get; set; }
    public int TotalReports { get; set; }
    public int OpenSupportTickets { get; set; }
    public decimal Mrr { get; set; }
    /// <summary>Percentage change in MRR vs previous snapshot (week-over-week)</summary>
    public decimal MrrChange { get; set; }
    /// <summary>Percentage change in total users vs previous snapshot</summary>
    public decimal UsersChange { get; set; }
    /// <summary>Percentage change in total vehicles vs previous snapshot</summary>
    public decimal VehiclesChange { get; set; }
    /// <summary>Percentage change in total dealers vs previous snapshot</summary>
    public decimal DealersChange { get; set; }
}

/// <summary>
/// Internal snapshot used for computing period-over-period deltas.
/// Stored in IMemoryCache with date-based keys.
/// KPI AUDIT: Replaces hardcoded 0% changes.
/// </summary>
public class DashboardSnapshot
{
    public decimal Mrr { get; set; }
    public int TotalUsers { get; set; }
    public int TotalVehicles { get; set; }
    public int TotalDealers { get; set; }
    public DateTime CapturedAt { get; set; }
}

/// <summary>
/// Dashboard activity item matching frontend RecentActivity interface
/// </summary>
public class DashboardActivityResponse
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty; // user, dealer, vehicle, payment, report
    public string SubjectId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Dashboard pending action matching frontend PendingAction interface
/// </summary>
public class DashboardPendingActionResponse
{
    public string Type { get; set; } = string.Empty; // moderation, verification, report, support
    public string Title { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Priority { get; set; } = "medium"; // high, medium, low
    public string Href { get; set; } = string.Empty;
}

/// <summary>
/// ChatAgent usage stats for a dealer — top users by AI conversation volume
/// </summary>
public class TopChatAgentDealerDto
{
    public Guid DealerId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public int TotalConversations { get; set; }
    public int ConversationsThisMonth { get; set; }
    public double AvgResponseTimeSec { get; set; }
    public double CacheHitRatePercent { get; set; }
    public DateTime LastActivityAt { get; set; }
}
