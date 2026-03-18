using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using BillingService.Domain.Interfaces;

namespace BillingService.Infrastructure.Services;

/// <summary>
/// PayPal REST API v2 implementation with circuit breaker resilience.
/// Uses HttpClient to call PayPal's Orders API v2 for one-time payments.
/// 
/// Auth flow: OAuth2 Client Credentials → Bearer token (cached 9h).
/// Payment flow: Create Order → User approves in popup → Capture Order.
/// </summary>
public class PayPalService : IPayPalService
{
    private readonly ILogger<PayPalService> _logger;
    private readonly PayPalSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ResiliencePipeline _resiliencePipeline;

    // Cached access token
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public PayPalService(
        ILogger<PayPalService> logger,
        IOptions<PayPalSettings> settings,
        HttpClient httpClient,
        ResiliencePipelineProvider<string> resiliencePipelineProvider)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClient = httpClient;
        _resiliencePipeline = resiliencePipelineProvider.GetPipeline("paypal-circuit-breaker");

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
    }

    // ========================================
    // RESILIENCE WRAPPER
    // ========================================

    private async Task<T> ExecuteWithResilienceAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        return await _resiliencePipeline.ExecuteAsync(
            async ct => await action(ct),
            cancellationToken);
    }

    // ========================================
    // AUTH — OAuth2 Client Credentials
    // ========================================

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
        {
            return _accessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
            {
                return _accessToken;
            }

            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<PayPalTokenResponse>(json);

            _accessToken = tokenResponse?.AccessToken
                ?? throw new InvalidOperationException("PayPal returned null access token");

            // Expire 5 minutes early to avoid edge cases
            _tokenExpiry = DateTime.UtcNow.AddSeconds((tokenResponse.ExpiresIn ?? 32400) - 300);

            _logger.LogDebug("PayPal access token refreshed, expires at {Expiry}", _tokenExpiry);
            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequest(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    // ========================================
    // ORDERS — Create, Get, Capture
    // ========================================

    public async Task<PayPalOrderResult> CreateOrderAsync(
        decimal amount,
        string currency,
        string description,
        string returnUrl,
        string cancelUrl,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilienceAsync(async ct =>
        {
            _logger.LogInformation(
                "Creating PayPal order: {Amount} {Currency} — {Description}",
                amount, currency, description);

            var request = await CreateAuthorizedRequest(HttpMethod.Post, "/v2/checkout/orders", ct);

            var orderPayload = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        amount = new
                        {
                            currency_code = currency.ToUpperInvariant(),
                            value = amount.ToString("F2")
                        },
                        description
                    }
                },
                application_context = new
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl,
                    brand_name = "OKLA",
                    landing_page = "LOGIN",
                    user_action = "PAY_NOW",
                    shipping_preference = "NO_SHIPPING"
                }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(orderPayload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "PayPal CreateOrder failed: {Status} — {Body}",
                    response.StatusCode, responseBody);
                throw new HttpRequestException(
                    $"PayPal CreateOrder failed: {response.StatusCode}");
            }

            var order = JsonSerializer.Deserialize<PayPalOrderResponse>(responseBody)
                ?? throw new InvalidOperationException("PayPal returned null order");

            var approvalUrl = order.Links?
                .FirstOrDefault(l => l.Rel == "approve")?.Href;

            _logger.LogInformation(
                "PayPal order created: {OrderId}, Status: {Status}",
                order.Id, order.Status);

            return new PayPalOrderResult(
                OrderId: order.Id,
                Status: order.Status,
                ApprovalUrl: approvalUrl,
                Amount: amount,
                Currency: currency,
                CreatedAt: DateTime.UtcNow);
        }, cancellationToken);
    }

    public async Task<PayPalCaptureResult> CaptureOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilienceAsync(async ct =>
        {
            _logger.LogInformation("Capturing PayPal order: {OrderId}", orderId);

            var request = await CreateAuthorizedRequest(
                HttpMethod.Post, $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture", ct);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "PayPal CaptureOrder failed: {Status} — {Body}",
                    response.StatusCode, responseBody);
                throw new HttpRequestException(
                    $"PayPal CaptureOrder failed: {response.StatusCode}");
            }

            var order = JsonSerializer.Deserialize<PayPalOrderResponse>(responseBody)
                ?? throw new InvalidOperationException("PayPal returned null capture");

            var capture = order.PurchaseUnits?
                .FirstOrDefault()?.Payments?.Captures?
                .FirstOrDefault();

            var capturedAmount = decimal.TryParse(capture?.Amount?.Value, out var amt) ? amt : 0m;

            _logger.LogInformation(
                "PayPal order captured: {OrderId}, CaptureId: {CaptureId}, Amount: {Amount}",
                orderId, capture?.Id, capturedAmount);

            return new PayPalCaptureResult(
                OrderId: orderId,
                CaptureId: capture?.Id ?? string.Empty,
                Status: order.Status,
                Amount: capturedAmount,
                Currency: capture?.Amount?.CurrencyCode ?? "USD",
                ReceiptUrl: null,
                PayerEmail: order.Payer?.EmailAddress,
                PayerId: order.Payer?.PayerId);
        }, cancellationToken);
    }

    public async Task<PayPalOrderResult?> GetOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilienceAsync(async ct =>
        {
            var request = await CreateAuthorizedRequest(
                HttpMethod.Get, $"/v2/checkout/orders/{Uri.EscapeDataString(orderId)}", ct);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var order = JsonSerializer.Deserialize<PayPalOrderResponse>(responseBody);

            if (order == null) return null;

            var pu = order.PurchaseUnits?.FirstOrDefault();
            var amount = decimal.TryParse(pu?.Amount?.Value, out var amt) ? amt : 0m;

            return new PayPalOrderResult(
                OrderId: order.Id,
                Status: order.Status,
                ApprovalUrl: null,
                Amount: amount,
                Currency: pu?.Amount?.CurrencyCode ?? "USD",
                CreatedAt: order.CreateTime ?? DateTime.UtcNow);
        }, cancellationToken);
    }

    // ========================================
    // REFUNDS
    // ========================================

    public async Task<PayPalRefundResult> RefundCaptureAsync(
        string captureId,
        decimal? amount = null,
        string? currency = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilienceAsync(async ct =>
        {
            _logger.LogInformation(
                "Refunding PayPal capture: {CaptureId}, Amount: {Amount}",
                captureId, amount?.ToString("F2") ?? "FULL");

            var request = await CreateAuthorizedRequest(
                HttpMethod.Post, $"/v2/payments/captures/{Uri.EscapeDataString(captureId)}/refund", ct);

            object refundPayload;
            if (amount.HasValue && !string.IsNullOrEmpty(currency))
            {
                refundPayload = new
                {
                    amount = new
                    {
                        value = amount.Value.ToString("F2"),
                        currency_code = currency.ToUpperInvariant()
                    },
                    note_to_payer = reason ?? "Reembolso OKLA"
                };
            }
            else
            {
                refundPayload = new
                {
                    note_to_payer = reason ?? "Reembolso OKLA"
                };
            }

            request.Content = new StringContent(
                JsonSerializer.Serialize(refundPayload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "PayPal Refund failed: {Status} — {Body}",
                    response.StatusCode, responseBody);
                throw new HttpRequestException(
                    $"PayPal Refund failed: {response.StatusCode}");
            }

            var refund = JsonSerializer.Deserialize<PayPalRefundResponse>(responseBody)
                ?? throw new InvalidOperationException("PayPal returned null refund");

            var refundedAmount = decimal.TryParse(refund.Amount?.Value, out var amt) ? amt : 0m;

            _logger.LogInformation(
                "PayPal refund completed: {RefundId}, Amount: {Amount}",
                refund.Id, refundedAmount);

            return new PayPalRefundResult(
                RefundId: refund.Id,
                Status: refund.Status,
                Amount: refundedAmount,
                Currency: refund.Amount?.CurrencyCode ?? "USD");
        }, cancellationToken);
    }

    // ========================================
    // WEBHOOKS — Signature Verification
    // ========================================

    public async Task<bool> VerifyWebhookSignatureAsync(
        string webhookId,
        IReadOnlyDictionary<string, string> headers,
        string body,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilienceAsync(async ct =>
        {
            var request = await CreateAuthorizedRequest(
                HttpMethod.Post, "/v1/notifications/verify-webhook-signature", ct);

            var verifyPayload = new
            {
                auth_algo = headers.GetValueOrDefault("PAYPAL-AUTH-ALGO", ""),
                cert_url = headers.GetValueOrDefault("PAYPAL-CERT-URL", ""),
                transmission_id = headers.GetValueOrDefault("PAYPAL-TRANSMISSION-ID", ""),
                transmission_sig = headers.GetValueOrDefault("PAYPAL-TRANSMISSION-SIG", ""),
                transmission_time = headers.GetValueOrDefault("PAYPAL-TRANSMISSION-TIME", ""),
                webhook_id = webhookId,
                webhook_event = JsonSerializer.Deserialize<JsonElement>(body)
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(verifyPayload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "PayPal webhook verification failed: {Status} — {Body}",
                    response.StatusCode, responseBody);
                return false;
            }

            var result = JsonSerializer.Deserialize<PayPalWebhookVerifyResponse>(responseBody);
            var isValid = result?.VerificationStatus == "SUCCESS";

            if (!isValid)
            {
                _logger.LogWarning("PayPal webhook signature invalid: {Status}", result?.VerificationStatus);
            }

            return isValid;
        }, cancellationToken);
    }
}

// =============================================================================
// SETTINGS
// =============================================================================

public class PayPalSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string WebhookId { get; set; } = string.Empty;
    public bool Sandbox { get; set; } = true;

    /// <summary>
    /// Computed base URL from sandbox flag
    /// </summary>
    public string BaseUrl => Sandbox
        ? "https://api-m.sandbox.paypal.com"
        : "https://api-m.paypal.com";
}

// =============================================================================
// INTERNAL JSON MODELS — PayPal API responses
// =============================================================================

internal class PayPalTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }
}

internal class PayPalOrderResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("links")]
    public List<PayPalLink>? Links { get; set; }

    [JsonPropertyName("purchase_units")]
    public List<PayPalPurchaseUnit>? PurchaseUnits { get; set; }

    [JsonPropertyName("payer")]
    public PayPalPayerInfo? Payer { get; set; }

    [JsonPropertyName("create_time")]
    public DateTime? CreateTime { get; set; }
}

internal class PayPalLink
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("rel")]
    public string Rel { get; set; } = string.Empty;

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
}

internal class PayPalPurchaseUnit
{
    [JsonPropertyName("amount")]
    public PayPalAmount? Amount { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("payments")]
    public PayPalPayments? Payments { get; set; }
}

internal class PayPalAmount
{
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; } = "USD";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "0.00";
}

internal class PayPalPayments
{
    [JsonPropertyName("captures")]
    public List<PayPalCapture>? Captures { get; set; }
}

internal class PayPalCapture
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public PayPalAmount? Amount { get; set; }
}

internal class PayPalRefundResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public PayPalAmount? Amount { get; set; }
}

internal class PayPalPayerInfo
{
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("payer_id")]
    public string? PayerId { get; set; }
}

internal class PayPalWebhookVerifyResponse
{
    [JsonPropertyName("verification_status")]
    public string VerificationStatus { get; set; } = string.Empty;
}
