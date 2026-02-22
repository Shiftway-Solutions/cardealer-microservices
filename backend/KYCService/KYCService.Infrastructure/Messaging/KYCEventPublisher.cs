using System.Text;
using System.Text.Json;
using CarDealer.Contracts.Events.KYC;
using KYCService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace KYCService.Infrastructure.Messaging;

/// <summary>
/// Publica eventos de KYC al exchange de RabbitMQ para que
/// otros microservicios (ej. NotificationService) los consuman.
/// </summary>
public sealed class KYCEventPublisher : IKYCEventPublisher, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KYCEventPublisher> _logger;

    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _lock = new();
    private bool _disposed;

    private const string ExchangeName = "cardealer.events";

    public KYCEventPublisher(
        IConfiguration configuration,
        ILogger<KYCEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task PublishStatusChangedAsync(
        KYCProfileStatusChangedEvent @event,
        CancellationToken cancellationToken = default)
    {
        var rabbitEnabled = _configuration.GetValue<bool>("RabbitMQ:Enabled");
        if (!rabbitEnabled)
        {
            _logger.LogInformation(
                "RabbitMQ is disabled. Skipping KYCProfileStatusChangedEvent publish for ProfileId={ProfileId}",
                @event.ProfileId);
            return Task.CompletedTask;
        }

        try
        {
            EnsureChannelOpen();

            var body = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(@event, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));

            var props = _channel!.CreateBasicProperties();
            props.Persistent = true;
            props.MessageId = @event.EventId.ToString();
            props.ContentType = "application/json";
            props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            props.Type = @event.EventType;
            if (@event.CorrelationId is not null)
                props.CorrelationId = @event.CorrelationId;

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: @event.EventType, // "kyc.profile.status_changed"
                mandatory: false,
                basicProperties: props,
                body: body);

            _logger.LogInformation(
                "Published {EventType} for ProfileId={ProfileId}, UserId={UserId}, NewStatus={NewStatus}",
                @event.EventType, @event.ProfileId, @event.UserId, @event.NewStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish {EventType} for ProfileId={ProfileId}",
                nameof(KYCProfileStatusChangedEvent), @event.ProfileId);
            // No relanzamos — la notificación es best-effort y no debe romper el flujo de negocio
        }

        return Task.CompletedTask;
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private void EnsureChannelOpen()
    {
        lock (_lock)
        {
            if (_channel is { IsOpen: true })
                return;

            _channel?.Dispose();
            _connection?.Dispose();

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"]
                           ?? throw new InvalidOperationException("RabbitMQ:UserName is required"),
                Password = _configuration["RabbitMQ:Password"]
                           ?? throw new InvalidOperationException("RabbitMQ:Password is required"),
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
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("KYCEventPublisher: RabbitMQ channel opened (exchange={Exchange})", ExchangeName);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { _channel?.Close(); } catch { /* ignored */ }
        try { _channel?.Dispose(); } catch { /* ignored */ }
        try { _connection?.Close(); } catch { /* ignored */ }
        try { _connection?.Dispose(); } catch { /* ignored */ }
    }
}
