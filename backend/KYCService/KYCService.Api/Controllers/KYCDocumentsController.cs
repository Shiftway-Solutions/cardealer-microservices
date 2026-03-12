using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using KYCService.Application.Commands;
using KYCService.Application.Queries;
using KYCService.Application.DTOs;

namespace KYCService.Api.Controllers;

/// <summary>
/// Controlador para documentos y verificaciones KYC
/// </summary>
[ApiController]
[Route("api/kyc")]
public class KYCDocumentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<KYCDocumentsController> _logger;

    public KYCDocumentsController(IMediator mediator, ILogger<KYCDocumentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Obtener documentos de un perfil KYC
    /// </summary>
    [HttpGet("profiles/{profileId:guid}/documents")]
    [Authorize]
    public async Task<ActionResult<List<KYCDocumentDto>>> GetDocuments(Guid profileId)
    {
        var userId = GetUserIdFromClaims();
        var isAdmin = IsAdminOrCompliance();
        var result = await _mediator.Send(new GetKYCDocumentsQuery(profileId, userId, isAdmin));
        return Ok(result);
    }

    /// <summary>
    /// Subir documento KYC
    /// </summary>
    [HttpPost("profiles/{profileId:guid}/documents")]
    [Authorize]
    public async Task<ActionResult<KYCDocumentDto>> UploadDocument(
        Guid profileId,
        [FromBody] UploadKYCDocumentCommand command)
    {
        _logger.LogInformation("=== CONTROLLER: UploadDocument START === ProfileId: {ProfileId}, DocumentType: {Type}, DocumentName: {DocumentName}",
            profileId, command.Type, command.DocumentName);

        if (profileId != command.KYCProfileId)
        {
            _logger.LogWarning("Profile ID mismatch: URL={UrlId}, Command={CommandId}", profileId, command.KYCProfileId);
            return BadRequest("Profile ID mismatch");
        }

        _logger.LogInformation("Sending command to handler: StorageKey={StorageKey}, FileUrl={FileUrl}", command.StorageKey, command.FileUrl);

        var result = await _mediator.Send(command);

        _logger.LogInformation("=== CONTROLLER: UploadDocument SUCCESS === Document uploaded with ID: {DocumentId}", result.Id);
        return CreatedAtAction(nameof(GetDocuments), new { profileId }, result);
    }

    /// <summary>
    /// Verificar documento KYC
    /// </summary>
    [HttpPost("documents/{documentId:guid}/verify")]
    [Authorize(Policy = "AdminOrCompliance")]
    public async Task<ActionResult<KYCDocumentDto>> VerifyDocument(
        Guid documentId,
        [FromBody] VerifyKYCDocumentCommand command)
    {
        if (documentId != command.Id)
            return BadRequest("Document ID mismatch");

        var result = await _mediator.Send(command);
        _logger.LogInformation("Document {DocumentId} verification: {Status}",
            documentId, command.Approved ? "Approved" : "Rejected");
        return Ok(result);
    }

    /// <summary>
    /// Obtener URL fresca (pre-firmada) para un documento KYC
    /// La URL retornada es válida por 1 hora
    /// Si no se puede obtener una URL fresca, retorna la URL almacenada
    /// </summary>
    [HttpGet("documents/{documentId:guid}/url")]
    [Authorize]
    public async Task<ActionResult<DocumentUrlDto>> GetDocumentUrl(Guid documentId)
    {
        try
        {
            _logger.LogInformation("Getting document URL for document: {DocumentId}", documentId);
            var userId = GetUserIdFromClaims();
            var isAdmin = IsAdminOrCompliance();
            var result = await _mediator.Send(new GetKYCDocumentUrlQuery(documentId, userId, isAdmin));
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("IDOR attempt: User tried to access document {DocumentId}: {Message}", documentId, ex.Message);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to get document URL for {DocumentId}: {Message}", documentId, ex.Message);
            return NotFound(new { error = ex.Message, documentId = documentId.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting document URL for {DocumentId}", documentId);
            return StatusCode(500, new { error = "An unexpected error occurred", documentId = documentId.ToString() });
        }
    }

    /// <summary>
    /// Obtener verificaciones de un perfil KYC
    /// </summary>
    [HttpGet("profiles/{profileId:guid}/verifications")]
    [Authorize]
    public async Task<ActionResult<List<KYCVerificationDto>>> GetVerifications(Guid profileId)
    {
        var result = await _mediator.Send(new GetKYCVerificationsQuery(profileId));
        return Ok(result);
    }

    /// <summary>
    /// Crear verificación KYC
    /// </summary>
    [HttpPost("profiles/{profileId:guid}/verifications")]
    [Authorize(Policy = "AdminOrCompliance")]
    public async Task<ActionResult<KYCVerificationDto>> CreateVerification(
        Guid profileId,
        [FromBody] CreateKYCVerificationCommand command)
    {
        if (profileId != command.KYCProfileId)
            return BadRequest("Profile ID mismatch");

        var result = await _mediator.Send(command);
        _logger.LogInformation("Verification {Type} created for KYC Profile {ProfileId}: {Passed}",
            command.VerificationType, profileId, command.Passed);
        return Ok(result);
    }

    /// <summary>
    /// Obtener historial de evaluaciones de riesgo
    /// </summary>
    [HttpGet("profiles/{profileId:guid}/risk-assessments")]
    [Authorize(Policy = "AdminOrCompliance")]
    public async Task<ActionResult<List<KYCRiskAssessmentDto>>> GetRiskAssessments(Guid profileId)
    {
        var result = await _mediator.Send(new GetKYCRiskAssessmentsQuery(profileId));
        return Ok(result);
    }

    /// <summary>
    /// Crear evaluación de riesgo KYC
    /// </summary>
    [HttpPost("profiles/{profileId:guid}/risk-assessments")]
    [Authorize(Policy = "AdminOrCompliance")]
    public async Task<ActionResult<KYCRiskAssessmentDto>> AssessRisk(
        Guid profileId,
        [FromBody] AssessKYCRiskCommand command)
    {
        if (profileId != command.KYCProfileId)
            return BadRequest("Profile ID mismatch");

        var result = await _mediator.Send(command);
        _logger.LogInformation("Risk assessment for KYC Profile {ProfileId}: {OldLevel} -> {NewLevel}",
            profileId, result.PreviousLevel, result.NewLevel);
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
