
// WebhooksController.cs — Webhook endpoints with HMAC signature verification
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IConfiguration configuration, ILogger<WebhooksController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// SendGrid Event Webhook — validates HMAC signature before processing
    /// </summary>
    [HttpPost("sendgrid")]
    public async Task<IActionResult> SendGridWebhook()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var signature = Request.Headers["X-Twilio-Email-Event-Webhook-Signature"].FirstOrDefault();
        var timestamp = Request.Headers["X-Twilio-Email-Event-Webhook-Timestamp"].FirstOrDefault();
        var verificationKey = _configuration["Webhooks:SendGrid:VerificationKey"];

        if (!string.IsNullOrEmpty(verificationKey))
        {
            if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
            {
                _logger.LogWarning("SendGrid webhook received without signature headers — rejected");
                return Unauthorized(new { error = "Missing webhook signature" });
            }

            var payload = timestamp + body;
            if (!VerifyHmacSignature(payload, signature, verificationKey))
            {
                _logger.LogWarning("SendGrid webhook signature verification FAILED — potential forgery attempt");
                return Unauthorized(new { error = "Invalid webhook signature" });
            }
        }
        else
        {
            _logger.LogError("SendGrid verification key not configured — REJECTING webhook (fail-closed). Configure Webhooks:SendGrid:VerificationKey");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Webhook verification not configured" });
        }

        _logger.LogInformation("SendGrid webhook processed. Body length: {Length}", body.Length);
        return Ok();
    }

    /// <summary>
    /// Twilio SMS Webhook — validates X-Twilio-Signature before processing
    /// </summary>
    [HttpPost("twilio")]
    public async Task<IActionResult> TwilioWebhook()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var signature = Request.Headers["X-Twilio-Signature"].FirstOrDefault();
        var authToken = _configuration["Webhooks:Twilio:AuthToken"];

        if (!string.IsNullOrEmpty(authToken))
        {
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Twilio webhook received without X-Twilio-Signature — rejected");
                return Unauthorized(new { error = "Missing webhook signature" });
            }

            var requestUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
            var payload = requestUrl + body;

            if (!VerifyHmacSignature(payload, signature, authToken, "SHA1"))
            {
                _logger.LogWarning("Twilio webhook signature verification FAILED — potential forgery attempt");
                return Unauthorized(new { error = "Invalid webhook signature" });
            }
        }
        else
        {
            _logger.LogError("Twilio auth token not configured — REJECTING webhook (fail-closed). Configure Webhooks:Twilio:AuthToken");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Webhook verification not configured" });
        }

        _logger.LogInformation("Twilio webhook processed. Body length: {Length}", body.Length);
        return Ok();
    }

    /// <summary>
    /// Verify HMAC signature — timing-safe comparison (OWASP)
    /// </summary>
    private static bool VerifyHmacSignature(string payload, string signature, string secret, string algorithm = "SHA256")
    {
        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            byte[] hash;
            if (algorithm == "SHA1")
            {
                using var hmac = new HMACSHA1(keyBytes);
                hash = hmac.ComputeHash(payloadBytes);
            }
            else
            {
                using var hmac = new HMACSHA256(keyBytes);
                hash = hmac.ComputeHash(payloadBytes);
            }

            var computedSignature = Convert.ToBase64String(hash);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedSignature),
                Encoding.UTF8.GetBytes(signature));
        }
        catch
        {
            return false;
        }
    }
}