using System.Text;
using System.Text.Json;
using CarDealer.Contracts.Abstractions;
using CarDealer.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using RabbitMQ.Client;
using RecoAgent.Domain.Interfaces;

namespace RecoAgent.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of IEventPublisher for RecoAgent.
/// Publishes recommendation events to cardealer.events exchange with Circuit Breaker + DLQ fallback.
/// Uses SharedRabbitMqConnection (singleton per pod) instead of creating its own connection.
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly SharedRabbitMqConnection _connection;
    private readonly ISharedDeadLetterQueue? _deadLetterQueue;
    private readonly string _exchangeName;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _publishLock = new();
    private IModel? _channel;

    public RabbitMqEventPublisher(
        SharedRabbitMqConnection connection,
        ILogger<RabbitMqEventPublisher> logger,
        ISharedDeadLetterQueue? deadLetterQueue = null,
        string exchangeName = "cardealer.events")
    {
        _connection = connection;
        _logger = logger;
        _deadLetterQueue = deadLetterQueue;
        _exchangeName = exchangeName;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Configure Circuit Breaker with Polly v8
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    _logger.LogWarning(
                        "🔴 Circuit Breaker OPEN: RabbitMQ unavailable for {Duration}s. Events queued to DLQ.",
                        args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    _logger.LogInformation("🟢 Circuit Breaker CLOSED: RabbitMQ connection restored.");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = _ =>
                {
                    _logger.LogInformation("🟡 Circuit Breaker HALF-OPEN: Testing RabbitMQ connection...");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        _logger.LogInformation(
            "RecoAgent RabbitMQ Event Publisher initialized with Circuit Breaker + DLQ fallback. Exchange={Exchange}",
            _exchangeName);
    }

    private IModel GetChannel()
    {
        if (_channel is { IsOpen: true })
            return _channel;

        lock (_publishLock)
        {
            if (_channel is { IsOpen: true })
                return _channel;

            _channel?.Dispose();
            _channel = _connection.CreateChannel();

            // Declare topic exchange (idempotent)
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            return _channel;
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                var routingKey = @event.EventType;
                var message = JsonSerializer.Serialize(@event, @event.GetType(), _jsonOptions);
                var body = Encoding.UTF8.GetBytes(message);

                lock (_publishLock)
                {
                    var channel = GetChannel();
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.MessageId = @event.EventId.ToString();
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    properties.Type = @event.EventType;
                    properties.ContentType = "application/json";

                    channel.BasicPublish(
                        exchange: _exchangeName,
                        routingKey: routingKey,
                        basicProperties: properties,
                        body: body);
                }

                _logger.LogInformation(
                    "Published event {EventType} EventId={EventId} to exchange={Exchange} routingKey={RoutingKey}",
                    @event.EventType, @event.EventId, _exchangeName, routingKey);

                await Task.CompletedTask;
            }, cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex,
                "⚠️ Circuit Breaker OPEN: Cannot publish {EventType} EventId={EventId}. Queuing to DLQ.",
                @event.EventType, @event.EventId);

            await EnqueueToDlqAsync(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Failed to publish {EventType} EventId={EventId}. Queuing to DLQ.",
                @event.EventType, @event.EventId);

            await EnqueueToDlqAsync(@event);
        }
    }

    private async Task EnqueueToDlqAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        if (_deadLetterQueue == null)
        {
            _logger.LogError(
                "No DLQ configured — event {EventType} EventId={EventId} LOST",
                @event.EventType, @event.EventId);
            return;
        }

        try
        {
            var failedEvent = new DeadLetterEvent
            {
                ServiceName = "RecoAgent",
                EventType = @event.EventType,
                EventJson = JsonSerializer.Serialize(@event, @event.GetType(), _jsonOptions),
                FailedAt = DateTime.UtcNow
            };
            failedEvent.ScheduleNextRetry();
            await _deadLetterQueue.EnqueueAsync(failedEvent);

            _logger.LogInformation(
                "📮 Event {EventId} queued to DLQ for retry in {Minutes}min",
                failedEvent.Id, 1);
        }
        catch (Exception dlqEx)
        {
            _logger.LogCritical(dlqEx,
                "🔥 CRITICAL: DLQ enqueue failed for {EventType} EventId={EventId}. Event LOST.",
                @event.EventType, @event.EventId);
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _logger.LogInformation("RecoAgent RabbitMQ Event Publisher disposed");
    }
}
