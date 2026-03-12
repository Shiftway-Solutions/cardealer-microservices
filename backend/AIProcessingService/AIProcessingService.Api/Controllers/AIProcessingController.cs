using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AIProcessingService.Application.DTOs;
using AIProcessingService.Application.Features.Commands;
using AIProcessingService.Application.Features.Queries;

namespace AIProcessingService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIProcessingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AIProcessingController> _logger;

    public AIProcessingController(IMediator mediator, ILogger<AIProcessingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Process a single image (segmentation, background replacement, etc.)
    /// </summary>
    [HttpPost("process")]
    [Authorize]
    public async Task<ActionResult<ProcessImageResponse>> ProcessImage([FromBody] ProcessImageRequest request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        var command = new ProcessImageCommand(
            request.VehicleId,
            userId,
            request.ImageUrl,
            request.Type,
            request.Options);

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Process multiple images in batch
    /// </summary>
    [HttpPost("process/batch")]
    [Authorize]
    public async Task<ActionResult<BatchProcessResponse>> ProcessBatch([FromBody] ProcessBatchRequest request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        var command = new ProcessBatchCommand(
            request.VehicleId,
            userId,
            request.ImageUrls,
            request.Type,
            request.Options);

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Generate 360° spin from video or images
    /// </summary>
    [HttpPost("spin360/generate")]
    [Authorize]
    public async Task<ActionResult<ProcessImageResponse>> Generate360Spin([FromBody] Generate360Request request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        var command = new Generate360Command(
            request.VehicleId,
            userId,
            request.SourceType,
            request.VideoUrl,
            request.ImageUrls,
            request.FrameCount,
            request.Options);

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get status of an image processing job
    /// </summary>
    [HttpGet("jobs/{jobId}")]
    [AllowAnonymous] // Allow public access for HTML viewer polling
    public async Task<ActionResult<JobStatusResponse>> GetJobStatus(Guid jobId, CancellationToken ct)
    {
        var query = new GetJobStatusQuery(jobId);
        var result = await _mediator.Send(query, ct);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Get status of a 360° spin job
    /// </summary>
    [HttpGet("spin360/{jobId}")]
    [Authorize]
    public async Task<ActionResult<Spin360StatusResponse>> GetSpin360Status(Guid jobId, CancellationToken ct)
    {
        var query = new GetSpin360StatusQuery(jobId);
        var result = await _mediator.Send(query, ct);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Get all processed images for a vehicle
    /// </summary>
    [HttpGet("vehicles/{vehicleId}/images")]
    public async Task<ActionResult<VehicleImagesResponse>> GetVehicleProcessedImages(Guid vehicleId, CancellationToken ct)
    {
        var query = new GetVehicleProcessedImagesQuery(vehicleId);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Cancel a job
    /// </summary>
    [HttpPost("jobs/{jobId}/cancel")]
    [Authorize]
    public async Task<ActionResult> CancelJob(Guid jobId, CancellationToken ct)
    {
        var command = new CancelJobCommand(jobId);
        await _mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Retry a failed job
    /// </summary>
    [HttpPost("jobs/{jobId}/retry")]
    [Authorize]
    public async Task<ActionResult> RetryJob(Guid jobId, CancellationToken ct)
    {
        var command = new RetryJobCommand(jobId);
        await _mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Callback endpoint for AI workers to report job completion
    /// </summary>
    [HttpPost("callback")]
    public async Task<ActionResult> ProcessingCallback([FromBody] ProcessingCallbackRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Received callback for job {JobId} with status {Status}",
            request.JobId, request.Success ? "success" : "failed");

        var command = new UpdateJobStatusCommand(
            Guid.Parse(request.JobId),
            request.Success,
            request.ProcessedImageUrl,
            request.MaskUrl,
            request.ProcessingTimeMs,
            request.Error
        );

        await _mediator.Send(command, ct);
        return Ok(new { received = true });
    }

    /// <summary>
    /// Get queue statistics (admin only)
    /// </summary>
    [HttpGet("stats/queue")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<QueueStatsResponse>> GetQueueStats(CancellationToken ct)
    {
        var query = new GetQueueStatsQuery();
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get AI features (classification, angle detection, etc.)
    /// </summary>
    [HttpPost("analyze")]
    [Authorize]
    public async Task<ActionResult<FeaturesResponse>> AnalyzeImage([FromBody] AnalyzeImageRequest request, CancellationToken ct)
    {
        var accountType = User.IsInRole("Dealer") ? "Dealer" : User.IsInRole("Admin") ? "Admin" : "Individual";
        var hasSubscription = User.HasClaim("subscription", "active");

        var query = new GetFeaturesQuery(accountType, hasSubscription);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Serve processed images from local storage
    /// </summary>
    [HttpGet("images/{filename}")]
    [AllowAnonymous]
    public IActionResult GetProcessedImage(string filename)
    {
        // Security: Prevent path traversal (CWE-22, OWASP A01:2021)
        if (string.IsNullOrWhiteSpace(filename) ||
            filename.Contains("..") ||
            filename.Contains('/') ||
            filename.Contains('\\') ||
            filename.Contains('~') ||
            Path.GetFileName(filename) != filename)
        {
            _logger.LogWarning("Path traversal attempt blocked: {Filename}", filename);
            return BadRequest(new { error = "Invalid filename" });
        }

        // Whitelist allowed extensions
        var extension = Path.GetExtension(filename).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp"))
        {
            return BadRequest(new { error = "Unsupported image format" });
        }

        var baseDir = Path.GetFullPath("/app/processed-images");
        var imagePath = Path.GetFullPath(Path.Combine(baseDir, filename));

        // Double-check resolved path stays within base directory
        if (!imagePath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Path traversal attempt blocked (resolved): {Path}", imagePath);
            return BadRequest(new { error = "Invalid filename" });
        }

        if (!System.IO.File.Exists(imagePath))
        {
            _logger.LogWarning("Image not found: {Path}", imagePath);
            return NotFound();
        }

        var contentType = extension switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return PhysicalFile(imagePath, contentType);
    }

    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ??
                         User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
