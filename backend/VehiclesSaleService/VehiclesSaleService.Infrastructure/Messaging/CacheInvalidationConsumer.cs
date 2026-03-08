using System.Text;
using System.Text.Json;
using CarDealer.Shared.Caching.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace VehiclesSaleService.Infrastructure.Messaging;

/// <summary>
/// Consumes vehicle lifecycle events from RabbitMQ and invalidates the
/// corresponding Redis/memory cache entries so stale data is never served.
///
/// Routing keys consumed:
///   vehicle.vehicle.created   → invalidate featured + catalog caches
///   vehicle.vehicle.updated   → invalidate detail + featured + catalog
///   vehicle.vehicle.deleted   → invalidate detail + featured + catalog
///   vehicle.vehicle.sold      → invalidate detail + featured
///   vehicle.vehicle.published → invalidate featured + catalog
/// </summary>
public class CacheInvalidationConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CacheInvalidationConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "vehiclessaleservice.cache-invalidation";

    private static readonly string[] RoutingKeys =
    {
        "vehicle.vehicle.created",
        "vehicle.vehicle.updated",
        "vehicle.vehicle.deleted",
        "vehicle.vehicle.sold",
        "vehicle.vehicle.published",
        "vehicles.vehicle.published" // alternate naming convention
    };

    public CacheInvalidationConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<CacheInvalidationConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitEnabled = _configuration.GetValue<bool>("RabbitMQ:Enabled");
        if (!rabbitEnabled)
        {
            _logger.LogInformation(
                "RabbitMQ is disabled. CacheInvalidationConsumer will not start.");
            return;
        }

        // RELIABILITY: Retry loop for RabbitMQ connection (handles K8s startup races)
        const int maxRetries = 10;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:Host"] ?? "rabbitmq",
                    Port = _configuration.GetValue<int>("RabbitMQ:Port", 5672),
                    UserName = _configuration["RabbitMQ:Username"]
                               ?? throw new InvalidOperationException("RabbitMQ:Username is not configured"),
                    Password = _configuration["RabbitMQ:Password"]
                               ?? throw new InvalidOperationException("RabbitMQ:Password is not configured"),
                    VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                    DispatchConsumersAsync = true
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Idempotent declarations
                _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
                _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);

                foreach (var key in RoutingKeys)
                {
                    _channel.QueueBind(QueueName, ExchangeName, key);
                }

                // Single prefetch — process one at a time for safety
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += HandleMessageAsync;
                _channel.BasicConsume(QueueName, autoAck: false, consumer: consumer);

                _logger.LogInformation(
                    "CacheInvalidationConsumer started — listening on queue {Queue} for {Count} routing keys",
                    QueueName, RoutingKeys.Length);

                // Keep ExecuteAsync alive so BackgroundService lifecycle is properly managed
                await Task.Delay(Timeout.Infinite, stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return; // Graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "CacheInvalidationConsumer: RabbitMQ connection attempt {Attempt}/{Max} failed. Retrying...",
                    attempt, maxRetries);

                if (attempt == maxRetries)
                {
                    _logger.LogError("CacheInvalidationConsumer: All {Max} connection attempts exhausted. Consumer will not start.", maxRetries);
                    return;
                }

                // Exponential backoff: 2s, 4s, 8s, 16s, ...
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var routingKey = ea.RoutingKey;
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            _logger.LogDebug(
                "CacheInvalidation received event: {RoutingKey}", routingKey);

            // Extract vehicleId from the payload
            var vehicleId = ExtractVehicleId(body);

            using var scope = _scopeFactory.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            await InvalidateCachesAsync(cacheService, routingKey, vehicleId);

            _channel?.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing cache invalidation for routing key {RoutingKey}",
                ea.RoutingKey);

            // Nack without requeue — cache invalidation failures are non-critical.
            // The cache will expire naturally via TTL.
            _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private async Task InvalidateCachesAsync(
        ICacheService cacheService, string routingKey, Guid? vehicleId)
    {
        var tasks = new List<Task>();

        // Always invalidate featured + catalog on any vehicle event
        tasks.Add(cacheService.InvalidateByPatternAsync("vehicle:featured:*"));
        tasks.Add(cacheService.InvalidateByPatternAsync("catalog:makes:*"));

        // Invalidate specific vehicle detail cache if we have an ID
        if (vehicleId.HasValue && vehicleId != Guid.Empty)
        {
            tasks.Add(cacheService.RemoveAsync($"vehicle:detail:{vehicleId}"));
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "Cache invalidated for event {RoutingKey}, vehicleId={VehicleId}",
            routingKey, vehicleId?.ToString() ?? "N/A");
    }

    private static Guid? ExtractVehicleId(string jsonBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonBody);
            var root = doc.RootElement;

            // Try common property names for vehicle ID
            if (root.TryGetProperty("VehicleId", out var vid) && vid.TryGetGuid(out var id1))
                return id1;
            if (root.TryGetProperty("vehicleId", out var vid2) && vid2.TryGetGuid(out var id2))
                return id2;
            if (root.TryGetProperty("Id", out var vid3) && vid3.TryGetGuid(out var id3))
                return id3;
            if (root.TryGetProperty("id", out var vid4) && vid4.TryGetGuid(out var id4))
                return id4;

            return null;
        }
        catch
        {
            return null;
        }
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
