using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.DTOs;
using NotificationService.Application.UseCases.SendEmailNotification;
using NotificationService.Application.UseCases.SendSmsNotification;
using NotificationService.Domain.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using CarDealer.Contracts.Events.Notification;

namespace NotificationService.Infrastructure.Messaging;

public class RabbitMQNotificationConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQNotificationConsumer> _logger;
    private readonly NotificationServiceRabbitMQSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions;

    public RabbitMQNotificationConsumer(
        IServiceProvider serviceProvider,
        IOptions<RabbitMQSettings> rabbitMqSettings,
        IOptions<NotificationServiceRabbitMQSettings> notificationSettings,
        ILogger<RabbitMQNotificationConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = notificationSettings.Value;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = rabbitMqSettings.Value.Host,
                Port = rabbitMqSettings.Value.Port,
                UserName = rabbitMqSettings.Value.Username,
                Password = rabbitMqSettings.Value.Password,
                VirtualHost = rabbitMqSettings.Value.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Configurar exchange y colas (deben coincidir con AuthService)
            SetupRabbitMQ();

            _logger.LogInformation("✅ RabbitMQ Notification Consumer initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize RabbitMQ Notification Consumer");
            throw;
        }
    }

    private void SetupRabbitMQ()
    {
        // Exchange principal
        _channel.ExchangeDeclare(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        // Dead Letter Exchange for persistent DLQ (CRIT-01: replaces InMemoryDeadLetterQueue)
        var dlxExchange = $"{_settings.ExchangeName}.dlx";
        _channel.ExchangeDeclare(dlxExchange, ExchangeType.Direct, durable: true, autoDelete: false);

        // DLQ arguments — rejected messages are routed to DLQ automatically by RabbitMQ
        var dlqArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", dlxExchange }
        };

        // Cola general with DLQ
        _channel.QueueDeclare(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: dlqArgs);

        // DLQ queue for general notifications
        _channel.QueueDeclare($"{_settings.QueueName}.dlq", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind($"{_settings.QueueName}.dlq", dlxExchange, _settings.RoutingKey);

        _channel.QueueBind(
            queue: _settings.QueueName,
            exchange: _settings.ExchangeName,
            routingKey: _settings.RoutingKey);

        // Cola específica para emails with DLQ
        _channel.QueueDeclare(
            queue: _settings.EmailQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: dlqArgs);

        _channel.QueueBind(
            queue: _settings.EmailQueueName,
            exchange: _settings.ExchangeName,
            routingKey: "notification.auth");

        // Cola específica para SMS with DLQ
        _channel.QueueDeclare(
            queue: _settings.SmsQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: dlqArgs);

        _channel.QueueBind(
            queue: _settings.SmsQueueName,
            exchange: _settings.ExchangeName,
            routingKey: "notification.sms");

        // Configurar QoS
        _channel.BasicQos(0, (ushort)_settings.PrefetchCount, _settings.GlobalQos);

        _logger.LogInformation("RabbitMQ setup completed for NotificationService");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() =>
        {
            _logger.LogInformation("🛑 RabbitMQ Notification Consumer is stopping");
            _channel?.Close();
            _connection?.Close();
        });

        // Iniciar consumo de todas las colas
        StartConsuming(_settings.QueueName, "general");
        StartConsuming(_settings.EmailQueueName, "email");
        StartConsuming(_settings.SmsQueueName, "sms");

        _logger.LogInformation("RabbitMQ Notification Consumer started successfully");
        return Task.CompletedTask;
    }

    private void StartConsuming(string queueName, string queueType)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                _logger.LogInformation("Received message from {QueueName} ({Type})", queueName, queueType);
                await ProcessMessage(queueType, message);
                _channel.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation("Successfully processed message from {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        };

        _channel.BasicConsume(
            queue: queueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Started consuming from queue: {QueueName} ({Type})", queueName, queueType);
    }

    private async Task ProcessMessage(string queueType, string message)
    {
        using var scope = _serviceProvider.CreateScope();

        try
        {
            switch (queueType)
            {
                case "email":
                    await ProcessEmailMessage(message, scope);
                    break;
                case "sms":
                    await ProcessSmsMessage(message, scope);
                    break;
                case "general":
                    await ProcessGeneralMessage(message, scope);
                    break;
                default:
                    _logger.LogWarning("Unknown queue type: {QueueType}", queueType);
                    break;
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization error for {QueueType} message", queueType);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {QueueType} message", queueType);
            throw;
        }
    }

    private async Task ProcessEmailMessage(string message, IServiceScope scope)
    {
        // Using CarDealer.Contracts event type
        var emailEvent = JsonSerializer.Deserialize<EmailNotificationRequestedEvent>(message, _jsonOptions);
        if (emailEvent == null)
        {
            _logger.LogWarning("Failed to deserialize email message");
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var request = new SendEmailNotificationRequest(
            To: emailEvent.To,
            Subject: emailEvent.Subject,
            Body: emailEvent.Body,
            IsHtml: emailEvent.IsHtml,
            Metadata: emailEvent.Data?.ToDictionary(k => k.Key, k => (object)k.Value)
        );

        var command = new SendEmailNotificationCommand(request);
        var result = await mediator.Send(command);

        if (result.NotificationId != Guid.Empty)
        {
            _logger.LogInformation("Successfully processed email notification {NotificationId} for {To}",
                result.NotificationId, emailEvent.To);
        }
        else
        {
            _logger.LogWarning("Failed to process email notification for {To}", emailEvent.To);
        }
    }

    private async Task ProcessSmsMessage(string message, IServiceScope scope)
    {
        // Parse JSON to extract fields - supports both Message and Body properties
        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;

        var to = root.TryGetProperty("to", out var toElem) ? toElem.GetString()
                : root.TryGetProperty("To", out var toElem2) ? toElem2.GetString() : null;

        // Support both "Message" (CarDealer.Contracts) and "Body" (AuthService.Shared) formats
        var smsMessage = root.TryGetProperty("message", out var msgElem) ? msgElem.GetString()
                        : root.TryGetProperty("Message", out var msgElem2) ? msgElem2.GetString()
                        : root.TryGetProperty("body", out var bodyElem) ? bodyElem.GetString()
                        : root.TryGetProperty("Body", out var bodyElem2) ? bodyElem2.GetString() : null;

        if (string.IsNullOrEmpty(to) || string.IsNullOrEmpty(smsMessage))
        {
            _logger.LogWarning("Failed to extract SMS fields - To: {To}, Message: {HasMessage}", to, !string.IsNullOrEmpty(smsMessage));
            return;
        }

        Dictionary<string, object>? metadata = null;
        if (root.TryGetProperty("data", out var dataElem) || root.TryGetProperty("Data", out dataElem))
        {
            metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(dataElem.GetRawText(), _jsonOptions);
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var request = new SendSmsNotificationRequest(
            To: to,
            Message: smsMessage,
            Metadata: metadata
        );

        var command = new SendSmsNotificationCommand(request);
        var result = await mediator.Send(command);

        if (result.NotificationId != Guid.Empty)
        {
            _logger.LogInformation("Successfully processed SMS notification {NotificationId} for {To}",
                result.NotificationId, to);
        }
        else
        {
            _logger.LogWarning("Failed to process SMS notification for {To}", to);
        }
    }

    private async Task ProcessGeneralMessage(string message, IServiceScope scope)
    {
        // Try to determine message type from JSON structure
        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;

        // Check for eventType property from CarDealer.Contracts.EventBase
        if (root.TryGetProperty("eventType", out var eventTypeElement))
        {
            var eventType = eventTypeElement.GetString()?.ToLower() ?? "";

            if (eventType.Contains("email"))
            {
                await ProcessEmailMessage(message, scope);
                return;
            }
            if (eventType.Contains("sms"))
            {
                await ProcessSmsMessage(message, scope);
                return;
            }
            if (eventType.Contains("push"))
            {
                await ProcessPushMessage(message, scope);
                return;
            }
        }

        // Fallback: check for Type property (legacy format)
        if (root.TryGetProperty("type", out var typeElement))
        {
            var type = typeElement.GetString()?.ToLower() ?? "";
            switch (type)
            {
                case "email":
                    await ProcessEmailMessage(message, scope);
                    break;
                case "sms":
                    await ProcessSmsMessage(message, scope);
                    break;
                case "push":
                    await ProcessPushMessage(message, scope);
                    break;
                default:
                    _logger.LogWarning("Unsupported notification type in general queue: {Type}", type);
                    break;
            }
        }
        else
        {
            _logger.LogWarning("Unable to determine notification type from message");
        }
    }

    private async Task ProcessPushMessage(string message, IServiceScope scope)
    {
        var pushEvent = JsonSerializer.Deserialize<PushNotificationRequestedEvent>(message, _jsonOptions);
        if (pushEvent == null)
        {
            _logger.LogWarning("Failed to deserialize push notification message");
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Note: SendPushNotificationCommand would need to be implemented
        _logger.LogInformation("Push notification requested for device {DeviceToken}: {Title}",
            pushEvent.DeviceToken, pushEvent.Title);

        // TODO: Implement when SendPushNotificationRequest is available
        await Task.CompletedTask;
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
        }
        catch { /* Channel may already be closed */ }

        try
        {
            _connection?.Close();
            _connection?.Dispose();
        }
        catch { /* Connection may already be closed */ }

        base.Dispose();
    }
}