using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using CarDealer.Contracts.Abstractions;

namespace VehiclesSaleService.Infrastructure.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent;
}

public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "cardealer.events";
    private readonly object _publishLock = new();

    public RabbitMqEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var rabbitEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled");
        if (!rabbitEnabled)
        {
            _logger.LogInformation("RabbitMQ is disabled. Event publisher will not initialize.");
            return;
        }

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:Username"] ?? throw new InvalidOperationException("RabbitMQ:Username is not configured"),
                Password = configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured"),
                VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declarar exchange de tipo topic
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );

            _logger.LogInformation("RabbitMQ event publisher initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ event publisher");
        }
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
    {
        if (_channel == null)
        {
            _logger.LogWarning("RabbitMQ channel is not initialized. Event will not be published.");
            return Task.CompletedTask;
        }

        try
        {
            var routingKey = @event.EventType; // e.g., "vehicle.created"
            var message = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            lock (_publishLock)
            {
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = @event.EventId.ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.ContentType = "application/json";
                properties.Headers = new Dictionary<string, object>
                {
                    ["event_type"] = @event.EventType
                };

                _channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body
                );
            }

            _logger.LogInformation(
                "Published event {EventType} with ID {EventId}",
                @event.EventType,
                @event.EventId);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", @event.EventType);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
