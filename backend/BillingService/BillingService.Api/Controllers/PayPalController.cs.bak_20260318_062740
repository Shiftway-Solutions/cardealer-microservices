using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BillingService.Domain.Interfaces;

namespace BillingService.Api.Controllers;

/// <summary>
/// PayPal payment endpoints — create orders, capture payments, refund
/// </summary>
[ApiController]
[Route("api/payments/paypal")]
[Authorize]
public class PayPalController : ControllerBase
{
    private readonly IPayPalService _payPalService;
    private readonly ILogger<PayPalController> _logger;

    public PayPalController(
        IPayPalService payPalService,
        ILogger<PayPalController> logger)
    {
        _payPalService = payPalService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a PayPal order for one-time payment.
    /// Frontend calls this, gets the orderId, then shows PayPal popup for approval.
    /// </summary>
    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreatePayPalOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Amount <= 0)
                return BadRequest(new { error = "Amount must be greater than 0" });

            var result = await _payPalService.CreateOrderAsync(
                request.Amount,
                request.Currency ?? "USD",
                request.Description ?? "Pago OKLA",
                request.ReturnUrl ?? $"{Request.Scheme}://{Request.Host}/checkout/exito",
                request.CancelUrl ?? $"{Request.Scheme}://{Request.Host}/checkout",
                request.Metadata,
                cancellationToken);

            return Ok(new
            {
                orderId = result.OrderId,
                status = result.Status,
                approvalUrl = result.ApprovalUrl,
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create PayPal order");
            return StatusCode(502, new { error = "PayPal service unavailable" });
        }
    }

    /// <summary>
    /// Captures (charges) an approved PayPal order.
    /// Called after the user approves in the PayPal popup.
    /// </summary>
    [HttpPost("capture")]
    public async Task<IActionResult> CaptureOrder(
        [FromBody] CapturePayPalOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OrderId))
                return BadRequest(new { error = "OrderId is required" });

            var result = await _payPalService.CaptureOrderAsync(
                request.OrderId,
                cancellationToken);

            return Ok(new
            {
                orderId = result.OrderId,
                captureId = result.CaptureId,
                status = result.Status,
                amount = result.Amount,
                currency = result.Currency,
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to capture PayPal order {OrderId}", request.OrderId);
            return StatusCode(502, new { error = "PayPal service unavailable" });
        }
    }

    /// <summary>
    /// Gets the status of a PayPal order
    /// </summary>
    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetOrder(
        string orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _payPalService.GetOrderAsync(orderId, cancellationToken);

            if (result == null)
                return NotFound(new { error = "Order not found" });

            return Ok(new
            {
                orderId = result.OrderId,
                status = result.Status,
                amount = result.Amount,
                currency = result.Currency,
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get PayPal order {OrderId}", orderId);
            return StatusCode(502, new { error = "PayPal service unavailable" });
        }
    }

    /// <summary>
    /// Refunds a captured PayPal payment (full or partial)
    /// </summary>
    [HttpPost("refund")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Refund(
        [FromBody] RefundPayPalRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CaptureId))
                return BadRequest(new { error = "CaptureId is required" });

            var result = await _payPalService.RefundCaptureAsync(
                request.CaptureId,
                request.Amount,
                request.Currency,
                request.Reason,
                cancellationToken);

            return Ok(new
            {
                refundId = result.RefundId,
                status = result.Status,
                amount = result.Amount,
                currency = result.Currency,
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to refund PayPal capture {CaptureId}", request.CaptureId);
            return StatusCode(502, new { error = "PayPal service unavailable" });
        }
    }
}

// =============================================================================
// REQUEST DTOs
// =============================================================================

public record CreatePayPalOrderRequest(
    decimal Amount,
    string? Currency = "USD",
    string? Description = null,
    string? ReturnUrl = null,
    string? CancelUrl = null,
    Dictionary<string, string>? Metadata = null
);

public record CapturePayPalOrderRequest(
    string OrderId
);

public record RefundPayPalRequest(
    string CaptureId,
    decimal? Amount = null,
    string? Currency = null,
    string? Reason = null
);
