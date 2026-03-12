using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.UseCases.DealerEmployees;

namespace UserService.Api.Controllers;

/// <summary>
/// Controller for handling invitation acceptance (public endpoints)
/// </summary>
[ApiController]
[Route("api/invitations")]
[Produces("application/json")]
public class InvitationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InvitationsController> _logger;

    public InvitationsController(IMediator mediator, ILogger<InvitationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get invitation details by token (before accepting)
    /// </summary>
    /// <remarks>
    /// Returns basic invitation info to show on the acceptance page.
    /// Does not require authentication.
    /// </remarks>
    [HttpGet("{token}")]
    [ProducesResponseType(typeof(InvitationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<ActionResult<InvitationDetailsDto>> GetInvitationDetails(string token)
    {
        _logger.LogInformation("Getting invitation details for token ...{TokenSuffix}", token.Length > 8 ? token[^8..] : "***");

        var result = await _mediator.Send(new GetInvitationDetailsQuery(token));

        if (result == null)
            return NotFound(new { error = "Invitación no encontrada" });

        if (result.IsExpired)
            return StatusCode(StatusCodes.Status410Gone, new { error = "Esta invitación ha expirado" });

        return Ok(result);
    }

    /// <summary>
    /// Accept an invitation to join a dealer team
    /// </summary>
    /// <remarks>
    /// - If the user already exists, they just join the team
    /// - If the user doesn't exist, a new account is created with the provided credentials
    /// </remarks>
    [HttpPost("{token}/accept")]
    [ProducesResponseType(typeof(AcceptInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<ActionResult<AcceptInvitationResponse>> AcceptInvitation(
        string token,
        [FromBody] AcceptInvitationRequest request)
    {
        _logger.LogInformation("Processing invitation acceptance for token ...{TokenSuffix}", token.Length > 8 ? token[^8..] : "***");

        var command = new AcceptInvitationCommand(
            Token: token,
            Password: request.Password,
            FirstName: request.FirstName,
            LastName: request.LastName,
            Phone: request.Phone
        );

        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Decline an invitation
    /// </summary>
    [HttpPost("{token}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeclineInvitation(string token)
    {
        _logger.LogInformation("Declining invitation for token ...{TokenSuffix}", token.Length > 8 ? token[^8..] : "***");

        await _mediator.Send(new DeclineInvitationCommand(token));

        return NoContent();
    }
}

// ============================================================================
// REQUEST DTOs
// ============================================================================

/// <summary>
/// Request body for accepting an invitation
/// </summary>
public class AcceptInvitationRequest
{
    /// <summary>
    /// Password for new users (required if user doesn't exist)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// First name (required if user doesn't exist)
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name (required if user doesn't exist)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Phone number (optional)
    /// </summary>
    public string? Phone { get; set; }
}

// ============================================================================
// RESPONSE DTOs
// ============================================================================
