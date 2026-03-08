using DealerAnalyticsService.Application.DTOs;
using DealerAnalyticsService.Application.Features.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DealerAnalyticsService.Api.Controllers;

/// <summary>
/// Controller para reportes de analytics
/// </summary>
[ApiController]
[Route("api/dealer-analytics/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReportsController> _logger;
    
    public ReportsController(IMediator mediator, ILogger<ReportsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    /// <summary>
    /// Obtener reporte diario
    /// </summary>
    [HttpGet("{dealerId:guid}/daily")]
    [ProducesResponseType(typeof(AnalyticsReportDto), 200)]
    public async Task<ActionResult<AnalyticsReportDto>> GetDailyReport(
        Guid dealerId,
        [FromQuery] DateTime? date = null)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var reportDate = date ?? DateTime.UtcNow.Date.AddDays(-1);
            var query = new GetDailyReportQuery(dealerId, reportDate);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily report for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving daily report" });
        }
    }
    
    /// <summary>
    /// Obtener reporte semanal
    /// </summary>
    [HttpGet("{dealerId:guid}/weekly")]
    [ProducesResponseType(typeof(AnalyticsReportDto), 200)]
    public async Task<ActionResult<AnalyticsReportDto>> GetWeeklyReport(
        Guid dealerId,
        [FromQuery] DateTime? weekStartDate = null)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var startDate = weekStartDate ?? GetStartOfWeek(DateTime.UtcNow.AddDays(-7));
            var query = new GetWeeklyReportQuery(dealerId, startDate);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weekly report for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving weekly report" });
        }
    }
    
    /// <summary>
    /// Obtener reporte mensual
    /// </summary>
    [HttpGet("{dealerId:guid}/monthly")]
    [ProducesResponseType(typeof(AnalyticsReportDto), 200)]
    public async Task<ActionResult<AnalyticsReportDto>> GetMonthlyReport(
        Guid dealerId,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var now = DateTime.UtcNow;
            var reportYear = year ?? now.Year;
            var reportMonth = month ?? (now.Month == 1 ? 12 : now.Month - 1);
            
            if (month == null && year == null && now.Month == 1)
                reportYear--;
            
            var query = new GetMonthlyReportQuery(dealerId, reportYear, reportMonth);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monthly report for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving monthly report" });
        }
    }
    
    /// <summary>
    /// Generar reporte personalizado
    /// </summary>
    [HttpPost("{dealerId:guid}/custom")]
    [ProducesResponseType(typeof(AnalyticsReportDto), 200)]
    public async Task<ActionResult<AnalyticsReportDto>> GetCustomReport(
        Guid dealerId,
        [FromBody] CustomReportRequest request)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var query = new GetCustomReportQuery(
                dealerId, 
                request.FromDate, 
                request.ToDate, 
                request.IncludeSections);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting custom report for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error retrieving custom report" });
        }
    }
    
    /// <summary>
    /// Exportar reporte en formato específico (PDF, Excel, CSV)
    /// </summary>
    [HttpGet("{dealerId:guid}/export/{format}")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> ExportReport(
        Guid dealerId,
        string format,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (!IsAuthorizedForDealer(dealerId))
            return Forbid();

        try
        {
            var validFormats = new[] { "pdf", "excel", "csv" };
            if (!validFormats.Contains(format.ToLower()))
            {
                return BadRequest(new { Message = "Invalid format. Use: pdf, excel, or csv" });
            }
            
            var end = toDate ?? DateTime.UtcNow;
            var start = fromDate ?? end.AddDays(-30);
            
            var query = new ExportReportQuery(dealerId, start, end, format.ToLower());
            var result = await _mediator.Send(query);
            
            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report for dealer {DealerId}", dealerId);
            return StatusCode(500, new { Message = "Error exporting report" });
        }
    }
    
    private static DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
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

public class CustomReportRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<string>? IncludeSections { get; set; }
}
