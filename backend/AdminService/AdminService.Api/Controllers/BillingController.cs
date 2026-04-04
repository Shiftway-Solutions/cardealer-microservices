using AdminService.Application.UseCases.Finance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Api.Controllers;

/// <summary>
/// Billing admin endpoints — subscription revenue, plan breakdown, and financial KPIs.
/// </summary>
[ApiController]
[Route("api/admin/billing")]
[Produces("application/json")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class BillingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BillingController> _logger;

    public BillingController(IMediator mediator, ILogger<BillingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get revenue breakdown by subscription plan tier (libre / visible / pro / elite).
    /// Returns MRR, dealer count, and unit price per plan.
    /// </summary>
    /// <param name="period">Optional period in YYYY-MM format. Defaults to current month.</param>
    [HttpGet("revenue-by-plan")]
    [ProducesResponseType(typeof(RevenueBreakdownDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RevenueBreakdownDto>> GetRevenueByPlan([FromQuery] string? period = null)
    {
        _logger.LogInformation("Admin requested revenue by plan, period={Period}", period);
        var result = await _mediator.Send(new GetRevenueByPlanQuery(period));
        return Ok(result);
    }

    /// <summary>
    /// Get total billing revenue summary (same as financial dashboard revenue section).
    /// </summary>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(RevenueBreakdownDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RevenueBreakdownDto>> GetRevenue([FromQuery] string? period = null)
    {
        _logger.LogInformation("Admin requested billing revenue summary, period={Period}", period);
        var result = await _mediator.Send(new GetRevenueByPlanQuery(period));
        return Ok(result);
    }

    /// <summary>
    /// Get recent billing transactions across all dealers.
    /// Returns empty list when BillingService data is unavailable.
    /// </summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(List<AdminBillingTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdminBillingTransactionDto>>> GetTransactions(
        [FromQuery] int limit = 10)
    {
        _logger.LogInformation("Admin requested billing transactions (limit={Limit})", limit);
        var result = await _mediator.Send(new GetBillingTransactionsQuery(limit));
        return Ok(result);
    }

    /// <summary>
    /// Get pending / overdue payments across all dealers.
    /// Returns empty list when BillingService data is unavailable.
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<AdminPendingPaymentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdminPendingPaymentDto>>> GetPendingPayments()
    {
        _logger.LogInformation("Admin requested pending payments");
        var result = await _mediator.Send(new GetPendingPaymentsQuery());
        return Ok(result);
    }
}
