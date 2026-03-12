using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces;
using System.Security.Claims;

namespace BillingService.Api.Controllers;

/// <summary>
/// Controlador para operaciones de billing de usuarios individuales (no dealers)
/// Proporciona endpoints para ver transacciones de Azul y estado de Early Bird
/// </summary>
[ApiController]
[Route("api/user-billing")]
[Authorize]
public class UserBillingController : ControllerBase
{
    private readonly IAzulTransactionRepository _azulTransactionRepository;
    private readonly IEarlyBirdRepository _earlyBirdRepository;
    private readonly ILogger<UserBillingController> _logger;

    public UserBillingController(
        IAzulTransactionRepository azulTransactionRepository,
        IEarlyBirdRepository earlyBirdRepository,
        ILogger<UserBillingController> logger)
    {
        _azulTransactionRepository = azulTransactionRepository;
        _earlyBirdRepository = earlyBirdRepository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el resumen de billing del usuario actual
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<UserBillingSummaryDto>> GetSummary(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        // Obtener transacciones del usuario
        var transactions = await _azulTransactionRepository.GetByUserIdAsync(userId);
        var approvedTransactions = transactions.Where(t => t.Status == "Approved").ToList();

        // Obtener estado de Early Bird
        var earlyBirdMember = await _earlyBirdRepository.GetByUserIdAsync(userId);

        var summary = new UserBillingSummaryDto
        {
            TotalTransactions = transactions.Count,
            TotalApproved = approvedTransactions.Count,
            TotalAmount = approvedTransactions.Sum(t => t.Amount),
            Currency = "DOP",
            IsEarlyBirdMember = earlyBirdMember != null,
            EarlyBirdStatus = earlyBirdMember != null ? new EarlyBirdStatusDto
            {
                IsEnrolled = true,
                HasFounderBadge = earlyBirdMember.HasFounderBadge(),
                IsInFreePeriod = earlyBirdMember.IsInFreePeriod(),
                RemainingFreeDays = earlyBirdMember.GetRemainingFreeDays(),
                EnrolledAt = earlyBirdMember.EnrolledAt,
                FreeUntil = earlyBirdMember.FreeUntil
            } : null
        };

        return Ok(summary);
    }

    /// <summary>
    /// Obtiene las transacciones de Azul del usuario actual
    /// </summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<List<UserTransactionDto>>> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var transactions = await _azulTransactionRepository.GetByUserIdAsync(userId);

        // Filtrar por estado si se especifica
        if (!string.IsNullOrEmpty(status))
        {
            transactions = transactions.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Ordenar por fecha descendente y paginar
        var paginatedTransactions = transactions
            .OrderByDescending(t => t.TransactionDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToTransactionDto)
            .ToList();

        return Ok(paginatedTransactions);
    }

    /// <summary>
    /// Obtiene una transacción específica del usuario
    /// </summary>
    [HttpGet("transactions/{transactionId:guid}")]
    public async Task<ActionResult<UserTransactionDto>> GetTransaction(Guid transactionId)
    {
        var userId = GetCurrentUserId();

        var transaction = await _azulTransactionRepository.GetByIdAsync(transactionId);

        if (transaction == null)
            return NotFound(new { error = "Transacción no encontrada" });

        // Verificar que la transacción pertenece al usuario
        if (transaction.UserId != userId)
            return Forbid();

        return Ok(MapToTransactionDto(transaction));
    }

    /// <summary>
    /// Obtiene el estado de Early Bird del usuario
    /// </summary>
    [HttpGet("early-bird")]
    public async Task<ActionResult<EarlyBirdStatusDto>> GetEarlyBirdStatus()
    {
        var userId = GetCurrentUserId();
        var member = await _earlyBirdRepository.GetByUserIdAsync(userId);

        if (member == null)
        {
            return Ok(new EarlyBirdStatusDto
            {
                IsEnrolled = false,
                HasFounderBadge = false,
                IsInFreePeriod = false,
                RemainingFreeDays = 0,
                Message = "No estás inscrito en el programa Early Bird"
            });
        }

        return Ok(new EarlyBirdStatusDto
        {
            IsEnrolled = true,
            HasFounderBadge = member.HasFounderBadge(),
            IsInFreePeriod = member.IsInFreePeriod(),
            RemainingFreeDays = member.GetRemainingFreeDays(),
            EnrolledAt = member.EnrolledAt,
            FreeUntil = member.FreeUntil,
            HasUsedBenefit = member.HasUsedBenefit,
            BenefitUsedAt = member.BenefitUsedAt,
            Message = member.IsInFreePeriod()
                ? $"¡Tienes {member.GetRemainingFreeDays()} días gratis restantes!"
                : member.HasUsedBenefit
                    ? "Beneficio usado - Tienes el badge de Miembro Fundador"
                    : "Período gratuito expirado"
        });
    }

    /// <summary>
    /// Inscribe al usuario actual en el programa Early Bird
    /// </summary>
    [HttpPost("early-bird/enroll")]
    public async Task<ActionResult<EarlyBirdStatusDto>> EnrollEarlyBird([FromBody] EnrollEarlyBirdRequest? request = null)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Verificar si ya está inscrito
            if (await _earlyBirdRepository.IsUserEnrolledAsync(userId))
            {
                return BadRequest(new { error = "Ya estás inscrito en el programa Early Bird" });
            }

            var freeMonths = request?.FreeMonths ?? 3; // Default 3 meses
            var member = new EarlyBirdMember(userId, freeMonths);

            await _earlyBirdRepository.CreateAsync(member);

            _logger.LogInformation(
                "User {UserId} enrolled in Early Bird program with {Months} months free",
                userId, freeMonths);

            return Ok(new EarlyBirdStatusDto
            {
                IsEnrolled = true,
                HasFounderBadge = true,
                IsInFreePeriod = true,
                RemainingFreeDays = member.GetRemainingFreeDays(),
                EnrolledAt = member.EnrolledAt,
                FreeUntil = member.FreeUntil,
                HasUsedBenefit = false,
                Message = $"¡Bienvenido al programa Early Bird! Tienes {freeMonths} meses gratis."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
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
        {
            throw new UnauthorizedAccessException("Usuario no autenticado");
        }

        return userId;
    }

    private static UserTransactionDto MapToTransactionDto(AzulTransaction transaction) => new(
        Id: transaction.Id.ToString(),
        OrderNumber: transaction.OrderNumber,
        Amount: transaction.Amount,
        ITBIS: transaction.ITBIS,
        Total: transaction.Amount + transaction.ITBIS,
        Currency: "DOP",
        Status: transaction.Status,
        StatusDisplay: GetStatusDisplay(transaction.Status),
        AuthorizationCode: transaction.AuthorizationCode,
        TransactionDate: transaction.TransactionDateTime,
        CardBrand: transaction.DataVaultBrand,
        CardLast4: !string.IsNullOrEmpty(transaction.DataVaultToken) ? "****" : null,
        Description: GetTransactionDescription(transaction)
    );

    private static string GetStatusDisplay(string status) => status switch
    {
        "Approved" => "Aprobado",
        "Declined" => "Declinado",
        "Cancelled" => "Cancelado",
        "Error" => "Error",
        _ => status
    };

    private static string GetTransactionDescription(AzulTransaction transaction)
    {
        // Puedes personalizar esto según el tipo de transacción
        return transaction.Status == "Approved"
            ? "Pago procesado exitosamente"
            : transaction.ResponseMessage;
    }
}

// ========================================
// DTOs
// ========================================

public record UserBillingSummaryDto
{
    public int TotalTransactions { get; init; }
    public int TotalApproved { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "DOP";
    public bool IsEarlyBirdMember { get; init; }
    public EarlyBirdStatusDto? EarlyBirdStatus { get; init; }
}

public record UserTransactionDto(
    string Id,
    string OrderNumber,
    decimal Amount,
    decimal ITBIS,
    decimal Total,
    string Currency,
    string Status,
    string StatusDisplay,
    string? AuthorizationCode,
    DateTime TransactionDate,
    string? CardBrand,
    string? CardLast4,
    string Description
);

public record EnrollEarlyBirdRequest
{
    public int FreeMonths { get; init; } = 3;
}

// Nota: EarlyBirdStatusDto ya está definido en EarlyBirdController.cs
