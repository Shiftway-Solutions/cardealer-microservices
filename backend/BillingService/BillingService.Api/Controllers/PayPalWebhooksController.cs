using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BillingService.Domain.Interfaces;
using BillingService.Infrastructure.Services;
using System.Text.Json;

namespace BillingService.Api.Controllers;

/// <summary>
/// PayPal webhook handler — receives events from PayPal IPN/webhooks.
/// Verifies signature via PayPal's verification API before processing.
/// </summary>
[ApiController]
[Route("api/webhooks/paypal")]
[AllowAnonymous] // Webhooks verify via signed payload, not JWT
public class PayPalWebhooksController : ControllerBase
{
    private readonly IPayPalService _payPalService;
    private readonly ILogger<PayPalWebhooksController> _logger;
    private readonly PayPalSettings _settings;

    public PayPalWebhooksController(
        IPayPalService payPalService,
        ILogger<PayPalWebhooksController> logger,
        IOptions<PayPalSettings> settings)
    {
        _payPalService = payPalService;
        _logger = logger;
        _settings = settings.Value;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        var body = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

        // Extract PayPal verification headers
        var headers = new Dictionary<string, string>();
        foreach (var headerName in new[]
        {
            "PAYPAL-AUTH-ALGO",
            "PAYPAL-CERT-URL",
            "PAYPAL-TRANSMISSION-ID",
            "PAYPAL-TRANSMISSION-SIG",
            "PAYPAL-TRANSMISSION-TIME"
        })
        {
            if (Request.Headers.TryGetValue(headerName, out var value))
            {
                headers[headerName] = value.ToString();
            }
        }

        // Verify webhook signature with PayPal
        if (!string.IsNullOrEmpty(_settings.WebhookId))
        {
            var isValid = await _payPalService.VerifyWebhookSignatureAsync(
                _settings.WebhookId,
                headers,
                body,
                cancellationToken);

            if (!isValid)
            {
                _logger.LogWarning("PayPal webhook signature verification failed");
                return Unauthorized(new { error = "Invalid webhook signature" });
            }
        }
        else
        {
            _logger.LogWarning("PayPal WebhookId not configured — skipping signature verification");
        }

        // Parse event
        try
        {
            using var doc = JsonDocument.Parse(body);
            var eventType = doc.RootElement.GetProperty("event_type").GetString();
            var resourceId = doc.RootElement
                .GetProperty("resource")
                .TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

            _logger.LogInformation(
                "PayPal webhook received: {EventType}, ResourceId: {ResourceId}",
                eventType, resourceId);

            switch (eventType)
            {
                case "PAYMENT.CAPTURE.COMPLETED":
                    await HandleCaptureCompleted(doc.RootElement);
                    break;
                case "PAYMENT.CAPTURE.DENIED":
                    await HandleCaptureDenied(doc.RootElement);
                    break;
                case "PAYMENT.CAPTURE.REFUNDED":
                    await HandleCaptureRefunded(doc.RootElement);
                    break;
                case "CHECKOUT.ORDER.APPROVED":
                    _logger.LogInformation("PayPal order approved via webhook");
                    break;
                default:
                    _logger.LogDebug("Unhandled PayPal event type: {EventType}", eventType);
                    break;
            }

            return Ok(new { received = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse PayPal webhook body");
            return BadRequest(new { error = "Invalid JSON" });
        }
    }

    private Task HandleCaptureCompleted(JsonElement root)
    {
        var resource = root.GetProperty("resource");
        var captureId = resource.GetProperty("id").GetString();
        var amount = resource.GetProperty("amount").GetProperty("value").GetString();
        var currency = resource.GetProperty("amount").GetProperty("currency_code").GetString();

        _logger.LogInformation(
            "PayPal capture completed: {CaptureId}, Amount: {Amount} {Currency}",
            captureId, amount, currency);

        // TODO: Update payment record in database, publish RabbitMQ event
        return Task.CompletedTask;
    }

    private Task HandleCaptureDenied(JsonElement root)
    {
        var resource = root.GetProperty("resource");
        var captureId = resource.GetProperty("id").GetString();

        _logger.LogWarning("PayPal capture denied: {CaptureId}", captureId);

        // TODO: Mark payment as failed, notify user
        return Task.CompletedTask;
    }

    private Task HandleCaptureRefunded(JsonElement root)
    {
        var resource = root.GetProperty("resource");
        var captureId = resource.GetProperty("id").GetString();

        _logger.LogInformation("PayPal capture refunded: {CaptureId}", captureId);

        // TODO: Update payment with refund status
        return Task.CompletedTask;
    }
}
