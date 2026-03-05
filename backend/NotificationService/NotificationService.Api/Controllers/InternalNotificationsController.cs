// NotificationService.Api/Controllers/InternalNotificationsController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Application.UseCases.SendEmailNotification;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Internal controller for service-to-service notification calls within the K8s cluster.
/// AllowAnonymous is safe here because this service is NEVER exposed to the internet —
/// it is only reachable from other pods within the private DOKS network.
/// </summary>
[ApiController]
[Route("api/internal")]
[AllowAnonymous]
public class InternalNotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InternalNotificationsController> _logger;

    public InternalNotificationsController(
        IMediator mediator,
        ILogger<InternalNotificationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Send an email from an internal service (e.g., ChatbotService appointment confirmations).
    /// No JWT required — protected only by the internal K8s network boundary.
    /// </summary>
    [HttpPost("notifications/email")]
    public async Task<ActionResult<SendEmailNotificationResponse>> SendEmail(
        [FromBody] SendEmailNotificationRequest request)
    {
        _logger.LogInformation(
            "Internal email request: To={To}, Subject={Subject}",
            request.To, request.Subject);

        var command = new SendEmailNotificationCommand(request);
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
