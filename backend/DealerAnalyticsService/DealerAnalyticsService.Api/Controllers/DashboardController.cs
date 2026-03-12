using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DealerAnalyticsService.Application.Features.Dashboard.Queries;
using DealerAnalyticsService.Application.DTOs;
using System.Security.Claims;

namespace DealerAnalyticsService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IMediator mediator, ILogger<DashboardController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Obtener resumen completo del dashboard para un dealer
    /// </summary>
    /// <param name="dealerId">ID del dealer</param>
    /// <param name="fromDate">Fecha de inicio (opcional, default: últimos 30 días)</param>
    /// <param name="toDate">Fecha de fin (opcional, default: hoy)</param>
    /// <returns>Resumen completo del dashboard</returns>
    [HttpGet("{dealerId:guid}/summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary(
        Guid dealerId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var endDate = toDate ?? DateTime.UtcNow;
            var startDate = fromDate ?? endDate.AddDays(-30);

            var query = new GetDashboardSummaryQuery(dealerId, startDate, endDate);
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard summary for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving dashboard data" });
        }
    }

    /// <summary>
    /// Obtener métricas rápidas para el header del dashboard
    /// </summary>
    /// <param name="dealerId">ID del dealer</param>
    /// <returns>Métricas principales</returns>
    [HttpGet("{dealerId:guid}/quick-stats")]
    public async Task<ActionResult<object>> GetQuickStats(Guid dealerId)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var today = DateTime.UtcNow;
            var thirtyDaysAgo = today.AddDays(-30);

            var query = new GetDashboardSummaryQuery(dealerId, thirtyDaysAgo, today);
            var result = await _mediator.Send(query);

            var quickStats = new
            {
                TotalViews = result.Analytics.TotalViews,
                TotalContacts = result.Analytics.TotalContacts,
                ActualSales = result.Analytics.ActualSales,
                TotalRevenue = result.Analytics.TotalRevenue,
                ConversionRate = result.Analytics.ConversionRate,
                ViewsGrowth = result.ViewsGrowth,
                ContactsGrowth = result.ContactsGrowth,
                SalesGrowth = result.SalesGrowth,
                RevenueGrowth = result.RevenueGrowth
            };

            return Ok(quickStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quick stats for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving quick stats" });
        }
    }

    /// <summary>
    /// Obtener comparación de performance del dealer vs mercado
    /// </summary>
    /// <param name="dealerId">ID del dealer</param>
    /// <param name="periodDays">Período en días (opcional, default: 30)</param>
    /// <returns>Comparación de performance</returns>
    [HttpGet("{dealerId:guid}/performance")]
    public async Task<ActionResult<object>> GetPerformanceComparison(Guid dealerId, [FromQuery] int periodDays = 30)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var today = DateTime.UtcNow;
            var startDate = today.AddDays(-periodDays);

            var query = new GetDashboardSummaryQuery(dealerId, startDate, today);
            var result = await _mediator.Send(query);

            // Return performance comparison format expected by frontend
            var performanceComparison = new
            {
                PeriodDays = periodDays,
                Dealer = new
                {
                    ConversionRate = result.Analytics.ConversionRate,
                    AverageResponseTime = 4.2m, // Mock value - would come from ContactEvents analysis
                    CustomerSatisfactionScore = 4.5m // Mock value - would come from reviews/ratings
                },
                Market = new
                {
                    ConversionRate = 5.2m, // Market average from benchmarks
                    AverageResponseTime = 6.8m,
                    CustomerSatisfactionScore = 4.1m
                }
            };

            return Ok(performanceComparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance comparison for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving performance data" });
        }
    }

    /// <summary>
    /// Obtener datos de tendencias diarias para el gráfico
    /// </summary>
    /// <param name="dealerId">ID del dealer</param>
    /// <param name="days">Número de días (default: 30)</param>
    /// <returns>Datos de tendencias diarias</returns>
    [HttpGet("{dealerId:guid}/trends")]
    public async Task<ActionResult<object>> GetDailyTrends(Guid dealerId, [FromQuery] int days = 30)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-days + 1);

            var query = new GetDashboardSummaryQuery(dealerId, startDate, today);
            var result = await _mediator.Send(query);

            // Get daily data from dealer_analytics table directly
            var dailyData = await GetDailyDataAsync(dealerId, startDate, today);

            return Ok(new
            {
                DealerId = dealerId,
                PeriodDays = days,
                FromDate = startDate,
                ToDate = today,
                DailyData = dailyData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily trends for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving trends data" });
        }
    }

    private async Task<List<object>> GetDailyDataAsync(Guid dealerId, DateTime startDate, DateTime endDate)
    {
        // Use the DbContext directly to query dealer_analytics
        var dbContext = HttpContext.RequestServices.GetRequiredService<DealerAnalyticsService.Infrastructure.Persistence.DealerAnalyticsDbContext>();

        var data = await dbContext.DealerAnalytics
            .Where(da => da.DealerId == dealerId && da.Date >= startDate && da.Date <= endDate)
            .OrderBy(da => da.Date)
            .Select(da => new
            {
                Date = da.Date.ToString("yyyy-MM-dd"),
                Views = da.TotalViews,
                UniqueViews = da.UniqueViews,
                Contacts = da.TotalContacts,
                Sales = da.ActualSales,
                Revenue = da.TotalRevenue
            })
            .ToListAsync<object>();

        return data;
    }

    private bool IsAuthorizedForDealer(Guid dealerId)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        if (role is "Admin" or "SuperAdmin")
            return true;

        var claimDealerId = User.FindFirst("dealerId")?.Value;
        return Guid.TryParse(claimDealerId, out var id) && id == dealerId;
    }
}
