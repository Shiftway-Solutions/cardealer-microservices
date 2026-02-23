using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BillingService.Application.DTOs;
using BillingService.Application.Services;

namespace BillingService.Api.Controllers;

/// <summary>
/// Controlador para operaciones de billing con Stripe
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly BillingApplicationService _billingService;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        BillingApplicationService billingService,
        ILogger<BillingController> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    // ========================================
    // PRICING & PLANS
    // ========================================

    /// <summary>
    /// Obtiene los precios de todos los planes disponibles
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PlanPricingInfo>>> GetPlans(
        [FromQuery] Guid? dealerId = null,
        CancellationToken cancellationToken = default)
    {
        var plans = await _billingService.GetPlanPricingAsync(dealerId, cancellationToken);
        return Ok(plans);
    }

    // ========================================
    // CUSTOMER OPERATIONS
    // ========================================

    /// <summary>
    /// Crea un cliente de Stripe para un dealer
    /// </summary>
    [HttpPost("customers")]
    public async Task<ActionResult<StripeCustomerResponse>> CreateCustomer(
        [FromBody] CreateStripeCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _billingService.CreateCustomerAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to create customer for dealer {DealerId}", request.DealerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene el cliente de Stripe de un dealer
    /// </summary>
    [HttpGet("customers/{dealerId:guid}")]
    public async Task<ActionResult<StripeCustomerResponse>> GetCustomer(
        Guid dealerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _billingService.GetCustomerByDealerIdAsync(dealerId, cancellationToken);
        if (customer == null)
            return NotFound(new { error = $"Customer not found for dealer {dealerId}" });

        return Ok(customer);
    }

    /// <summary>
    /// Obtiene el cliente de Stripe de un dealer (ruta alternativa)
    /// </summary>
    [HttpGet("customers/by-dealer/{dealerId:guid}")]
    public async Task<ActionResult<StripeCustomerResponse>> GetCustomerByDealer(
        Guid dealerId,
        CancellationToken cancellationToken = default)
    {
        return await GetCustomer(dealerId, cancellationToken);
    }

    /// <summary>
    /// Adjunta un método de pago al cliente
    /// </summary>
    [HttpPost("customers/{dealerId:guid}/payment-methods")]
    public async Task<ActionResult<StripeCustomerResponse>> AttachPaymentMethod(
        Guid dealerId,
        [FromBody] AttachPaymentMethodRequestBody body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new AttachPaymentMethodRequest(
                dealerId,
                body.PaymentMethodId,
                body.SetAsDefault
            );

            var result = await _billingService.AttachPaymentMethodAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to attach payment method for dealer {DealerId}", dealerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ========================================
    // SUBSCRIPTION OPERATIONS
    // ========================================

    /// <summary>
    /// Obtiene la suscripción del usuario autenticado (dealer).
    /// BUG-D004 fix: was missing GET subscriptions without dealerId — returned 405.
    /// Reads dealerId from JWT claim and delegates to GetSubscription.
    /// </summary>
    [HttpGet("subscriptions")]
    public async Task<ActionResult<SubscriptionResponse>> GetMySubscription(
        CancellationToken cancellationToken = default)
    {
        var dealerIdClaim = User.FindFirst("dealerId")?.Value;
        if (string.IsNullOrEmpty(dealerIdClaim) || !Guid.TryParse(dealerIdClaim, out var dealerId))
            return BadRequest(new { error = "dealerId claim missing or invalid in token" });

        var subscription = await _billingService.GetSubscriptionByDealerIdAsync(dealerId, cancellationToken);
        if (subscription == null)
            return NotFound(new { error = $"No active subscription found for your account" });

        return Ok(subscription);
    }

    /// <summary>
    /// Obtiene la suscripción actual de un dealer
    /// </summary>
    [HttpGet("subscriptions/{dealerId:guid}")]
    public async Task<ActionResult<SubscriptionResponse>> GetSubscription(
        Guid dealerId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _billingService.GetSubscriptionByDealerIdAsync(dealerId, cancellationToken);
        if (subscription == null)
            return NotFound(new { error = $"Subscription not found for dealer {dealerId}" });

        return Ok(subscription);
    }

    /// <summary>
    /// Obtiene la suscripción actual de un dealer (ruta alternativa)
    /// </summary>
    [HttpGet("subscriptions/by-dealer/{dealerId:guid}")]
    public async Task<ActionResult<SubscriptionResponse>> GetSubscriptionByDealer(
        Guid dealerId,
        CancellationToken cancellationToken = default)
    {
        return await GetSubscription(dealerId, cancellationToken);
    }

    /// <summary>
    /// Crea una nueva suscripción
    /// </summary>
    [HttpPost("subscriptions")]
    public async Task<ActionResult<SubscriptionResponse>> CreateSubscription(
        [FromBody] CreateStripeSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _billingService.CreateSubscriptionAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetSubscription),
                new { dealerId = result.DealerId },
                result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to create subscription for dealer {DealerId}", request.DealerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza una suscripción (upgrade/downgrade)
    /// </summary>
    [HttpPut("subscriptions/{subscriptionId:guid}")]
    public async Task<ActionResult<SubscriptionResponse>> UpdateSubscription(
        Guid subscriptionId,
        [FromBody] UpdateSubscriptionRequestBody body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new UpdateStripeSubscriptionRequest(
                subscriptionId,
                body.NewPlan,
                body.NewCycle,
                body.ProrationEnabled
            );

            var result = await _billingService.UpdateSubscriptionAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to update subscription {SubscriptionId}", subscriptionId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancela una suscripción
    /// </summary>
    [HttpDelete("subscriptions/{subscriptionId:guid}")]
    public async Task<ActionResult<SubscriptionResponse>> CancelSubscription(
        Guid subscriptionId,
        [FromQuery] string? reason = null,
        [FromQuery] bool cancelImmediately = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new CancelStripeSubscriptionRequest(
                subscriptionId,
                reason,
                cancelImmediately
            );

            var result = await _billingService.CancelSubscriptionAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription {SubscriptionId}", subscriptionId);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ========================================
    // CHECKOUT & BILLING PORTAL
    // ========================================

    /// <summary>
    /// Crea una sesión de Stripe Checkout para nueva suscripción
    /// </summary>
    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutSessionResponse>> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _billingService.CreateCheckoutSessionAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to create checkout session for dealer {DealerId}", request.DealerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Crea una sesión del portal de billing
    /// </summary>
    [HttpPost("billing-portal")]
    public async Task<ActionResult<BillingPortalSessionResponse>> CreateBillingPortalSession(
        [FromBody] CreateBillingPortalSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _billingService.CreateBillingPortalSessionAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to create billing portal session for dealer {DealerId}", request.DealerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ========================================
    // BILLING SUMMARY
    // ========================================

    /// <summary>
    /// Obtiene el resumen de billing para un dealer
    /// </summary>
    [HttpGet("summary/{dealerId:guid}")]
    public async Task<ActionResult<BillingSummaryResponse>> GetBillingSummary(
        Guid dealerId,
        CancellationToken cancellationToken = default)
    {
        var summary = await _billingService.GetBillingSummaryAsync(dealerId, cancellationToken);
        return Ok(summary);
    }
}

// ========================================
// REQUEST BODY DTOs
// ========================================

public record AttachPaymentMethodRequestBody(
    string PaymentMethodId,
    bool SetAsDefault = true
);

public record UpdateSubscriptionRequestBody(
    Domain.Entities.SubscriptionPlan NewPlan,
    Domain.Entities.BillingCycle? NewCycle = null,
    bool ProrationEnabled = true
);
