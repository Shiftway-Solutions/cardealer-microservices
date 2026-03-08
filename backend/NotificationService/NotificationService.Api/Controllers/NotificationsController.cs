// NotificationService.Api\Controllers\NotificationsController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Application.UseCases.SendEmailNotification;
using NotificationService.Application.UseCases.SendSmsNotification;
using NotificationService.Application.UseCases.SendPushNotification;
using NotificationService.Application.UseCases.SendWhatsAppNotification;
using NotificationService.Application.UseCases.SendAdminAlert;
using NotificationService.Application.UseCases.GetNotificationStatus;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller for sending notifications (email, SMS, push, WhatsApp)
/// All endpoints require authentication to prevent abuse (OWASP A01:2021)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("email")]
    public async Task<ActionResult<SendEmailNotificationResponse>> SendEmail([FromBody] SendEmailNotificationRequest request)
    {
        var command = new SendEmailNotificationCommand(request);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("sms")]
    public async Task<ActionResult<SendSmsNotificationResponse>> SendSms([FromBody] SendSmsNotificationRequest request)
    {
        var command = new SendSmsNotificationCommand(request);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("push")]
    public async Task<ActionResult<SendPushNotificationResponse>> SendPush([FromBody] SendPushNotificationRequest request)
    {
        var command = new SendPushNotificationCommand(request);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Send a WhatsApp message (free-form or template-based).
    /// Reads provider config from ConfigurationService (whatsapp.provider = twilio|meta|mock).
    /// </summary>
    [HttpPost("whatsapp")]
    public async Task<ActionResult<SendWhatsAppNotificationResponse>> SendWhatsApp(
        [FromBody] SendWhatsAppNotificationCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Send a WhatsApp template message.
    /// </summary>
    [HttpPost("whatsapp/template")]
    public async Task<ActionResult<SendWhatsAppNotificationResponse>> SendWhatsAppTemplate(
        [FromBody] SendWhatsAppTemplateRequest request)
    {
        var command = new SendWhatsAppNotificationCommand(
            To: request.To,
            TemplateName: request.TemplateName,
            TemplateParameters: request.Parameters,
            LanguageCode: request.LanguageCode ?? "es");
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Send an admin alert to configured channels (email, SMS, Teams, Slack).
    /// Reads admin alert toggles and channels from ConfigurationService.
    /// </summary>
    [HttpPost("admin-alert")]
    [Authorize(Policy = "NotificationServiceAdmin")]
    public async Task<ActionResult<SendAdminAlertResponse>> SendAdminAlert(
        [FromBody] SendAdminAlertCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<GetNotificationStatusResponse>> GetStatus(Guid id)
    {
        try
        {
            // ✅ Usar el namespace completo para evitar ambigüedad
            var query = new Application.UseCases.GetNotificationStatus.GetNotificationStatusQuery(id);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (ErrorService.Shared.Exceptions.NotFoundException)
        {
            return NotFound(new { Message = $"Notification with ID {id} not found" });
        }
    }
}

/// <summary>
/// Request DTO for WhatsApp template messages.
/// </summary>
public record SendWhatsAppTemplateRequest(
    string To,
    string TemplateName,
    Dictionary<string, string>? Parameters = null,
    string? LanguageCode = "es"
);