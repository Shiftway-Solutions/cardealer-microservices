using System.Text;
using System.Text.Json;
using AdvertisingService.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AdvertisingService.Infrastructure.Messaging.Consumers;

public class BillingPaymentCompletedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnection _connection;
    private readonly ILogger<BillingPaymentCompletedConsumer> _logger;
    private IModel? _channel;
    private IModel? _publishChannel;  // separate channel for outbound events

    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "advertising.billing.payment.completed";
    private const string RoutingKey = "billing.payment.completed";

    public BillingPaymentCompletedConsumer(
        IServiceScopeFactory scopeFactory,
        IConnection connection,
        ILogger<BillingPaymentCompletedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _connection = connection;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BillingPaymentCompletedConsumer starting...");

        try
        {
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(QueueName, ExchangeName, RoutingKey);

            // Separate channel for publishing campaign-activated events
            _publishChannel = _connection.CreateModel();

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogDebug("Received billing.payment.completed event: {Body}", body);

                try
                {
                    await ProcessPaymentCompletedAsync(body, stoppingToken);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing billing.payment.completed event");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(QueueName, autoAck: false, consumer);
            _logger.LogInformation("BillingPaymentCompletedConsumer listening on queue {Queue}", QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start BillingPaymentCompletedConsumer");
        }

        return Task.CompletedTask;
    }

    private async Task ProcessPaymentCompletedAsync(string body, CancellationToken ct)
    {
        var paymentEvent = JsonSerializer.Deserialize<BillingPaymentCompletedEvent>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (paymentEvent == null)
        {
            _logger.LogWarning("Failed to deserialize billing.payment.completed event");
            return;
        }

        // Only process advertising-related payments
        if (paymentEvent.PaymentType != "advertising_campaign")
        {
            _logger.LogDebug("Ignoring non-advertising payment {PaymentId}", paymentEvent.PaymentId);
            return;
        }

        if (!paymentEvent.CampaignId.HasValue)
        {
            _logger.LogWarning("Advertising payment {PaymentId} has no CampaignId", paymentEvent.PaymentId);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var campaignRepo = scope.ServiceProvider.GetRequiredService<IAdCampaignRepository>();

        var campaign = await campaignRepo.GetByIdAsync(paymentEvent.CampaignId.Value, ct);
        if (campaign == null)
        {
            _logger.LogWarning("Campaign {CampaignId} not found for payment {PaymentId}",
                paymentEvent.CampaignId, paymentEvent.PaymentId);
            return;
        }

        campaign.Activate(paymentEvent.PaymentId);
        await campaignRepo.UpdateAsync(campaign, ct);

        _logger.LogInformation(
            "Campaign {CampaignId} activated via billing payment {PaymentId}",
            campaign.Id, paymentEvent.PaymentId);

        // Publish advertising.campaign.activated so VehiclesSaleService can sync IsPremium
        PublishCampaignActivatedEvent(campaign.Id, campaign.VehicleId,
            campaign.PlacementType.ToString());
    }

    private void PublishCampaignActivatedEvent(Guid campaignId, Guid vehicleId, string placementType)
    {
        if (_publishChannel == null) return;

        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                CampaignId = campaignId,
                VehicleId  = vehicleId,
                PlacementType = placementType,
                ActivatedAt = DateTime.UtcNow
            });

            var props = _publishChannel.CreateBasicProperties();
            props.Persistent    = true;
            props.ContentType   = "application/json";

            _publishChannel.BasicPublish(
                exchange:   ExchangeName,
                routingKey: "advertising.campaign.activated",
                basicProperties: props,
                body:       Encoding.UTF8.GetBytes(payload));

            _logger.LogInformation(
                "Published advertising.campaign.activated for campaign {CampaignId}", campaignId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish advertising.campaign.activated for campaign {CampaignId}", campaignId);
        }
    }

    public override void Dispose()
    {
        _publishChannel?.Close();
        _publishChannel?.Dispose();
        _channel?.Close();
        _channel?.Dispose();
        base.Dispose();
    }
}

public class BillingPaymentCompletedEvent
{
    public Guid PaymentId { get; set; }
    public Guid UserId { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public Guid? CampaignId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "DOP";
    public DateTime CompletedAt { get; set; }
}
