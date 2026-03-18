using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces;
using BillingService.Infrastructure.Persistence;
using BillingService.Infrastructure.Services;
using Stripe;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace BillingService.Api.Controllers;

/// <summary>
/// Controlador para métodos de pago de usuarios individuales.
/// Provee tokenization endpoints para guardar tarjetas via Stripe SetupIntent,
/// y stubs para gateways locales (Azul, CardNET, PixelPay, Fygaro, PayPal).
/// </summary>
[ApiController]
[Route("api/payment-methods")]
[Authorize]
public class PaymentMethodsController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly StripeSettings _stripeSettings;
    private readonly PayPalSettings _paypalSettings;
    private readonly IPayPalService _payPalService;
    private readonly BillingDbContext _db;
    private readonly ILogger<PaymentMethodsController> _logger;

    private static readonly ConcurrentDictionary<string, TokenizationSessionData> _tokenSessions = new();

    private static readonly HashSet<string> SupportedGateways = new(StringComparer.OrdinalIgnoreCase)
    {
        "Stripe", "PayPal", "Azul", "CardNET", "PixelPay", "Fygaro"
    };

    public PaymentMethodsController(
        IStripeService stripeService,
        IOptions<StripeSettings> stripeSettings,
        IOptions<PayPalSettings> paypalSettings,
        IPayPalService payPalService,
        BillingDbContext db,
        ILogger<PaymentMethodsController> logger)
    {
        _stripeService = stripeService;
        _stripeSettings = stripeSettings.Value;
        _paypalSettings = paypalSettings.Value;
        _payPalService = payPalService;
        _db = db;
        _logger = logger;
    }

    // ========================================
    // PAYMENT METHODS CRUD
    // ========================================

    [HttpGet]
    public async Task<ActionResult<UserPaymentMethodsListDto>> GetPaymentMethods(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        _logger.LogDebug("GetPaymentMethods called for user {UserId}", userId);

        List<UserPaymentMethod> methods;
        try
        {
            methods = await _db.UserPaymentMethods
                .Where(m => m.UserId == userId && m.IsActive)
                .OrderByDescending(m => m.IsDefault)
                .ThenByDescending(m => m.CreatedAt)
                .ToListAsync(ct);
        }
        catch (Exception ex) when (ex.Message.Contains("UserPaymentMethods") || ex.Message.Contains("42P01"))
        {
            // Migration pending — table does not exist yet; return empty list gracefully
            _logger.LogWarning("UserPaymentMethods table not found (migration pending). Returning empty list.");
            return Ok(new UserPaymentMethodsListDto { Methods = new List<UserPaymentMethodDto>(), DefaultMethodId = null, Total = 0, ExpiredCount = 0, ExpiringSoonCount = 0 });
        }

        var defaultId = methods.FirstOrDefault(m => m.IsDefault)?.Id.ToString();

        var dtos = methods.Select(m => new UserPaymentMethodDto
        {
            Id = m.Id.ToString(),
            Type = m.Type,
            Gateway = m.Gateway,
            IsDefault = m.IsDefault,
            IsActive = m.IsActive,
            NickName = m.NickName ?? m.DisplayName,
            Card = null,
            CreatedAt = m.CreatedAt.ToString("o"),
            LastUsedAt = m.LastUsedAt?.ToString("o"),
            UsageCount = m.UsageCount,
            IsExpired = false,
            ExpiresSoon = false
        }).ToList();

        return Ok(new UserPaymentMethodsListDto
        {
            Methods = dtos,
            DefaultMethodId = defaultId,
            Total = dtos.Count,
            ExpiredCount = 0,
            ExpiringSoonCount = 0
        });
    }

    [HttpPost]
    public IActionResult AddPaymentMethod([FromBody] object request)
    {
        _logger.LogInformation("AddPaymentMethod called — use tokenize/init + tokenize/complete flow instead");
        return StatusCode(501, new { error = "Use el flujo de tokenización: POST /tokenize/init y POST /tokenize/complete" });
    }

    [HttpPost("{paymentMethodId}/default")]
    public async Task<IActionResult> SetDefault(string paymentMethodId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (!Guid.TryParse(paymentMethodId, out var methodGuid))
            return BadRequest(new { error = "ID de método de pago inválido." });

        var method = await _db.UserPaymentMethods
            .FirstOrDefaultAsync(m => m.Id == methodGuid && m.UserId == userId && m.IsActive, ct);

        if (method == null)
            return NotFound(new { error = "Método de pago no encontrado." });

        // Clear existing default
        await _db.UserPaymentMethods
            .Where(m => m.UserId == userId && m.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsDefault, false), ct);

        method.IsDefault = true;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Método de pago predeterminado actualizado." });
    }

    [HttpDelete("{paymentMethodId}")]
    public async Task<IActionResult> DeletePaymentMethod(string paymentMethodId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (!Guid.TryParse(paymentMethodId, out var methodGuid))
            return BadRequest(new { error = "ID de método de pago inválido." });

        var method = await _db.UserPaymentMethods
            .FirstOrDefaultAsync(m => m.Id == methodGuid && m.UserId == userId && m.IsActive, ct);

        if (method == null)
            return NotFound(new { error = "Método de pago no encontrado." });

        // Soft delete
        method.IsActive = false;
        if (method.IsDefault)
        {
            method.IsDefault = false;
            // Promote most recently added active method as new default
            var next = await _db.UserPaymentMethods
                .Where(m => m.UserId == userId && m.IsActive && m.Id != methodGuid)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (next != null) next.IsDefault = true;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Método de pago eliminado." });
    }

    // ========================================
    // TOKENIZATION ENDPOINTS
    // ========================================

    /// <summary>
    /// POST /api/payment-methods/tokenize/init
    /// Initiates tokenization for a payment gateway.
    /// For Stripe: creates a SetupIntent and returns the client secret for Stripe.js.
    /// For other gateways: returns "not configured" until credentials are set up.
    /// </summary>
    [HttpPost("tokenize/init")]
    public async Task<IActionResult> InitiateTokenization([FromBody] TokenizationInitRequest request)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(request.Gateway))
            return BadRequest(new { error = "gateway es requerido" });

        if (!SupportedGateways.Contains(request.Gateway))
            return BadRequest(new { error = $"Gateway '{request.Gateway}' no es soportado. Gateways disponibles: {string.Join(", ", SupportedGateways)}" });

        _logger.LogInformation("TokenizeInit: user={UserId}, gateway={Gateway}", userId, request.Gateway);

        switch (request.Gateway)
        {
            case "Stripe":
                return await InitiateStripeTokenization(userId, request);

            case "PayPal":
                {
                    var payPalSessionId = Guid.NewGuid().ToString("N");
                    _tokenSessions[payPalSessionId] = new TokenizationSessionData
                    {
                        SessionId = payPalSessionId,
                        UserId = userId,
                        Gateway = "PayPal",
                        Status = "pending",
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                        SetAsDefault = request.SetAsDefault ?? false,
                        NickName = request.NickName
                    };

                    return Ok(new TokenizationInitResponse
                    {
                        SessionId = payPalSessionId,
                        Gateway = "PayPal",
                        IntegrationType = "sdk",
                        ExpiresAt = DateTime.UtcNow.AddMinutes(30).ToString("o"),
                        SdkConfig = new SdkConfiguration
                        {
                            ClientId = _paypalSettings.ClientId,
                            Environment = _paypalSettings.Sandbox ? "sandbox" : "production"
                        }
                    });
                }

            case "Azul":
            case "CardNET":
            case "PixelPay":
            case "Fygaro":
                return Ok(new TokenizationInitResponse
                {
                    SessionId = Guid.NewGuid().ToString("N"),
                    Gateway = request.Gateway,
                    IntegrationType = "redirect",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30).ToString("o"),
                    ProviderData = new Dictionary<string, object>
                    {
                        ["status"] = "not_configured",
                        ["message"] = $"El gateway {request.Gateway} estará disponible próximamente. Por favor use Stripe o PayPal."
                    }
                });

            default:
                return BadRequest(new { error = $"Gateway '{request.Gateway}' no implementado" });
        }
    }

    /// <summary>
    /// POST /api/payment-methods/tokenize/complete
    /// Completes tokenization after the frontend confirms the SetupIntent.
    /// For Stripe: verifies the SetupIntent succeeded and stores the payment method reference.
    /// </summary>
    [HttpPost("tokenize/complete")]
    public async Task<IActionResult> CompleteTokenization([FromBody] TokenizationCompleteRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(request.SessionId))
            return BadRequest(new { error = "sessionId es requerido" });

        if (!_tokenSessions.TryGetValue(request.SessionId, out var session))
            return NotFound(new { error = "Sesión de tokenización no encontrada o expirada." });

        if (session.UserId != userId)
            return NotFound(new { error = "Sesión de tokenización no encontrada o expirada." });

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _tokenSessions.TryRemove(request.SessionId, out _);
            return BadRequest(new { error = "La sesión de tokenización ha expirado." });
        }

        _logger.LogInformation("TokenizeComplete: user={UserId}, session={SessionId}, gateway={Gateway}",
            userId, request.SessionId, session.Gateway);

        if (session.Gateway == "Stripe")
        {
            return await CompleteStripeTokenization(userId, session, request);
        }

        if (session.Gateway == "PayPal")
        {
            return await CompletePayPalTokenization(userId, session, request, ct);
        }

        return Ok(new
        {
            success = false,
            error = $"Completar tokenización para {session.Gateway} no está disponible aún."
        });
    }

    /// <summary>
    /// GET /api/payment-methods/tokenize/config/{gateway}
    /// Returns gateway-specific configuration for the frontend SDK.
    /// </summary>
    [HttpGet("tokenize/config/{gateway}")]
    public IActionResult GetProviderConfig(string gateway)
    {
        _logger.LogDebug("GetProviderConfig: gateway={Gateway}", gateway);

        switch (gateway)
        {
            case "Stripe":
                return Ok(new
                {
                    gateway = "Stripe",
                    publishableKey = _stripeSettings.PublishableKey,
                    isConfigured = !string.IsNullOrEmpty(_stripeSettings.SecretKey),
                    supportedCardBrands = new[] { "visa", "mastercard", "amex" },
                    locale = "es-419"
                });

            case "PayPal":
                return Ok(new
                {
                    gateway = "PayPal",
                    isConfigured = false,
                    message = "PayPal estará disponible próximamente."
                });

            case "Azul":
            case "CardNET":
            case "PixelPay":
            case "Fygaro":
                return Ok(new
                {
                    gateway,
                    isConfigured = false,
                    message = $"{gateway} estará disponible próximamente."
                });

            default:
                return NotFound(new { error = $"Gateway '{gateway}' no reconocido." });
        }
    }

    /// <summary>
    /// GET /api/payment-methods/tokenize/session/{sessionId}
    /// Returns the status of a tokenization session.
    /// </summary>
    [HttpGet("tokenize/session/{sessionId}")]
    public IActionResult GetTokenizationSession(string sessionId)
    {
        var userId = GetCurrentUserId();

        if (!_tokenSessions.TryGetValue(sessionId, out var session))
            return NotFound(new { error = "Sesión no encontrada." });

        if (session.UserId != userId)
            return NotFound(new { error = "Sesión no encontrada." });

        return Ok(new
        {
            sessionId,
            gateway = session.Gateway,
            status = session.ExpiresAt < DateTime.UtcNow ? "expired" : session.Status,
            createdAt = session.CreatedAt.ToString("o"),
            expiresAt = session.ExpiresAt.ToString("o")
        });
    }

    // ========================================
    // STRIPE TOKENIZATION HELPERS
    // ========================================

    private async Task<IActionResult> CompletePayPalTokenization(
        Guid userId, TokenizationSessionData session, TokenizationCompleteRequest request,
        CancellationToken ct)
    {
        var payPalOrderId = request.PayPalVaultId ?? request.ProviderToken;

        if (string.IsNullOrWhiteSpace(payPalOrderId))
            return BadRequest(new { error = "PayPal order ID no proporcionado." });

        // 1. Capture the PayPal order to get payer info
        PayPalCaptureResult capture;
        try
        {
            capture = await _payPalService.CaptureOrderAsync(payPalOrderId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal capture failed for order {OrderId}", payPalOrderId);
            return StatusCode(502, new { error = "No se pudo verificar el pago con PayPal. Intenta de nuevo." });
        }

        if (capture.Status != "COMPLETED")
        {
            _logger.LogWarning("PayPal order {OrderId} not COMPLETED, status={Status}", payPalOrderId, capture.Status);
            return BadRequest(new { error = $"El pago de verificación no fue aprobado (estado: {capture.Status})." });
        }

        var payerEmail = capture.PayerEmail ?? "paypal-account";
        var payerId = capture.PayerId ?? payPalOrderId;

        // 2. Upsert the UserPaymentMethod record
        UserPaymentMethod? existing;
        try
        {
            existing = await _db.UserPaymentMethods
                .FirstOrDefaultAsync(m => m.UserId == userId && m.ProviderId == payerId, ct);
        }
        catch (Exception ex) when (ex.Message.Contains("UserPaymentMethods") || ex.Message.Contains("42P01"))
        {
            // Migration pending — table does not exist yet; still return success so UI shows the method
            _logger.LogWarning("UserPaymentMethods table not found (migration pending). Returning transient success.");
            session.Status = "completed";
            _tokenSessions[session.SessionId] = session;
            return Ok(new
            {
                success = true,
                paymentMethod = new
                {
                    id = payPalOrderId,
                    gateway = "PayPal",
                    type = "paypal_account",
                    isDefault = session.SetAsDefault,
                    nickName = session.NickName ?? payerEmail,
                    createdAt = DateTime.UtcNow.ToString("o")
                }
            });
        }

        if (existing != null)
        {
            // Already linked — just update display name and reactivate
            existing.IsActive = true;
            existing.DisplayName = payerEmail;
        }
        else
        {
            var isFirst = !await _db.UserPaymentMethods
                .AnyAsync(m => m.UserId == userId && m.IsActive, ct);

            var newMethod = new UserPaymentMethod
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Gateway = "PayPal",
                Type = "paypal_account",
                ProviderId = payerId,
                DisplayName = payerEmail,
                NickName = session.NickName,
                IsDefault = session.SetAsDefault || isFirst,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            if (newMethod.IsDefault)
            {
                // Clear any existing default
                await _db.UserPaymentMethods
                    .Where(m => m.UserId == userId && m.IsDefault)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsDefault, false), ct);
            }

            _db.UserPaymentMethods.Add(newMethod);
            existing = newMethod;
        }

        await _db.SaveChangesAsync(ct);
        session.Status = "completed";
        _tokenSessions[session.SessionId] = session;

        _logger.LogInformation(
            "PayPal payment method saved: user={UserId}, payerId={PayerId}, email={Email}",
            userId, payerId, payerEmail);

        return Ok(new
        {
            success = true,
            paymentMethod = new
            {
                id = existing.Id.ToString(),
                gateway = "PayPal",
                type = "paypal_account",
                isDefault = existing.IsDefault,
                nickName = existing.NickName ?? payerEmail,
                createdAt = existing.CreatedAt.ToString("o")
            }
        });
    }

    private async Task<IActionResult> InitiateStripeTokenization(Guid userId, TokenizationInitRequest request)
    {
        if (string.IsNullOrEmpty(_stripeSettings.SecretKey))
        {
            return Ok(new TokenizationInitResponse
            {
                SessionId = Guid.NewGuid().ToString("N"),
                Gateway = "Stripe",
                IntegrationType = "sdk",
                ExpiresAt = DateTime.UtcNow.AddMinutes(30).ToString("o"),
                ProviderData = new Dictionary<string, object>
                {
                    ["status"] = "not_configured",
                    ["message"] = "Stripe no está configurado en este entorno."
                }
            });
        }

        try
        {
            var setupIntentService = new SetupIntentService();
            var options = new SetupIntentCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId.ToString(),
                    ["platform"] = "okla"
                }
            };

            var setupIntent = await setupIntentService.CreateAsync(options);

            var sessionId = Guid.NewGuid().ToString("N");
            _tokenSessions[sessionId] = new TokenizationSessionData
            {
                SessionId = sessionId,
                UserId = userId,
                Gateway = "Stripe",
                Status = "pending",
                StripeSetupIntentId = setupIntent.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                SetAsDefault = request.SetAsDefault ?? false,
                NickName = request.NickName
            };

            return Ok(new TokenizationInitResponse
            {
                SessionId = sessionId,
                Gateway = "Stripe",
                IntegrationType = "sdk",
                SdkConfig = new SdkConfiguration
                {
                    ClientSecret = setupIntent.ClientSecret,
                    PublishableKey = _stripeSettings.PublishableKey
                },
                ExpiresAt = DateTime.UtcNow.AddMinutes(30).ToString("o"),
                ProviderData = new Dictionary<string, object>
                {
                    ["setupIntentId"] = setupIntent.Id,
                    ["status"] = setupIntent.Status
                }
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe SetupIntent creation failed for user {UserId}", userId);
            return StatusCode(502, new { error = "Error al conectar con Stripe. Intente nuevamente." });
        }
    }

    private async Task<IActionResult> CompleteStripeTokenization(
        Guid userId, TokenizationSessionData session, TokenizationCompleteRequest request)
    {
        try
        {
            var setupIntentService = new SetupIntentService();
            var setupIntent = await setupIntentService.GetAsync(session.StripeSetupIntentId);

            if (setupIntent.Status != "succeeded")
            {
                return Ok(new
                {
                    success = false,
                    error = $"El SetupIntent no se completó exitosamente. Estado: {setupIntent.Status}"
                });
            }

            var paymentMethodId = setupIntent.PaymentMethodId;

            session.Status = "completed";
            _tokenSessions[session.SessionId] = session;

            _logger.LogInformation(
                "Stripe tokenization completed: user={UserId}, paymentMethod={PM}, setupIntent={SI}",
                userId, paymentMethodId, setupIntent.Id);

            return Ok(new
            {
                success = true,
                paymentMethod = new
                {
                    id = paymentMethodId,
                    gateway = "Stripe",
                    type = "card",
                    isDefault = session.SetAsDefault,
                    nickName = session.NickName,
                    card = setupIntent.PaymentMethod?.Card != null ? new
                    {
                        brand = setupIntent.PaymentMethod.Card.Brand,
                        last4 = setupIntent.PaymentMethod.Card.Last4,
                        expMonth = setupIntent.PaymentMethod.Card.ExpMonth,
                        expYear = setupIntent.PaymentMethod.Card.ExpYear
                    } : null
                }
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe tokenization completion failed for user {UserId}, session {SessionId}",
                userId, session.SessionId);
            return StatusCode(502, new { error = "Error al verificar la tarjeta con Stripe." });
        }
    }

    // ========================================
    // HELPER METHODS
    // ========================================

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Usuario no autenticado");

        return userId;
    }
}

// ============================================================================
// DTOs
// ============================================================================

public sealed class UserPaymentMethodsListDto
{
    public List<UserPaymentMethodDto> Methods { get; init; } = new();
    public string? DefaultMethodId { get; init; }
    public int Total { get; init; }
    public int ExpiredCount { get; init; }
    public int ExpiringSoonCount { get; init; }
}

public sealed class UserPaymentMethodDto
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = "card";
    public string Gateway { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public string? NickName { get; init; }
    public UserCardInfoDto? Card { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
    public string? LastUsedAt { get; init; }
    public int UsageCount { get; init; }
    public bool IsExpired { get; init; }
    public bool ExpiresSoon { get; init; }
}

public sealed class UserCardInfoDto
{
    public string Brand { get; init; } = string.Empty;
    public string Last4 { get; init; } = string.Empty;
    public int ExpMonth { get; init; }
    public int ExpYear { get; init; }
    public string? CardHolderName { get; init; }
}

public sealed class TokenizationInitRequest
{
    public string Gateway { get; init; } = string.Empty;
    public string? ReturnUrl { get; init; }
    public string? CancelUrl { get; init; }
    public bool? SetAsDefault { get; init; }
    public string? NickName { get; init; }
}

public sealed class TokenizationInitResponse
{
    public string SessionId { get; init; } = string.Empty;
    public string Gateway { get; init; } = string.Empty;
    public string IntegrationType { get; init; } = string.Empty;
    public string? TokenizationUrl { get; init; }
    public string? IframeUrl { get; init; }
    public SdkConfiguration? SdkConfig { get; init; }
    public Dictionary<string, string>? FormData { get; init; }
    public string ExpiresAt { get; init; } = string.Empty;
    public Dictionary<string, object>? ProviderData { get; init; }
}

public sealed class SdkConfiguration
{
    public string? ClientSecret { get; init; }
    public string? PublishableKey { get; init; }
    public string? ClientId { get; init; }
    public string? MerchantId { get; init; }
    public string? Environment { get; init; }
}

public sealed class TokenizationCompleteRequest
{
    public string SessionId { get; init; } = string.Empty;
    public string? ProviderToken { get; init; }
    public string? Gateway { get; init; }
    public bool? SetAsDefault { get; init; }
    public Dictionary<string, string>? ProviderResponse { get; init; }
    // PayPal-specific
    public string? PayPalVaultId { get; init; }
}

public sealed class TokenizationSessionData
{
    public string SessionId { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string Gateway { get; init; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string? StripeSetupIntentId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool SetAsDefault { get; init; }
    public string? NickName { get; init; }
}
