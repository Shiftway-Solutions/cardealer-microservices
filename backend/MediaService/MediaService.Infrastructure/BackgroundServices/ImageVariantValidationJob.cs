using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using MediaService.Domain.Interfaces.Repositories;
using MediaService.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediaService.Infrastructure.BackgroundServices;

/// <summary>
/// Nightly background job that validates every processed image in the database
/// has all 3 expected WebP variant sizes: thumbnail (300×200), medium (800×600), original (WebP).
/// 
/// For each image missing one or more variants, or whose CDN URL returns non-200,
/// the job automatically re-enqueues the image for reprocessing via RabbitMQ
/// without requiring dealer intervention.
///
/// Sends a daily email report with re-processing statistics per variant size.
///
/// Schedule: Every 24h (configurable via VariantValidation:ScanIntervalHours).
/// Default start: 2:00 AM AST (Dominican Republic time).
/// </summary>
public class ImageVariantValidationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImageVariantValidationJob> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private const int DefaultScanIntervalHours = 24;
    private const int DefaultBatchSize = 200;
    private const int HeadRequestTimeoutSeconds = 5;

    /// <summary>
    /// Exactly 3 expected variant names matching the pipeline output.
    /// thumbnail: 300×200 WebP, medium: 800×600 WebP, original: full-size WebP.
    /// </summary>
    private static readonly string[] ExpectedVariants = { "thumbnail", "medium", "original" };

    public ImageVariantValidationJob(
        IServiceProvider serviceProvider,
        ILogger<ImageVariantValidationJob> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = _configuration.GetValue("VariantValidation:ScanIntervalHours", DefaultScanIntervalHours);
        _logger.LogInformation("🔄 ImageVariantValidationJob started — validating every {Interval}h", intervalHours);

        // Wait until 2:00 AM AST to start (avoid peak hours)
        var initialDelay = CalculateDelayUntilNextRun();
        _logger.LogInformation("🔄 First variant validation scan scheduled in {Delay}", initialDelay);
        await Task.Delay(initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunVariantValidationAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("🔄 ImageVariantValidationJob stopping (cancellation requested)");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔄 ImageVariantValidationJob encountered an error. Will retry in {Interval}h.", intervalHours);
            }

            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }

    private TimeSpan CalculateDelayUntilNextRun()
    {
        try
        {
            var rdTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo");
            var nowRd = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, rdTimeZone);

            // Target: 2:00 AM AST
            var targetHour = _configuration.GetValue("VariantValidation:TargetHour", 2);
            var targetToday = nowRd.Date.AddHours(targetHour);

            var nextRun = nowRd < targetToday ? targetToday : targetToday.AddDays(1);
            var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRun, rdTimeZone);

            return nextRunUtc - DateTime.UtcNow;
        }
        catch
        {
            // Fallback: start in 5 minutes
            return TimeSpan.FromMinutes(5);
        }
    }

    private async Task RunVariantValidationAsync(CancellationToken stoppingToken)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("🔄 Nightly variant validation scan starting...");

        var batchSize = _configuration.GetValue("VariantValidation:BatchSize", DefaultBatchSize);
        var totalScanned = 0;
        var totalMissing = 0;
        var totalRequeued = 0;
        var totalRequeueFailed = 0;
        var totalCdnUnreachable = 0;
        var missingByVariant = new Dictionary<string, int>();

        foreach (var variant in ExpectedVariants)
        {
            missingByVariant[variant] = 0;
        }

        var offset = 0;
        var httpClient = _httpClientFactory.CreateClient("VariantHealthCheck");
        httpClient.Timeout = TimeSpan.FromSeconds(HeadRequestTimeoutSeconds);

        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            IList<ImageVariantStatus> batch;
            using (var scope = _serviceProvider.CreateScope())
            {
                var mediaRepo = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
                batch = await mediaRepo.GetImagesWithMissingVariantsAsync(
                    ExpectedVariants, batchSize, offset, stoppingToken);
            }

            if (batch.Count == 0)
                break;

            totalScanned += batchSize;

            foreach (var image in batch)
            {
                var needsReprocess = false;

                // 1. Check DB-level missing variants
                if (image.MissingVariantNames.Count > 0)
                {
                    needsReprocess = true;
                    foreach (var missing in image.MissingVariantNames)
                    {
                        if (missingByVariant.ContainsKey(missing))
                            missingByVariant[missing]++;
                    }
                }

                // 2. HTTP HEAD verify that existing variant CDN URLs respond with 200
                foreach (var existingVariant in image.ExistingVariantNames)
                {
                    // Build CDN URL from the variant info — the repo stores StorageKey
                    // We need to query the actual CdnUrl from the variant record
                    var cdnUrl = await GetVariantCdnUrlAsync(image.MediaId, existingVariant, stoppingToken);
                    if (string.IsNullOrEmpty(cdnUrl))
                    {
                        // No CDN URL stored — treat as missing
                        needsReprocess = true;
                        if (missingByVariant.ContainsKey(existingVariant))
                            missingByVariant[existingVariant]++;
                        continue;
                    }

                    var isReachable = await VerifyCdnUrlAsync(httpClient, cdnUrl, stoppingToken);
                    if (!isReachable)
                    {
                        needsReprocess = true;
                        totalCdnUnreachable++;
                        if (missingByVariant.ContainsKey(existingVariant))
                            missingByVariant[existingVariant]++;

                        _logger.LogWarning(
                            "🔄 Variant {Variant} for {MediaId} exists in DB but CDN URL is unreachable: {Url}",
                            existingVariant, image.MediaId, cdnUrl);
                    }
                }

                if (!needsReprocess)
                    continue;

                totalMissing++;

                // Re-enqueue for reprocessing via RabbitMQ
                try
                {
                    var producer = _serviceProvider.GetService<IRabbitMQMediaProducer>();
                    if (producer != null && producer.IsConnected)
                    {
                        await producer.PublishProcessMediaCommandAsync(image.MediaId, "variant-reprocess");
                        totalRequeued++;

                        _logger.LogInformation(
                            "🔄 Re-enqueued {MediaId} for reprocessing — missing/broken variants: {MissingVariants}",
                            image.MediaId, string.Join(", ", image.MissingVariantNames));
                    }
                    else
                    {
                        totalRequeueFailed++;
                        _logger.LogWarning("🔄 Cannot re-enqueue {MediaId} — RabbitMQ producer not available", image.MediaId);
                    }
                }
                catch (Exception ex)
                {
                    totalRequeueFailed++;
                    _logger.LogWarning(ex, "🔄 Failed to re-enqueue {MediaId} for reprocessing", image.MediaId);
                }
            }

            offset += batchSize;
            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
        }

        sw.Stop();

        _logger.LogInformation(
            "🔄 Variant validation complete in {Elapsed}. Scanned: ~{Total}, Missing/Broken: {Missing}, " +
            "CDN unreachable: {Unreachable}, Re-queued: {Requeued}, Failed: {Failed}",
            sw.Elapsed, totalScanned, totalMissing, totalCdnUnreachable, totalRequeued, totalRequeueFailed);

        await SendVariantReportEmailAsync(totalScanned, totalMissing, totalRequeued,
            totalRequeueFailed, missingByVariant, totalCdnUnreachable, sw.Elapsed, stoppingToken);
    }

    /// <summary>
    /// Fetch the CDN URL for a specific variant of a media asset.
    /// </summary>
    private async Task<string?> GetVariantCdnUrlAsync(string mediaId, string variantName, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediaRepo = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
            var media = await mediaRepo.GetByIdAsync(mediaId, ct);
            if (media == null) return null;

            var variant = media.Variants?.FirstOrDefault(v =>
                string.Equals(v.Name, variantName, StringComparison.OrdinalIgnoreCase));
            return variant?.CdnUrl;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get CDN URL for {MediaId}/{Variant}", mediaId, variantName);
            return null;
        }
    }

    /// <summary>
    /// Verify that a CDN URL responds with HTTP 200 using a HEAD request.
    /// Returns false on non-200 status, timeout, or any exception.
    /// </summary>
    private async Task<bool> VerifyCdnUrlAsync(HttpClient httpClient, string cdnUrl, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, cdnUrl);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch
        {
            return false;
        }
    }

    private async Task SendVariantReportEmailAsync(
        int totalScanned, int totalMissing, int totalRequeued,
        int totalRequeueFailed, Dictionary<string, int> missingByVariant,
        int totalCdnUnreachable, TimeSpan elapsed, CancellationToken stoppingToken)
    {
        try
        {
            var smtpHost = _configuration["ImageHealthScan:Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["ImageHealthScan:Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["ImageHealthScan:Email:SmtpUser"] ?? "";
            var smtpPassword = _configuration["ImageHealthScan:Email:SmtpPassword"] ?? "";
            var fromEmail = _configuration["ImageHealthScan:Email:From"] ?? "noreply@okla.com.do";
            var toEmail = _configuration["ImageHealthScan:Email:AdminRecipient"] ?? "admin@okla.com.do";
            var enableSsl = bool.Parse(_configuration["ImageHealthScan:Email:EnableSsl"] ?? "true");

            if (string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPassword))
            {
                _logger.LogWarning("🔄 SMTP credentials not configured. Variant validation report will NOT be sent.");
                return;
            }

            // Only send email if there were missing variants
            if (totalMissing == 0)
            {
                _logger.LogInformation("🔄 All images have complete variants — no email report needed.");
                return;
            }

            var variantRows = string.Join("", missingByVariant
                .Where(kv => kv.Value > 0)
                .Select(kv => $"<tr><td style='padding: 6px; border: 1px solid #ddd;'>{kv.Key}</td>" +
                              $"<td style='padding: 6px; border: 1px solid #ddd;'>{kv.Value:N0}</td></tr>"));

            var subject = $"[OKLA MediaService] 🔄 Variant Validation — {totalMissing:N0} images re-processed";

            var body = $@"<html><body style='font-family: Arial, sans-serif;'>
<h2 style='color: #2563eb;'>🔄 Nightly Image Variant Validation Report</h2>
<p>The nightly variant validation job has completed. Expected variants: <b>thumbnail</b>, <b>medium</b>, <b>original</b> (all WebP).</p>
<table style='border-collapse: collapse; margin: 16px 0;'>
  <tr><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Images scanned (approx)</td><td style='padding: 8px; border: 1px solid #ddd;'>{totalScanned:N0}</td></tr>
  <tr><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Images with missing/broken variants</td><td style='padding: 8px; border: 1px solid #ddd; color: #f59e0b;'>{totalMissing:N0}</td></tr>
  <tr><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>CDN URLs unreachable (non-200)</td><td style='padding: 8px; border: 1px solid #ddd; color: #ef4444;'>{totalCdnUnreachable:N0}</td></tr>
  <tr><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Successfully re-queued ✅</td><td style='padding: 8px; border: 1px solid #ddd; color: #16a34a;'>{totalRequeued:N0}</td></tr>
  <tr><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Re-queue failed ❌</td><td style='padding: 8px; border: 1px solid #ddd; color: #dc2626;'>{totalRequeueFailed:N0}</td></tr>
  <tr><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Scan duration</td><td style='padding: 8px; border: 1px solid #ddd;'>{elapsed:hh\:mm\:ss}</td></tr>
</table>
<h3>Re-processed images by missing variant size:</h3>
<table style='border-collapse: collapse; margin: 16px 0;'>
  <tr style='background: #f1f5f9;'><th style='padding: 6px; border: 1px solid #ddd;'>Variant</th><th style='padding: 6px; border: 1px solid #ddd;'>Count</th></tr>
  {variantRows}
</table>
<p style='color: #999; font-size: 12px;'>This report is auto-generated by MediaService ImageVariantValidationJob. Images are re-queued automatically — no dealer intervention required.</p>
</body></html>";

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = enableSsl
            };

            var mailMessage = new MailMessage(fromEmail, toEmail, subject, body)
            {
                IsBodyHtml = true
            };

            await smtpClient.SendMailAsync(mailMessage, stoppingToken);
            _logger.LogInformation("🔄 Variant validation email report sent to {Recipient}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔄 Failed to send variant validation email report");
        }
    }
}
