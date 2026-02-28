using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CarDealer.Contracts.Events.Alert;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Background service that listens for saved search activation events
/// and sends email notifications to the user confirming their search is active.
/// 
/// Routing key: alert.savedsearch.activated
/// Queue: notificationservice.alert.savedsearch_activated
/// Exchange: cardealer.events (Topic)
/// 
/// Published by: AlertService (when a user creates or activates a saved search)
/// </summary>
public class SavedSearchActivatedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SavedSearchActivatedConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "notificationservice.alert.savedsearch_activated";
    private const string RoutingKey = "alert.savedsearch.activated";

    public SavedSearchActivatedConsumer(
        IServiceProvider serviceProvider,
        ILogger<SavedSearchActivatedConsumer> logger,
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
            _logger.LogInformation("RabbitMQ is disabled. SavedSearchActivatedConsumer will not start.");
            return;
        }

        try
        {
            InitializeRabbitMQ();

            if (_channel == null)
            {
                _logger.LogError("Failed to initialize RabbitMQ channel for SavedSearchActivatedConsumer");
                return;
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var searchEvent = JsonSerializer.Deserialize<SavedSearchMatchEvent>(message,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (searchEvent != null)
                    {
                        _logger.LogInformation(
                            "Received SavedSearchMatchEvent: SearchId={SearchId}, UserId={UserId}, " +
                            "Action={ActionType}, Name={SearchName}",
                            searchEvent.SavedSearchId, searchEvent.UserId,
                            searchEvent.ActionType, searchEvent.SearchName);

                        await HandleSavedSearchActivatedAsync(searchEvent, stoppingToken);

                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        _logger.LogDebug("Message acknowledged for SearchId: {SearchId}", searchEvent.SavedSearchId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize SavedSearchMatchEvent");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing SavedSearchMatchEvent");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation(
                "✅ SavedSearchActivatedConsumer started listening on queue: {Queue} with routing key: {RoutingKey}",
                QueueName, RoutingKey);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in SavedSearchActivatedConsumer");
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
                ClientProvidedName = $"notificationservice-savedsearch-consumer-{Environment.MachineName}"
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

            _logger.LogInformation("RabbitMQ initialized for SavedSearchActivatedConsumer");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for SavedSearchActivatedConsumer");
            throw;
        }
    }

    private async Task HandleSavedSearchActivatedAsync(
        SavedSearchMatchEvent searchEvent,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            // If we don't have the user's email from the event, try to resolve it
            var userEmail = searchEvent.UserEmail;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                try
                {
                    var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();
                    if (httpClientFactory != null)
                    {
                        var client = httpClientFactory.CreateClient("UserService");
                        var response = await client.GetAsync(
                            $"/api/users/{searchEvent.UserId}/email", cancellationToken);
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
                        searchEvent.UserId);
                }
            }

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                _logger.LogWarning(
                    "Could not resolve email for UserId: {UserId}. Cannot send saved search notification for SearchId: {SearchId}",
                    searchEvent.UserId, searchEvent.SavedSearchId);
                return;
            }

            var searchDescription = !string.IsNullOrWhiteSpace(searchEvent.SearchDescription)
                ? searchEvent.SearchDescription
                : "Búsqueda personalizada";

            var isCreated = searchEvent.ActionType == "created";
            var actionLabel = isCreated ? "creada" : "reactivada";

            var frequencyLabel = searchEvent.Frequency switch
            {
                "Instant" => "Inmediatamente",
                "Daily" => "Diariamente",
                "Weekly" => "Semanalmente",
                _ => "Según configuración"
            };

            var searchUrl = $"https://okla.com.do/cuenta/alertas";

            var subject = isCreated
                ? $"🔍 Búsqueda guardada: {searchEvent.SearchName}"
                : $"✅ Búsqueda reactivada: {searchEvent.SearchName}";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Búsqueda Guardada - OKLA</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <div style=""background: linear-gradient(135deg, #6c5ce7, #a29bfe); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
            <h1 style=""margin: 0; font-size: 24px;"">🔍 Búsqueda {actionLabel}</h1>
            <p style=""margin: 10px 0 0; opacity: 0.9;"">Te avisaremos cuando haya vehículos que coincidan</p>
        </div>
        
        <div style=""background: white; padding: 30px; border-radius: 0 0 10px 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);"">
            <h2 style=""color: #2c3e50; margin-top: 0;"">{searchEvent.SearchName}</h2>
            
            <div style=""background-color: #f0f0ff; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #6c5ce7;"">
                <p style=""margin: 0 0 10px; font-weight: bold; color: #6c5ce7;"">Criterios de búsqueda:</p>
                <p style=""margin: 0; color: #555;"">{searchDescription}</p>
            </div>
            
            <table style=""width: 100%; border-collapse: collapse; margin: 20px 0;"">
                <tr>
                    <td style=""padding: 8px 0; color: #666;"">Estado:</td>
                    <td style=""padding: 8px 0; font-weight: bold; text-align: right; color: #4caf50;"">✅ Activa</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: #666;"">Frecuencia de alertas:</td>
                    <td style=""padding: 8px 0; font-weight: bold; text-align: right;"">{frequencyLabel}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: #666;"">{(isCreated ? "Creada:" : "Reactivada:")}</td>
                    <td style=""padding: 8px 0; text-align: right;"">{DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC</td>
                </tr>
            </table>
            
            <div style=""background-color: #fff3cd; padding: 15px; border-radius: 8px; margin: 20px 0;"">
                <p style=""margin: 0; color: #856404; font-size: 14px;"">
                    📬 Recibirás notificaciones <strong>{frequencyLabel.ToLower()}</strong> cuando se publiquen 
                    vehículos que coincidan con tus criterios.
                </p>
            </div>
            
            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{searchUrl}"" 
                   style=""background-color: #6c5ce7; color: white; padding: 14px 32px; text-decoration: none; border-radius: 8px; font-size: 16px; font-weight: bold; display: inline-block;"">
                    Ver Mis Alertas
                </a>
            </div>
            
            <p style=""color: #999; font-size: 12px; text-align: center; margin-top: 30px; border-top: 1px solid #eee; padding-top: 20px;"">
                Recibiste este correo porque guardaste una búsqueda en OKLA.
                <br>Puedes desactivar esta búsqueda desde tu panel de alertas.
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
                "✅ Saved search notification sent to {Email} for SearchId: {SearchId}, Action: {Action}",
                userEmail, searchEvent.SavedSearchId, searchEvent.ActionType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Failed to send saved search notification for SearchId: {SearchId}, UserId: {UserId}",
                searchEvent.SavedSearchId, searchEvent.UserId);
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
            _logger.LogError(ex, "Error disposing RabbitMQ connection for SavedSearchActivatedConsumer");
        }

        base.Dispose();
    }
}
