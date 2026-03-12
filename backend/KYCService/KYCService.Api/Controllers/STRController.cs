using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KYCService.Application.Commands;
using KYCService.Application.Queries;
using KYCService.Application.DTOs;
using KYCService.Domain.Entities;

namespace KYCService.Api.Controllers;

/// <summary>
/// Controlador para Reportes de Transacciones Sospechosas (STR/ROS)
/// Según Ley 155-17 - Reportes a UAF
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOrCompliance")]
public class STRController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<STRController> _logger;

    public STRController(IMediator mediator, ILogger<STRController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Listar reportes de transacciones sospechosas
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<SuspiciousTransactionReportDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] STRStatus? status = null,
        [FromQuery] bool? isOverdue = null)
    {
        // SECURITY: Clamp pagination to prevent data exfiltration
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);
        var query = new GetSTRsQuery
        {
            Page = page,
            PageSize = pageSize,
            Status = status,
            IsOverdue = isOverdue
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Obtener reporte por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SuspiciousTransactionReportDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetSTRByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Obtener reporte por número
    /// </summary>
    [HttpGet("number/{reportNumber}")]
    public async Task<ActionResult<SuspiciousTransactionReportDto>> GetByNumber(string reportNumber)
    {
        var result = await _mediator.Send(new GetSTRByNumberQuery(reportNumber));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Crear reporte de transacción sospechosa
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SuspiciousTransactionReportDto>> Create(
        [FromBody] CreateSuspiciousTransactionReportCommand command)
    {
        var result = await _mediator.Send(command);
        _logger.LogWarning("STR created: {ReportNumber} for user {UserId} - {ActivityType}",
            result.ReportNumber, command.UserId, command.SuspiciousActivityType);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Aprobar reporte para envío a UAF
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<SuspiciousTransactionReportDto>> Approve(
        Guid id,
        [FromBody] ApproveSTRCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await _mediator.Send(command);
        _logger.LogWarning("STR {ReportNumber} approved for UAF submission", result.ReportNumber);
        return Ok(result);
    }

    /// <summary>
    /// Enviar reporte a UAF
    /// </summary>
    [HttpPost("{id:guid}/send-to-uaf")]
    public async Task<ActionResult<SuspiciousTransactionReportDto>> SendToUAF(
        Guid id,
        [FromBody] SendSTRToUAFCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await _mediator.Send(command);
        _logger.LogWarning("STR {ReportNumber} sent to UAF as {UAFNumber}",
            result.ReportNumber, result.UAFReportNumber);
        return Ok(result);
    }

    /// <summary>
    /// Obtener estadísticas de STR
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<STRStatisticsDto>> GetStatistics()
    {
        var result = await _mediator.Send(new GetSTRStatisticsQuery());
        return Ok(result);
    }
}
