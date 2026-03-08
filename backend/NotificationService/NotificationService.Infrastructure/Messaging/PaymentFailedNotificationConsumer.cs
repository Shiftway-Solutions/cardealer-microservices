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
/// Consumes SubscriptionPaymentFailedEvent from RabbitMQ and sends
/// an actionable email to the dealer with payment update instructions.
/// 
/// RETENTION FIX: Payment failures had NO notification — dealers didn't know
/// their card was declined until their account was silently suspended.
/// </summary>
public class PaymentFailedNotificationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentFailedNotificationConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "notificationservice.subscription.payment_failed";
    private const string RoutingKey = "billing.subscription.payment_failed";
    private const string DlxExchange = "cardealer.events.dlx";
    private const string DlqQueue = "notificationservice.subscription.payment_failed.dlq";

    public PaymentFailedNotificationConsumer(
        IServiceProvider serviceProvider,
        ILogger<PaymentFailedNotificationConsumer> logger,
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
            _logger.LogInformation("RabbitMQ is disabled. PaymentFailedNotificationConsumer will not start.");
            return;
        }

        try
        {
            InitializeRabbitMQ();

            if (_channel == null)
            {
                _logger.LogWarning("RabbitMQ channel is null. PaymentFailedNotificationConsumer will not start.");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var paymentEvent = JsonSerializer.Deserialize<SubscriptionPaymentFailedEvent>(message);

                    if (paymentEvent != null)
                    {
                        _logger.LogInformation(
                            "Received SubscriptionPaymentFailedEvent: DealerId={DealerId}, Attempt={Attempt}, Amount={Amount}",
                            paymentEvent.DealerId, paymentEvent.AttemptNumber, paymentEvent.Amount);

                        await HandlePaymentFailedAsync(paymentEvent, stoppingToken);

                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize SubscriptionPaymentFailedEvent");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing SubscriptionPaymentFailedEvent");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("PaymentFailedNotificationConsumer started listening on queue: {Queue}", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in PaymentFailedNotificationConsumer");
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

            _logger.LogInformation("RabbitMQ initialized for PaymentFailedNotificationConsumer with DLQ support");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for PaymentFailedNotificationConsumer");
            throw;
        }
    }

    private async Task HandlePaymentFailedAsync(SubscriptionPaymentFailedEvent eventData, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<ITemplateEngine>();

        try
        {
            var urgencyPrefix = eventData.AttemptNumber >= 3 ? "🚨 URGENTE: " : "⚠️ ";
            var subject = $"{urgencyPrefix}No pudimos procesar tu pago - OKLA";

            var templateParams = new Dictionary<string, object>
            {
                { "DealerName", eventData.DealerName ?? "Dealer" },
                { "PlanName", eventData.Plan ?? "Plan" },
                { "Amount", $"US${eventData.Amount:N2}" },
                { "Currency", eventData.Currency ?? "USD" },
                { "AttemptNumber", eventData.AttemptNumber.ToString() },
                { "FailureReason", TranslateFailureReason(eventData.FailureReason) },
                { "NextRetryDate", eventData.NextRetryAt?.ToString("dd/MM/yyyy") ?? "N/A" },
                { "UpdatePaymentUrl", "https://okla.do/dashboard/billing/payment-methods" },
                { "Year", DateTime.UtcNow.Year.ToString() },
                { "UnsubscribeUrl", "https://okla.do/settings/notifications" }
            };

            var body = await templateEngine.RenderTemplateAsync("PaymentFailed", templateParams);

            await emailService.SendEmailAsync(
                to: eventData.DealerEmail,
                subject: subject,
                body: body,
                isHtml: true);

            _logger.LogInformation("Payment failed email sent to {Email}, attempt #{Attempt}",
                eventData.DealerEmail, eventData.AttemptNumber);

            // In-app notification (high priority)
            var userNotifService = scope.ServiceProvider.GetService<IUserNotificationService>();
            if (userNotifService != null && eventData.DealerId != Guid.Empty)
            {
                await userNotifService.CreateAsync(
                    userId: eventData.DealerId,
                    type: "payment_failed",
                    title: "⚠️ Problema con tu pago",
                    message: $"No pudimos cobrar US${eventData.Amount:N2} de tu suscripción {eventData.Plan}. Actualiza tu método de pago para evitar la suspensión.",
                    link: "/dashboard/billing/payment-methods");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment failed notification to {Email}", eventData.DealerEmail);
            throw;
        }
    }

    /// <summary>
    /// Translate Stripe failure reasons to user-friendly Spanish messages.
    /// </summary>
    private static string TranslateFailureReason(string? reason) => reason?.ToLower() switch
    {
        "card_declined" => "Tarjeta rechazada",
        "insufficient_funds" => "Fondos insuficientes",
        "expired_card" => "Tarjeta vencida",
        "incorrect_cvc" => "CVC incorrecto",
        "processing_error" => "Error de procesamiento del banco",
        "authentication_required" => "Requiere autenticación 3D Secure",
        "do_not_honor" => "Transacción rechazada por el banco",
        "generic_decline" => "Rechazada por el banco",
        _ => reason ?? "Error desconocido"
    };

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
            _logger.LogError(ex, "Error disposing RabbitMQ connection in PaymentFailedNotificationConsumer");
        }
        base.Dispose();
    }
}
