using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Privacy;
using UserService.Application.UseCases.Privacy.CancelAccountDeletion;
using UserService.Application.UseCases.Privacy.ConfirmAccountDeletion;
using UserService.Application.UseCases.Privacy.GetAccountDeletionStatus;
using UserService.Application.UseCases.Privacy.GetCommunicationPreferences;
using UserService.Application.UseCases.Privacy.GetExportStatus;
using UserService.Application.UseCases.Privacy.GetPrivacyRequestHistory;
using UserService.Application.UseCases.Privacy.GetUserDataSummary;
using UserService.Application.UseCases.Privacy.GetUserFullData;
using UserService.Application.UseCases.Privacy.RequestAccountDeletion;
using UserService.Application.UseCases.Privacy.RequestDataExport;
using UserService.Application.UseCases.Privacy.DownloadExportData;
using UserService.Application.UseCases.Privacy.UpdateCommunicationPreferences;
using UserService.Domain.Entities.Privacy;

namespace UserService.Api.Controllers;

/// <summary>
/// Controller para gestión de derechos ARCO (Ley 172-13 República Dominicana)
/// - Acceso: Ver datos personales
/// - Rectificación: Corregir datos (manejado por UsersController)
/// - Cancelación: Eliminar cuenta
/// - Oposición: Gestionar preferencias de comunicación
/// - Portabilidad: Exportar datos
/// </summary>
[ApiController]
[Route("api/privacy")]
[Authorize]
public class PrivacyController : ControllerBase
{
    private readonly IMediator _mediator;

    public PrivacyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId()
    {
        // El JWT de AuthService usa ClaimTypes.NameIdentifier como claim principal
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("sub")
                       ?? User.FindFirst("userId");
        return userIdClaim != null ? Guid.Parse(userIdClaim.Value) : Guid.Empty;
    }

    private string? GetUserEmail()
    {
        // El JWT de AuthService incluye el email en ClaimTypes.Email
        var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email);
        return emailClaim?.Value;
    }

    #region Derecho de Acceso

    /// <summary>
    /// Obtener resumen de datos del usuario (Acceso ARCO)
    /// </summary>
    /// <returns>Resumen de datos personales</returns>
    [HttpGet("my-data")]
    [ProducesResponseType(typeof(UserDataSummaryDto), 200)]
    public async Task<ActionResult<UserDataSummaryDto>> GetMyDataSummary()
    {
        var userId = GetUserId();
        var query = new GetUserDataSummaryQuery(userId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Obtener todos los datos del usuario (Acceso completo ARCO)
    /// </summary>
    /// <returns>Todos los datos personales del usuario</returns>
    [HttpGet("my-data/full")]
    [ProducesResponseType(typeof(UserFullDataDto), 200)]
    public async Task<ActionResult<UserFullDataDto>> GetMyFullData()
    {
        var userId = GetUserId();
        var query = new GetUserFullDataQuery(userId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    #endregion

    #region Derecho de Portabilidad

    /// <summary>
    /// Solicitar exportación de datos (Portabilidad ARCO)
    /// </summary>
    /// <param name="request">Opciones de exportación</param>
    /// <returns>Información de la solicitud</returns>
    [HttpPost("export/request")]
    [ProducesResponseType(typeof(DataExportRequestResponseDto), 202)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<DataExportRequestResponseDto>> RequestDataExport([FromBody] RequestDataExportDto request)
    {
        var userId = GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var command = new RequestDataExportCommand(
            UserId: userId,
            Format: request.Format,
            IncludeProfile: request.IncludeProfile,
            IncludeActivity: request.IncludeActivity,
            IncludeMessages: request.IncludeMessages,
            IncludeFavorites: request.IncludeFavorites,
            IncludeTransactions: request.IncludeTransactions,
            IpAddress: ipAddress,
            UserAgent: userAgent
        );

        var result = await _mediator.Send(command);
        return Accepted(result);
    }

    /// <summary>
    /// Obtener estado de exportación
    /// </summary>
    /// <returns>Estado de la última solicitud de exportación</returns>
    [HttpGet("export/status")]
    [ProducesResponseType(typeof(DataExportStatusDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<DataExportStatusDto>> GetExportStatus()
    {
        var userId = GetUserId();
        var query = new GetExportStatusQuery(userId);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { message = "No hay solicitud de exportación pendiente" });

        return Ok(result);
    }

    /// <summary>
    /// Descargar archivo de exportación
    /// </summary>
    /// <param name="token">Token de descarga</param>
    /// <returns>Archivo de datos</returns>
    [HttpGet("export/download/{token}")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(410)] // Gone - expired
    public async Task<IActionResult> DownloadExport(string token)
    {
        var result = await _mediator.Send(new DownloadExportDataQuery(token));

        if (result == null)
            return NotFound(new { message = "Token de descarga no válido o no encontrado" });

        if (result.IsExpired)
            return StatusCode(410, new { message = "El enlace de descarga ha expirado (24h). Solicite una nueva exportación." });

        if (string.IsNullOrEmpty(result.FilePath) || !System.IO.File.Exists(result.FilePath))
            return NotFound(new { message = "El archivo de exportación no se encontró en el servidor" });

        var stream = System.IO.File.OpenRead(result.FilePath);
        return File(stream, "application/zip", result.FileName);
    }

    #endregion

    #region Derecho de Cancelación

    /// <summary>
    /// Solicitar eliminación de cuenta (Cancelación ARCO)
    /// </summary>
    /// <param name="request">Motivo de eliminación</param>
    /// <returns>Información de la solicitud</returns>
    [HttpPost("delete-account/request")]
    [ProducesResponseType(typeof(AccountDeletionResponseDto), 202)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<AccountDeletionResponseDto>> RequestAccountDeletion([FromBody] RequestAccountDeletionDto request)
    {
        var userId = GetUserId();
        var userEmail = GetUserEmail();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var command = new RequestAccountDeletionCommand(
            UserId: userId,
            Email: userEmail,
            Reason: request.Reason,
            OtherReason: request.OtherReason,
            Feedback: request.Feedback,
            IpAddress: ipAddress,
            UserAgent: userAgent
        );

        var result = await _mediator.Send(command);
        return Accepted(result);
    }

    /// <summary>
    /// Confirmar eliminación de cuenta con código
    /// </summary>
    /// <param name="request">Código de confirmación y contraseña</param>
    /// <returns>Estado de confirmación</returns>
    [HttpPost("delete-account/confirm")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> ConfirmAccountDeletion([FromBody] ConfirmAccountDeletionDto request)
    {
        var userId = GetUserId();
        var command = new ConfirmAccountDeletionCommand(
            UserId: userId,
            ConfirmationCode: request.ConfirmationCode,
            Password: request.Password
        );

        var result = await _mediator.Send(command);

        if (!result)
            return BadRequest(new { message = "Código de confirmación inválido o contraseña incorrecta" });

        return Ok(new { message = "Eliminación confirmada. Tu cuenta será eliminada al finalizar el período de gracia." });
    }

    /// <summary>
    /// Cancelar solicitud de eliminación (dentro del período de gracia)
    /// </summary>
    /// <returns>Estado de cancelación</returns>
    [HttpPost("delete-account/cancel")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> CancelAccountDeletion()
    {
        var userId = GetUserId();
        var command = new CancelAccountDeletionCommand(userId);
        var result = await _mediator.Send(command);

        if (!result)
            return BadRequest(new { message = "No se puede cancelar la solicitud. Puede que ya haya sido procesada o no exista." });

        return Ok(new { message = "Solicitud de eliminación cancelada exitosamente. Tu cuenta se mantendrá activa." });
    }

    /// <summary>
    /// Obtener estado de solicitud de eliminación
    /// </summary>
    /// <returns>Estado de la solicitud</returns>
    [HttpGet("delete-account/status")]
    [ProducesResponseType(typeof(AccountDeletionStatusDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<AccountDeletionStatusDto>> GetAccountDeletionStatus()
    {
        var userId = GetUserId();
        var query = new GetAccountDeletionStatusQuery(userId);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { message = "No hay solicitud de eliminación pendiente" });

        return Ok(result);
    }

    #endregion

    #region Derecho de Oposición

    /// <summary>
    /// Obtener preferencias de comunicación
    /// </summary>
    /// <returns>Preferencias actuales</returns>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(CommunicationPreferencesDto), 200)]
    public async Task<ActionResult<CommunicationPreferencesDto>> GetCommunicationPreferences()
    {
        var userId = GetUserId();
        var query = new GetCommunicationPreferencesQuery(userId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Actualizar preferencias de comunicación (Oposición ARCO)
    /// </summary>
    /// <param name="request">Nuevas preferencias</param>
    /// <returns>Preferencias actualizadas</returns>
    [HttpPut("preferences")]
    [ProducesResponseType(typeof(CommunicationPreferencesDto), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<CommunicationPreferencesDto>> UpdateCommunicationPreferences([FromBody] UpdateCommunicationPreferencesDto request)
    {
        var userId = GetUserId();
        var command = new UpdateCommunicationPreferencesCommand(userId, request);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Opt-out de todo el marketing (acceso rápido)
    /// </summary>
    /// <returns>Confirmación</returns>
    [HttpPost("preferences/unsubscribe-all")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> UnsubscribeFromAllMarketing()
    {
        var userId = GetUserId();
        var command = new UpdateCommunicationPreferencesCommand(userId, new UpdateCommunicationPreferencesDto(
            EmailNewsletter: false,
            EmailPromotions: false,
            SmsPromotions: false,
            WhatsAppMarketing: false,
            WhatsAppPriceAlerts: false,
            PushRecommendations: false,
            AllowRetargeting: false
        ));

        await _mediator.Send(command);
        return Ok(new { message = "Te has dado de baja de todas las comunicaciones de marketing" });
    }

    #endregion

    #region Historial de Solicitudes

    /// <summary>
    /// Obtener historial de solicitudes ARCO
    /// </summary>
    /// <param name="page">Página</param>
    /// <param name="pageSize">Tamaño de página</param>
    /// <returns>Lista de solicitudes</returns>
    [HttpGet("requests")]
    [ProducesResponseType(typeof(PrivacyRequestsListDto), 200)]
    public async Task<ActionResult<PrivacyRequestsListDto>> GetPrivacyRequestHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        var query = new GetPrivacyRequestHistoryQuery(userId, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    #endregion

    #region Información Legal

    /// <summary>
    /// Obtener información sobre derechos ARCO (público)
    /// </summary>
    /// <returns>Información sobre derechos</returns>
    [HttpGet("rights-info")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    public ActionResult GetARCORightsInfo()
    {
        return Ok(new
        {
            law = "Ley 172-13 de Protección de Datos Personales (República Dominicana)",
            rights = new[]
            {
                new
                {
                    name = "Acceso",
                    description = "Derecho a conocer qué datos personales tenemos sobre ti y cómo los utilizamos.",
                    endpoint = "/api/privacy/my-data"
                },
                new
                {
                    name = "Rectificación",
                    description = "Derecho a corregir datos personales incorrectos o desactualizados.",
                    endpoint = "/api/users/{id}"
                },
                new
                {
                    name = "Cancelación",
                    description = "Derecho a solicitar la eliminación de tus datos personales y cuenta.",
                    endpoint = "/api/privacy/delete-account/request"
                },
                new
                {
                    name = "Oposición",
                    description = "Derecho a oponerte al tratamiento de tus datos para ciertos fines.",
                    endpoint = "/api/privacy/preferences"
                },
                new
                {
                    name = "Portabilidad",
                    description = "Derecho a recibir una copia de tus datos en formato estructurado.",
                    endpoint = "/api/privacy/export/request"
                }
            },
            contact = new
            {
                email = "privacidad@okla.com.do",
                phone = "+1 809 123 4567",
                address = "Santo Domingo, República Dominicana"
            },
            responseTimes = new
            {
                access = "5 días hábiles",
                rectification = "Inmediato",
                cancellation = "15 días (período de gracia)",
                opposition = "Inmediato",
                portability = "24-48 horas"
            }
        });
    }

    #endregion
}
