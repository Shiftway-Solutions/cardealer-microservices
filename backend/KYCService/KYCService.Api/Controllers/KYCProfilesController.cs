using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using KYCService.Application.Commands;
using KYCService.Application.Queries;
using KYCService.Application.DTOs;
using KYCService.Domain.Entities;
using CarDealer.Shared.Configuration;

namespace KYCService.Api.Controllers;

/// <summary>
/// Controlador para gestión de perfiles KYC
/// Según Ley 155-17 de Prevención de Lavado de Activos
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class KYCProfilesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<KYCProfilesController> _logger;
    private readonly IConfigurationServiceClient _configClient;

    public KYCProfilesController(
        IMediator mediator,
        ILogger<KYCProfilesController> logger,
        IConfigurationServiceClient configClient)
    {
        _mediator = mediator;
        _logger = logger;
        _configClient = configClient;
    }

    /// <summary>
    /// Get effective KYC verification settings from ConfigurationService (admin panel).
    /// Used by frontend to display current limits and toggles.
    /// SECURITY: Admin/Compliance only - exposes internal thresholds.
    /// </summary>
    [HttpGet("settings")]
    [Authorize(Policy = "AdminOrCompliance")]
    public async Task<ActionResult> GetSettings()
    {
        var settings = new
        {
            MaxVerificationAttempts = await _configClient.GetIntAsync("kyc.max_verification_attempts", 3),
            VerificationTimeoutMinutes = await _configClient.GetIntAsync("kyc.verification_timeout_minutes", 30),
            DocumentExpirationDays = await _configClient.GetIntAsync("kyc.document_expiration_days", 365),
            HighConfidenceThreshold = await _configClient.GetIntAsync("kyc.high_confidence_threshold", 95),
            FaceMatchThreshold = await _configClient.GetIntAsync("kyc.face_match_threshold", 80),
            RequireLivenessCheck = await _configClient.IsEnabledAsync("kyc.require_liveness_check", defaultValue: true),
            AutoApproveHighConfidence = await _configClient.IsEnabledAsync("kyc.auto_approve_high_confidence", defaultValue: false),
        };

        return Ok(settings);
    }

    /// <summary>
    /// Obtener todos los perfiles KYC con paginación y filtros
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOrCompliance")]
    public async Task<ActionResult<PaginatedResult<KYCProfileSummaryDto>>> GetProfiles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] KYCStatus? status = null,
        [FromQuery] RiskLevel? riskLevel = null,
        [FromQuery] bool? isPEP = null)
    {
        // SECURITY: Clamp pagination to prevent data exfiltration
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);
        var query = new GetKYCProfilesQuery
        {
            Page = page,
            PageSize = pageSize,
            Status = status,
            RiskLevel = riskLevel,
            IsPEP = isPEP
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Obtener perfil KYC por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<KYCProfileDto>> GetById(Guid id)
    {
        var userId = GetUserIdFromClaims();
        var isAdmin = IsAdminOrCompliance();
        var result = await _mediator.Send(new GetKYCProfileByIdQuery(id, userId, isAdmin));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Obtener perfil KYC por ID de usuario
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [Authorize]
    public async Task<ActionResult<KYCProfileDto>> GetByUserId(Guid userId)
    {
        // IDOR Protection (Ley 172-13 Art. 31): Non-admin users can only access their own profile
        var requestingUserId = GetUserIdFromClaims();
        if (!IsAdminOrCompliance() && requestingUserId != userId)
        {
            return Forbid();
        }

        var result = await _mediator.Send(new GetKYCProfileByUserIdQuery(userId));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Obtener perfil KYC por número de documento
    /// </summary>
    [HttpGet("document/{documentNumber}")]
    [Authorize(Policy = "AdminOrCompliance")]
    public async Task<ActionResult<KYCProfileDto>> GetByDocument(string documentNumber)
    {
        var result = await _mediator.Send(new GetKYCProfileByDocumentQuery(documentNumber));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Crear nuevo perfil KYC
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<KYCProfileDto>> Create([FromBody] CreateKYCProfileCommand command)
    {
        var result = await _mediator.Send(command);
        _logger.LogInformation("KYC Profile created for user {UserId}", command.UserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Actualizar perfil KYC
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<KYCProfileDto>> Update(Guid id, [FromBody] UpdateKYCProfileCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Enviar perfil KYC para revisión
    /// Cambia el estado de Pending/InProgress a UnderReview
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [Authorize]
    public async Task<ActionResult<KYCProfileDto>> Submit(Guid id)
    {
        var result = await _mediator.Send(new SubmitKYCForReviewCommand { Id = id });
        _logger.LogInformation("KYC Profile {Id} submitted for review", id);
        return Ok(result);
    }

    /// <summary>
    /// Aprobar perfil KYC
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "AdminOrCompliance")]
    public async Task<ActionResult<KYCProfileDto>> Approve(Guid id, [FromBody] ApproveKYCProfileCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await _mediator.Send(command);
        _logger.LogInformation("KYC Profile {Id} approved by {ApprovedBy}", id, command.ApprovedBy);
        return Ok(result);
    }

    /// <summary>
    /// Rechazar perfil KYC
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "AdminOrCompliance")]
    public async Task<ActionResult<KYCProfileDto>> Reject(Guid id, [FromBody] RejectKYCProfileCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await _mediator.Send(command);
        _logger.LogInformation("KYC Profile {Id} rejected: {Reason}", id, command.RejectionReason);
        return Ok(result);
    }

    /// <summary>
    /// Obtener perfiles pendientes de revisión
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Policy = "AdminOrCompliance")]
    public async Task<ActionResult<PaginatedResult<KYCProfileSummaryDto>>> GetPending(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        // SECURITY: Clamp pagination
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);
        var result = await _mediator.Send(new GetPendingKYCProfilesQuery { Page = page, PageSize = pageSize });
        return Ok(result);
    }

    /// <summary>
    /// Obtener perfiles próximos a expirar
    /// </summary>
    [HttpGet("expiring")]
    [Authorize(Policy = "AdminOrCompliance")]
    public async Task<ActionResult<PaginatedResult<KYCProfileSummaryDto>>> GetExpiring(
        [FromQuery] int daysUntilExpiry = 30,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // SECURITY: Clamp pagination
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);
        var result = await _mediator.Send(new GetExpiringKYCProfilesQuery
        {
            DaysUntilExpiry = daysUntilExpiry,
            Page = page,
            PageSize = pageSize
        });
        return Ok(result);
    }

    /// <summary>
    /// Obtener estadísticas KYC
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Policy = "AdminOrCompliance")]
    public async Task<ActionResult<KYCStatisticsDto>> GetStatistics()
    {
        var result = await _mediator.Send(new GetKYCStatisticsQuery());
        return Ok(result);
    }

    #region Helper Methods

    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value
                       ?? User.FindFirst("userId")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private bool IsAdminOrCompliance()
    {
        var accountType = User.FindFirst("account_type")?.Value;
        return accountType == "4" || accountType == "5"
            || accountType == "admin" || accountType == "platform_employee";
    }

    #endregion
}
