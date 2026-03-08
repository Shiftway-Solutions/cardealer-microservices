using System.Text;
using System.Text.Json;
using DealerAnalyticsService.Domain.Events;
using DealerAnalyticsService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DealerAnalyticsService.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of the event publisher
/// Publishes analytics events to the message broker for consumption by other services
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly string _exchangeName;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _publishLock = new();

    public RabbitMqEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _logger = logger;
        _exchangeName = configuration["RabbitMQ:ExchangeName"] ?? "dealer-analytics-events";
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:Username"] ?? throw new InvalidOperationException("RabbitMQ:Username is not configured"),
                Password = configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured"),
                VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare the exchange for analytics events
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _logger.LogInformation("RabbitMQ connection established for analytics events. Exchange: {Exchange}", _exchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to RabbitMQ. Events will not be published.");
        }
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IDomainEvent
    {
        if (_channel == null || !_channel.IsOpen)
        {
            _logger.LogWarning("RabbitMQ channel not available. Skipping event: {EventType}", @event.EventType);
            return Task.CompletedTask;
        }

        try
        {
            var routingKey = @event.EventType;
            var messageBody = JsonSerializer.Serialize(@event, _jsonOptions);
            var body = Encoding.UTF8.GetBytes(messageBody);

            lock (_publishLock)
            {
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.MessageId = @event.EventId.ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.Type = @event.EventType;
                properties.Headers = new Dictionary<string, object>
                {
                    { "x-event-type", @event.EventType },
                    { "x-occurred-at", @event.OccurredAt.ToString("O") }
                };

                _channel.BasicPublish(
                    exchange: _exchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body);
            }

            _logger.LogDebug(
                "Published event {EventType} with ID {EventId} to exchange {Exchange}",
                @event.EventType,
                @event.EventId,
                _exchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", @event.EventType);
        }

        return Task.CompletedTask;
    }

    public async Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken ct = default) where TEvent : IDomainEvent
    {
        foreach (var @event in events)
        {
            await PublishAsync(@event, ct);
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _logger.LogInformation("RabbitMQ connection closed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing RabbitMQ connection");
        }
    }
}

/// <summary>
/// Null implementation for when RabbitMQ is not available
/// </summary>
public class NullEventPublisher : IEventPublisher
{
    private readonly ILogger<NullEventPublisher> _logger;

    public NullEventPublisher(ILogger<NullEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IDomainEvent
    {
        _logger.LogDebug("NullEventPublisher: Skipping event {EventType}", @event.EventType);
        return Task.CompletedTask;
    }

    public Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken ct = default) where TEvent : IDomainEvent
    {
        return Task.CompletedTask;
    }
}
