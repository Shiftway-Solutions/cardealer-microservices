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
/// Background service que escucha eventos de facturas generadas
/// y envía la factura formal por email al usuario.
/// Cumple con requisito legal DGII de enviar comprobante fiscal electrónico (e-CF).
/// </summary>
public class InvoiceNotificationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InvoiceNotificationConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "notificationservice.invoice.generated";
    private const string RoutingKey = "billing.invoice.generated";

    public InvoiceNotificationConsumer(
        IServiceProvider serviceProvider,
        ILogger<InvoiceNotificationConsumer> logger,
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
            _logger.LogInformation("RabbitMQ is disabled. InvoiceNotificationConsumer will not start.");
            return;
        }

        try
        {
            InitializeRabbitMQ();

            if (_channel == null)
            {
                _logger.LogError("Failed to initialize RabbitMQ channel for InvoiceNotificationConsumer");
                return;
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var invoiceEvent = JsonSerializer.Deserialize<InvoiceGeneratedEvent>(message);

                    if (invoiceEvent != null)
                    {
                        _logger.LogInformation(
                            "Received InvoiceGeneratedEvent: InvoiceId={InvoiceId}, InvoiceNumber={InvoiceNumber}, Amount={Amount} {Currency}",
                            invoiceEvent.InvoiceId,
                            invoiceEvent.InvoiceNumber,
                            invoiceEvent.TotalAmount,
                            invoiceEvent.Currency);

                        await HandleInvoiceGeneratedEventAsync(invoiceEvent, stoppingToken);

                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        _logger.LogDebug("Invoice message acknowledged: {DeliveryTag}", ea.DeliveryTag);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize InvoiceGeneratedEvent");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing InvoiceGeneratedEvent");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("InvoiceNotificationConsumer started listening on queue: {Queue}", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in InvoiceNotificationConsumer");
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

            _logger.LogInformation("RabbitMQ initialized successfully for InvoiceNotificationConsumer");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection for InvoiceNotificationConsumer");
            throw;
        }
    }

    private async Task HandleInvoiceGeneratedEventAsync(
        InvoiceGeneratedEvent eventData,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            // Calcular ITBIS (18%) y subtotal
            var itbisRate = 0.18m;
            var subtotal = eventData.TotalAmount / (1 + itbisRate);
            var itbis = eventData.TotalAmount - subtotal;

            var subject = $"Factura #{eventData.InvoiceNumber} - OKLA Marketplace";

            var body = BuildInvoiceEmailHtml(eventData, subtotal, itbis);

            await emailService.SendEmailAsync(
                to: eventData.BuyerEmail,
                subject: subject,
                body: body,
                isHtml: true);

            _logger.LogInformation(
                "Invoice email sent to {Email} for InvoiceNumber: {InvoiceNumber}",
                eventData.BuyerEmail,
                eventData.InvoiceNumber);

            // Crear notificación in-app si el usuario tiene ID
            if (eventData.UserId != Guid.Empty)
            {
                var userNotifService = scope.ServiceProvider.GetService<IUserNotificationService>();
                if (userNotifService != null)
                {
                    await userNotifService.CreateAsync(
                        userId: eventData.UserId,
                        type: "invoice_generated",
                        title: "🧾 Factura generada",
                        message: $"Tu factura #{eventData.InvoiceNumber} por RD${eventData.TotalAmount:N2} {eventData.Currency} está disponible.",
                        icon: "🧾",
                        link: "/cuenta/facturacion",
                        cancellationToken: cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send invoice email for InvoiceNumber: {InvoiceNumber}, Email: {Email}",
                eventData.InvoiceNumber,
                eventData.BuyerEmail);
            throw;
        }
    }

    /// <summary>
    /// Genera el HTML del email de factura con datos fiscales dominicanos
    /// conforme a los requisitos de la DGII para e-CF
    /// </summary>
    private static string BuildInvoiceEmailHtml(
        InvoiceGeneratedEvent eventData,
        decimal subtotal,
        decimal itbis)
    {
        var pdfSection = string.IsNullOrEmpty(eventData.PdfUrl)
            ? string.Empty
            : $@"
                            <p style='margin-top: 20px; text-align: center;'>
                                <a href='{eventData.PdfUrl}' 
                                   style='display: inline-block; background-color: #dc3545; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px;'>
                                    📄 Descargar Factura PDF
                                </a>
                            </p>";

        return $@"
            <html>
            <body style='font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4;'>
                <div style='max-width: 600px; margin: 0 auto;'>
                    <!-- Header -->
                    <div style='background-color: #1a237e; color: white; padding: 25px; text-align: center;'>
                        <h1 style='margin: 0; font-size: 24px;'>🧾 Factura Electrónica</h1>
                        <p style='margin: 5px 0 0 0; font-size: 14px; opacity: 0.9;'>OKLA Marketplace - República Dominicana</p>
                    </div>
                    
                    <!-- Content -->
                    <div style='padding: 30px; background-color: #ffffff;'>
                        <p>Estimado/a <strong>{eventData.BuyerName}</strong>,</p>
                        <p>Le informamos que se ha generado la siguiente factura electrónica:</p>
                        
                        <!-- Invoice Details -->
                        <div style='background-color: #f8f9fa; border-radius: 8px; padding: 20px; margin: 20px 0; border: 1px solid #dee2e6;'>
                            <table style='width: 100%;'>
                                <tr>
                                    <td style='padding: 8px 0;'><strong>No. Factura:</strong></td>
                                    <td style='text-align: right; font-family: monospace; font-size: 16px; color: #1a237e;'>
                                        <strong>{eventData.InvoiceNumber}</strong>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px 0;'><strong>Fecha de Emisión:</strong></td>
                                    <td style='text-align: right;'>
                                        {eventData.IssuedAt:dd/MM/yyyy}
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px 0;'><strong>Cliente:</strong></td>
                                    <td style='text-align: right;'>
                                        {eventData.BuyerName}
                                    </td>
                                </tr>
                            </table>
                        </div>

                        <!-- Amounts Table -->
                        <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                            <tr style='background-color: #1a237e; color: white;'>
                                <th style='padding: 12px; text-align: left;'>Concepto</th>
                                <th style='padding: 12px; text-align: right;'>Monto</th>
                            </tr>
                            <tr style='border-bottom: 1px solid #dee2e6;'>
                                <td style='padding: 12px;'>{eventData.Description}</td>
                                <td style='padding: 12px; text-align: right;'>RD${subtotal:N2}</td>
                            </tr>
                            <tr style='border-bottom: 1px solid #dee2e6; background-color: #f8f9fa;'>
                                <td style='padding: 12px;'>ITBIS (18%)</td>
                                <td style='padding: 12px; text-align: right;'>RD${itbis:N2}</td>
                            </tr>
                            <tr style='background-color: #e8eaf6;'>
                                <td style='padding: 12px;'><strong>Total</strong></td>
                                <td style='padding: 12px; text-align: right; font-size: 20px; color: #1a237e;'>
                                    <strong>RD${eventData.TotalAmount:N2} {eventData.Currency}</strong>
                                </td>
                            </tr>
                        </table>

                        <!-- Legal Notice -->
                        <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;'>
                            <p style='margin: 0; font-size: 13px;'>
                                <strong>⚠️ Aviso Legal:</strong> Este documento constituye una factura electrónica 
                                emitida conforme a la normativa de la DGII (Dirección General de Impuestos Internos) 
                                de la República Dominicana. Conserve este documento para sus registros fiscales.
                            </p>
                        </div>
                        {pdfSection}

                        <p style='margin-top: 25px; text-align: center;'>
                            <a href='https://okla.com.do/cuenta/facturacion' 
                               style='display: inline-block; background-color: #1a237e; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px;'>
                                Ver Historial de Facturación
                            </a>
                        </p>
                    </div>
                    
                    <!-- Footer -->
                    <div style='background-color: #263238; color: #90a4ae; padding: 20px; text-align: center; font-size: 12px;'>
                        <p style='margin: 0;'><strong>OKLA Marketplace</strong> - Plataforma de Venta de Vehículos</p>
                        <p style='margin: 5px 0 0 0;'>República Dominicana | soporte@okla.com.do</p>
                        <p style='margin: 5px 0 0 0;'>Esta es una factura electrónica generada automáticamente. No responder a este correo.</p>
                    </div>
                </div>
            </body>
            </html>
        ";
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
            _logger.LogError(ex, "Error disposing RabbitMQ connection in InvoiceNotificationConsumer");
        }

        base.Dispose();
    }
}
