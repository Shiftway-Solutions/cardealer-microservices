using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Interfaces.Repositories;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TemplatesController : ControllerBase
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly ITemplateEngine _templateEngine;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(
        INotificationTemplateRepository templateRepository,
        ITemplateEngine templateEngine,
        ILogger<TemplatesController> logger)
    {
        _templateRepository = templateRepository;
        _templateEngine = templateEngine;
        _logger = logger;
    }

    /// <summary>
    /// Create a new notification template
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "NotificationServiceAdmin")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        try
        {
            // Check if template name already exists
            if (await _templateRepository.ExistsAsync(request.Name))
            {
                return BadRequest(new { error = $"Template with name '{request.Name}' already exists" });
            }

            // Validate template content
            if (!_templateEngine.ValidateTemplate(request.Body, out var errors))
            {
                return BadRequest(new { error = "Template validation failed", details = errors });
            }

            var template = NotificationTemplate.Create(
                request.Name,
                request.Subject,
                request.Body,
                request.Type,
                request.Description,
                request.Category,
                User.Identity?.Name
            );

            // Set variables
            if (request.Variables != null)
            {
                foreach (var variable in request.Variables)
                {
                    template.AddVariable(variable.Key, variable.Value);
                }
            }

            // Set tags
            if (!string.IsNullOrWhiteSpace(request.Tags))
            {
                template.Tags = request.Tags;
            }

            // Set preview data
            if (!string.IsNullOrWhiteSpace(request.PreviewData))
            {
                template.PreviewData = request.PreviewData;
            }

            await _templateRepository.AddAsync(template);

            var response = MapToResponse(template);
            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get template by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(Guid id)
    {
        var template = await _templateRepository.GetByIdAsync(id);
        if (template == null)
            return NotFound();

        return Ok(MapToResponse(template));
    }

    /// <summary>
    /// Get template by name
    /// </summary>
    [HttpGet("by-name/{name}")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplateByName(string name)
    {
        var template = await _templateRepository.GetByNameAsync(name);
        if (template == null)
            return NotFound();

        return Ok(MapToResponse(template));
    }

    /// <summary>
    /// Get all templates with optional filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetTemplatesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates([FromQuery] GetTemplatesRequest request)
    {
        IEnumerable<NotificationTemplate> templates;

        if (request.Type.HasValue)
        {
            templates = await _templateRepository.GetByTypeAsync(request.Type.Value);
        }
        else
        {
            templates = await _templateRepository.GetActiveTemplatesAsync();
        }

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            templates = templates.Where(t => t.Category == request.Category);
        }

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            templates = templates.Where(t => t.GetTagsList().Contains(request.Tag));
        }

        if (request.IsActive.HasValue)
        {
            templates = templates.Where(t => t.IsActive == request.IsActive.Value);
        }

        var totalCount = templates.Count();

        // Pagination
        templates = templates
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);

        var response = new GetTemplatesResponse(
            templates.Select(MapToResponse).ToList(),
            totalCount,
            request.PageNumber,
            request.PageSize
        );

        return Ok(response);
    }

    /// <summary>
    /// Update template
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "NotificationServiceAdmin")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateTemplateRequest request)
    {
        try
        {
            var template = await _templateRepository.GetByIdAsync(id);
            if (template == null)
                return NotFound();

            // Validate new body
            if (!_templateEngine.ValidateTemplate(request.Body, out var errors))
            {
                return BadRequest(new { error = "Template validation failed", details = errors });
            }

            // Update properties
            template.Update(request.Subject, request.Body, request.Description, User.Identity?.Name);

            // Update variables
            if (request.Variables != null)
            {
                template.Variables = request.Variables;
            }

            // Update tags
            if (request.Tags != null)
            {
                template.Tags = request.Tags;
            }

            // Update preview data
            if (request.PreviewData != null)
            {
                template.PreviewData = request.PreviewData;
            }

            await _templateRepository.UpdateAsync(template);

            return Ok(MapToResponse(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete template
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "NotificationServiceAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        var template = await _templateRepository.GetByIdAsync(id);
        if (template == null)
            return NotFound();

        await _templateRepository.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Activate template
    /// </summary>
    [HttpPost("{id}/activate")]
    [Authorize(Policy = "NotificationServiceAdmin")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateTemplate(Guid id)
    {
        var template = await _templateRepository.GetByIdAsync(id);
        if (template == null)
            return NotFound();

        template.Activate();
        await _templateRepository.UpdateAsync(template);

        return Ok(MapToResponse(template));
    }

    /// <summary>
    /// Deactivate template
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [Authorize(Policy = "NotificationServiceAdmin")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateTemplate(Guid id)
    {
        var template = await _templateRepository.GetByIdAsync(id);
        if (template == null)
            return NotFound();

        template.Deactivate();
        await _templateRepository.UpdateAsync(template);

        return Ok(MapToResponse(template));
    }

    /// <summary>
    /// Preview template rendering with sample data
    /// </summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(PreviewTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewTemplate([FromBody] PreviewTemplateRequest request)
    {
        try
        {
            string templateContent;
            List<string> availableParameters = new();

            if (!string.IsNullOrWhiteSpace(request.TemplateId))
            {
                if (Guid.TryParse(request.TemplateId, out var templateId))
                {
                    var template = await _templateRepository.GetByIdAsync(templateId);
                    if (template == null)
                        return BadRequest(new { error = "Template not found" });

                    templateContent = template.Body;
                    availableParameters = template.Variables?.Keys.ToList() ?? new List<string>();
                }
                else
                {
                    return BadRequest(new { error = "Invalid template ID format" });
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.TemplateContent))
            {
                templateContent = request.TemplateContent;
            }
            else
            {
                return BadRequest(new { error = "Either TemplateId or TemplateContent must be provided" });
            }

            // Validate template
            var isValid = _templateEngine.ValidateTemplate(templateContent, out var errors);
            var placeholders = _templateEngine.ExtractPlaceholders(templateContent);

            // Check for missing parameters
            var parameters = request.Parameters ?? new Dictionary<string, object>();
            var missingParameters = placeholders.Where(p => !parameters.ContainsKey(p)).ToList();

            // Render if no missing parameters
            string renderedContent = string.Empty;
            if (missingParameters.Count == 0 && parameters.Any())
            {
                renderedContent = templateContent;
                foreach (var param in parameters)
                {
                    renderedContent = renderedContent.Replace($"{{{{{param.Key}}}}}", param.Value?.ToString() ?? string.Empty);
                }
            }
            else
            {
                renderedContent = templateContent;
            }

            var response = new PreviewTemplateResponse(
                renderedContent,
                isValid && missingParameters.Count == 0,
                errors,
                missingParameters,
                availableParameters.Any() ? availableParameters : placeholders
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing template");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Validate template content
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(TemplateValidationResponse), StatusCodes.Status200OK)]
    public IActionResult ValidateTemplate([FromBody] string templateContent)
    {
        var isValid = _templateEngine.ValidateTemplate(templateContent, out var errors);
        var placeholders = _templateEngine.ExtractPlaceholders(templateContent);

        var response = new TemplateValidationResponse(isValid, errors, placeholders);
        return Ok(response);
    }

    /// <summary>
    /// Create a new version of an existing template
    /// </summary>
    [HttpPost("{id}/version")]
    [Authorize(Policy = "NotificationServiceAdmin")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTemplateVersion(Guid id)
    {
        var template = await _templateRepository.GetByIdAsync(id);
        if (template == null)
            return NotFound();

        var newVersion = template.CreateNewVersion(User.Identity?.Name);
        await _templateRepository.AddAsync(newVersion);

        return CreatedAtAction(nameof(GetTemplate), new { id = newVersion.Id }, MapToResponse(newVersion));
    }

    private TemplateResponse MapToResponse(NotificationTemplate template)
    {
        return new TemplateResponse(
            template.Id,
            template.Name,
            template.Subject,
            template.Body,
            template.Type,
            template.IsActive,
            template.CreatedAt,
            template.UpdatedAt,
            template.Variables,
            template.Description,
            template.Category,
            template.Version,
            template.PreviousVersionId,
            template.Tags,
            template.GetTagsList(),
            template.CreatedBy,
            template.UpdatedBy
        );
    }
}
