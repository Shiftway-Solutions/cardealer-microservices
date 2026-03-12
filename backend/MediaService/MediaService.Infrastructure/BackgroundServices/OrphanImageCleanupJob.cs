using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using MediaService.Domain.Interfaces.Repositories;
using MediaService.Infrastructure.Services.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediaService.Infrastructure.BackgroundServices;

/// <summary>
/// Monthly background job that identifies orphan images in DO Spaces:
/// 1. Lists all S3 objects in the bucket
/// 2. Compares each S3 key against the 'media_assets' + 'media_variants' tables
/// 3. Generates a report of orphans (no DB reference) with count and total size
/// 4. Stores the pending report — deletion ONLY proceeds after admin approval via API
/// 5. When approved, deletes each orphan and logs every deletion to an audit file
///
/// Schedule: Every 30 days (configurable via OrphanCleanup:ScanIntervalDays).
/// The job also checks for admin-approved deletion requests every 30 seconds.
/// </summary>
public class OrphanImageCleanupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrphanImageCleanupJob> _logger;
    private readonly IConfiguration _configuration;

    private const int DefaultScanIntervalDays = 30;
    private const int DefaultBatchSize = 500;

    public OrphanImageCleanupJob(
        IServiceProvider serviceProvider,
        ILogger<OrphanImageCleanupJob> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalDays = _configuration.GetValue("OrphanCleanup:ScanIntervalDays", DefaultScanIntervalDays);
        _logger.LogInformation("🧹 OrphanImageCleanupJob started — scanning every {Interval} days", intervalDays);

        // Initial delay: 5 minutes after startup to let other services initialize
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Phase 1: Generate orphan report (no deletions)
                var report = await GenerateOrphanReportAsync(stoppingToken);

                if (report != null && report.OrphanKeys.Count > 0)
                {
                    // Store the pending report for admin review
                    OrphanCleanupState.SetPendingReport(report);

                    _logger.LogWarning(
                        "🧹 Orphan report generated: {Count} orphan files, {Size} total. Awaiting admin approval.",
                        report.OrphanKeys.Count, FormatBytes(report.TotalSizeBytes));

                    // Send email notification to admin about pending report
                    await SendOrphanReportEmailAsync(report, stoppingToken);
                }
                else
                {
                    _logger.LogInformation("🧹 No orphan images found in DO Spaces. All objects have DB references.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("🧹 OrphanImageCleanupJob stopping (cancellation requested)");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🧹 OrphanImageCleanupJob scan error. Will retry in {Interval} days.", intervalDays);
            }

            // Wait for next scan, but check every 30s for approved deletions
            var waitEnd = DateTime.UtcNow.AddDays(intervalDays);
            while (DateTime.UtcNow < waitEnd && !stoppingToken.IsCancellationRequested)
            {
                // Check if admin approved deletion
                if (OrphanCleanupState.ConsumeApproval())
                {
                    var pending = OrphanCleanupState.GetPendingReport();
                    if (pending != null)
                    {
                        _logger.LogInformation("🧹 Admin approved orphan cleanup — starting deletion of {Count} files",
                            pending.OrphanKeys.Count);

                        try
                        {
                            await ExecuteApprovedCleanupAsync(pending, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "🧹 Error during approved orphan cleanup");
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Phase 1: Scans DO Spaces and compares keys against DB. Produces a report.
    /// NO deletions happen here.
    /// </summary>
    private async Task<OrphanCleanupReport?> GenerateOrphanReportAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("🧹 Starting orphan image scan...");

        var s3Options = new S3StorageOptions();
        _configuration.GetSection("Storage:S3").Bind(s3Options);

        if (string.IsNullOrEmpty(s3Options.AccessKey) || string.IsNullOrEmpty(s3Options.SecretKey))
        {
            _logger.LogWarning("🧹 Orphan scan skipped: S3 credentials not configured");
            return null;
        }

        // Create S3 client (same pattern as AclVerificationJob)
        var s3Config = new AmazonS3Config();
        if (!string.IsNullOrEmpty(s3Options.ServiceUrl))
        {
            s3Config.ServiceURL = s3Options.ServiceUrl;
            s3Config.ForcePathStyle = false;
        }
        else
        {
            s3Config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(s3Options.Region);
        }

        using var s3Client = new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, s3Config);

        // Step 1: Get all storage keys from the database
        var allDbKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var scope = _serviceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IMediaRepository>();

            // Get all MediaAsset storage keys
            var batchSize = _configuration.GetValue("OrphanCleanup:DbBatchSize", 1000);
            var offset = 0;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var assets = await repository.GetAllStorageKeysAsync(batchSize, offset, ct);
                if (assets.Count == 0) break;

                foreach (var key in assets)
                {
                    allDbKeys.Add(key);
                }
                offset += batchSize;
            }
        }

        _logger.LogInformation("🧹 Loaded {Count} storage keys from database", allDbKeys.Count);

        // Step 2: List all S3 objects and find orphans
        var orphanKeys = new List<OrphanFileInfo>();
        var totalS3Objects = 0;
        string? continuationToken = null;
        var listBatchSize = _configuration.GetValue("OrphanCleanup:S3BatchSize", DefaultBatchSize);

        do
        {
            ct.ThrowIfCancellationRequested();

            var listRequest = new ListObjectsV2Request
            {
                BucketName = s3Options.BucketName,
                MaxKeys = listBatchSize,
                ContinuationToken = continuationToken
            };

            ListObjectsV2Response listResponse;
            try
            {
                listResponse = await s3Client.ListObjectsV2Async(listRequest, ct);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "🧹 Failed to list objects in bucket {Bucket}. Aborting scan.", s3Options.BucketName);
                return null;
            }

            foreach (var s3Object in listResponse.S3Objects)
            {
                totalS3Objects++;

                // Check if this S3 key exists in the database
                if (!allDbKeys.Contains(s3Object.Key))
                {
                    orphanKeys.Add(new OrphanFileInfo
                    {
                        StorageKey = s3Object.Key,
                        SizeBytes = s3Object.Size,
                        LastModified = s3Object.LastModified,
                        CdnUrl = !string.IsNullOrEmpty(s3Options.CdnBaseUrl)
                            ? $"{s3Options.CdnBaseUrl}/{s3Object.Key}"
                            : $"s3://{s3Options.BucketName}/{s3Object.Key}"
                    });
                }
            }

            continuationToken = listResponse.IsTruncated ? listResponse.NextContinuationToken : null;

            if (totalS3Objects % 5000 == 0)
            {
                _logger.LogInformation("🧹 Scanned {Total} S3 objects, found {Orphans} orphans so far...",
                    totalS3Objects, orphanKeys.Count);
            }
        } while (continuationToken != null);

        sw.Stop();

        var report = new OrphanCleanupReport
        {
            GeneratedAt = DateTime.UtcNow,
            BucketName = s3Options.BucketName,
            TotalS3Objects = totalS3Objects,
            TotalDbKeys = allDbKeys.Count,
            OrphanKeys = orphanKeys,
            TotalSizeBytes = orphanKeys.Sum(o => o.SizeBytes),
            ScanDurationSeconds = sw.Elapsed.TotalSeconds
        };

        _logger.LogInformation(
            "🧹 Orphan scan complete: {Total} S3 objects, {DbKeys} DB keys, {Orphans} orphans ({Size}), took {Duration:F1}s",
            totalS3Objects, allDbKeys.Count, orphanKeys.Count, FormatBytes(report.TotalSizeBytes), report.ScanDurationSeconds);

        return report;
    }

    /// <summary>
    /// Phase 2: Executes deletion of approved orphan files. Logs each deletion to audit file.
    /// Only called after admin approval.
    /// </summary>
    private async Task ExecuteApprovedCleanupAsync(OrphanCleanupReport report, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        var s3Options = new S3StorageOptions();
        _configuration.GetSection("Storage:S3").Bind(s3Options);

        var s3Config = new AmazonS3Config();
        if (!string.IsNullOrEmpty(s3Options.ServiceUrl))
        {
            s3Config.ServiceURL = s3Options.ServiceUrl;
            s3Config.ForcePathStyle = false;
        }
        else
        {
            s3Config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(s3Options.Region);
        }

        using var s3Client = new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, s3Config);

        var deletedCount = 0;
        var failedCount = 0;
        var auditEntries = new List<string>();

        foreach (var orphan in report.OrphanKeys)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // Delete from S3
                await s3Client.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = s3Options.BucketName,
                    Key = orphan.StorageKey
                }, ct);

                deletedCount++;

                // Build audit log entry
                var auditEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [ORPHAN_DELETED] " +
                    $"Key={orphan.StorageKey} | URL={orphan.CdnUrl} | " +
                    $"Size={orphan.SizeBytes} ({FormatBytes(orphan.SizeBytes)}) | " +
                    $"S3LastModified={orphan.LastModified:yyyy-MM-dd HH:mm:ss} | " +
                    $"ListingId={ExtractListingIdFromKey(orphan.StorageKey)}";

                auditEntries.Add(auditEntry);

                if (deletedCount % 100 == 0)
                {
                    _logger.LogInformation("🧹 Deleted {Count}/{Total} orphan files...",
                        deletedCount, report.OrphanKeys.Count);

                    // Flush audit entries periodically
                    await FlushAuditEntriesAsync(auditEntries);
                    auditEntries.Clear();
                }
            }
            catch (AmazonS3Exception ex)
            {
                failedCount++;
                _logger.LogError(ex, "🧹 Failed to delete orphan {Key}", orphan.StorageKey);
            }
        }

        // Flush remaining audit entries
        if (auditEntries.Count > 0)
        {
            await FlushAuditEntriesAsync(auditEntries);
        }

        sw.Stop();

        var result = new OrphanCleanupResult
        {
            CompletedAt = DateTime.UtcNow,
            DeletedCount = deletedCount,
            FailedCount = failedCount,
            FreedBytes = report.OrphanKeys.Where((_, i) => i < deletedCount).Sum(o => o.SizeBytes),
            DurationSeconds = sw.Elapsed.TotalSeconds
        };

        // Store result for the admin dashboard
        OrphanCleanupState.SetLastResult(result);
        OrphanCleanupState.ClearPendingReport();

        _logger.LogInformation(
            "🧹 Orphan cleanup complete: {Deleted} deleted, {Failed} failed, freed {Size}, took {Duration:F1}s",
            deletedCount, failedCount, FormatBytes(result.FreedBytes), result.DurationSeconds);

        // Send completion email
        await SendCleanupCompleteEmailAsync(result, ct);
    }

    /// <summary>
    /// Extracts the listing/owner ID from an S3 key path.
    /// Keys typically look like: {ownerId}/{context}/{timestamp}_{random}_{filename}.ext
    /// </summary>
    private static string ExtractListingIdFromKey(string key)
    {
        var parts = key.Split('/');
        return parts.Length > 0 ? parts[0] : "unknown";
    }

    /// <summary>
    /// Writes audit entries to the orphan cleanup audit log file.
    /// </summary>
    private async Task FlushAuditEntriesAsync(List<string> entries)
    {
        try
        {
            var auditPath = _configuration.GetValue("OrphanCleanup:AuditLogPath", "/app/logs/orphan-cleanup-audit.log") ?? "/app/logs/orphan-cleanup-audit.log";
            var directory = Path.GetDirectoryName(auditPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.AppendAllLinesAsync(auditPath, entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🧹 Failed to write {Count} audit entries to log file", entries.Count);
            // Fallback: log to standard logger
            foreach (var entry in entries)
            {
                _logger.LogInformation("{AuditEntry}", entry);
            }
        }
    }

    private async Task SendOrphanReportEmailAsync(OrphanCleanupReport report, CancellationToken ct)
    {
        try
        {
            var smtpHost = _configuration.GetValue<string>("ImageHealthScan:Email:SmtpHost");
            var smtpPort = _configuration.GetValue("ImageHealthScan:Email:SmtpPort", 587);
            var smtpUser = _configuration.GetValue<string>("ImageHealthScan:Email:SmtpUser");
            var smtpPassword = _configuration.GetValue<string>("ImageHealthScan:Email:SmtpPassword");
            var fromEmail = _configuration.GetValue("ImageHealthScan:Email:From", "noreply@okla.com.do") ?? "noreply@okla.com.do";
            var adminEmail = _configuration.GetValue("ImageHealthScan:Email:AdminRecipient", "admin@okla.com.do") ?? "admin@okla.com.do";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
            {
                _logger.LogWarning("🧹 Email not configured — orphan report notification skipped");
                return;
            }

            var subject = $"🧹 OKLA — {report.OrphanKeys.Count} imágenes huérfanas detectadas ({FormatBytes(report.TotalSizeBytes)})";
            var body = $"""
                <h2>Reporte de Imágenes Huérfanas en DO Spaces</h2>
                <p><strong>Fecha del escaneo:</strong> {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC</p>
                <p><strong>Bucket:</strong> {report.BucketName}</p>
                <hr/>
                <table style="border-collapse:collapse;font-family:monospace;">
                    <tr><td style="padding:4px 12px;">Objetos en S3:</td><td><strong>{report.TotalS3Objects:N0}</strong></td></tr>
                    <tr><td style="padding:4px 12px;">Registros en DB:</td><td><strong>{report.TotalDbKeys:N0}</strong></td></tr>
                    <tr><td style="padding:4px 12px;">Huérfanos encontrados:</td><td style="color:red;"><strong>{report.OrphanKeys.Count:N0}</strong></td></tr>
                    <tr><td style="padding:4px 12px;">Tamaño total huérfanos:</td><td><strong>{FormatBytes(report.TotalSizeBytes)}</strong></td></tr>
                    <tr><td style="padding:4px 12px;">Duración escaneo:</td><td>{report.ScanDurationSeconds:F1}s</td></tr>
                </table>
                <hr/>
                <p style="color:orange;"><strong>⚠️ La eliminación NO se ejecuta automáticamente.</strong></p>
                <p>Ingresa al panel de administración OKLA → <em>Limpieza de Imágenes</em> para revisar y aprobar la eliminación.</p>
                <h3>Primeras 10 imágenes huérfanas (muestra):</h3>
                <ul>
                {string.Join("\n", report.OrphanKeys.Take(10).Select(o =>
                    $"<li><code>{o.StorageKey}</code> — {FormatBytes(o.SizeBytes)} — modificado: {o.LastModified:yyyy-MM-dd}</li>"))}
                </ul>
                """;

            using var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = true
            };

            var message = new MailMessage(fromEmail, adminEmail, subject, body)
            {
                IsBodyHtml = true
            };

            await smtp.SendMailAsync(message, ct);
            _logger.LogInformation("🧹 Orphan report email sent to {Admin}", adminEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🧹 Failed to send orphan report email");
        }
    }

    private async Task SendCleanupCompleteEmailAsync(OrphanCleanupResult result, CancellationToken ct)
    {
        try
        {
            var smtpHost = _configuration.GetValue<string>("ImageHealthScan:Email:SmtpHost");
            var smtpUser = _configuration.GetValue<string>("ImageHealthScan:Email:SmtpUser");
            var smtpPassword = _configuration.GetValue<string>("ImageHealthScan:Email:SmtpPassword");
            var fromEmail = _configuration.GetValue("ImageHealthScan:Email:From", "noreply@okla.com.do") ?? "noreply@okla.com.do";
            var adminEmail = _configuration.GetValue("ImageHealthScan:Email:AdminRecipient", "admin@okla.com.do") ?? "admin@okla.com.do";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser)) return;

            var subject = $"✅ OKLA — Limpieza de huérfanos completada: {result.DeletedCount} eliminados, {FormatBytes(result.FreedBytes)} liberados";
            var body = $"""
                <h2>Limpieza de Imágenes Huérfanas Completada</h2>
                <p><strong>Fecha:</strong> {result.CompletedAt:yyyy-MM-dd HH:mm:ss} UTC</p>
                <table style="border-collapse:collapse;font-family:monospace;">
                    <tr><td style="padding:4px 12px;">Eliminados:</td><td style="color:green;"><strong>{result.DeletedCount}</strong></td></tr>
                    <tr><td style="padding:4px 12px;">Fallidos:</td><td style="color:red;"><strong>{result.FailedCount}</strong></td></tr>
                    <tr><td style="padding:4px 12px;">Espacio liberado:</td><td><strong>{FormatBytes(result.FreedBytes)}</strong></td></tr>
                    <tr><td style="padding:4px 12px;">Duración:</td><td>{result.DurationSeconds:F1}s</td></tr>
                </table>
                <p>El log de auditoría detallado está en el archivo de logs del servidor.</p>
                """;

            using var smtp = new SmtpClient(
                smtpHost,
                _configuration.GetValue("ImageHealthScan:Email:SmtpPort", 587))
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = true
            };

            var message = new MailMessage(fromEmail, adminEmail, subject, body) { IsBodyHtml = true };
            await smtp.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🧹 Failed to send cleanup completion email");
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:F2} {sizes[order]}";
    }
}

// ============================================================================
// State + Models — Shared between the job and the API controller
// ============================================================================

/// <summary>
/// Thread-safe singleton state for orphan cleanup workflow.
/// Allows the background job and API controller to communicate:
/// - Job stores a pending report
/// - Admin reviews via API and approves
/// - Job picks up the approval and executes deletions
/// </summary>
public static class OrphanCleanupState
{
    private static OrphanCleanupReport? _pendingReport;
    private static OrphanCleanupResult? _lastResult;
    private static int _approvalRequested;
    private static readonly object _lock = new();

    public static void SetPendingReport(OrphanCleanupReport report)
    {
        lock (_lock) { _pendingReport = report; _approvalRequested = 0; }
    }

    public static OrphanCleanupReport? GetPendingReport()
    {
        lock (_lock) { return _pendingReport; }
    }

    public static void ClearPendingReport()
    {
        lock (_lock) { _pendingReport = null; }
    }

    public static void ApproveCleanup() => Interlocked.Exchange(ref _approvalRequested, 1);

    public static bool ConsumeApproval() => Interlocked.Exchange(ref _approvalRequested, 0) == 1;

    public static void SetLastResult(OrphanCleanupResult result)
    {
        lock (_lock) { _lastResult = result; }
    }

    public static OrphanCleanupResult? GetLastResult()
    {
        lock (_lock) { return _lastResult; }
    }
}

/// <summary>
/// Information about a single orphan file found in DO Spaces.
/// </summary>
public class OrphanFileInfo
{
    public string StorageKey { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
    public string CdnUrl { get; set; } = string.Empty;
}

/// <summary>
/// Full report of orphan detection scan, pending admin approval.
/// </summary>
public class OrphanCleanupReport
{
    public DateTime GeneratedAt { get; set; }
    public string BucketName { get; set; } = string.Empty;
    public int TotalS3Objects { get; set; }
    public int TotalDbKeys { get; set; }
    public List<OrphanFileInfo> OrphanKeys { get; set; } = new();
    public long TotalSizeBytes { get; set; }
    public double ScanDurationSeconds { get; set; }
}

/// <summary>
/// Result of an approved orphan cleanup execution.
/// </summary>
public class OrphanCleanupResult
{
    public DateTime CompletedAt { get; set; }
    public int DeletedCount { get; set; }
    public int FailedCount { get; set; }
    public long FreedBytes { get; set; }
    public double DurationSeconds { get; set; }
}
