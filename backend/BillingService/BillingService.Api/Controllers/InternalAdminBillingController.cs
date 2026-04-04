using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingService.Domain.Entities;
using BillingService.Infrastructure.Persistence;

namespace BillingService.Api.Controllers;

/// <summary>
/// Internal-only endpoints consumed by AdminService via service-to-service HTTP.
/// NOT exposed through the API Gateway — accessible only inside Docker / K8s network.
/// AllowAnonymous is intentional: no external traffic reaches this controller.
/// </summary>
[ApiController]
[Route("api/internal/admin")]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]   // hide from Swagger
public class InternalAdminBillingController : ControllerBase
{
    private readonly BillingDbContext _db;
    private readonly ILogger<InternalAdminBillingController> _logger;

    public InternalAdminBillingController(BillingDbContext db, ILogger<InternalAdminBillingController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Returns the N most recent payments across ALL dealers (for admin billing transactions table).
    /// </summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        if (limit <= 0 || limit > 200) limit = 10;

        _logger.LogInformation("[Internal] Admin requested last {Limit} billing transactions", limit);

        var payments = await _db.Payments
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new
            {
                id          = p.Id,
                dealerId    = p.DealerId,
                dealerName  = p.DealerId.ToString("N").Substring(0, 8),
                plan        = p.Subscription != null ? p.Subscription.Plan.ToString() : string.Empty,
                amount      = p.Amount,
                currency    = p.Currency,
                status      = p.Status.ToString(),
                date        = p.CreatedAt,
                method      = p.Method.ToString()
            })
            .ToListAsync(ct);

        return Ok(payments);
    }

    /// <summary>
    /// Returns invoices that are Overdue or have outstanding balance across ALL dealers.
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingPayments(CancellationToken ct = default)
    {
        _logger.LogInformation("[Internal] Admin requested pending/overdue payments");

        var now = DateTime.UtcNow;

        var pending = await _db.Invoices
            .AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Overdue
                     || i.Status == InvoiceStatus.Issued
                     || i.Status == InvoiceStatus.Sent
                     || i.Status == InvoiceStatus.PartiallyPaid)
            .OrderByDescending(i => i.DueDate)
            .Take(50)
            .Select(i => new
            {
                id          = i.Id,
                dealerId    = i.DealerId,
                dealerName  = i.DealerId.ToString("N").Substring(0, 8),
                amount      = i.TotalAmount - i.PaidAmount,
                currency    = i.Currency,
                dueDate     = i.DueDate,
                daysOverdue = i.DueDate < now ? (int)(now - i.DueDate).TotalDays : 0
            })
            .ToListAsync(ct);

        return Ok(pending);
    }
}
