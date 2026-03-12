using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CarDealer.Contracts.Events.Billing;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Consumes ReportPurchaseCompletedEvent from RabbitMQ and sends
/// a receipt email to the buyer with the OKLA Score report purchase details.
/// </summary>
public class ReportPurchaseReceiptConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReportPurchaseReceiptConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "notificationservice.report.purchase.completed";
    private const string RoutingKey = "billing.report.purchase.completed";

    public ReportPurchaseReceiptConsumer(
        IServiceProvider serviceProvider,
        ILogger<ReportPurchaseReceiptConsumer> logger,
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
            _logger.LogInformation("RabbitMQ is disabled. ReportPurchaseReceiptConsumer will not start.");
            return;
        }

        try
        {
            InitializeRabbitMQ();

            if (_channel == null)
            {
                _logger.LogWarning("RabbitMQ channel is null. ReportPurchaseReceiptConsumer will not start");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (_, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var purchaseEvent = JsonSerializer.Deserialize<ReportPurchaseCompletedEvent>(message);

                    if (purchaseEvent != null)
                    {
                        _logger.LogInformation(
                            "Received ReportPurchaseCompletedEvent: PurchaseId={PurchaseId}, Vehicle={VehicleId}, Email={Email}",
                            purchaseEvent.PurchaseId,
                            purchaseEvent.VehicleId,
                            purchaseEvent.BuyerEmail);

                        await HandleReportPurchaseAsync(purchaseEvent, stoppingToken);
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize ReportPurchaseCompletedEvent");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing ReportPurchaseCompletedEvent");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("ReportPurchaseReceiptConsumer started listening on queue: {Queue}", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in ReportPurchaseReceiptConsumer");
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
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ initialized for ReportPurchaseReceiptConsumer");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection for ReportPurchaseReceiptConsumer");
            throw;
        }
    }

    private async Task HandleReportPurchaseAsync(
        ReportPurchaseCompletedEvent eventData,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            var amountDisplay = eventData.Currency.Equals("usd", StringComparison.OrdinalIgnoreCase)
                ? $"US${eventData.AmountCents / 100m:N2}"
                : $"RD${eventData.AmountCents / 100m:N0}";

            var subject = $"Recibo — Informe OKLA Score™ ({amountDisplay})";

            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; margin: 0; padding: 0;'>
                    <div style='max-width: 600px; margin: 0 auto;'>
                        <div style='background-color: #1a56db; color: white; padding: 24px; text-align: center;'>
                            <h1 style='margin: 0; font-size: 22px;'>✅ Compra Exitosa — OKLA Score™</h1>
                        </div>

                        <div style='padding: 30px; background-color: #f9fafb;'>
                            <p>Hola,</p>
                            <p>Tu compra del <strong>Informe OKLA Score™ completo</strong> se procesó exitosamente.</p>

                            <table style='width: 100%; background-color: white; border-radius: 8px; padding: 20px; margin: 20px 0; border: 1px solid #e5e7eb;'>
                                <tr>
                                    <td style='padding: 10px; border-bottom: 1px solid #f3f4f6;'><strong>ID de Compra:</strong></td>
                                    <td style='padding: 10px; border-bottom: 1px solid #f3f4f6; text-align: right; font-family: monospace; font-size: 12px;'>{eventData.PurchaseId}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; border-bottom: 1px solid #f3f4f6;'><strong>Vehículo:</strong></td>
                                    <td style='padding: 10px; border-bottom: 1px solid #f3f4f6; text-align: right;'>{eventData.VehicleId}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; border-bottom: 1px solid #f3f4f6;'><strong>Monto:</strong></td>
                                    <td style='padding: 10px; border-bottom: 1px solid #f3f4f6; text-align: right; font-size: 18px; color: #059669;'><strong>{amountDisplay}</strong></td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; border-bottom: 1px solid #f3f4f6;'><strong>Fecha:</strong></td>
                                    <td style='padding: 10px; border-bottom: 1px solid #f3f4f6; text-align: right;'>{eventData.CompletedAt:dd/MM/yyyy HH:mm:ss} UTC</td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px;'><strong>Stripe Ref:</strong></td>
                                    <td style='padding: 10px; text-align: right; font-family: monospace; font-size: 12px;'>{eventData.StripePaymentIntentId}</td>
                                </tr>
                            </table>

                            <div style='background-color: #dbeafe; border-left: 4px solid #1a56db; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                                <p style='margin: 0;'><strong>Tu informe está disponible ahora.</strong> Accede al desglose completo de las 7 dimensiones directamente en la página del vehículo.</p>
                            </div>

                            <div style='background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                                <p style='margin: 0;'><strong>Consejo:</strong> Si te registras en OKLA con este mismo email ({eventData.BuyerEmail}), el informe se vinculará automáticamente a tu cuenta y lo podrás consultar desde tu historial.</p>
                            </div>

                            <p style='margin-top: 20px; text-align: center;'>
                                <a href='https://okla.com.do/vehiculos?reportAccess={eventData.PurchaseId}'
                                   style='display: inline-block; background-color: #1a56db; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: bold;'>
                                    Ver Mi Informe
                                </a>
                            </p>
                        </div>

                        <div style='background-color: #1f2937; color: #9ca3af; padding: 20px; text-align: center; font-size: 12px;'>
                            <p style='margin: 0;'>OKLA — Marketplace de Vehículos de República Dominicana</p>
                            <p style='margin: 5px 0 0 0;'>Guarda este correo como comprobante de pago. No responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await emailService.SendEmailAsync(
                to: eventData.BuyerEmail,
                subject: subject,
                body: body,
                isHtml: true);

            _logger.LogInformation(
                "Report purchase receipt email sent to {Email} for PurchaseId: {PurchaseId}",
                eventData.BuyerEmail,
                eventData.PurchaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send report purchase receipt for PurchaseId: {PurchaseId}", eventData.PurchaseId);
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
            _logger.LogError(ex, "Error disposing RabbitMQ connection for ReportPurchaseReceiptConsumer");
        }

        base.Dispose();
    }
}
