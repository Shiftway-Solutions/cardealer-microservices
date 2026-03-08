using ErrorService.Domain.Entities;
using ErrorService.Domain.Interfaces;
using ErrorService.Application.Helpers;
using ErrorService.Shared.ErrorMessages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ErrorService.Infrastructure.Services.Messaging;

public class RabbitMQSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
}

public class ErrorServiceRabbitMQSettings
{
    public string QueueName { get; set; } = "error-queue";
    public string ExchangeName { get; set; } = "error-exchange";
    public string RoutingKey { get; set; } = "error.routing.key";
}

public class RabbitMQErrorConsumer : BackgroundService
{
    private const int MaxRetryCount = 5;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQErrorConsumer> _logger;
    private readonly ErrorServiceRabbitMQSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly RabbitMQSettings _rabbitMqSettings;

    public RabbitMQErrorConsumer(
        IOptions<RabbitMQSettings> rabbitMqSettings,
        IOptions<ErrorServiceRabbitMQSettings> errorServiceSettings,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQErrorConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = errorServiceSettings.Value;
        _rabbitMqSettings = rabbitMqSettings.Value;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqSettings.HostName,
                Port = _rabbitMqSettings.Port,
                UserName = _rabbitMqSettings.UserName,
                Password = _rabbitMqSettings.Password,
                VirtualHost = _rabbitMqSettings.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declarar exchange y queue (mismo que el productor)
            // ⚠️ errors.exchange is Topic type (matches CarDealer.Shared.ErrorHandling.RabbitMQErrorPublisher)
            _channel.ExchangeDeclare(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Dead Letter Exchange for persistent DLQ (CRIT-01: replaces InMemoryDeadLetterQueue)
            var dlxExchange = $"{_settings.ExchangeName}.dlx";
            var dlqQueue = $"{_settings.QueueName}.dlq";
            _channel.ExchangeDeclare(dlxExchange, ExchangeType.Direct, durable: true, autoDelete: false);
            _channel.QueueDeclare(dlqQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(dlqQueue, dlxExchange, _settings.RoutingKey);

            // Main queue with DLX arguments — rejected messages routed to DLQ automatically
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", dlxExchange },
                { "x-dead-letter-routing-key", _settings.RoutingKey }
            };
            _channel.QueueDeclare(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            _channel.QueueBind(
                queue: _settings.QueueName,
                exchange: _settings.ExchangeName,
                routingKey: _settings.RoutingKey);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ Error Consumer initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ Error Consumer");
            _channel = null;
            _connection = null;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for RabbitMQ to be ready, then retry with backoff
        await Task.Delay(5000, stoppingToken);

        const int maxRetries = 10;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (stoppingToken.IsCancellationRequested) return;

            InitializeRabbitMQ();
            if (_channel != null) break;

            var delay = TimeSpan.FromSeconds(Math.Min(attempt * 5, 60));
            _logger.LogWarning(
                "RabbitMQErrorConsumer: RabbitMQ not available, retry {Attempt}/{MaxRetries} in {Delay}s",
                attempt, maxRetries, delay.TotalSeconds);
            await Task.Delay(delay, stoppingToken);
        }

        if (_channel == null)
        {
            _logger.LogError("RabbitMQ channel not available after {MaxRetries} retries, RabbitMQErrorConsumer will not start", maxRetries);
            return;
        }

        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                await ProcessMessageAsync(message, ea.DeliveryTag);
            }
            catch (Exception ex)
            {
                var retryCount = GetRetryCount(ea.BasicProperties);
                if (retryCount >= MaxRetryCount)
                {
                    _logger.LogError(ex, "Max retries ({MaxRetries}) exceeded for message. Sending to DLQ", MaxRetryCount);
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
                else
                {
                    _logger.LogWarning(ex, "Error processing message (attempt {Attempt}/{MaxRetries}). Requeueing", retryCount + 1, MaxRetryCount);
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            }
        };

        _channel.BasicConsume(
            queue: _settings.QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQErrorConsumer started, listening on queue: {Queue}", _settings.QueueName);

        // Keep running until cancellation
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessMessageAsync(string message, ulong deliveryTag)
    {
        try
        {
            var errorEvent = JsonSerializer.Deserialize<RabbitMQErrorEvent>(message, _jsonOptions);
            if (errorEvent == null)
            {
                _logger.LogWarning("Received null or invalid error event from RabbitMQ");
                _channel.BasicAck(deliveryTag, multiple: false);
                return;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(errorEvent.ServiceName) || string.IsNullOrWhiteSpace(errorEvent.ErrorMessage))
            {
                _logger.LogWarning("Error event missing required fields (ServiceName or ErrorMessage). Discarding");
                _channel.BasicAck(deliveryTag, multiple: false);
                return;
            }

            if (!Guid.TryParse(errorEvent.Id, out var errorId))
            {
                _logger.LogWarning("Invalid GUID format for error event Id: {Id}. Generating new Id", errorEvent.Id);
                errorId = Guid.NewGuid();
            }

            using var scope = _serviceProvider.CreateScope();
            var errorLogRepository = scope.ServiceProvider.GetRequiredService<IErrorLogRepository>();

            // Convertir RabbitMQErrorEvent a ErrorLog entity
            var errorLog = new ErrorLog
            {
                Id = errorId,
                ServiceName = errorEvent.ServiceName,
                ExceptionType = errorEvent.ErrorCode,
                Message = errorEvent.ErrorMessage,
                StackTrace = StackTraceSanitizer.Sanitize(errorEvent.StackTrace),
                OccurredAt = errorEvent.Timestamp,
                Endpoint = errorEvent.Endpoint,
                HttpMethod = errorEvent.HttpMethod,
                StatusCode = errorEvent.StatusCode,
                UserId = errorEvent.UserId,
                Metadata = errorEvent.Metadata ?? new Dictionary<string, object>()
            };

            await errorLogRepository.AddAsync(errorLog);

            _logger.LogInformation("Successfully processed error event from {ServiceName}: {ErrorCode}",
                errorEvent.ServiceName, errorEvent.ErrorCode);

            // Acknowledge the message
            _channel.BasicAck(deliveryTag, multiple: false);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Failed to deserialize RabbitMQ message");
            // Reject and don't requeue malformed messages
            _channel.BasicNack(deliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing RabbitMQ message");
            // Reject and requeue for transient errors
            _channel.BasicNack(deliveryTag, multiple: false, requeue: true);
        }
    }

    private static int GetRetryCount(IBasicProperties? properties)
    {
        if (properties?.Headers == null) return 0;
        if (!properties.Headers.TryGetValue("x-death", out var xDeath)) return 0;

        if (xDeath is List<object> deathList && deathList.Count > 0)
        {
            if (deathList[0] is Dictionary<string, object> deathInfo &&
                deathInfo.TryGetValue("count", out var count))
            {
                return Convert.ToInt32(count);
            }
        }

        return 0;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}