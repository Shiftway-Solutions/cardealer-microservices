using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminService.Application.DTOs;
using AdminService.Application.UseCases.Moderation;
using System.Security.Claims;

namespace AdminService.Api.Controllers;

/// <summary>
/// Controller for content moderation operations
/// Handles the moderation queue for vehicles, dealers, users, etc.
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ModerationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ModerationController> _logger;

    public ModerationController(IMediator mediator, ILogger<ModerationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get the moderation queue with optional filters
    /// </summary>
    /// <param name="type">Filter by type (vehicle, dealer, user, content)</param>
    /// <param name="priority">Filter by priority (normal, high, urgent)</param>
    /// <param name="status">Filter by status (pending, approved, rejected, escalated)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    [HttpGet("queue")]
    [ProducesResponseType(typeof(PaginatedModerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetModerationQueue(
        [FromQuery] string? type = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var query = new GetModerationQueueQuery(type, priority, status, page, pageSize);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation queue");
            return StatusCode(500, new { Error = "Failed to get moderation queue" });
        }
    }

    /// <summary>
    /// Get moderation statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ModerationStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetModerationStats()
    {
        try
        {
            var query = new GetModerationStatsQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation stats");
            return StatusCode(500, new { Error = "Failed to get moderation stats" });
        }
    }

    /// <summary>
    /// Get a specific moderation item by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ModerationItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetModerationItem(Guid id)
    {
        try
        {
            var query = new GetModerationItemQuery(id);
            var result = await _mediator.Send(query);

            if (result is null)
                return NotFound(new { Error = $"Moderation item {id} not found" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation item {Id}", id);
            return StatusCode(500, new { Error = "Failed to get moderation item" });
        }
    }

    /// <summary>
    /// Process a moderation action on an item
    /// </summary>
    /// <param name="id">Item ID</param>
    /// <param name="request">Action details (action, reason, notes)</param>
    [HttpPost("{id:guid}/action")]
    [ProducesResponseType(typeof(ModerationActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessAction(Guid id, [FromBody] ModerationActionDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Action))
                return BadRequest(new { Error = "Action is required" });

            // Get reviewer ID from claims or use a default for now
            var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var command = new ProcessModerationActionCommand(
                id,
                request.Action,
                reviewerId,
                request.Reason,
                request.Notes);

            var result = await _mediator.Send(command);

            if (!result.Success)
                return NotFound(new { Error = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing moderation action for item {Id}", id);
            return StatusCode(500, new { Error = "Failed to process moderation action" });
        }
    }

    /// <summary>
    /// Approve a moderation item (shortcut endpoint)
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ModerationActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApproveItem(Guid id, [FromBody] ModerationApproveRequest? request = null)
    {
        try
        {
            var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var command = new ProcessModerationActionCommand(
                id,
                "approve",
                reviewerId,
                null,
                request?.Notes);

            var result = await _mediator.Send(command);

            if (!result.Success)
                return NotFound(new { Error = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving moderation item {Id}", id);
            return StatusCode(500, new { Error = "Failed to approve item" });
        }
    }

    /// <summary>
    /// Reject a moderation item (shortcut endpoint)
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ModerationActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RejectItem(Guid id, [FromBody] ModerationRejectRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { Error = "Rejection reason is required" });

            var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var command = new ProcessModerationActionCommand(
                id,
                "reject",
                reviewerId,
                request.Reason,
                request.Notes);

            var result = await _mediator.Send(command);

            if (!result.Success)
                return NotFound(new { Error = result.Message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting moderation item {Id}", id);
            return StatusCode(500, new { Error = "Failed to reject item" });
        }
    }
}

/// <summary>
/// Request to approve a moderation item
/// </summary>
public record ModerationApproveRequest(string? Notes = null);

/// <summary>
/// Request to reject a moderation item
/// </summary>
public record ModerationRejectRequest(string Reason, string? Notes = null);
