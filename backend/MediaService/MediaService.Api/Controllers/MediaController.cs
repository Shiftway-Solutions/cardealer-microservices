using MediaService.Application.Features.Media.Commands.InitUpload;
using MediaService.Application.Features.Media.Commands.FinalizeUpload;
using MediaService.Application.Features.Media.Commands.DeleteMedia;
using MediaService.Application.Features.Media.Commands.UploadVehicleImage;
using MediaService.Application.Features.Media.Commands.GetPresignedUrlsBatch;
using MediaService.Application.Features.Media.Queries.GetMedia;
using MediaService.Application.Features.Media.Queries.ValidateImageQuality;
using MediaService.Domain.Interfaces.Services;
using MediaService.Infrastructure.Services.Storage;
using MediaService.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using CarDealer.Shared.Configuration;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MediaService.Api.Controllers;

/// <summary>
/// Controller for media upload, retrieval, and deletion
/// Requires authentication by default to prevent storage abuse (OWASP A01:2021)
/// Read-only endpoints (GET) are allowed anonymous for public media access
/// Rate-limited with "uploads" policy on all POST endpoints (OWASP API4:2023)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("uploads")]
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMediaStorageService _storageService;
    private readonly IConfigurationServiceClient _configClient;
    private readonly ILogger<MediaController> _logger;
    private readonly S3StorageOptions _s3Options;

    // Magic bytes for file type validation (prevents content-type spoofing)
    private static readonly Dictionary<string, byte[][]> MagicBytes = new()
    {
        ["image/jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        ["image/png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        ["image/gif"] = new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } },
        ["image/webp"] = new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } },
        ["application/pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } },
        ["video/mp4"] = new[] {
            new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 },
            new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 },
            new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 }
        },
    };

    // Allowed content types (NO SVG — XSS vector)
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "video/mp4", "video/mpeg", "video/webm",
        "application/pdf"
    };

    // Dangerous file extensions
    private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".bat", ".cmd", ".sh", ".ps1", ".dll", ".so", ".php",
        ".jsp", ".asp", ".aspx", ".cgi", ".py", ".rb", ".pl",
        ".svg", ".html", ".htm", ".js", ".mjs", ".vbs"
    };

    public MediaController(
        IMediator mediator,
        IMediaStorageService storageService,
        IConfigurationServiceClient configClient,
        IOptions<S3StorageOptions> s3Options,
        ILogger<MediaController> logger)
    {
        _mediator = mediator;
        _storageService = storageService;
        _configClient = configClient;
        _s3Options = s3Options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Get effective media/storage settings from ConfigurationService (admin panel).
    /// </summary>
    [HttpGet("settings")]
    [Authorize]
    public async Task<ActionResult> GetSettings()
    {
        var settings = new
        {
            StorageProvider = await _configClient.GetValueAsync("media.storage_provider", "local"),
            MaxUploadSizeMb = await _configClient.GetIntAsync("media.max_upload_size_mb", 100),
            AllowedContentTypes = await _configClient.GetValueAsync("media.allowed_content_types", "jpg,jpeg,png,gif,webp,mp4,pdf"),
            CdnBaseUrl = await _configClient.GetValueAsync("media.cdn_base_url", ""),
        };

        return Ok(settings);
    }

    /// <summary>
    /// Generic file upload endpoint - supports images, videos and documents.
    /// SECURITY: Validates content-type, file extension, and magic bytes.
    /// Max size enforced dynamically from admin panel (media.max_upload_size_mb).
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(104_857_600)] // 100MB hard limit at Kestrel level (reduced from 500MB)
    public async Task<ActionResult<object>> Upload(
        [FromForm] IFormFile file,
        [FromForm] string? folder = "uploads",
        [FromForm] string? type = "file")
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided" });
        }

        // SECURITY: Sanitize folder parameter to prevent path traversal
        folder = SanitizeFolderPath(folder ?? "uploads");

        // SECURITY: Validate file extension
        var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
        if (DangerousExtensions.Contains(fileExtension))
        {
            _logger.LogWarning("Blocked upload of dangerous file type: {Extension}, ContentType: {ContentType}",
                fileExtension, file.ContentType);
            return BadRequest(new { error = $"File type '{fileExtension}' is not allowed" });
        }

        // SECURITY: Validate content type
        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            _logger.LogWarning("Blocked upload with disallowed content type: {ContentType}", file.ContentType);
            return BadRequest(new { error = $"Content type '{file.ContentType}' is not allowed" });
        }

        // SECURITY: Validate magic bytes to prevent content-type spoofing
        if (!await ValidateMagicBytesAsync(file, file.ContentType))
        {
            _logger.LogWarning("Blocked upload: magic bytes don't match declared content type {ContentType} for file {FileName}",
                file.ContentType, file.FileName);
            return BadRequest(new { error = "File content does not match the declared type" });
        }

        // Enforce dynamic max size from admin panel
        var maxSizeMb = await _configClient.GetIntAsync("media.max_upload_size_mb", 100);
        if (file.Length > maxSizeMb * 1024L * 1024L)
        {
            return BadRequest(new { error = $"File exceeds maximum allowed size of {maxSizeMb} MB" });
        }

        try
        {
            // Generate storage key with sanitized folder
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var storageKey = $"{folder}/{DateTime.UtcNow:yyyy/MM/dd}/{fileName}";

            // Upload to storage
            using var stream = file.OpenReadStream();
            await _storageService.UploadFileAsync(storageKey, stream, file.ContentType);

            // Get public URL
            var url = await _storageService.GetFileUrlAsync(storageKey);

            return StatusCode(201, new
            {
                url = url,
                publicId = storageKey,
                storageKey = storageKey,
                size = file.Length,
                contentType = file.ContentType,
                fileName = file.FileName
            });
        }
        catch (Amazon.S3.AmazonS3Exception ex) when (ex.ErrorCode == "RequestTimeTooSkewed")
        {
            // Docker container clock drift — macOS sleep/wake desynchronizes Docker VM clock.
            // CorrectForClockSkew=true on S3Config should auto-correct; if still thrown,
            // the drift is > 15 min (beyond SDK correction threshold).
            _logger.LogError(ex,
                "S3 clock drift detected for file {FileName}. Container clock is out of sync with AWS. " +
                "Restart MediaService container or Docker Desktop to resync. AWS Error: {ErrorCode}",
                file.FileName, ex.ErrorCode);
            return StatusCode(503, new { error = "Storage service temporarily unavailable. Please try again in a moment." });
        }
        catch (Amazon.S3.AmazonS3Exception ex) when (ex.ErrorCode == "AccessControlListNotSupported")
        {
            _logger.LogError(ex,
                "S3 bucket does not allow ACLs for file {FileName}. " +
                "Set Storage:S3:UseAcl=false in configuration and ensure the bucket has a public-read bucket policy. " +
                "AWS Error: {ErrorCode}",
                file.FileName, ex.ErrorCode);
            return StatusCode(503, new { error = "Storage configuration error. Please contact support." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed for file {FileName}: {ErrorMessage}", file.FileName, ex.Message);
            return StatusCode(500, new { error = "Upload failed. Please try again later." });
        }
    }

    /// <summary>
    /// Image upload endpoint for KYC documents and other use cases.
    /// SECURITY: Validates content-type, magic bytes, and extension.
    /// </summary>
    [HttpPost("upload/image")]
    [RequestSizeLimit(52_428_800)] // 50MB hard limit for images
    public async Task<ActionResult<object>> UploadImage([FromForm] IFormFile file, [FromForm] string? folder = "uploads")
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided" });
        }

        // SECURITY: Sanitize folder parameter
        folder = SanitizeFolderPath(folder ?? "uploads");

        // Enforce dynamic max size from admin panel
        var maxSizeMb = await _configClient.GetIntAsync("media.max_upload_size_mb", 100);
        if (file.Length > maxSizeMb * 1024L * 1024L)
        {
            return BadRequest(new { error = $"File exceeds maximum allowed size of {maxSizeMb} MB" });
        }

        // SECURITY: Validate content type against whitelist
        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new { error = $"Content type '{file.ContentType}' is not allowed" });
        }

        // SECURITY: Validate file extension
        var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
        if (DangerousExtensions.Contains(fileExtension))
        {
            return BadRequest(new { error = $"File type '{fileExtension}' is not allowed" });
        }

        // SECURITY: Validate magic bytes
        if (!await ValidateMagicBytesAsync(file, file.ContentType))
        {
            return BadRequest(new { error = "File content does not match the declared type" });
        }

        try
        {
            // Generate storage key
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var storageKey = $"{folder}/{DateTime.UtcNow:yyyy/MM/dd}/{fileName}";

            // Upload to storage
            using var stream = file.OpenReadStream();
            await _storageService.UploadFileAsync(storageKey, stream, file.ContentType);

            // Get public URL
            var url = await _storageService.GetFileUrlAsync(storageKey);

            return StatusCode(201, new
            {
                url = url,
                publicId = storageKey,
                width = 0,
                height = 0,
                format = Path.GetExtension(file.FileName).TrimStart('.'),
                size = file.Length
            });
        }
        catch (Amazon.S3.AmazonS3Exception ex) when (ex.ErrorCode == "RequestTimeTooSkewed")
        {
            _logger.LogError(ex,
                "S3 clock drift detected for file {FileName}. Restart MediaService container to resync. Error: {ErrorCode}",
                file.FileName, ex.ErrorCode);
            return StatusCode(503, new { error = "Storage service temporarily unavailable. Please try again in a moment." });
        }
        catch (Amazon.S3.AmazonS3Exception ex) when (ex.ErrorCode == "AccessControlListNotSupported")
        {
            _logger.LogError(ex,
                "S3 bucket does not allow ACLs for file {FileName}. " +
                "Set Storage:S3:UseAcl=false in configuration and ensure the bucket has a public-read bucket policy. " +
                "Error: {ErrorCode}",
                file.FileName, ex.ErrorCode);
            return StatusCode(503, new { error = "Storage configuration error. Please contact support." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image upload failed for file {FileName}: {ErrorMessage}", file.FileName, ex.Message);
            return StatusCode(500, new { error = "Upload failed. Please try again later." });
        }
    }

    [HttpPost("upload/init")]
    public async Task<ActionResult<ApiResponse<InitUploadResponse>>> InitUpload(InitUploadCommand command)
    {
        // Extract DealerId from JWT claims
        var dealerIdClaim = User.FindFirst("dealerId")?.Value;
        if (string.IsNullOrEmpty(dealerIdClaim) || !Guid.TryParse(dealerIdClaim, out var dealerId))
        {
            return BadRequest(ApiResponse<InitUploadResponse>.Fail("Invalid or missing dealerId claim"));
        }

        // Override DealerId from token (security measure)
        var commandWithDealerId = new InitUploadCommand(
            dealerId,
            command.OwnerId,
            command.Context,
            command.FileName,
            command.ContentType,
            command.FileSize
        );

        var result = await _mediator.Send(commandWithDealerId);
        return Ok(result);
    }

    [HttpPost("upload/finalize/{mediaId}")]
    public async Task<ActionResult<ApiResponse<FinalizeUploadResponse>>> FinalizeUpload(string mediaId)
    {
        var command = new FinalizeUploadCommand(mediaId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("{mediaId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<GetMediaResponse>>> GetMedia(string mediaId)
    {
        var query = new GetMediaQuery(mediaId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Generates a fresh pre-signed URL for accessing a file by its storageKey.
    /// SECURITY: AllowAnonymous is intentional — this is a service-to-service endpoint
    /// (e.g. KYCService → MediaService, pod-to-pod). External traffic reaches this path
    /// via the Ocelot Gateway which already enforces Bearer auth on /api/media/{everything}.
    /// The storageKey is UUID-based (unguessable) and path traversal is blocked below.
    /// </summary>
    [HttpGet("url")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetFreshUrl([FromQuery] string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return BadRequest(new { error = "storageKey is required" });
        }

        // SECURITY: Prevent path traversal in storage key
        if (storageKey.Contains("..") || storageKey.Contains("~"))
        {
            _logger.LogWarning("Blocked path traversal attempt in storageKey: {StorageKey}", storageKey);
            return BadRequest(new { error = "Invalid storage key" });
        }

        try
        {
            // Verify the file exists
            var exists = await _storageService.FileExistsAsync(storageKey);
            if (!exists)
            {
                return NotFound(new { error = "File not found", storageKey });
            }

            // Generate fresh URL
            var url = await _storageService.GetFileUrlAsync(storageKey);
            var expiresAt = DateTime.UtcNow.AddMinutes(_s3Options.PreSignedUrlExpirationMinutes);

            return Ok(new
            {
                url,
                storageKey,
                expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate URL for storageKey: {StorageKey}", storageKey);
            return StatusCode(500, new { error = "Failed to generate URL. Please try again later." });
        }
    }

    /// <summary>
    /// Deletes a media file by its ID.
    /// SECURITY: Uses MediatR command with ownership verification via DeleteMediaCommand.
    /// </summary>
    [HttpDelete("{mediaId}")]
    [Authorize]
    public async Task<IActionResult> DeleteMedia(string mediaId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            // Use MediatR command which handles ownership check, storage deletion, AND DB record cleanup
            var command = new DeleteMediaCommand(mediaId, userId);
            var result = await _mediator.Send(command);

            if (result == null || !result.Success)
            {
                return NotFound(new { error = "Media not found or you don't have permission to delete it", mediaId });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete media {MediaId}", mediaId);
            return StatusCode(500, new { error = "Failed to delete media. Please try again later." });
        }
    }

    #region Vehicle Image Upload Endpoints

    /// <summary>
    /// Upload optimized vehicle image with server-side compression and inline thumbnail generation.
    /// SECURITY: Validates content-type, magic bytes, extension, and ownership.
    /// </summary>
    [HttpPost("upload/vehicle-image")]
    [RequestSizeLimit(15_728_640)] // 15MB hard limit
    public async Task<ActionResult<ApiResponse<VehicleImageUploadResponse>>> UploadVehicleImage(
        [FromForm] IFormFile file,
        [FromForm] Guid? vehicleId = null,
        [FromForm] string? imageType = null,
        [FromForm] int? sortOrder = null,
        [FromForm] bool? isPrimary = null,
        [FromForm] bool compress = true)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<VehicleImageUploadResponse>.Fail("No se proporcionó archivo"));

        // SECURITY: Validate file
        var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
        if (DangerousExtensions.Contains(fileExtension))
            return BadRequest(ApiResponse<VehicleImageUploadResponse>.Fail($"Tipo de archivo '{fileExtension}' no permitido"));

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(ApiResponse<VehicleImageUploadResponse>.Fail($"Tipo de contenido '{file.ContentType}' no permitido"));

        if (!await ValidateMagicBytesAsync(file, file.ContentType))
            return BadRequest(ApiResponse<VehicleImageUploadResponse>.Fail("El contenido del archivo no coincide con el tipo declarado"));

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var dealerIdClaim = User.FindFirst("dealerId")?.Value;
        Guid.TryParse(dealerIdClaim, out var dealerId);

        var command = new UploadVehicleImageCommand
        {
            File = file,
            OwnerId = userId,
            DealerId = dealerId,
            VehicleId = vehicleId,
            ImageType = imageType,
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            Compress = compress
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Generate batch of pre-signed S3 URLs for direct browser-to-S3 uploads.
    /// More efficient than routing through MediaService for large uploads.
    /// </summary>
    [HttpPost("upload/presigned-urls")]
    public async Task<ActionResult<ApiResponse<GetPresignedUrlsBatchResponse>>> GetPresignedUrlsBatch(
        [FromBody] GetPresignedUrlsBatchRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var dealerIdClaim = User.FindFirst("dealerId")?.Value;
        Guid.TryParse(dealerIdClaim, out var dealerId);

        var command = new GetPresignedUrlsBatchCommand
        {
            Files = request.Files.Select(f => new FileUploadInfo
            {
                FileName = f.FileName,
                ContentType = f.ContentType,
                Size = f.Size
            }).ToList(),
            VehicleId = request.VehicleId,
            Category = request.Category ?? "vehicles",
            OwnerId = userId,
            DealerId = dealerId
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Validate image quality before upload — checks resolution, blur, exposure, aspect ratio.
    /// </summary>
    [HttpPost("validate/quality")]
    [RequestSizeLimit(15_728_640)] // 15MB
    public async Task<ActionResult<ApiResponse<ImageQualityResult>>> ValidateImageQuality([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<ImageQualityResult>.Fail("No se proporcionó archivo"));

        if (!file.ContentType.StartsWith("image/"))
            return BadRequest(ApiResponse<ImageQualityResult>.Fail("El archivo debe ser una imagen"));

        var query = new ValidateImageQualityQuery { File = file };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    #endregion

    #region Security Helpers

    /// <summary>
    /// Security (CWE-22): Sanitizes folder path to prevent path traversal attacks.
    /// Uses canonical path validation instead of string replacement (which is bypassable).
    /// </summary>
    private static string SanitizeFolderPath(string folder)
    {
        // Only allow safe characters first (whitelist approach)
        var sanitized = new string(folder
            .Where(c => char.IsLetterOrDigit(c) || c == '/' || c == '-' || c == '_')
            .ToArray());

        // Remove leading/trailing slashes and collapse multiple slashes
        sanitized = string.Join("/", sanitized.Split('/', StringSplitOptions.RemoveEmptyEntries));

        if (string.IsNullOrEmpty(sanitized)) return "uploads";

        // Canonical path validation: ensure resolved path stays within allowed base
        var basePath = Path.GetFullPath("/storage/uploads");
        var resolvedPath = Path.GetFullPath(Path.Combine(basePath, sanitized));

        if (!resolvedPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            // Path traversal attempt detected — return safe default
            return "uploads";
        }

        return sanitized;
    }

    /// <summary>
    /// Validates file content matches its declared MIME type by checking magic bytes.
    /// Prevents content-type spoofing (e.g., .exe renamed to .jpg).
    /// </summary>
    private static async Task<bool> ValidateMagicBytesAsync(IFormFile file, string contentType)
    {
        if (!MagicBytes.TryGetValue(contentType, out var expectedSignatures))
        {
            // For types without magic byte definitions, allow if content-type is in whitelist
            return AllowedContentTypes.Contains(contentType);
        }

        var headerBytes = new byte[16];
        using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(headerBytes, 0, headerBytes.Length);

        if (bytesRead == 0) return false;

        return expectedSignatures.Any(signature =>
            bytesRead >= signature.Length &&
            headerBytes.Take(signature.Length).SequenceEqual(signature));
    }

    #endregion
}