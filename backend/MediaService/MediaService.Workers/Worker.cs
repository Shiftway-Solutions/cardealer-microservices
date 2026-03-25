using MediaService.Workers.Handlers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MediaService.Workers;

/// <summary>
/// Background worker that consumes RabbitMQ 'media.process' queue
/// and delegates to ImageProcessingHandler for async variant generation.
/// </summary>
public class ImageProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImageProcessingWorker> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    private const string QueueName = "media.process";
    private const string ExchangeName = "media.commands";
    private const string RoutingKey = "media.process";
    private const string DlxExchange = "media.commands.dlx";
    private const string DlqName = "media.process.dlq";

    public ImageProcessingWorker(
        IServiceProvider serviceProvider,
        ILogger<ImageProcessingWorker> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ImageProcessingWorker starting — listening on queue '{Queue}'", QueueName);

        try
        {
            SetupRabbitMq();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ. ImageProcessingWorker will not process messages.");
            // Wait and retry in case RabbitMQ is not ready yet
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            return;
        }

        if (_channel == null) return;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogDebug("Received message from queue '{Queue}': {Message}", QueueName, message);

            try
            {
                var command = JsonSerializer.Deserialize<ProcessMediaMessage>(message, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (command?.MediaId is not null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<ImageProcessingHandler>();
                    await handler.HandleAsync(command.MediaId, stoppingToken);
                }

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("Successfully processed media {MediaId}", command?.MediaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue '{Queue}'", QueueName);
                // Nack with requeue=false (will go to DLQ via DLX)
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        // Keep alive until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ImageProcessingWorker stopping");
        }
    }

    private void SetupRabbitMq()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare DLX exchange and DLQ
        _channel.ExchangeDeclare(DlxExchange, ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(DlqName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(DlqName, DlxExchange, RoutingKey);

        // Declare main exchange and queue with DLX
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", DlxExchange },
                { "x-dead-letter-routing-key", RoutingKey }
            });
        _channel.QueueBind(QueueName, ExchangeName, RoutingKey);

        // Prefetch 1 — process one image at a time (image processing is CPU-intensive)
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation("RabbitMQ connected. Queue: {Queue}, Exchange: {Exchange}", QueueName, ExchangeName);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Periodic cleanup worker that runs every 6 hours to clean up
/// stale uploads (>48h) and orphaned media (>7 days).
/// </summary>
public class MediaCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MediaCleanupWorker> _logger;

    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(6);

    public MediaCleanupWorker(
        IServiceProvider serviceProvider,
        ILogger<MediaCleanupWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MediaCleanupWorker starting — cleanup every {Hours}h", CleanupInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<MediaCleanupHandler>();
                await handler.HandleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during media cleanup cycle");
            }

            try
            {
                await Task.Delay(CleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("MediaCleanupWorker stopping");
            }
        }
    }
}

/// <summary>DTO for deserializing RabbitMQ process media messages</summary>
public record ProcessMediaMessage(string? MediaId, string? ProcessingType = null);

/// <summary>
/// Periodic worker that refreshes S3 presigned URLs for all processed media assets.
/// Runs every 5 days to ensure URLs are refreshed before the 7-day AWS presigned URL expiry.
/// Only activates when Storage__S3__UseAcl=false (private bucket mode).
/// </summary>
public class MediaUrlRefreshWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MediaUrlRefreshWorker> _logger;
    private readonly IConfiguration _configuration;

    // Refresh every 5 days — well within the 7-day presigned URL expiry window
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromDays(5);

    public MediaUrlRefreshWorker(
        IServiceProvider serviceProvider,
        ILogger<MediaUrlRefreshWorker> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Only run when bucket is private (UseAcl=false) — public buckets don't need presigned URL refresh
        var useAcl = _configuration.GetValue<bool>("Storage:S3:UseAcl", false);
        if (useAcl)
        {
            _logger.LogInformation("MediaUrlRefreshWorker disabled — UseAcl=true (public bucket), presigned URLs not required");
            return;
        }

        _logger.LogInformation("MediaUrlRefreshWorker starting — refresh every {Days}d (private S3 bucket, UseAcl=false)",
            RefreshInterval.TotalDays);

        // Initial delay of 2 minutes to let other services start
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("[MediaUrlRefresh] Starting scheduled presigned URL refresh at {Time}", DateTime.UtcNow);

                using var scope = _serviceProvider.CreateScope();
                var mediaRepository = scope.ServiceProvider.GetRequiredService<MediaService.Domain.Interfaces.Repositories.IMediaRepository>();
                var storageService = scope.ServiceProvider.GetRequiredService<MediaService.Domain.Interfaces.Services.IMediaStorageService>();

                var processedAssets = await mediaRepository.GetByStatusAsync(
                    MediaService.Domain.Enums.MediaStatus.Processed, stoppingToken);

                var assets = processedAssets.Where(a => !string.IsNullOrEmpty(a.StorageKey)).ToList();
                int refreshed = 0, failed = 0, skipped = 0;
                const int BatchSize = 100;
                var batch = new List<MediaService.Domain.Entities.MediaAsset>(BatchSize);

                foreach (var asset in assets)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        var freshUrl = await storageService.GetFileUrlAsync(asset.StorageKey);
                        if (freshUrl != asset.CdnUrl)
                        {
                            asset.MarkAsProcessed(freshUrl);
                            batch.Add(asset);
                            refreshed++;
                        }
                        else
                        {
                            skipped++;
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _logger.LogWarning(ex, "[MediaUrlRefresh] Failed to refresh URL for asset {Id}", asset.Id);
                    }

                    if (batch.Count >= BatchSize)
                    {
                        await FlushBatchAsync(mediaRepository, batch, stoppingToken);
                        batch.Clear();
                    }
                }

                if (batch.Count > 0)
                    await FlushBatchAsync(mediaRepository, batch, stoppingToken);

                _logger.LogInformation(
                    "[MediaUrlRefresh] Completed. Inspected={Inspected} Refreshed={Refreshed} Failed={Failed} Skipped={Skipped}",
                    assets.Count, refreshed, failed, skipped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MediaUrlRefresh] Unhandled error during scheduled URL refresh");
            }

            try
            {
                await Task.Delay(RefreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("MediaUrlRefreshWorker stopping");
            }
        }
    }

    private static async Task FlushBatchAsync(
        MediaService.Domain.Interfaces.Repositories.IMediaRepository repo,
        List<MediaService.Domain.Entities.MediaAsset> batch,
        CancellationToken cancellationToken)
    {
        foreach (var asset in batch)
        {
            try { await repo.UpdateAsync(asset, cancellationToken); }
            catch { /* individual update failure logged at caller */ }
        }
    }
}

