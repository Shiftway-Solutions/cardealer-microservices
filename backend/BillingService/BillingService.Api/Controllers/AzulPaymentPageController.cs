using BillingService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Cryptography;

namespace BillingService.Api.Controllers;

/// <summary>
/// Controller for Azul payment page initialization and callbacks
/// Used for adding/managing payment methods via Azul gateway
/// </summary>
[ApiController]
[Route("api/azul")]
public class AzulPaymentPageController : BillingBaseController
{
    private readonly IAzulPaymentService _azulService;
    private readonly ILogger<AzulPaymentPageController> _logger;

    public AzulPaymentPageController(
        IAzulPaymentService azulService,
        ILogger<AzulPaymentPageController> logger)
    {
        _azulService = azulService;
        _logger = logger;
    }

    /// <summary>
    /// Initialize Azul payment page for adding a new payment method
    /// </summary>
    /// <param name="request">Request containing dealer info and return URLs</param>
    /// <returns>Redirect URL and session ID for Azul payment page</returns>
    [HttpPost("payment-page/init")]
    [Authorize]
    [EnableRateLimiting("BillingPolicy")]
    public async Task<ActionResult<PaymentPageInitResponse>> InitPaymentPage(
        [FromBody] PaymentPageInitRequest request)
    {
        try
        {
            // Security: Validate DealerId matches JWT (IDOR prevention — OWASP A01:2021)
            var jwtDealerId = GetDealerIdFromJwt();
            if (request.DealerId != jwtDealerId && !IsAdmin())
                return StatusCode(403, new { error = "Access denied: cannot perform operations for another dealer" });

            _logger.LogInformation("Initializing Azul payment page for dealer {DealerId}, purpose: {Purpose}",
                request.DealerId, request.Purpose);

            // SECURITY: Validate redirect URLs to prevent open redirect (OWASP A01:2021)
            if (!BillingApplicationService.IsAllowedRedirectUrl(request.ReturnUrl))
                return BadRequest(new { Error = "ReturnUrl inválida. Solo se permiten dominios de OKLA." });
            if (!BillingApplicationService.IsAllowedRedirectUrl(request.CancelUrl))
                return BadRequest(new { Error = "CancelUrl inválida. Solo se permiten dominios de OKLA." });

            // Generate unique session ID for this payment page session
            var sessionId = Guid.NewGuid().ToString("N");

            // Get Azul payment page URL
            var paymentPageUrl = _azulService.GetPaymentPageUrl();

            // For card tokenization (adding payment method), we create a special request
            // The amount is 0 for tokenization-only requests
            var tokenizationRequest = _azulService.CreatePaymentRequest(
                amount: 0,
                itbis: 0,
                orderNumber: $"TOKEN-{request.DealerId}-{sessionId[..8]}"
            );

            // Build redirect URL with all necessary parameters
            var formFields = _azulService.GetPaymentFormFields(tokenizationRequest);

            // Construct the full redirect URL with returnUrl parameters
            var redirectUrl = BuildAzulRedirectUrl(
                paymentPageUrl,
                formFields,
                request.ReturnUrl,
                request.CancelUrl,
                sessionId
            );

            return Ok(new PaymentPageInitResponse
            {
                RedirectUrl = redirectUrl,
                SessionId = sessionId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30).ToString("o")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Azul payment page for dealer {DealerId}", request.DealerId);
            return StatusCode(500, new { Error = "Error al inicializar la página de pago de Azul" });
        }
    }

    /// <summary>
    /// Callback endpoint for Azul after successful card tokenization
    /// </summary>
    [HttpPost("payment-page/callback")]
    [AllowAnonymous]
    public async Task<ActionResult> PaymentPageCallback([FromForm] AzulCallbackData data)
    {
        try
        {
            _logger.LogInformation("Received Azul callback. ResponseCode: {ResponseCode}, AuthorizationCode: {AuthCode}",
                data.ResponseCode, data.AuthorizationCode);

            // Verify the response signature if provided
            if (!string.IsNullOrEmpty(data.AuthHash))
            {
                var isValid = VerifyAzulSignature(data);
                if (!isValid)
                {
                    _logger.LogWarning("Invalid Azul signature in callback");
                    return BadRequest(new { Error = "Invalid signature" });
                }
            }

            // Process successful tokenization
            if (data.ResponseCode == "00" || data.ResponseCode == "ISO8583-000")
            {
                _logger.LogInformation("Card tokenization successful. DataVaultToken: {Token}",
                    data.DataVaultToken?.Substring(0, 4) + "****");

                // Here you would save the token to the dealer's payment methods
                // await _billingService.SavePaymentMethod(dealerId, data.DataVaultToken, ...);

                return Ok(new { Success = true, Message = "Card added successfully" });
            }

            _logger.LogWarning("Card tokenization failed. ResponseCode: {Code}, Message: {Msg}",
                data.ResponseCode, data.ResponseMessage);

            return BadRequest(new
            {
                Success = false,
                Error = data.ResponseMessage ?? "Error adding card"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Azul callback");
            return StatusCode(500, new { Error = "Error processing callback" });
        }
    }

    /// <summary>
    /// Get available card brands for display
    /// </summary>
    [HttpGet("card-brands")]
    [AllowAnonymous]
    public ActionResult<List<CardBrandInfo>> GetCardBrands()
    {
        return Ok(new List<CardBrandInfo>
        {
            new("Visa", "visa", true),
            new("Mastercard", "mastercard", true),
            new("American Express", "amex", true),
            new("Discover", "discover", false)
        });
    }

    // ========================================
    // PRIVATE HELPER METHODS
    // ========================================

    private string BuildAzulRedirectUrl(
        string baseUrl,
        Dictionary<string, string> formFields,
        string returnUrl,
        string cancelUrl,
        string sessionId)
    {
        // For now, return the base URL with parameters as query string
        // In production, this would be a form POST or proper redirect
        var queryParams = new List<string>
        {
            $"MerchantId={Uri.EscapeDataString(formFields.GetValueOrDefault("MerchantId", ""))}",
            $"MerchantType={Uri.EscapeDataString(formFields.GetValueOrDefault("MerchantType", ""))}",
            $"ReturnUrl={Uri.EscapeDataString(returnUrl)}",
            $"CancelUrl={Uri.EscapeDataString(cancelUrl)}",
            $"SessionId={Uri.EscapeDataString(sessionId)}",
            $"SaveCard=1" // Flag to save card for future use
        };

        return $"{baseUrl}?{string.Join("&", queryParams)}";
    }

    private bool VerifyAzulSignature(AzulCallbackData data)
    {
        // SECURITY: Verify Azul callback signature using HMAC-SHA512
        // The AuthHash must match the computed hash of the response fields
        var authKey = Environment.GetEnvironmentVariable("AZUL_AUTH_KEY");
        if (string.IsNullOrEmpty(authKey))
        {
            _logger.LogError("AZUL_AUTH_KEY environment variable is not configured. Cannot verify Azul signatures.");
            return false; // FAIL CLOSED — reject if no key configured
        }

        // Azul computes hash over: OrderNumber + ResponseCode + DataVaultToken + AuthorizationCode
        var dataToSign = string.Concat(
            data.OrderNumber ?? "",
            data.ResponseCode ?? "",
            data.DataVaultToken ?? "",
            data.AuthorizationCode ?? "");

        using var hmac = new HMACSHA512(System.Text.Encoding.UTF8.GetBytes(authKey));
        var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dataToSign));
        var computedHashHex = Convert.ToHexString(computedHash).ToLowerInvariant();

        return string.Equals(computedHashHex, data.AuthHash, StringComparison.OrdinalIgnoreCase);
    }
}

// ========================================
// REQUEST/RESPONSE DTOs
// ========================================

public record PaymentPageInitRequest
{
    public Guid DealerId { get; init; }
    public string Purpose { get; init; } = "add_payment_method";
    public string ReturnUrl { get; init; } = "";
    public string CancelUrl { get; init; } = "";
}

public record PaymentPageInitResponse
{
    public string RedirectUrl { get; init; } = "";
    public string SessionId { get; init; } = "";
    public string ExpiresAt { get; init; } = "";
}

public record AzulCallbackData
{
    public string? ResponseCode { get; init; }
    public string? ResponseMessage { get; init; }
    public string? AuthorizationCode { get; init; }
    public string? DataVaultToken { get; init; }
    public string? CardNumber { get; init; } // Last 4 digits only
    public string? CardBrand { get; init; }
    public string? ExpirationDate { get; init; }
    public string? AuthHash { get; init; }
    public string? OrderNumber { get; init; }
    public string? CustomOrderId { get; init; }
}

public record CardBrandInfo(string Name, string Code, bool IsSupported);
