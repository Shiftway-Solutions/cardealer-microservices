using System.Text;
using System.Text.Json;
using CarDealer.Contracts.Events.KYC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Background service que escucha eventos de cambio de estado de perfiles KYC
/// y envía emails de aprobación/rechazo al usuario.
/// </summary>
public class KYCStatusChangedNotificationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KYCStatusChangedNotificationConsumer> _logger;
    private readonly IConfiguration _configuration;

    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "cardealer.events";
    private const string QueueName    = "notificationservice.kyc.status_changed";
    private const string RoutingKey   = "kyc.profile.status_changed";
    private const string DlqQueueName = "notificationservice.kyc.status_changed.dlq";
    private const string DlxExchange  = "cardealer.events.dlx";

    public KYCStatusChangedNotificationConsumer(
        IServiceProvider serviceProvider,
        ILogger<KYCStatusChangedNotificationConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitEnabled = _configuration.GetValue<bool>("RabbitMQ:Enabled");
        if (!rabbitEnabled)
        {
            _logger.LogInformation("RabbitMQ is disabled. KYCStatusChangedNotificationConsumer will not start.");
            return;
        }

        try
        {
            InitializeRabbitMQ();

            if (_channel == null)
            {
                _logger.LogError("Failed to initialize RabbitMQ channel for KYCStatusChangedNotificationConsumer");
                return;
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (_, ea) =>
            {
                var body    = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var kycEvent = JsonSerializer.Deserialize<KYCProfileStatusChangedEvent>(
                        message,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (kycEvent != null)
                    {
                        _logger.LogInformation(
                            "Received KYCProfileStatusChangedEvent: ProfileId={ProfileId}, UserId={UserId}, NewStatus={NewStatus}",
                            kycEvent.ProfileId, kycEvent.UserId, kycEvent.NewStatus);

                        await HandleKYCStatusChangedAsync(kycEvent, stoppingToken);
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize KYCProfileStatusChangedEvent — discarding message");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing KYCProfileStatusChangedEvent — requeuing");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

            _logger.LogInformation("KYCStatusChangedNotificationConsumer started listening on queue: {Queue}", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("KYCStatusChangedNotificationConsumer stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in KYCStatusChangedNotificationConsumer");
        }
    }

    // ─── Init ────────────────────────────────────────────────────────────────────

    private void InitializeRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"]
                           ?? _configuration["RabbitMQ:HostName"]
                           ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"]
                           ?? _configuration["RabbitMQ:User"]
                           ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel    = _connection.CreateModel();

            // Main exchange (Topic, durable)
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Dead-letter exchange (Direct, durable)
            _channel.ExchangeDeclare(
                exchange: DlxExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // Dead-letter queue
            _channel.QueueDeclare(
                queue: DlqQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueBind(DlqQueueName, DlxExchange, RoutingKey);

            // Main queue with DLX routing
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"]    = DlxExchange,
                    ["x-dead-letter-routing-key"] = RoutingKey
                });

            _channel.QueueBind(QueueName, ExchangeName, RoutingKey);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ initialized for KYCStatusChangedNotificationConsumer (queue={Queue})", QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for KYCStatusChangedNotificationConsumer");
            throw;
        }
    }

    // ─── Business Logic ──────────────────────────────────────────────────────────

    private async Task HandleKYCStatusChangedAsync(
        KYCProfileStatusChangedEvent kycEvent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(kycEvent.Email))
        {
            _logger.LogWarning(
                "KYCProfileStatusChangedEvent for ProfileId={ProfileId} has no email — skipping notification",
                kycEvent.ProfileId);
            return;
        }

        using var scope        = _serviceProvider.CreateScope();
        var       emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            var (subject, body) = kycEvent.NewStatus switch
            {
                "Approved" => BuildApprovedEmail(kycEvent),
                "Rejected" => BuildRejectedEmail(kycEvent),
                _          => BuildGenericStatusEmail(kycEvent)
            };

            await emailService.SendEmailAsync(
                to: kycEvent.Email,
                subject: subject,
                body: body,
                isHtml: true);

            _logger.LogInformation(
                "KYC {Status} notification sent to {Email} for ProfileId={ProfileId}",
                kycEvent.NewStatus, kycEvent.Email, kycEvent.ProfileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send KYC {Status} email to {Email} for ProfileId={ProfileId}",
                kycEvent.NewStatus, kycEvent.Email, kycEvent.ProfileId);
            throw; // Let the consumer NACK and requeue
        }
    }

    // ─── Email Templates ─────────────────────────────────────────────────────────

    private static (string subject, string body) BuildApprovedEmail(KYCProfileStatusChangedEvent e)
    {
        var subject = "✅ Tu verificación de identidad ha sido aprobada — OKLA";
        var validityNote = e.ValidityDays.HasValue
            ? $"<p>Tu verificación es válida por <strong>{e.ValidityDays} días</strong>.</p>"
            : string.Empty;

        var body = $@"
<html>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='text-align: center; margin-bottom: 30px;'>
        <h1 style='color: #2c3e50; font-size: 24px;'>OKLA</h1>
    </div>

    <div style='background-color: #d4edda; border-left: 4px solid #28a745; padding: 20px; border-radius: 5px; margin-bottom: 20px;'>
        <h2 style='color: #155724; margin-top: 0;'>✅ Verificación Aprobada</h2>
        <p style='color: #155724; margin: 0;'>
            ¡Felicitaciones, <strong>{e.FullName}</strong>! Tu verificación de identidad en OKLA ha sido aprobada.
        </p>
    </div>

    <p>Ya puedes acceder a todas las funciones de la plataforma que requieren verificación.</p>
    {validityNote}

    <div style='margin: 30px 0; text-align: center;'>
        <a href='https://okla.com.do/cuenta/verificacion'
           style='background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold;'>
            Ver mi verificación
        </a>
    </div>

    <hr style='margin: 30px 0; border: none; border-top: 1px solid #dee2e6;'>
    <p style='color: #6c757d; font-size: 12px; text-align: center;'>
        Este es un mensaje automático de OKLA. No responder a este correo.<br>
        © {DateTime.UtcNow.Year} OKLA — República Dominicana
    </p>
</body>
</html>";

        return (subject, body);
    }

    private static (string subject, string body) BuildRejectedEmail(KYCProfileStatusChangedEvent e)
    {
        var subject = "⚠️ Tu verificación de identidad requiere atención — OKLA";
        var reasonHtml = !string.IsNullOrWhiteSpace(e.Reason)
            ? $@"<div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                    <p style='margin: 0;'><strong>Motivo:</strong> {e.Reason}</p>
                </div>"
            : string.Empty;

        var body = $@"
<html>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='text-align: center; margin-bottom: 30px;'>
        <h1 style='color: #2c3e50; font-size: 24px;'>OKLA</h1>
    </div>

    <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 20px; border-radius: 5px; margin-bottom: 20px;'>
        <h2 style='color: #856404; margin-top: 0;'>⚠️ Verificación No Aprobada</h2>
        <p style='color: #856404; margin: 0;'>
            Hola <strong>{e.FullName}</strong>, no fue posible aprobar tu verificación de identidad en este momento.
        </p>
    </div>

    {reasonHtml}

    <p>Por favor, revisa la información y documentos enviados, y vuelve a intentarlo desde tu cuenta.</p>

    <div style='margin: 30px 0; text-align: center;'>
        <a href='https://okla.com.do/cuenta/verificacion'
           style='background-color: #ffc107; color: #212529; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold;'>
            Reintentar verificación
        </a>
    </div>

    <p style='font-size: 13px; color: #6c757d;'>
        Si tienes dudas, comunícate con nuestro soporte en
        <a href='mailto:soporte@okla.com.do'>soporte@okla.com.do</a>.
    </p>

    <hr style='margin: 30px 0; border: none; border-top: 1px solid #dee2e6;'>
    <p style='color: #6c757d; font-size: 12px; text-align: center;'>
        Este es un mensaje automático de OKLA. No responder a este correo.<br>
        © {DateTime.UtcNow.Year} OKLA — República Dominicana
    </p>
</body>
</html>";

        return (subject, body);
    }

    private static (string subject, string body) BuildGenericStatusEmail(KYCProfileStatusChangedEvent e)
    {
        var subject = $"Actualización de tu verificación de identidad — OKLA";
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <h2>Actualización de Verificación</h2>
    <p>Hola <strong>{e.FullName}</strong>,</p>
    <p>El estado de tu verificación de identidad ha cambiado a: <strong>{e.NewStatus}</strong>.</p>
    <p>
        <a href='https://okla.com.do/cuenta/verificacion'>Ver detalles</a>
    </p>
</body>
</html>";

        return (subject, body);
    }

    // ─── Cleanup ─────────────────────────────────────────────────────────────────

    public override void Dispose()
    {
        try { _channel?.Close();    } catch { /* ignored */ }
        try { _connection?.Close(); } catch { /* ignored */ }
        try { _channel?.Dispose();  } catch { /* ignored */ }
        try { _connection?.Dispose(); } catch { /* ignored */ }
        base.Dispose();
    }
}
