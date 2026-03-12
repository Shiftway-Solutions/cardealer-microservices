using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace MediaService.Api.Controllers;

/// <summary>
/// Controller for 360° video upload and processing (consolidated from Video360Service).
/// TODO: Implement full CQRS handlers with MediatR when Video360Service logic is migrated.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class Video360Controller : ControllerBase
{
    private readonly ILogger<Video360Controller> _logger;

    // XSS/SQL injection detection patterns
    private static readonly Regex DangerousPatternRegex = new(
        @"(<script|</script>|javascript:|onerror\s*=|onload\s*=|onclick\s*=|<iframe|<object|<embed|eval\s*\(|<svg|alert\s*\(|';--|OR\s+1\s*=\s*1|UNION\s+SELECT|DROP\s+TABLE|INSERT\s+INTO|DELETE\s+FROM|EXEC\s*\()",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Video360Controller(ILogger<Video360Controller> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Upload a new 360° video for processing
    /// </summary>
    /// <param name="file">Video file (MP4 format recommended)</param>
    /// <param name="vehicleId">Associated vehicle ID</param>
    /// <param name="title">Optional title for the video</param>
    /// <returns>Upload result with processing status</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(524_288_000)] // 500MB hard limit for video uploads
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Upload(
        [FromForm] IFormFile? file,
        [FromForm] Guid vehicleId,
        [FromForm] string? title = null)
    {
        _logger.LogInformation("Uploading 360° video for vehicle {VehicleId}", vehicleId);

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No video file provided." });

        if (vehicleId == Guid.Empty)
            return BadRequest(new { error = "VehicleId is required." });

        // SECURITY: Validate title for XSS/SQL injection patterns
        if (!string.IsNullOrEmpty(title) && DangerousPatternRegex.IsMatch(title))
        {
            _logger.LogWarning("Blocked 360° video upload with dangerous title pattern from user {UserId}",
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown");
            return BadRequest(new { error = "Title contains invalid characters." });
        }

        // Validate content type
        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "video/mp4", "video/mpeg", "video/webm", "video/quicktime"
        };

        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { error = $"Content type '{file.ContentType}' is not supported for 360° videos. Use MP4, MPEG, WebM, or MOV." });

        var currentUserId = GetCurrentUserId();

        // TODO: Replace with MediatR command when Video360Service logic is migrated
        // In production, this would queue the video for background processing
        var stubData = new
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            Title = title ?? file.FileName,
            FileName = file.FileName,
            FileSize = file.Length,
            ContentType = file.ContentType,
            Status = "processing",
            UploadedBy = currentUserId,
            EstimatedProcessingTime = "2-5 minutes",
            CreatedAt = DateTime.UtcNow
        };

        return AcceptedAtAction(nameof(GetById), new { id = stubData.Id }, stubData);
    }

    /// <summary>
    /// Get 360° video details and processing status
    /// </summary>
    /// <param name="id">Video ID</param>
    /// <returns>Video details with processing status</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        _logger.LogInformation("Getting 360° video {VideoId}", id);

        // TODO: Replace with MediatR query when Video360Service logic is migrated
        var stubData = new
        {
            Id = id,
            VehicleId = Guid.NewGuid(),
            Title = "360° Interior View",
            FileName = "vehicle-360.mp4",
            FileSize = 52428800L,
            ContentType = "video/mp4",
            Status = "completed",
            VideoUrl = $"https://cdn.okla.do/360/{id}/video.mp4",
            ThumbnailUrl = $"https://cdn.okla.do/360/{id}/thumbnail.webp",
            Duration = TimeSpan.FromSeconds(45),
            Resolution = "3840x1920",
            FrameCount = 36,
            ProcessingStartedAt = DateTime.UtcNow.AddMinutes(-3),
            ProcessingCompletedAt = DateTime.UtcNow.AddMinutes(-1),
            UploadedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        return Ok(stubData);
    }

    /// <summary>
    /// Get extracted frames from a 360° video
    /// </summary>
    /// <param name="id">Video ID</param>
    /// <param name="count">Number of frames to return (default 36 for 10° intervals)</param>
    /// <returns>List of frame URLs and metadata</returns>
    [HttpGet("{id:guid}/frames")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetFrames(Guid id, [FromQuery] int count = 36)
    {
        _logger.LogInformation("Getting frames for 360° video {VideoId}, count={Count}", id, count);

        // TODO: Replace with MediatR query when Video360Service logic is migrated
        var frames = Enumerable.Range(0, count).Select(i => new
        {
            Index = i,
            Angle = i * (360.0 / count),
            Url = $"https://cdn.okla.do/360/{id}/frames/frame_{i:D3}.webp",
            ThumbnailUrl = $"https://cdn.okla.do/360/{id}/frames/thumb_{i:D3}.webp",
            Width = 1920,
            Height = 960
        }).ToArray();

        var stubData = new
        {
            VideoId = id,
            TotalFrames = count,
            Frames = frames
        };

        return Ok(stubData);
    }

    /// <summary>
    /// Delete a 360° video and its processed frames
    /// </summary>
    /// <param name="id">Video ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Delete(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} deleting 360° video {VideoId}", currentUserId, id);

        // TODO: Replace with MediatR command when Video360Service logic is migrated
        // SECURITY: When real implementation is added, MUST verify ownership:
        //   var video = await _repo.GetByIdAsync(id);
        //   if (video == null) return NotFound(...);
        //   if (video.UploadedBy != currentUserId) return Forbid();
        // In production, this would also clean up S3/CDN storage
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID claim is missing or invalid in the JWT token.");
        }

        return userId;
    }
}
