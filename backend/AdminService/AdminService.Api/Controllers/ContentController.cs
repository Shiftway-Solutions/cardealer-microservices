using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminService.Application.UseCases.Content;

namespace AdminService.Api.Controllers;

/// <summary>
/// Controller for platform content management endpoints (/admin/contenido page)
/// </summary>
[ApiController]
[Route("api/admin/content")]
[Produces("application/json")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ContentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ContentController> _logger;

    public ContentController(IMediator mediator, ILogger<ContentController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Get content overview (banners + pages + blog)</summary>
    [HttpGet]
    public async Task<IActionResult> GetContentOverview()
    {
        _logger.LogInformation("Getting content overview");
        var result = await _mediator.Send(new GetContentOverviewQuery());
        return Ok(result);
    }

    /// <summary>Get all banners</summary>
    [HttpGet("banners")]
    public async Task<IActionResult> GetBanners()
    {
        var result = await _mediator.Send(new GetBannersQuery());
        return Ok(result);
    }

    /// <summary>Create a banner (stub)</summary>
    [HttpPost("banners")]
    public IActionResult CreateBanner([FromBody] object data)
    {
        _logger.LogInformation("Create banner requested");
        return Ok(new { message = "Banner creado exitosamente" });
    }

    /// <summary>Update a banner (stub)</summary>
    [HttpPut("banners/{id}")]
    public IActionResult UpdateBanner(string id, [FromBody] object data)
    {
        _logger.LogInformation("Update banner {Id} requested", id);
        return Ok(new { message = "Banner actualizado exitosamente" });
    }

    /// <summary>Delete a banner</summary>
    [HttpDelete("banners/{id}")]
    public async Task<IActionResult> DeleteBanner(string id)
    {
        await _mediator.Send(new DeleteBannerCommand(id));
        return Ok(new { message = "Banner eliminado exitosamente" });
    }

    /// <summary>Get static pages</summary>
    [HttpGet("pages")]
    public async Task<IActionResult> GetPages()
    {
        var result = await _mediator.Send(new GetStaticPagesQuery());
        return Ok(result);
    }

    /// <summary>Get blog posts</summary>
    [HttpGet("blog")]
    public async Task<IActionResult> GetBlog()
    {
        var result = await _mediator.Send(new GetBlogPostsQuery());
        return Ok(result);
    }
}
