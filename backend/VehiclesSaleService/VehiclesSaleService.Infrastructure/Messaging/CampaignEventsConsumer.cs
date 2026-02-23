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
    private const string QueueName    = "vehiclessaleservice.campaign-events";

    public CampaignEventsConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<CampaignEventsConsumer> logger)
    {
        _scopeFactory   = scopeFactory;
        _configuration  = configuration;
        _logger         = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitEnabled = _configuration.GetValue<bool>("RabbitMQ:Enabled");
        if (!rabbitEnabled)
        {
            _logger.LogInformation(
                "RabbitMQ is disabled. CampaignEventsConsumer will not start.");
            return Task.CompletedTask;
        }

        try
        {
            var factory = new ConnectionFactory
            {
                HostName    = _configuration["RabbitMQ:Host"] ?? "rabbitmq",
                Port        = _configuration.GetValue<int>("RabbitMQ:Port", 5672),
                UserName    = _configuration["RabbitMQ:Username"]
                              ?? throw new InvalidOperationException("RabbitMQ:Username is not configured"),
                Password    = _configuration["RabbitMQ:Password"]
                              ?? throw new InvalidOperationException("RabbitMQ:Password is not configured"),
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel    = _connection.CreateModel();

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start CampaignEventsConsumer. " +
                                 "Vehicle promotion flags will NOT be synced automatically.");
        }

        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        var body       = Encoding.UTF8.GetString(ea.Body.Span);
        var routingKey = ea.RoutingKey;

        try
        {
            using var scope       = _scopeFactory.CreateScope();
            var vehicleRepo       = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();

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
            _logger.LogError(ex,
                "Error processing campaign event [{RoutingKey}]. Message will be requeued. Body: {Body}",
                routingKey, body);
            _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task HandleCampaignActivatedAsync(string body, IVehicleRepository vehicleRepo)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var evt = JsonSerializer.Deserialize<CampaignActivatedPayload>(body, options);

        if (evt?.VehicleId == null)
        {
            _logger.LogWarning("CampaignActivated event missing VehicleId. Body: {Body}", body);
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
        try { _channel?.Close();    } catch { /* ignore */ }
        try { _connection?.Close(); } catch { /* ignore */ }
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }

    // ── Payload models (internal, only used for deserialisation) ─────────────

    private sealed record CampaignActivatedPayload(
        Guid? CampaignId,
        Guid? VehicleId,
        string? PlacementType);

    private sealed record CampaignEndedPayload(
        Guid? CampaignId,
        Guid? VehicleId);
}
