using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BillingService.Infrastructure.Services;
using BillingService.Application.Configuration;

namespace BillingService.Api.Controllers;

/// <summary>
/// Returns configured/available payment gateways for the checkout page.
/// </summary>
[ApiController]
[Route("api/payments/providers")]
public class PaymentProvidersController : ControllerBase
{
    private readonly StripeSettings _stripe;
    private readonly PayPalSettings _paypal;
    private readonly AzulConfiguration _azul;

    public PaymentProvidersController(
        IOptions<StripeSettings> stripe,
        IOptions<PayPalSettings> paypal,
        IOptions<AzulConfiguration> azul)
    {
        _stripe = stripe.Value;
        _paypal = paypal.Value;
        _azul = azul.Value;
    }

    /// <summary>
    /// GET /api/payments/providers/available
    /// Returns all payment gateways and whether they are configured.
    /// </summary>
    [HttpGet("available")]
    [AllowAnonymous]
    public IActionResult GetAvailable()
    {
        var providers = new List<object>
        {
            new
            {
                gateway = "Azul",
                name = "Azul (Banco Popular)",
                type = "CreditCard",
                isConfigured = !string.IsNullOrEmpty(_azul.MerchantId)
            },
            new
            {
                gateway = "Stripe",
                name = "Stripe",
                type = "CreditCard",
                isConfigured = !string.IsNullOrEmpty(_stripe.SecretKey)
            },
            new
            {
                gateway = "PayPal",
                name = "PayPal",
                type = "PayPal",
                isConfigured = !string.IsNullOrEmpty(_paypal.ClientId)
            }
        };

        return Ok(new { providers });
    }
}
