using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CarDealer.Contracts.Events.Alert;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Background service que escucha eventos de alertas de precio disparadas
/// y envía notificaciones por email al usuario.
/// 
/// Routing key: alert.price.triggered
/// Queue: notificationservice.alert.price_triggered
/// Exchange: cardealer.events (Topic)
/// 
/// Published by: AlertService (when a vehicle price meets the user's alert condition)
/// </summary>
public class PriceAlertTriggeredConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PriceAlertTriggeredConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "notificationservice.alert.price_triggered";
    private const string RoutingKey = "alert.price.triggered";

    public PriceAlertTriggeredConsumer(
        IServiceProvider serviceProvider,
        ILogger<PriceAlertTriggeredConsumer> logger,
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
            _logger.LogInformation("RabbitMQ is disabled. PriceAlertTriggeredConsumer will not start.");
            return;
        }

        try
        {
            InitializeRabbitMQ();

            if (_channel == null)
            {
                _logger.LogError("Failed to initialize RabbitMQ channel for PriceAlertTriggeredConsumer");
                return;
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var alertEvent = JsonSerializer.Deserialize<PriceAlertTriggeredEvent>(message,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (alertEvent != null)
                    {
                        _logger.LogInformation(
                            "Received PriceAlertTriggeredEvent: AlertId={AlertId}, UserId={UserId}, " +
                            "VehicleId={VehicleId}, OldPrice={OldPrice} → NewPrice={NewPrice}, Target={TargetPrice}",
                            alertEvent.AlertId, alertEvent.UserId, alertEvent.VehicleId,
                            alertEvent.OldPrice, alertEvent.NewPrice, alertEvent.TargetPrice);

                        await HandlePriceAlertTriggeredAsync(alertEvent, stoppingToken);

                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        _logger.LogDebug("Message acknowledged for AlertId: {AlertId}", alertEvent.AlertId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize PriceAlertTriggeredEvent");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing PriceAlertTriggeredEvent");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation(
                "✅ PriceAlertTriggeredConsumer started listening on queue: {Queue} with routing key: {RoutingKey}",
                QueueName, RoutingKey);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in PriceAlertTriggeredConsumer");
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
                UserName = _configuration["RabbitMQ:Username"]
                    ?? throw new InvalidOperationException("RabbitMQ:Username is not configured"),
                Password = _configuration["RabbitMQ:Password"]
                    ?? throw new InvalidOperationException("RabbitMQ:Password is not configured"),
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                ClientProvidedName = $"notificationservice-price-alert-consumer-{Environment.MachineName}"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange (idempotent)
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare queue with DLX for failed messages
            var queueArgs = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = $"{ExchangeName}.dlx",
                ["x-dead-letter-routing-key"] = $"dlx.{RoutingKey}",
                ["x-message-ttl"] = 86400000 // 24h TTL
            };

            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey);

            // One message at a time for reliable processing
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ initialized for PriceAlertTriggeredConsumer");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for PriceAlertTriggeredConsumer");
            throw;
        }
    }

    private async Task HandlePriceAlertTriggeredAsync(
        PriceAlertTriggeredEvent alertEvent,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            // If we don't have the user's email from the event, try to resolve it
            var userEmail = alertEvent.UserEmail;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                try
                {
                    var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();
                    if (httpClientFactory != null)
                    {
                        var client = httpClientFactory.CreateClient("UserService");
                        var response = await client.GetAsync(
                            $"/api/users/{alertEvent.UserId}/email", cancellationToken);
                        if (response.IsSuccessStatusCode)
                        {
                            userEmail = await response.Content.ReadAsStringAsync(cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to resolve user email from UserService for UserId: {UserId}",
                        alertEvent.UserId);
                }
            }

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                _logger.LogWarning(
                    "Could not resolve email for UserId: {UserId}. Cannot send price alert notification for AlertId: {AlertId}",
                    alertEvent.UserId, alertEvent.AlertId);
                return;
            }

            // Build the vehicle URL
            var vehicleUrl = !string.IsNullOrWhiteSpace(alertEvent.VehicleSlug)
                ? $"https://okla.com.do/vehiculos/{alertEvent.VehicleSlug}"
                : $"https://okla.com.do/vehiculos/{alertEvent.VehicleId}";

            var vehicleTitle = !string.IsNullOrWhiteSpace(alertEvent.VehicleTitle)
                ? alertEvent.VehicleTitle
                : "Vehículo";

            // Calculate savings
            var savings = alertEvent.OldPrice - alertEvent.NewPrice;
            var savingsPercent = alertEvent.OldPrice > 0
                ? Math.Round((savings / alertEvent.OldPrice) * 100, 1)
                : 0;

            var subject = $"🔔 ¡Bajó de precio! {vehicleTitle} - RD${alertEvent.NewPrice:N0}";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Alerta de Precio - OKLA</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <div style=""background: linear-gradient(135deg, #007bff, #0056b3); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
            <h1 style=""margin: 0; font-size: 24px;"">🔔 ¡Alerta de Precio!</h1>
            <p style=""margin: 10px 0 0; opacity: 0.9;"">El vehículo que estás siguiendo bajó de precio</p>
        </div>
        
        <div style=""background: white; padding: 30px; border-radius: 0 0 10px 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);"">
            <h2 style=""color: #2c3e50; margin-top: 0;"">{vehicleTitle}</h2>
            
            <div style=""background-color: #e8f5e9; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #4caf50;"">
                <div style=""display: flex; justify-content: space-between; align-items: center;"">
                    <div>
                        <p style=""margin: 0; color: #666; font-size: 14px;"">Precio anterior</p>
                        <p style=""margin: 0; text-decoration: line-through; color: #999; font-size: 18px;"">RD${alertEvent.OldPrice:N0}</p>
                    </div>
                    <div style=""font-size: 24px;"">→</div>
                    <div>
                        <p style=""margin: 0; color: #666; font-size: 14px;"">Nuevo precio</p>
                        <p style=""margin: 0; color: #4caf50; font-weight: bold; font-size: 24px;"">RD${alertEvent.NewPrice:N0}</p>
                    </div>
                </div>
                <p style=""margin: 15px 0 0; text-align: center; color: #4caf50; font-weight: bold;"">
                    ¡Ahorras RD${savings:N0} ({savingsPercent}% menos)!
                </p>
            </div>
            
            <table style=""width: 100%; border-collapse: collapse; margin: 20px 0;"">
                <tr>
                    <td style=""padding: 8px 0; color: #666;"">Tu precio objetivo:</td>
                    <td style=""padding: 8px 0; font-weight: bold; text-align: right;"">RD${alertEvent.TargetPrice:N0}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: #666;"">Condición:</td>
                    <td style=""padding: 8px 0; text-align: right;"">{alertEvent.Condition}</td>
                </tr>
            </table>
            
            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{vehicleUrl}"" 
                   style=""background-color: #007bff; color: white; padding: 14px 32px; text-decoration: none; border-radius: 8px; font-size: 16px; font-weight: bold; display: inline-block;"">
                    Ver Vehículo
                </a>
            </div>
            
            <p style=""color: #999; font-size: 12px; text-align: center; margin-top: 30px; border-top: 1px solid #eee; padding-top: 20px;"">
                Recibiste este correo porque configuraste una alerta de precio en OKLA.
                <br>Puedes desactivar esta alerta desde tu panel de alertas.
            </p>
        </div>
        
        <div style=""text-align: center; padding: 20px; color: #999; font-size: 11px;"">
            &copy; {DateTime.UtcNow.Year} OKLA - Marketplace de Vehículos en República Dominicana
        </div>
    </div>
</body>
</html>";

            await emailService.SendEmailAsync(
                to: userEmail,
                subject: subject,
                body: body,
                isHtml: true);

            _logger.LogInformation(
                "✅ Price alert notification sent to {Email} for AlertId: {AlertId}, VehicleId: {VehicleId}",
                userEmail, alertEvent.AlertId, alertEvent.VehicleId);

            // Persist in-app user notification
            if (alertEvent.UserId != Guid.Empty)
            {
                var userNotifService = scope.ServiceProvider.GetService<IUserNotificationService>();
                if (userNotifService != null)
                {
                    await userNotifService.CreateAsync(
                        userId: alertEvent.UserId,
                        type: "price",
                        title: "💰 Alerta de precio activada",
                        message: $"{vehicleTitle} bajó a RD${alertEvent.NewPrice:N0} (ahorro: RD${savings:N0})",
                        icon: "💰",
                        link: $"/vehiculos/{alertEvent.VehicleId}",
                        cancellationToken: cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Failed to send price alert notification for AlertId: {AlertId}, UserId: {UserId}",
                alertEvent.AlertId, alertEvent.UserId);
            throw; // Will trigger nack+requeue
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
            _logger.LogError(ex, "Error disposing RabbitMQ connection for PriceAlertTriggeredConsumer");
        }

        base.Dispose();
    }
}
