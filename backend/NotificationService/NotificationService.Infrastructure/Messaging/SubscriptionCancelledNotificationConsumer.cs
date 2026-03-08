using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CarDealer.Contracts.Events.Billing;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Consumes SubscriptionCancelledEvent from RabbitMQ and sends:
/// 1. Immediate cancellation confirmation email
/// 2. Win-back offer email after configurable delay (default 7 days, handled by SubscriptionRenewalWorker)
/// 
/// RETENTION FIX: Cancellation had ZERO communication — dealers cancelled silently
/// with no feedback loop, no win-back opportunity, and no confirmation email.
/// </summary>
public class SubscriptionCancelledNotificationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionCancelledNotificationConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "notificationservice.subscription.cancelled";
    private const string RoutingKey = "billing.subscription.cancelled";
    private const string DlxExchange = "cardealer.events.dlx";
    private const string DlqQueue = "notificationservice.subscription.cancelled.dlq";

    public SubscriptionCancelledNotificationConsumer(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionCancelledNotificationConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitMQEnabled = _configuration.GetValue<bool>("RabbitMQ:Enabled");
        if (!rabbitMQEnabled)
        {
            _logger.LogInformation("RabbitMQ is disabled. SubscriptionCancelledNotificationConsumer will not start.");
            return;
        }

        try
        {
            InitializeRabbitMQ();

            if (_channel == null)
            {
                _logger.LogWarning("RabbitMQ channel is null. SubscriptionCancelledNotificationConsumer will not start.");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var cancelEvent = JsonSerializer.Deserialize<SubscriptionCancelledEvent>(message);

                    if (cancelEvent != null)
                    {
                        _logger.LogInformation(
                            "Received SubscriptionCancelledEvent: DealerId={DealerId}, Plan={Plan}, Reason={Reason}",
                            cancelEvent.DealerId, cancelEvent.PreviousPlan, cancelEvent.CancellationReasonType);

                        await HandleCancellationAsync(cancelEvent, stoppingToken);

                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize SubscriptionCancelledEvent");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing SubscriptionCancelledEvent");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("SubscriptionCancelledNotificationConsumer started listening on queue: {Queue}", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in SubscriptionCancelledNotificationConsumer");
        }
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"] ?? throw new InvalidOperationException("RabbitMQ:Username not configured"),
                Password = _configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password not configured"),
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: true, autoDelete: false);

            // DLX + DLQ
            _channel.ExchangeDeclare(exchange: DlxExchange, type: ExchangeType.Topic, durable: true, autoDelete: false);
            _channel.QueueDeclare(queue: DlqQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueBind(queue: DlqQueue, exchange: DlxExchange, routingKey: RoutingKey);

            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", DlxExchange },
                { "x-dead-letter-routing-key", RoutingKey }
            };

            _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
            _channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ initialized for SubscriptionCancelledNotificationConsumer with DLQ support");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for SubscriptionCancelledNotificationConsumer");
            throw;
        }
    }

    private async Task HandleCancellationAsync(SubscriptionCancelledEvent eventData, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<ITemplateEngine>();

        try
        {
            // 1. Send cancellation confirmation email
            var subject = "Tu suscripción ha sido cancelada - OKLA";

            var templateParams = new Dictionary<string, object>
            {
                { "DealerName", eventData.DealerName ?? "Dealer" },
                { "PlanName", eventData.PreviousPlan ?? "Plan" },
                { "MonthlyAmount", $"US${eventData.MonthlyAmount:N2}" },
                { "DaysOnPlan", eventData.DaysOnPlan.ToString() },
                { "EffectiveAt", eventData.EffectiveAt?.ToString("dd/MM/yyyy") ?? "Inmediata" },
                { "ReactivateUrl", "https://okla.do/dashboard/billing/reactivate" },
                { "FeedbackUrl", $"https://okla.do/feedback/cancellation?dealerId={eventData.DealerId}" },
                { "Year", DateTime.UtcNow.Year.ToString() },
                { "UnsubscribeUrl", "https://okla.do/settings/notifications" }
            };

            var body = await templateEngine.RenderTemplateAsync("SubscriptionCancelled", templateParams);

            await emailService.SendEmailAsync(
                to: eventData.DealerEmail,
                subject: subject,
                body: body,
                isHtml: true);

            _logger.LogInformation(
                "Cancellation confirmation email sent to {Email}, plan={Plan}, reason={Reason}",
                eventData.DealerEmail, eventData.PreviousPlan, eventData.CancellationReasonType);

            // 2. In-app notification
            var userNotifService = scope.ServiceProvider.GetService<IUserNotificationService>();
            if (userNotifService != null && eventData.DealerId != Guid.Empty)
            {
                await userNotifService.CreateAsync(
                    userId: eventData.DealerId,
                    type: "subscription_cancelled",
                    title: "Suscripción cancelada",
                    message: $"Tu suscripción {eventData.PreviousPlan} ha sido cancelada. Mantienes acceso hasta {eventData.EffectiveAt?.ToString("dd/MM/yyyy") ?? "ahora"}.",
                    link: "/dashboard/billing");
            }

            // 3. Log cancellation reason for analytics
            _logger.LogWarning(
                "CHURN_EVENT: DealerId={DealerId}, Plan={Plan}, Reason={Reason}, Details={Details}, DaysOnPlan={Days}, MRR_Lost={Amount}",
                eventData.DealerId,
                eventData.PreviousPlan,
                eventData.CancellationReasonType,
                eventData.CancellationReasonDetails,
                eventData.DaysOnPlan,
                eventData.MonthlyAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending cancellation notification to {Email}", eventData.DealerEmail);
            throw;
        }
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection in SubscriptionCancelledNotificationConsumer");
        }
        base.Dispose();
    }
}
