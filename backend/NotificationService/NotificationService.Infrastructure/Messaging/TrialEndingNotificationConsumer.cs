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
/// Consumes SubscriptionTrialEndingEvent from RabbitMQ and sends
/// a warning email to the dealer with trial expiration info and CTA to subscribe.
/// 
/// RETENTION FIX: This consumer was completely missing — trial expiration had ZERO
/// notification, causing silent churn when trials ended without dealer awareness.
/// </summary>
public class TrialEndingNotificationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrialEndingNotificationConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "notificationservice.subscription.trial_ending";
    private const string RoutingKey = "billing.subscription.trial_ending";
    private const string DlxExchange = "cardealer.events.dlx";
    private const string DlqQueue = "notificationservice.subscription.trial_ending.dlq";

    public TrialEndingNotificationConsumer(
        IServiceProvider serviceProvider,
        ILogger<TrialEndingNotificationConsumer> logger,
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
            _logger.LogInformation("RabbitMQ is disabled. TrialEndingNotificationConsumer will not start.");
            return;
        }

        try
        {
            InitializeRabbitMQ();

            if (_channel == null)
            {
                _logger.LogWarning("RabbitMQ channel is null. TrialEndingNotificationConsumer will not start.");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var trialEvent = JsonSerializer.Deserialize<SubscriptionTrialEndingEvent>(message);

                    if (trialEvent != null)
                    {
                        _logger.LogInformation(
                            "Received SubscriptionTrialEndingEvent: DealerId={DealerId}, Plan={Plan}, DaysRemaining={Days}",
                            trialEvent.DealerId, trialEvent.TrialPlan, trialEvent.DaysRemaining);

                        await HandleTrialEndingAsync(trialEvent, stoppingToken);

                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize SubscriptionTrialEndingEvent");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing SubscriptionTrialEndingEvent");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("TrialEndingNotificationConsumer started listening on queue: {Queue}", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in TrialEndingNotificationConsumer");
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

            // Main exchange
            _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: true, autoDelete: false);

            // DLX exchange and DLQ
            _channel.ExchangeDeclare(exchange: DlxExchange, type: ExchangeType.Topic, durable: true, autoDelete: false);
            _channel.QueueDeclare(queue: DlqQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueBind(queue: DlqQueue, exchange: DlxExchange, routingKey: RoutingKey);

            // Main queue with DLQ support
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", DlxExchange },
                { "x-dead-letter-routing-key", RoutingKey }
            };

            _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
            _channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ initialized for TrialEndingNotificationConsumer with DLQ support");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for TrialEndingNotificationConsumer");
            throw;
        }
    }

    private async Task HandleTrialEndingAsync(SubscriptionTrialEndingEvent eventData, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<ITemplateEngine>();

        try
        {
            var subject = eventData.DaysRemaining <= 1
                ? "⚠️ Tu periodo de prueba termina MAÑANA - OKLA"
                : $"⏰ Tu periodo de prueba termina en {eventData.DaysRemaining} días - OKLA";

            var templateParams = new Dictionary<string, object>
            {
                { "DealerName", eventData.DealerName ?? "Dealer" },
                { "PlanName", eventData.TrialPlan ?? "Plan" },
                { "DaysRemaining", eventData.DaysRemaining.ToString() },
                { "TrialEndsAt", eventData.TrialEndsAt.ToString("dd/MM/yyyy") },
                { "MonthlyPrice", $"US${eventData.MonthlyPrice:N2}" },
                { "ActivateUrl", "https://okla.do/dashboard/billing" },
                { "Year", DateTime.UtcNow.Year.ToString() },
                { "UnsubscribeUrl", "https://okla.do/settings/notifications" }
            };

            var body = await templateEngine.RenderTemplateAsync("TrialEndingWarning", templateParams);

            await emailService.SendEmailAsync(
                to: eventData.DealerEmail,
                subject: subject,
                body: body,
                isHtml: true);

            _logger.LogInformation("Trial ending email sent to {Email}, {Days} days remaining",
                eventData.DealerEmail, eventData.DaysRemaining);

            // In-app notification
            var userNotifService = scope.ServiceProvider.GetService<IUserNotificationService>();
            if (userNotifService != null && eventData.DealerId != Guid.Empty)
            {
                await userNotifService.CreateAsync(
                    userId: eventData.DealerId,
                    type: "trial_ending",
                    title: $"⏰ Tu prueba termina en {eventData.DaysRemaining} día(s)",
                    message: $"Tu periodo de prueba del plan {eventData.TrialPlan} termina el {eventData.TrialEndsAt:dd/MM/yyyy}. Activa tu suscripción para no perder acceso.",
                    link: "/dashboard/billing");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending trial ending notification to {Email}", eventData.DealerEmail);
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
            _logger.LogError(ex, "Error disposing RabbitMQ connection in TrialEndingNotificationConsumer");
        }
        base.Dispose();
    }
}
