using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VehiclesSaleService.Domain.Interfaces;

namespace VehiclesSaleService.Infrastructure.Messaging;

/// <summary>
/// Consumes advertising campaign events from AdvertisingService via RabbitMQ and
/// synchronises vehicle promotion flags (IsFeatured, IsPremium) in the local DB.
///
/// Routing keys consumed:
///   advertising.campaign.activated    → Vehicle.MarkAsPremium / MarkAsFeaturedByAdmin
///   advertising.campaign.completed    → Vehicle.ClearPromotion
///   advertising.campaign.budget_depleted → Vehicle.ClearPromotion
/// </summary>
public class CampaignEventsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CampaignEventsConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "vehiclessaleservice.campaign-events";

    public CampaignEventsConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<CampaignEventsConsumer> logger)
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
                "RabbitMQ is disabled. CampaignEventsConsumer will not start.");
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

                // Idempotent declarations — safe to run multiple times.
                _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
                _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(QueueName, ExchangeName, "advertising.campaign.activated");
                _channel.QueueBind(QueueName, ExchangeName, "advertising.campaign.completed");
                _channel.QueueBind(QueueName, ExchangeName, "advertising.campaign.budget_depleted");

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += HandleMessageAsync;
                _channel.BasicConsume(QueueName, autoAck: false, consumer: consumer);

                _logger.LogInformation(
                    "CampaignEventsConsumer started. Listening on queue: {Queue}", QueueName);

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
                    "CampaignEventsConsumer: RabbitMQ connection attempt {Attempt}/{Max} failed. Retrying...",
                    attempt, maxRetries);

                if (attempt == maxRetries)
                {
                    _logger.LogError("CampaignEventsConsumer: All {Max} attempts exhausted. " +
                                     "Vehicle promotion flags will NOT be synced automatically.", maxRetries);
                    return;
                }

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        var body = Encoding.UTF8.GetString(ea.Body.Span);
        var routingKey = ea.RoutingKey;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var vehicleRepo = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();

            switch (routingKey)
            {
                case "advertising.campaign.activated":
                    await HandleCampaignActivatedAsync(body, vehicleRepo);
                    break;

                case "advertising.campaign.completed":
                case "advertising.campaign.budget_depleted":
                    await HandleCampaignEndedAsync(body, vehicleRepo);
                    break;

                default:
                    _logger.LogDebug("CampaignEventsConsumer: ignoring unknown routing key {Key}", routingKey);
                    break;
            }

            _channel!.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            // SECURITY: Sanitize body to prevent log injection (strip newlines, truncate)
            var sanitizedBody = SanitizeLogPayload(body);

            // RELIABILITY: Track retry count to prevent infinite poison message loop
            var retryCount = GetRetryCount(ea.BasicProperties);
            const int maxMessageRetries = 3;

            if (retryCount >= maxMessageRetries)
            {
                _logger.LogError(ex,
                    "Poison message detected [{RoutingKey}] after {Retries} retries. Sending to DLX. Body: {Body}",
                    routingKey, retryCount, sanitizedBody);
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); // → DLX
            }
            else
            {
                _logger.LogWarning(ex,
                    "Error processing campaign event [{RoutingKey}] (retry {Retry}/{Max}). Requeuing. Body: {Body}",
                    routingKey, retryCount + 1, maxMessageRetries, sanitizedBody);
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task HandleCampaignActivatedAsync(string body, IVehicleRepository vehicleRepo)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var evt = JsonSerializer.Deserialize<CampaignActivatedPayload>(body, options);

        if (evt?.VehicleId == null)
        {
            _logger.LogWarning("CampaignActivated event missing VehicleId. Body: {Body}", SanitizeLogPayload(body));
            return;
        }

        var vehicle = await vehicleRepo.GetByIdAsync(evt.VehicleId.Value);
        if (vehicle == null)
        {
            _logger.LogWarning(
                "Vehicle {VehicleId} not found for campaign {CampaignId}. Skipping promotion.",
                evt.VehicleId, evt.CampaignId);
            return;
        }

        if (string.Equals(evt.PlacementType, "PremiumSpot", StringComparison.OrdinalIgnoreCase))
            vehicle.MarkAsPremium(evt.CampaignId ?? Guid.Empty, priority: 100);
        else
            vehicle.MarkAsFeaturedByAdmin(priority: 50);

        await vehicleRepo.UpdateAsync(vehicle);

        _logger.LogInformation(
            "Vehicle {VehicleId} promoted via campaign {CampaignId} [{PlacementType}]",
            evt.VehicleId, evt.CampaignId, evt.PlacementType);
    }

    private async Task HandleCampaignEndedAsync(string body, IVehicleRepository vehicleRepo)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var evt = JsonSerializer.Deserialize<CampaignEndedPayload>(body, options);

        if (evt?.VehicleId == null) return;

        var vehicle = await vehicleRepo.GetByIdAsync(evt.VehicleId.Value);
        if (vehicle == null) return;

        // Only clear if this is the campaign that set the flag
        if (evt.CampaignId.HasValue && vehicle.LinkedCampaignId.HasValue
            && vehicle.LinkedCampaignId.Value != evt.CampaignId.Value)
        {
            _logger.LogDebug(
                "Campaign {CampaignId} ended but vehicle {VehicleId} is linked to a different campaign. Skipping.",
                evt.CampaignId, evt.VehicleId);
            return;
        }

        vehicle.ClearPromotion();
        await vehicleRepo.UpdateAsync(vehicle);

        _logger.LogInformation(
            "Vehicle {VehicleId} promotion cleared (campaign {CampaignId} ended).",
            evt.VehicleId, evt.CampaignId);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void Dispose()
    {
        try { _channel?.Close(); } catch { /* ignore */ }
        try { _connection?.Close(); } catch { /* ignore */ }
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// RELIABILITY: Extract the x-death retry count from RabbitMQ message headers.
    /// RabbitMQ populates x-death when a message is requeued via BasicNack.
    /// </summary>
    private static int GetRetryCount(RabbitMQ.Client.IBasicProperties properties)
    {
        if (properties?.Headers == null)
            return 0;

        if (!properties.Headers.TryGetValue("x-death", out var xDeath))
            return 0;

        if (xDeath is IList<object> deathEntries && deathEntries.Count > 0 &&
            deathEntries[0] is IDictionary<string, object> firstDeath &&
            firstDeath.TryGetValue("count", out var countObj))
        {
            return Convert.ToInt32(countObj);
        }

        return 0;
    }

    // ── Payload models (internal, only used for deserialisation) ─────────────

    private sealed record CampaignActivatedPayload(
        Guid? CampaignId,
        Guid? VehicleId,
        string? PlacementType);

    private sealed record CampaignEndedPayload(
        Guid? CampaignId,
        Guid? VehicleId);

    /// <summary>
    /// SECURITY: Sanitize message body for safe logging.
    /// Strips newlines/control chars (prevents log injection) and truncates to 500 chars.
    /// </summary>
    private static string SanitizeLogPayload(string payload)
    {
        if (string.IsNullOrEmpty(payload))
            return "[empty]";

        var sanitized = payload
            .Replace("\r", "")
            .Replace("\n", " ")
            .Replace("\t", " ");

        // Remove other control characters
        sanitized = new string(sanitized.Where(c => !char.IsControl(c) || c == ' ').ToArray());

        return sanitized.Length > 500
            ? string.Concat(sanitized.AsSpan(0, 500), "...[truncated]")
            : sanitized;
    }
}
