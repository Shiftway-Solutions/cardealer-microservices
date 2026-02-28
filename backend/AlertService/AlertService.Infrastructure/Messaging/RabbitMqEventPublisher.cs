using System.Text;
using System.Text.Json;
using AlertService.Domain.Interfaces;
using CarDealer.Contracts.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace AlertService.Infrastructure.Messaging;

/// <summary>
/// Publishes domain events to RabbitMQ using the shared 'cardealer.events' exchange.
/// Uses Topic exchange type — routing key = event.EventType property.
/// 
/// Pattern: same as AuthService and VehiclesSaleService publishers.
/// </summary>
public sealed class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _lock = new();
    private bool _disposed;

    private const string ExchangeName = "cardealer.events";

    public RabbitMqEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : EventBase
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqEventPublisher));

        var rabbitMQEnabled = _configuration.GetValue<bool>("RabbitMQ:Enabled");
        if (!rabbitMQEnabled)
        {
            _logger.LogWarning(
                "RabbitMQ is disabled. Event {EventType} will NOT be published. AlertId context lost.",
                @event.EventType);
            return Task.CompletedTask;
        }

        try
        {
            EnsureConnection();

            if (_channel == null)
            {
                _logger.LogError("RabbitMQ channel is null — cannot publish event {EventType}", @event.EventType);
                return Task.CompletedTask;
            }

            var message = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var body = Encoding.UTF8.GetBytes(message);
            var routingKey = @event.EventType;

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = @event.EventId.ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Headers = new Dictionary<string, object>
            {
                ["event_type"] = @event.EventType,
                ["schema_version"] = @event.SchemaVersion.ToString(),
                ["correlation_id"] = @event.CorrelationId ?? ""
            };

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "✅ Published event {EventType} to exchange {Exchange} with routing key {RoutingKey}. EventId={EventId}",
                @event.EventType, ExchangeName, routingKey, @event.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Failed to publish event {EventType}. EventId={EventId}",
                @event.EventType, @event.EventId);
            // Don't throw — the alert is already saved in DB. 
            // The notification failure shouldn't rollback the trigger.
            // TODO: Consider adding DLQ/retry for failed publishes
        }

        return Task.CompletedTask;
    }

    private void EnsureConnection()
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            return;

        lock (_lock)
        {
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
                return;

            try
            {
                _channel?.Dispose();
                _connection?.Dispose();

                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                    Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest",
                    VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                    RequestedHeartbeat = TimeSpan.FromSeconds(30),
                    ClientProvidedName = $"alertservice-publisher-{Environment.MachineName}"
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare exchange (idempotent — safe to call multiple times)
                _channel.ExchangeDeclare(
                    exchange: ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                _logger.LogInformation("✅ RabbitMQ connection established for AlertService publisher");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to establish RabbitMQ connection for AlertService publisher");
                throw;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

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
    }
}
