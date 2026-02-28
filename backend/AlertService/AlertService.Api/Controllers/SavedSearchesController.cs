using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AlertService.Domain.Entities;
using AlertService.Domain.Interfaces;
using CarDealer.Contracts.Events.Alert;
using System.Security.Claims;
using System.Text.Json;

namespace AlertService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SavedSearchesController : ControllerBase
{
    private readonly ISavedSearchRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<SavedSearchesController> _logger;

    public SavedSearchesController(
        ISavedSearchRepository repository,
        IEventPublisher eventPublisher,
        ILogger<SavedSearchesController> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todas las búsquedas guardadas del usuario
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SavedSearchDto>>> GetMySearches()
    {
        var userId = GetCurrentUserId();
        var searches = await _repository.GetByUserIdAsync(userId);

        return Ok(searches.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Obtiene una búsqueda guardada específica
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SavedSearchDto>> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        var search = await _repository.GetByIdAsync(id);

        if (search == null)
            return NotFound();

        if (search.UserId != userId)
            return Forbid();

        return Ok(MapToDto(search));
    }

    /// <summary>
    /// Crea una nueva búsqueda guardada
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SavedSearchDto>> Create([FromBody] CreateSavedSearchRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            var search = new SavedSearch(
                userId,
                request.Name,
                request.SearchCriteria,
                request.SendEmailNotifications,
                request.Frequency);

            await _repository.CreateAsync(search);

            _logger.LogInformation(
                "User {UserId} created saved search {SearchId}: {Name}",
                userId, search.Id, search.Name);

            // Publish event → RabbitMQ → NotificationService sends confirmation email
            await PublishSavedSearchEventAsync(search, "created");

            return CreatedAtAction(
                nameof(GetById),
                new { id = search.Id },
                MapToDto(search));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza el nombre de una búsqueda guardada
    /// </summary>
    [HttpPut("{id:guid}/name")]
    public async Task<ActionResult<SavedSearchDto>> UpdateName(
        Guid id,
        [FromBody] UpdateNameRequest request)
    {
        var userId = GetCurrentUserId();
        var search = await _repository.GetByIdAsync(id);

        if (search == null)
            return NotFound();

        if (search.UserId != userId)
            return Forbid();

        try
        {
            search.UpdateName(request.Name);
            await _repository.UpdateAsync(search);

            return Ok(MapToDto(search));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza los criterios de búsqueda
    /// </summary>
    [HttpPut("{id:guid}/criteria")]
    public async Task<ActionResult<SavedSearchDto>> UpdateCriteria(
        Guid id,
        [FromBody] UpdateCriteriaRequest request)
    {
        var userId = GetCurrentUserId();
        var search = await _repository.GetByIdAsync(id);

        if (search == null)
            return NotFound();

        if (search.UserId != userId)
            return Forbid();

        try
        {
            search.UpdateSearchCriteria(request.SearchCriteria);
            await _repository.UpdateAsync(search);

            return Ok(MapToDto(search));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza configuración de notificaciones
    /// </summary>
    [HttpPut("{id:guid}/notifications")]
    public async Task<ActionResult<SavedSearchDto>> UpdateNotifications(
        Guid id,
        [FromBody] UpdateNotificationsRequest request)
    {
        var userId = GetCurrentUserId();
        var search = await _repository.GetByIdAsync(id);

        if (search == null)
            return NotFound();

        if (search.UserId != userId)
            return Forbid();

        search.UpdateNotificationSettings(request.SendEmailNotifications, request.Frequency);
        await _repository.UpdateAsync(search);

        return Ok(MapToDto(search));
    }

    /// <summary>
    /// Activa una búsqueda guardada
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<SavedSearchDto>> Activate(Guid id)
    {
        var userId = GetCurrentUserId();
        var search = await _repository.GetByIdAsync(id);

        if (search == null)
            return NotFound();

        if (search.UserId != userId)
            return Forbid();

        search.Activate();
        await _repository.UpdateAsync(search);

        // Publish event → RabbitMQ → NotificationService sends reactivation email
        await PublishSavedSearchEventAsync(search, "activated");

        return Ok(MapToDto(search));
    }

    /// <summary>
    /// Desactiva una búsqueda guardada
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<SavedSearchDto>> Deactivate(Guid id)
    {
        var userId = GetCurrentUserId();
        var search = await _repository.GetByIdAsync(id);

        if (search == null)
            return NotFound();

        if (search.UserId != userId)
            return Forbid();

        search.Deactivate();
        await _repository.UpdateAsync(search);

        return Ok(MapToDto(search));
    }

    /// <summary>
    /// Elimina una búsqueda guardada
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var search = await _repository.GetByIdAsync(id);

        if (search == null)
            return NotFound();

        if (search.UserId != userId)
            return Forbid();

        await _repository.DeleteAsync(id);

        return NoContent();
    }

    // Helper methods

    private async Task PublishSavedSearchEventAsync(SavedSearch search, string actionType)
    {
        try
        {
            var searchEvent = new SavedSearchMatchEvent
            {
                SavedSearchId = search.Id,
                UserId = search.UserId,
                SearchName = search.Name,
                SearchCriteria = search.SearchCriteria,
                ActionType = actionType,
                Frequency = search.Frequency.ToString(),
                SearchDescription = BuildSearchDescription(search.SearchCriteria),
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _eventPublisher.PublishAsync(searchEvent);

            _logger.LogInformation(
                "SavedSearch event published: {ActionType} SearchId={SearchId}, UserId={UserId}",
                actionType, search.Id, search.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish saved search event for SearchId={SearchId}. " +
                "The search was saved successfully but notification may not be sent.",
                search.Id);
        }
    }

    private static string BuildSearchDescription(string searchCriteriaJson)
    {
        try
        {
            var criteria = JsonDocument.Parse(searchCriteriaJson);
            var parts = new List<string>();

            if (criteria.RootElement.TryGetProperty("make", out var make) && make.GetString() is string makeVal)
                parts.Add($"Marca: {makeVal}");
            if (criteria.RootElement.TryGetProperty("model", out var model) && model.GetString() is string modelVal)
                parts.Add($"Modelo: {modelVal}");
            if (criteria.RootElement.TryGetProperty("yearMin", out var yearMin))
                parts.Add($"Año desde: {yearMin}");
            if (criteria.RootElement.TryGetProperty("yearMax", out var yearMax))
                parts.Add($"Año hasta: {yearMax}");
            if (criteria.RootElement.TryGetProperty("priceMin", out var priceMin))
                parts.Add($"Precio desde: RD${priceMin}");
            if (criteria.RootElement.TryGetProperty("priceMax", out var priceMax))
                parts.Add($"Precio hasta: RD${priceMax}");
            if (criteria.RootElement.TryGetProperty("condition", out var condition) && condition.GetString() is string condVal)
                parts.Add($"Condición: {condVal}");
            if (criteria.RootElement.TryGetProperty("fuelType", out var fuel) && fuel.GetString() is string fuelVal)
                parts.Add($"Combustible: {fuelVal}");
            if (criteria.RootElement.TryGetProperty("city", out var city) && city.GetString() is string cityVal)
                parts.Add($"Ciudad: {cityVal}");

            return parts.Count > 0 ? string.Join(" • ", parts) : "Búsqueda personalizada";
        }
        catch
        {
            return "Búsqueda personalizada";
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        return userId;
    }

    private static SavedSearchDto MapToDto(SavedSearch search)
    {
        return new SavedSearchDto
        {
            Id = search.Id,
            Name = search.Name,
            SearchCriteria = search.SearchCriteria,
            SendEmailNotifications = search.SendEmailNotifications,
            Frequency = search.Frequency.ToString(),
            LastNotificationSent = search.LastNotificationSent,
            IsActive = search.IsActive,
            CreatedAt = search.CreatedAt,
            UpdatedAt = search.UpdatedAt
        };
    }
}

#region DTOs

public record CreateSavedSearchRequest
{
    public string Name { get; init; } = string.Empty;
    public string SearchCriteria { get; init; } = string.Empty;
    public bool SendEmailNotifications { get; init; } = true;
    public NotificationFrequency Frequency { get; init; } = NotificationFrequency.Daily;
}

public record UpdateNameRequest
{
    public string Name { get; init; } = string.Empty;
}

public record UpdateCriteriaRequest
{
    public string SearchCriteria { get; init; } = string.Empty;
}

public record UpdateNotificationsRequest
{
    public bool SendEmailNotifications { get; init; }
    public NotificationFrequency Frequency { get; init; }
}

public record SavedSearchDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SearchCriteria { get; init; } = string.Empty;
    public bool SendEmailNotifications { get; init; }
    public string Frequency { get; init; } = string.Empty;
    public DateTime? LastNotificationSent { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

#endregion
