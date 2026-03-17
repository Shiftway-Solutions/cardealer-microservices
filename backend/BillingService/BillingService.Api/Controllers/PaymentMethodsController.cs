using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BillingService.Api.Controllers;

/// <summary>
/// Controlador para métodos de pago de usuarios individuales (no dealers).
/// Provee endpoints para gestionar tarjetas guardadas para compras de vehículos.
/// 
/// NOTE: El almacenamiento persistente de métodos de pago para usuarios individuales
/// está planificado en una fase futura. Por ahora, GET devuelve lista vacía para que
/// el UI muestre el estado vacío en lugar de un 404/500.
/// </summary>
[ApiController]
[Route("api/payment-methods")]
[Authorize]
public class PaymentMethodsController : ControllerBase
{
    private readonly ILogger<PaymentMethodsController> _logger;

    public PaymentMethodsController(ILogger<PaymentMethodsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Obtiene los métodos de pago guardados del usuario actual.
    /// </summary>
    [HttpGet]
    public ActionResult<UserPaymentMethodsListDto> GetPaymentMethods()
    {
        var userId = GetCurrentUserId();
        _logger.LogDebug("GetPaymentMethods called for user {UserId}", userId);

        // Payment method persistence for individual users is planned for a future sprint.
        // Return empty list so UI renders gracefully (empty-state component) instead of crashing.
        return Ok(new UserPaymentMethodsListDto
        {
            Methods = new List<UserPaymentMethodDto>(),
            DefaultMethodId = null,
            Total = 0,
            ExpiredCount = 0,
            ExpiringSoonCount = 0
        });
    }

    /// <summary>
    /// Agrega un nuevo método de pago para el usuario actual.
    /// Feature pendiente de implementación completa (tokenización con gateways locales).
    /// </summary>
    [HttpPost]
    public IActionResult AddPaymentMethod([FromBody] object request)
    {
        _logger.LogInformation("AddPaymentMethod called — feature pending full implementation");
        return StatusCode(501, new { error = "La funcionalidad de agregar métodos de pago estará disponible próximamente." });
    }

    /// <summary>
    /// Establece un método de pago como predeterminado.
    /// </summary>
    [HttpPost("{paymentMethodId}/default")]
    public IActionResult SetDefault(string paymentMethodId)
    {
        _logger.LogWarning("SetDefault called for non-existent payment method {Id}", paymentMethodId);
        return NotFound(new { error = "Método de pago no encontrado." });
    }

    /// <summary>
    /// Elimina un método de pago guardado.
    /// </summary>
    [HttpDelete("{paymentMethodId}")]
    public IActionResult DeletePaymentMethod(string paymentMethodId)
    {
        _logger.LogWarning("DeletePaymentMethod called for non-existent payment method {Id}", paymentMethodId);
        return NotFound(new { error = "Método de pago no encontrado." });
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
// DTOs — User payment methods (different from dealer Stripe PaymentMethodInfo)
// ============================================================================

/// <summary>Response para GET /api/payment-methods</summary>
public sealed class UserPaymentMethodsListDto
{
    public List<UserPaymentMethodDto> Methods { get; init; } = new();
    public string? DefaultMethodId { get; init; }
    public int Total { get; init; }
    public int ExpiredCount { get; init; }
    public int ExpiringSoonCount { get; init; }
}

/// <summary>Información de un método de pago guardado por un usuario individual</summary>
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

/// <summary>Datos de tarjeta para un método de pago de usuario</summary>
public sealed class UserCardInfoDto
{
    public string Brand { get; init; } = string.Empty;
    public string Last4 { get; init; } = string.Empty;
    public int ExpMonth { get; init; }
    public int ExpYear { get; init; }
    public string? CardHolderName { get; init; }
}
