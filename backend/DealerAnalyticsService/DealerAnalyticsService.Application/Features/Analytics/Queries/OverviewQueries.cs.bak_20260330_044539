using DealerAnalyticsService.Application.DTOs;
using MediatR;

namespace DealerAnalyticsService.Application.Features.Analytics.Queries;

/// <summary>
/// Query para obtener el overview completo del dashboard
/// </summary>
public record GetAnalyticsOverviewQuery(
    Guid DealerId,
    DateTime FromDate,
    DateTime ToDate
) : IRequest<AnalyticsOverviewDto>;

/// <summary>
/// Query para obtener KPIs principales
/// </summary>
public record GetKpisQuery(
    Guid DealerId,
    DateTime FromDate,
    DateTime ToDate
) : IRequest<KpiSummaryDto>;

/// <summary>
/// Query para obtener tendencias
/// </summary>
public record GetTrendsQuery(
    Guid DealerId,
    DateTime FromDate,
    DateTime ToDate,
    string MetricType // views, contacts, sales, revenue
) : IRequest<List<TrendDataPointDto>>;

/// <summary>
/// Query para obtener comparación de snapshots
/// </summary>
public record GetSnapshotComparisonQuery(
    Guid DealerId,
    DateTime CurrentDate,
    int CompareDays = 30
) : IRequest<SnapshotComparisonDto>;
