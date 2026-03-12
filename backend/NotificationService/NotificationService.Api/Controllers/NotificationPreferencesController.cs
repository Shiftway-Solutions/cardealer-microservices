using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller for managing user notification preferences
/// </summary>
[ApiController]
[Route("api/notifications/preferences")]
[Authorize]
public class NotificationPreferencesController : ControllerBase
{
    private readonly ILogger<NotificationPreferencesController> _logger;

    public NotificationPreferencesController(ILogger<NotificationPreferencesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get notification preferences for the current user/dealer
    /// </summary>
    [HttpGet]
    public ActionResult<List<NotificationPreferenceDto>> GetPreferences()
    {
        // Extract user identity from JWT claims (NEVER trust request headers for identity)
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var dealerId = User.FindFirst("dealerId")?.Value;

        _logger.LogInformation("Getting notification preferences for dealer {DealerId}, user {UserId}",
            dealerId, userId);

        // Return default preferences - in production these would come from the database
        var preferences = GetDefaultPreferences();
        return Ok(preferences);
    }

    /// <summary>
    /// Update a specific notification preference
    /// </summary>
    [HttpPut("{type}")]
    public ActionResult<NotificationPreferenceDto> UpdatePreference(
        string type,
        [FromBody] UpdateNotificationPreferenceRequest request)
    {
        // Extract user identity from JWT claims (NEVER trust request headers for identity)
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var dealerId = User.FindFirst("dealerId")?.Value;

        _logger.LogInformation("Updating notification preference {Type} for dealer {DealerId}, user {UserId}",
            type, dealerId, userId);

        // Find the preference and update it
        var preferences = GetDefaultPreferences();
        var preference = preferences.FirstOrDefault(p => p.Type == type);

        if (preference == null)
        {
            return NotFound(new { Message = $"Preference type '{type}' not found" });
        }

        // Return updated preference
        var updated = preference with
        {
            Enabled = request.Enabled,
            Channels = request.Channels ?? preference.Channels
        };

        return Ok(updated);
    }

    /// <summary>
    /// Bulk update notification preferences
    /// </summary>
    [HttpPut]
    public ActionResult<List<NotificationPreferenceDto>> UpdatePreferences(
        [FromBody] List<UpdateNotificationPreferenceRequest> requests)
    {
        // Extract user identity from JWT claims (NEVER trust request headers for identity)
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var dealerId = User.FindFirst("dealerId")?.Value;

        _logger.LogInformation("Bulk updating {Count} notification preferences for dealer {DealerId}",
            requests.Count, dealerId);

        var preferences = GetDefaultPreferences();
        var updated = new List<NotificationPreferenceDto>();

        foreach (var request in requests)
        {
            var preference = preferences.FirstOrDefault(p => p.Type == request.Type);
            if (preference != null)
            {
                updated.Add(preference with
                {
                    Enabled = request.Enabled,
                    Channels = request.Channels ?? preference.Channels
                });
            }
        }

        return Ok(updated);
    }

    /// <summary>
    /// Reset all preferences to defaults
    /// </summary>
    [HttpPost("reset")]
    public ActionResult<List<NotificationPreferenceDto>> ResetPreferences()
    {
        var dealerId = User.FindFirst("dealerId")?.Value;
        _logger.LogInformation("Resetting notification preferences to defaults for dealer {DealerId}", dealerId);
        return Ok(GetDefaultPreferences());
    }

    private static List<NotificationPreferenceDto> GetDefaultPreferences()
    {
        return new List<NotificationPreferenceDto>
        {
            new(
                Id: "pref_1",
                Type: "new_leads",
                Title: "Nuevos Leads",
                Description: "Recibe notificaciones cuando un cliente potencial contacte sobre un vehículo",
                Enabled: true,
                Channels: new List<string> { "email", "push", "whatsapp" }
            ),
            new(
                Id: "pref_2",
                Type: "vehicle_inquiries",
                Title: "Consultas de Vehículos",
                Description: "Notificaciones sobre preguntas de vehículos en tu inventario",
                Enabled: true,
                Channels: new List<string> { "email", "push" }
            ),
            new(
                Id: "pref_3",
                Type: "daily_summary",
                Title: "Resumen Diario",
                Description: "Recibe un resumen diario de actividad de tu concesionario",
                Enabled: true,
                Channels: new List<string> { "email" }
            ),
            new(
                Id: "pref_4",
                Type: "inventory_alerts",
                Title: "Alertas de Inventario",
                Description: "Notificaciones sobre bajo inventario o vehículos sin actividad",
                Enabled: true,
                Channels: new List<string> { "email", "push" }
            ),
            new(
                Id: "pref_5",
                Type: "system_updates",
                Title: "Actualizaciones del Sistema",
                Description: "Notificaciones sobre nuevas funcionalidades y mantenimientos",
                Enabled: true,
                Channels: new List<string> { "email" }
            ),
            new(
                Id: "pref_6",
                Type: "payment_reminders",
                Title: "Recordatorios de Pago",
                Description: "Recordatorios antes del vencimiento de tu suscripción",
                Enabled: true,
                Channels: new List<string> { "email", "sms" }
            ),
            new(
                Id: "pref_7",
                Type: "marketing",
                Title: "Marketing y Promociones",
                Description: "Ofertas especiales y tips para mejorar tus ventas",
                Enabled: false,
                Channels: new List<string> { "email" }
            )
        };
    }
}
