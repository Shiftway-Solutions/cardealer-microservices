using MediatR;

namespace AdminService.Application.UseCases.Dashboard;

/// <summary>
/// Query to get dashboard statistics
/// </summary>
public record GetDashboardStatsQuery() : IRequest<DashboardStatsResponse>;

/// <summary>
/// Query to get recent dashboard activity
/// </summary>
public record GetDashboardActivityQuery(int Limit = 10) : IRequest<List<DashboardActivityResponse>>;

/// <summary>
/// Query to get pending actions for dashboard
/// </summary>
public record GetDashboardPendingQuery() : IRequest<List<DashboardPendingActionResponse>>;

/// <summary>
/// Query to get top dealers by ChatAgent conversation volume
/// </summary>
public record GetTopChatAgentDealersQuery(int Limit = 10) : IRequest<List<TopChatAgentDealerDto>>;
