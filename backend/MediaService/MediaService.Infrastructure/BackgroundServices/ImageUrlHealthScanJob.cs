using System.Diagnostics;
using CarDealer.Contracts.Events.Alert;
using MediaService.Application.DTOs;
using MediaService.Application.Services;
using MediaService.Application.Interfaces;
using MediaService.Domain.Interfaces;
using MediaService.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediaService.Infrastructure.BackgroundServices;

/// <summary>
/// Background job that runs every 6 hours to scan all image URLs stored in the database.
/// For each image, it performs a HEAD request to the CDN URL and marks images as broken
/// when the URL returns 403, 404, 410, 500, or times out (>5 seconds).
///
/// Additionally, at 8:00 AM AST (Atlantic Standard Time / Dominican Republic) each day,
/// it sends a comprehensive health report via email to the OKLA administrator.
/// </summary>
public class ImageUrlHealthScanJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImageUrlHealthScanJob> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private const int DefaultBatchSize = 100;
    private const int DefaultTimeoutSeconds = 5;
    private const int DefaultScanIntervalHours = 6;
    private const string DominicanRepublicTimeZoneId = "America/Santo_Domingo"; // AST UTC-4

    /// <summary>
    /// HTTP status codes that indicate a broken image URL.
    /// </summary>
    private static readonly HashSet<int> BrokenStatusCodes = new() { 403, 404, 410, 500 };

    public ImageUrlHealthScanJob(
        IServiceProvider serviceProvider,
        ILogger<ImageUrlHealthScanJob> logger,
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
        _logger.LogInformation("🖼️ ImageUrlHealthScanJob started — scanning every {Interval}h, email report at 8:00 AM RD time",
            GetScanIntervalHours());

        // Initial delay to let the service start fully
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var report = await RunScanAsync(stoppingToken);

                // Check if it's time to send the daily email report (8:00 AM Dominican Republic time)
                if (ShouldSendDailyEmail())
                {
                    await SendEmailReportAsync(report, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("🖼️ ImageUrlHealthScanJob stopping (cancellation requested)");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🖼️ ImageUrlHealthScanJob encountered an error. Will retry in {Interval}h.",
                    GetScanIntervalHours());
            }

            // Wait for next scan interval, but check every 30s for manual trigger requests
            var interval = TimeSpan.FromHours(GetScanIntervalHours());
            var elapsed = TimeSpan.Zero;
            var checkInterval = TimeSpan.FromSeconds(30);
            while (elapsed < interval && !stoppingToken.IsCancellationRequested)
            {
                if (ManualScanTrigger.ConsumeRequest())
                {
                    _logger.LogInformation("🖼️ Manual scan trigger detected — starting immediate scan");
                    break;
                }
                await Task.Delay(checkInterval, stoppingToken);
                elapsed += checkInterval;
            }
        }
    }

    /// <summary>
    /// Executes a full scan of all image URLs in the database.
    /// </summary>
    private async Task<ImageHealthReportDto> RunScanAsync(CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("🖼️ Starting image URL health scan...");

        var report = new ImageHealthReportDto();
        var brokenByStatus = new Dictionary<int, int>();
        var batchSize = GetBatchSize();
        var timeoutSeconds = GetTimeoutSeconds();
        var offset = 0;
        var totalScanned = 0;
        var totalBroken = 0;
        var totalTimeouts = 0;

        using var httpClient = _httpClientFactory.CreateClient("ImageHealthScan");
        httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMediaRepository>();

        // Get total count for progress logging
        var totalImages = await repository.GetProcessedImageCountAsync(stoppingToken);
        _logger.LogInformation("🖼️ Found {TotalImages} processed images to scan", totalImages);

        while (!stoppingToken.IsCancellationRequested)
        {
            var images = await repository.GetImagesForHealthScanAsync(batchSize, offset, stoppingToken);
            if (images.Count == 0) break;

            var batchBroken = 0;
            var imagesToUpdate = new List<Domain.Entities.ImageMedia>();

            // Process batch with parallelism (max 10 concurrent requests to avoid overwhelming CDN)
            var semaphore = new SemaphoreSlim(10, 10);
            var tasks = images.Select(async image =>
            {
                await semaphore.WaitAsync(stoppingToken);
                try
                {
                    var (isBroken, httpStatus, isTimeout) = await CheckImageUrlAsync(httpClient, image.CdnUrl!, stoppingToken);

                    if (isBroken)
                    {
                        image.MarkAsBrokenImage(httpStatus);
                        Interlocked.Increment(ref batchBroken);

                        lock (brokenByStatus)
                        {
                            if (!brokenByStatus.ContainsKey(httpStatus))
                                brokenByStatus[httpStatus] = 0;
                            brokenByStatus[httpStatus]++;
                        }

                        if (isTimeout)
                            Interlocked.Increment(ref totalTimeouts);
                    }
                    else
                    {
                        image.MarkAsHealthyImage();
                    }

                    lock (imagesToUpdate)
                    {
                        imagesToUpdate.Add(image);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks);

            // Bulk update this batch
            if (imagesToUpdate.Count > 0)
            {
                await repository.BulkUpdateImageHealthStatusAsync(imagesToUpdate, stoppingToken);
            }

            totalScanned += images.Count;
            totalBroken += batchBroken;
            offset += batchSize;

            _logger.LogInformation("🖼️ Scan progress: {Scanned}/{Total} images, {Broken} broken so far",
                totalScanned, totalImages, totalBroken);
        }

        stopwatch.Stop();

        // Build the report
        var topDealers = await repository.GetTopDealersWithBrokenImagesAsync(10, stoppingToken);

        report.TotalImagesScanned = totalScanned;
        report.BrokenUrlCount = totalBroken;
        report.HealthyUrlCount = totalScanned - totalBroken;
        report.TimeoutCount = totalTimeouts;
        report.ScanDurationSeconds = stopwatch.Elapsed.TotalSeconds;
        report.BrokenByStatusCode = brokenByStatus;
        report.TopDealersWithBrokenImages = topDealers
            .Select(d => new DealerBrokenImageSummary { DealerId = d.DealerId, BrokenCount = d.BrokenCount })
            .ToList();

        _logger.LogInformation(
            "🖼️ Image URL health scan completed in {Duration:F1}s — " +
            "Total: {Total}, Healthy: {Healthy}, Broken: {Broken}, Health: {HealthPct}%",
            report.ScanDurationSeconds,
            report.TotalImagesScanned,
            report.HealthyUrlCount,
            report.BrokenUrlCount,
            report.HealthPercentage);

        // ═══════════════════════════════════════════════════════════════
        // PER-LISTING BROKEN IMAGE DETECTION
        // If >50% of a listing's images are broken, publish an alert event
        // to NotificationService for WhatsApp/email notification to dealer.
        // ═══════════════════════════════════════════════════════════════
        try
        {
            await DetectAndAlertBrokenListingsAsync(scope.ServiceProvider, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🖼️ Failed to detect/alert broken listings. Non-critical.");
        }

        return report;
    }

    /// <summary>
    /// Checks a single image URL with a HEAD request.
    /// Returns (isBroken, httpStatusCode, isTimeout).
    /// </summary>
    private static async Task<(bool IsBroken, int HttpStatus, bool IsTimeout)> CheckImageUrlAsync(
        HttpClient httpClient, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var statusCode = (int)response.StatusCode;

            if (BrokenStatusCodes.Contains(statusCode))
            {
                return (true, statusCode, false);
            }

            return (false, statusCode, false);
        }
        catch (TaskCanceledException)
        {
            // Timeout (>5 seconds)
            return (true, 0, true);
        }
        catch (HttpRequestException)
        {
            // Connection error — treat as broken
            return (true, 0, false);
        }
    }

    /// <summary>
    /// Determines if the daily email should be sent.
    /// Checks if current time in Dominican Republic (AST UTC-4) is between 8:00 and 8:59 AM.
    /// </summary>
    private bool ShouldSendDailyEmail()
    {
        try
        {
            var rdTimeZone = TimeZoneInfo.FindSystemTimeZoneById(DominicanRepublicTimeZoneId);
            var rdNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, rdTimeZone);

            // Send if it's 8:00 AM - 8:59 AM RD time (first scan of the day window)
            return rdNow.Hour == 8;
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback: UTC-4 manual calculation
            var rdNow = DateTime.UtcNow.AddHours(-4);
            return rdNow.Hour == 8;
        }
    }

    /// <summary>
    /// Sends the email report using the configured email service.
    /// </summary>
    private async Task SendEmailReportAsync(ImageHealthReportDto report, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IImageHealthReportEmailService>();
            await emailService.SendReportAsync(report, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🖼️ Failed to send daily email report");
        }
    }

    /// <summary>
    /// Detects listings where >50% of images are broken and publishes
    /// ListingBrokenImagesAlertEvent to RabbitMQ for NotificationService consumption.
    /// Groups broken images by OwnerId (vehicleId) to calculate per-listing statistics.
    /// </summary>
    private async Task DetectAndAlertBrokenListingsAsync(
        IServiceProvider scopedProvider,
        CancellationToken cancellationToken)
    {
        var repository = scopedProvider.GetRequiredService<IMediaRepository>();

        // Get per-owner (listing) broken image stats
        var listingStats = await repository.GetBrokenImageStatsByOwnerAsync(cancellationToken);

        var alertCount = 0;
        IEventPublisher? eventPublisher = null;
        try
        {
            eventPublisher = scopedProvider.GetService<IEventPublisher>();
        }
        catch
        {
            _logger.LogWarning("🖼️ IEventPublisher not available. Broken listing alerts will only be logged.");
        }

        foreach (var stat in listingStats)
        {
            if (stat.TotalImages == 0) continue;

            var brokenPercentage = (double)stat.BrokenCount / stat.TotalImages * 100;

            // Only alert if >50% of images are broken
            if (brokenPercentage <= 50) continue;

            _logger.LogWarning(
                "🖼️ Listing {OwnerId} has {Broken}/{Total} ({Pct:F1}%) broken images — exceeds 50% threshold",
                stat.OwnerId, stat.BrokenCount, stat.TotalImages, brokenPercentage);

            if (eventPublisher != null)
            {
                try
                {
                    var alertEvent = new ListingBrokenImagesAlertEvent
                    {
                        DealerId = stat.DealerId,
                        VehicleId = Guid.TryParse(stat.OwnerId, out var vid) ? vid : Guid.Empty,
                        Make = stat.OwnerId, // Will be enriched by NotificationService via VehiclesSaleService lookup
                        Model = string.Empty,
                        Year = 0,
                        SellerId = Guid.Empty, // Will be resolved by NotificationService
                        DealerWhatsApp = null, // Will be resolved by NotificationService
                        TotalImages = stat.TotalImages,
                        BrokenImages = stat.BrokenCount,
                        BrokenPercentage = Math.Round(brokenPercentage, 1),
                        BrokenStatusCodes = stat.BrokenStatusCodes
                    };

                    await eventPublisher.PublishAsync(alertEvent, cancellationToken);
                    alertCount++;

                    _logger.LogInformation(
                        "🖼️ Published ListingBrokenImagesAlertEvent for OwnerId={OwnerId}, DealerId={DealerId}",
                        stat.OwnerId, stat.DealerId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "🖼️ Failed to publish ListingBrokenImagesAlertEvent for OwnerId={OwnerId}",
                        stat.OwnerId);
                }
            }
        }

        if (alertCount > 0)
        {
            _logger.LogInformation("🖼️ Published {AlertCount} broken listing alerts", alertCount);
        }
    }

    private int GetScanIntervalHours() =>
        int.Parse(_configuration["ImageHealthScan:ScanIntervalHours"] ?? DefaultScanIntervalHours.ToString());

    private int GetBatchSize() =>
        int.Parse(_configuration["ImageHealthScan:BatchSize"] ?? DefaultBatchSize.ToString());

    private int GetTimeoutSeconds() =>
        int.Parse(_configuration["ImageHealthScan:TimeoutSeconds"] ?? DefaultTimeoutSeconds.ToString());
}
