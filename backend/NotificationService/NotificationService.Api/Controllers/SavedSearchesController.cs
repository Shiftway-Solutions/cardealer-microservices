using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces.Repositories;
using System.Security.Claims;
using System.Text.Json;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SavedSearchesController : ControllerBase
{
    private readonly ILogger<SavedSearchesController> _logger;
    private readonly ISavedSearchRepository _repository;

    public SavedSearchesController(
        ILogger<SavedSearchesController> logger,
        ISavedSearchRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        _logger.LogInformation("Getting saved searches for user {UserId}, page={Page}", userId, page);

        var items = await _repository.GetByUserIdAsync(userId, page, pageSize);
        var totalCount = await _repository.GetCountByUserIdAsync(userId);

        return Ok(new
        {
            Items = items.Select(MapToResponse),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var search = await _repository.GetByIdAndUserAsync(id, userId);
        if (search == null)
            return NotFound(new { error = "Saved search not found." });

        return Ok(MapToResponse(search));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSavedSearchRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        _logger.LogInformation("Creating saved search: {Name}", request.Name);

        var search = SavedSearch.Create(
            userId: userId,
            name: request.Name,
            criteria: request.Criteria,
            notifyOnNewResults: request.NotifyOnNewResults ?? true,
            notifyByEmail: request.NotifyByEmail ?? true,
            notifyByPush: request.NotifyByPush ?? true,
            notificationFrequency: request.NotificationFrequency ?? "daily");

        await _repository.AddAsync(search);

        return CreatedAtAction(nameof(GetById), new { id = search.Id }, MapToResponse(search));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSavedSearchRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var search = await _repository.GetByIdAndUserAsync(id, userId);
        if (search == null)
            return NotFound(new { error = "Saved search not found." });

        _logger.LogInformation("Updating saved search {SearchId}", id);

        if (!string.IsNullOrWhiteSpace(request.Name)) search.Name = request.Name;
        if (request.Criteria != null) search.CriteriaJson = JsonSerializer.Serialize(request.Criteria);
        if (request.NotifyOnNewResults.HasValue) search.NotifyOnNewResults = request.NotifyOnNewResults.Value;
        if (request.NotifyByEmail.HasValue) search.NotifyByEmail = request.NotifyByEmail.Value;
        if (request.NotifyByPush.HasValue) search.NotifyByPush = request.NotifyByPush.Value;
        if (!string.IsNullOrWhiteSpace(request.NotificationFrequency)) search.NotificationFrequency = request.NotificationFrequency;
        search.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(search);

        return Ok(MapToResponse(search));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        if (!await _repository.ExistsAsync(id, userId))
            return NotFound(new { error = "Saved search not found." });

        _logger.LogInformation("Deleting saved search {SearchId}", id);
        await _repository.DeleteAsync(id);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private static SavedSearchResponse MapToResponse(SavedSearch search)
    {
        SavedSearchCriteria? criteria = null;
        try { criteria = JsonSerializer.Deserialize<SavedSearchCriteria>(search.CriteriaJson); }
        catch { /* Gracefully handle corrupt JSON */ }

        return new SavedSearchResponse(
            Id: search.Id,
            UserId: search.UserId,
            Name: search.Name,
            Criteria: criteria,
            NotifyOnNewResults: search.NotifyOnNewResults,
            NotifyByEmail: search.NotifyByEmail,
            NotifyByPush: search.NotifyByPush,
            NotificationFrequency: search.NotificationFrequency,
            MatchCount: search.MatchCount,
            LastMatchAt: search.LastMatchAt,
            LastNotifiedAt: search.LastNotifiedAt,
            CreatedAt: search.CreatedAt,
            UpdatedAt: search.UpdatedAt
        );
    }
}
