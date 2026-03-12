using MediaService.Infrastructure.BackgroundServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediaService.Api.Controllers;

/// <summary>
/// Admin API for orphan image cleanup management.
/// Exposes the pending orphan report for admin review and provides
/// an approval endpoint to start the actual deletion.
/// </summary>
[ApiController]
[Route("api/media/orphan-cleanup")]
[Authorize]
public class OrphanCleanupController : ControllerBase
{
    private readonly ILogger<OrphanCleanupController> _logger;

    public OrphanCleanupController(ILogger<OrphanCleanupController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// GET /api/media/orphan-cleanup/status
    /// Returns the current state: pending report, last result, or idle.
    /// </summary>
    [HttpGet("status")]
    public ActionResult<OrphanCleanupStatusResponse> GetStatus()
    {
        var pending = OrphanCleanupState.GetPendingReport();
        var lastResult = OrphanCleanupState.GetLastResult();

        return Ok(new OrphanCleanupStatusResponse
        {
            HasPendingReport = pending != null,
            PendingReport = pending != null ? new OrphanReportDto
            {
                GeneratedAt = pending.GeneratedAt,
                BucketName = pending.BucketName,
                TotalS3Objects = pending.TotalS3Objects,
                TotalDbKeys = pending.TotalDbKeys,
                OrphanCount = pending.OrphanKeys.Count,
                TotalSizeBytes = pending.TotalSizeBytes,
                TotalSizeGb = Math.Round((double)pending.TotalSizeBytes / (1024.0 * 1024.0 * 1024.0), 3),
                ScanDurationSeconds = pending.ScanDurationSeconds,
                SampleOrphans = pending.OrphanKeys.Take(20).Select(o => new OrphanFileSampleDto
                {
                    StorageKey = o.StorageKey,
                    SizeBytes = o.SizeBytes,
                    LastModified = o.LastModified,
                    CdnUrl = o.CdnUrl,
                    ListingId = o.StorageKey.Split('/').FirstOrDefault() ?? "unknown"
                }).ToList()
            } : null,
            LastResult = lastResult != null ? new OrphanCleanupResultDto
            {
                CompletedAt = lastResult.CompletedAt,
                DeletedCount = lastResult.DeletedCount,
                FailedCount = lastResult.FailedCount,
                FreedBytes = lastResult.FreedBytes,
                FreedGb = Math.Round((double)lastResult.FreedBytes / (1024.0 * 1024.0 * 1024.0), 3),
                DurationSeconds = lastResult.DurationSeconds
            } : null
        });
    }

    /// <summary>
    /// POST /api/media/orphan-cleanup/approve
    /// Admin approves the pending orphan cleanup. The background job will start
    /// deleting orphan files within 30 seconds.
    /// </summary>
    [HttpPost("approve")]
    public ActionResult ApproveCleanup()
    {
        var pending = OrphanCleanupState.GetPendingReport();
        if (pending == null)
        {
            return BadRequest(new { error = "No hay reporte de huérfanos pendiente para aprobar." });
        }

        OrphanCleanupState.ApproveCleanup();
        _logger.LogWarning(
            "🧹 Admin approved orphan cleanup: {Count} files ({Size} bytes) will be deleted",
            pending.OrphanKeys.Count, pending.TotalSizeBytes);

        return Ok(new
        {
            message = $"Eliminación aprobada. {pending.OrphanKeys.Count} archivos huérfanos serán eliminados en los próximos 30 segundos.",
            orphanCount = pending.OrphanKeys.Count,
            approvedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// POST /api/media/orphan-cleanup/dismiss
    /// Admin dismisses the pending report without executing deletion.
    /// </summary>
    [HttpPost("dismiss")]
    public ActionResult DismissReport()
    {
        var pending = OrphanCleanupState.GetPendingReport();
        if (pending == null)
        {
            return BadRequest(new { error = "No hay reporte pendiente para descartar." });
        }

        OrphanCleanupState.ClearPendingReport();
        _logger.LogInformation("🧹 Admin dismissed orphan report ({Count} orphans)", pending.OrphanKeys.Count);

        return Ok(new { message = "Reporte descartado. No se eliminará ningún archivo." });
    }
}

// ============================================================================
// DTOs
// ============================================================================

public class OrphanCleanupStatusResponse
{
    public bool HasPendingReport { get; set; }
    public OrphanReportDto? PendingReport { get; set; }
    public OrphanCleanupResultDto? LastResult { get; set; }
}

public class OrphanReportDto
{
    public DateTime GeneratedAt { get; set; }
    public string BucketName { get; set; } = string.Empty;
    public int TotalS3Objects { get; set; }
    public int TotalDbKeys { get; set; }
    public int OrphanCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public double TotalSizeGb { get; set; }
    public double ScanDurationSeconds { get; set; }
    public List<OrphanFileSampleDto> SampleOrphans { get; set; } = new();
}

public class OrphanFileSampleDto
{
    public string StorageKey { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
    public string CdnUrl { get; set; } = string.Empty;
    public string ListingId { get; set; } = string.Empty;
}

public class OrphanCleanupResultDto
{
    public DateTime CompletedAt { get; set; }
    public int DeletedCount { get; set; }
    public int FailedCount { get; set; }
    public long FreedBytes { get; set; }
    public double FreedGb { get; set; }
    public double DurationSeconds { get; set; }
}
