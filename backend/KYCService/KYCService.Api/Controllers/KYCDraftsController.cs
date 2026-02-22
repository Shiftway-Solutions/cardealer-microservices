using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using KYCService.Application.DTOs;
using KYCService.Domain.Entities;
using KYCService.Domain.Interfaces;

namespace KYCService.Api.Controllers;

/// <summary>
/// Controlador para borradores de KYC (autosave del wizard del frontend).
/// Permite guardar progreso parcial y recuperarlo al recargar.
/// Un usuario solo puede tener un borrador activo.
/// Ruta: api/KYCProfiles/draft/...
/// </summary>
[ApiController]
[Route("api/KYCProfiles")]
[Authorize]
public class KYCDraftsController : ControllerBase
{
    private readonly IKYCProfileDraftRepository _draftRepo;
    private readonly ILogger<KYCDraftsController> _logger;

    public KYCDraftsController(
        IKYCProfileDraftRepository draftRepo,
        ILogger<KYCDraftsController> logger)
    {
        _draftRepo = draftRepo;
        _logger = logger;
    }

    /// <summary>
    /// Guardar o actualizar borrador KYC (upsert).
    /// El frontend llama esto onStep y cada 30 segundos.
    /// </summary>
    [HttpPost("draft")]
    public async Task<ActionResult<KYCProfileDraftDto>> UpsertDraft(
        [FromBody] UpsertKYCProfileDraftRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "User ID not found in token" });

        // Validar que FormData es JSON válido
        try
        {
            using var doc = JsonDocument.Parse(request.FormData);
        }
        catch (JsonException)
        {
            return BadRequest(new { error = "FormData must be valid JSON" });
        }

        if (request.CurrentStep < 1 || request.CurrentStep > 10)
            return BadRequest(new { error = "CurrentStep must be between 1 and 10" });

        var draft = new KYCProfileDraft
        {
            UserId = userId,
            CurrentStep = request.CurrentStep,
            FormData = request.FormData
        };

        var saved = await _draftRepo.UpsertAsync(draft, cancellationToken);

        return Ok(MapToDto(saved));
    }

    /// <summary>
    /// Obtener el borrador activo de un usuario.
    /// El frontend llama esto al cargar el wizard de verificación.
    /// </summary>
    [HttpGet("draft/{userId:guid}")]
    public async Task<ActionResult<KYCProfileDraftDto>> GetDraft(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new { error = "User ID not found in token" });

        // Solo el dueño o un admin pueden ver el borrador
        var isAdmin = User.HasClaim("account_type", "4");
        if (currentUserId != userId && !isAdmin)
            return Forbid();

        var draft = await _draftRepo.GetByUserIdAsync(userId, cancellationToken);
        if (draft is null)
            return NotFound(new { error = "No active draft found" });

        return Ok(MapToDto(draft));
    }

    /// <summary>
    /// Eliminar el borrador de un usuario (al completar o cancelar).
    /// </summary>
    [HttpDelete("draft/{userId:guid}")]
    public async Task<ActionResult> DeleteDraft(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new { error = "User ID not found in token" });

        var isAdmin = User.HasClaim("account_type", "4");
        if (currentUserId != userId && !isAdmin)
            return Forbid();

        var deleted = await _draftRepo.DeleteByUserIdAsync(userId, cancellationToken);
        if (!deleted)
            return NotFound(new { error = "No draft found to delete" });

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private static KYCProfileDraftDto MapToDto(KYCProfileDraft draft) => new()
    {
        Id = draft.Id,
        UserId = draft.UserId,
        CurrentStep = draft.CurrentStep,
        FormData = draft.FormData,
        CreatedAt = draft.CreatedAt,
        UpdatedAt = draft.UpdatedAt,
        ExpiresAt = draft.ExpiresAt
    };
}
