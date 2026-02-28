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
using NotificationService.Application.DTOs;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Background service que escucha eventos de pagos completados
/// y envía recibos por email
/// </summary>
public class PaymentReceiptNotificationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentReceiptNotificationConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "notificationservice.payment.completed";
    private const string RoutingKey = "billing.payment.completed";

    public PaymentReceiptNotificationConsumer(
        IServiceProvider serviceProvider,
        ILogger<PaymentReceiptNotificationConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Verificar si RabbitMQ está habilitado
        var rabbitMQEnabled = _configuration.GetValue<bool>("RabbitMQ:Enabled");
        if (!rabbitMQEnabled)
        {
            _logger.LogInformation("RabbitMQ is disabled. PaymentReceiptNotificationConsumer will not start.");
            return;
        }

        try
        {
            InitializeRabbitMQ();

            if (_channel == null)
            {
                _logger.LogError("Failed to initialize RabbitMQ channel");
                return;
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var paymentEvent = JsonSerializer.Deserialize<PaymentCompletedEvent>(message);

                    if (paymentEvent != null)
                    {
                        _logger.LogInformation(
                            "Received PaymentCompletedEvent: PaymentId={PaymentId}, Amount={Amount} {Currency}",
                            paymentEvent.PaymentId,
                            paymentEvent.Amount,
                            paymentEvent.Currency);

                        await HandlePaymentCompletedEventAsync(paymentEvent, stoppingToken);

                        // Acknowledge message
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        _logger.LogDebug("Message acknowledged: {MessageId}", ea.BasicProperties.MessageId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize PaymentCompletedEvent");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing PaymentCompletedEvent");

                    // Requeue message for retry
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("PaymentReceiptNotificationConsumer started listening on queue: {Queue}", QueueName);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in PaymentReceiptNotificationConsumer");
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
                UserName = _configuration["RabbitMQ:Username"] ?? throw new InvalidOperationException("RabbitMQ:Username is not configured"),
                Password = _configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured"),
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declarar exchange
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declarar queue
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Bind queue al exchange
            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey);

            // Set prefetch count
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ initialized successfully for PaymentReceiptNotificationConsumer");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
            throw;
        }
    }

    private async Task HandlePaymentCompletedEventAsync(
        PaymentCompletedEvent eventData,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var adminAlertService = scope.ServiceProvider.GetService<IAdminAlertService>();

        try
        {
            var subject = $"Recibo de Pago - ${eventData.Amount:N2} {eventData.Currency}";

            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto;'>
                        <div style='background-color: #28a745; color: white; padding: 20px; text-align: center;'>
                            <h1 style='margin: 0;'>✅ Pago Exitoso</h1>
                        </div>
                        
                        <div style='padding: 30px; background-color: #f8f9fa;'>
                            <p>Hola <strong>{eventData.UserName}</strong>,</p>
                            <p>Tu pago se ha procesado exitosamente. Aquí están los detalles:</p>
                            
                            <table style='width: 100%; background-color: white; border-radius: 5px; padding: 20px; margin: 20px 0;'>
                                <tr>
                                    <td style='padding: 10px; border-bottom: 1px solid #dee2e6;'>
                                        <strong>ID de Pago:</strong>
                                    </td>
                                    <td style='padding: 10px; border-bottom: 1px solid #dee2e6; text-align: right;'>
                                        {eventData.PaymentId}
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; border-bottom: 1px solid #dee2e6;'>
                                        <strong>Monto:</strong>
                                    </td>
                                    <td style='padding: 10px; border-bottom: 1px solid #dee2e6; text-align: right; font-size: 20px; color: #28a745;'>
                                        <strong>${eventData.Amount:N2} {eventData.Currency}</strong>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; border-bottom: 1px solid #dee2e6;'>
                                        <strong>Fecha:</strong>
                                    </td>
                                    <td style='padding: 10px; border-bottom: 1px solid #dee2e6; text-align: right;'>
                                        {eventData.PaidAt:dd/MM/yyyy HH:mm:ss}
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; border-bottom: 1px solid #dee2e6;'>
                                        <strong>Descripción:</strong>
                                    </td>
                                    <td style='padding: 10px; border-bottom: 1px solid #dee2e6; text-align: right;'>
                                        {eventData.Description}
                                    </td>
                                </tr>";

            if (!string.IsNullOrEmpty(eventData.SubscriptionPlan))
            {
                body += $@"
                                <tr>
                                    <td style='padding: 10px; border-bottom: 1px solid #dee2e6;'>
                                        <strong>Plan:</strong>
                                    </td>
                                    <td style='padding: 10px; border-bottom: 1px solid #dee2e6; text-align: right;'>
                                        {eventData.SubscriptionPlan}
                                    </td>
                                </tr>";
            }

            body += $@"
                                <tr>
                                    <td style='padding: 10px;'>
                                        <strong>Stripe Payment ID:</strong>
                                    </td>
                                    <td style='padding: 10px; text-align: right; font-family: monospace; font-size: 12px;'>
                                        {eventData.StripePaymentIntentId}
                                    </td>
                                </tr>
                            </table>
                            
                            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;'>
                                <p style='margin: 0;'><strong>Nota:</strong> Guarda este correo como comprobante de tu pago.</p>
                            </div>
                            
                            <p>Si tienes alguna pregunta sobre este pago, no dudes en contactarnos.</p>
                            
                            <p style='margin-top: 30px;'>
                                <a href='https://cardealer.com/billing/payments' 
                                   style='display: inline-block; background-color: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px;'>
                                    Ver Historial de Pagos
                                </a>
                            </p>
                        </div>
                        
                        <div style='background-color: #343a40; color: #adb5bd; padding: 20px; text-align: center; font-size: 12px;'>
                            <p style='margin: 0;'>CarDealer - Plataforma de Venta de Vehículos</p>
                            <p style='margin: 5px 0 0 0;'>Este es un recibo automático. No responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await emailService.SendEmailAsync(
                to: eventData.UserEmail,
                subject: subject,
                body: body,
                isHtml: true);

            _logger.LogInformation(
                "Payment receipt email sent to {Email} for PaymentId: {PaymentId}",
                eventData.UserEmail,
                eventData.PaymentId);

            // Persist in-app user notification
            if (eventData.UserId != Guid.Empty)
            {
                var userNotifService = scope.ServiceProvider.GetService<IUserNotificationService>();
                if (userNotifService != null)
                {
                    await userNotifService.CreateAsync(
                        userId: eventData.UserId,
                        type: "payment_received",
                        title: "💳 Pago procesado",
                        message: $"Tu pago de RD${eventData.Amount:N0} {eventData.Currency} fue procesado exitosamente.",
                        icon: "💳",
                        link: "/cuenta/facturacion",
                        cancellationToken: cancellationToken);
                }
            }

            // Send admin alert for payment received
            if (adminAlertService != null)
            {
                try
                {
                    await adminAlertService.SendAlertAsync(
                        alertType: "payment_received",
                        title: $"💰 Pago Recibido - ${eventData.Amount:N2} {eventData.Currency}",
                        message: $"Pago de {eventData.UserName} ({eventData.UserEmail}) por ${eventData.Amount:N2} {eventData.Currency}. Plan: {eventData.SubscriptionPlan ?? "N/A"}.",
                        severity: "Info",
                        metadata: new Dictionary<string, string>
                        {
                            ["PaymentId"] = eventData.PaymentId.ToString(),
                            ["Amount"] = $"{eventData.Amount:N2}",
                            ["Currency"] = eventData.Currency,
                            ["UserEmail"] = eventData.UserEmail,
                            ["Plan"] = eventData.SubscriptionPlan ?? "N/A"
                        },
                        cancellationToken);
                }
                catch (Exception alertEx)
                {
                    _logger.LogWarning(alertEx, "Failed to send admin alert for payment {PaymentId}", eventData.PaymentId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send payment receipt email for PaymentId: {PaymentId}",
                eventData.PaymentId);
            throw;
        }
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }

        base.Dispose();
    }
}
