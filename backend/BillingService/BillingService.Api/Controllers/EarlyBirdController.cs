using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces;
using System.Security.Claims;

namespace BillingService.Api.Controllers;

[ApiController]
[Route("api/billing/[controller]")]
public class EarlyBirdController : ControllerBase
{
    private readonly IEarlyBirdRepository _repository;
    private readonly ILogger<EarlyBirdController> _logger;

    public EarlyBirdController(
        IEarlyBirdRepository repository,
        ILogger<EarlyBirdController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el estado Early Bird del usuario actual
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult<EarlyBirdStatusDto>> GetStatus()
    {
        var userId = GetCurrentUserId();
        var member = await _repository.GetByUserIdAsync(userId);

        if (member == null)
        {
            return Ok(new EarlyBirdStatusDto
            {
                IsEnrolled = false,
                HasFounderBadge = false,
                IsInFreePeriod = false,
                RemainingFreeDays = 0,
                Message = "Usuario no inscrito en Early Bird"
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
    [HttpPost("enroll")]
    [Authorize]
    public async Task<ActionResult<EarlyBirdStatusDto>> Enroll([FromBody] EnrollRequest? request = null)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Verificar si ya está inscrito
            if (await _repository.IsUserEnrolledAsync(userId))
            {
                return BadRequest(new { error = "Usuario ya inscrito en Early Bird" });
            }

            var freeMonths = request?.FreeMonths ?? 3; // Default 3 meses
            var member = new EarlyBirdMember(userId, freeMonths);

            await _repository.CreateAsync(member);

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

    /// <summary>
    /// Verifica el estado de Early Bird de un usuario específico (Admin only)
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EarlyBirdStatusDto>> GetUserStatus(Guid userId)
    {
        var member = await _repository.GetByUserIdAsync(userId);

        if (member == null)
        {
            return NotFound(new { error = "Usuario no inscrito en Early Bird" });
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
            BenefitUsedAt = member.BenefitUsedAt
        });
    }

    /// <summary>
    /// Inscribe manualmente a un usuario (Admin only)
    /// </summary>
    [HttpPost("admin/enroll/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EarlyBirdStatusDto>> AdminEnroll(
        Guid userId,
        [FromBody] EnrollRequest? request = null)
    {
        try
        {
            if (await _repository.IsUserEnrolledAsync(userId))
            {
                return BadRequest(new { error = "Usuario ya inscrito en Early Bird" });
            }

            var freeMonths = request?.FreeMonths ?? 3;
            var member = new EarlyBirdMember(userId, freeMonths);

            await _repository.CreateAsync(member);

            _logger.LogInformation(
                "Admin enrolled user {UserId} in Early Bird with {Months} months",
                userId, freeMonths);

            return Ok(new EarlyBirdStatusDto
            {
                IsEnrolled = true,
                HasFounderBadge = true,
                IsInFreePeriod = true,
                RemainingFreeDays = member.GetRemainingFreeDays(),
                EnrolledAt = member.EnrolledAt,
                FreeUntil = member.FreeUntil,
                Message = $"Usuario inscrito exitosamente con {freeMonths} meses gratis"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene estadísticas del programa Early Bird (Admin only)
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EarlyBirdStatsDto>> GetStats()
    {
        var totalEnrolled = await _repository.GetTotalEnrolledCountAsync();
        var activeMembers = await _repository.GetAllActiveAsync();

        return Ok(new EarlyBirdStatsDto
        {
            TotalEnrolled = totalEnrolled,
            ActiveMembers = activeMembers.Count,
            MembersWhoUsedBenefit = totalEnrolled - activeMembers.Count
        });
    }

    // Helper methods
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        return userId;
    }
}

#region DTOs

public record EnrollRequest
{
    public int FreeMonths { get; init; } = 3;
}

public record EarlyBirdStatusDto
{
    public bool IsEnrolled { get; init; }
    public bool HasFounderBadge { get; init; }
    public bool IsInFreePeriod { get; init; }
    public int RemainingFreeDays { get; init; }
    public DateTime? EnrolledAt { get; init; }
    public DateTime? FreeUntil { get; init; }
    public bool HasUsedBenefit { get; init; }
    public DateTime? BenefitUsedAt { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record EarlyBirdStatsDto
{
    public int TotalEnrolled { get; init; }
    public int ActiveMembers { get; init; }
    public int MembersWhoUsedBenefit { get; init; }
}

#endregion
