using CarDealer.Contracts.Events.KYC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using UserService.Infrastructure.Persistence;

namespace UserService.Infrastructure.Services.Messaging;

/// <summary>
/// Consumer for KYCProfileStatusChangedEvent from KYCService.
/// Sets IsVerified = true on the User when KYC is Approved,
/// or IsVerified = false when KYC is Rejected or Expired.
/// </summary>
public class KYCProfileApprovedEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KYCProfileApprovedEventConsumer> _logger;
    private readonly RabbitMQSettings _rabbitMQSettings;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "userservice.kyc.profile.status_changed";
    private const string RoutingKey = "kyc.profile.status_changed";

    public KYCProfileApprovedEventConsumer(
        IOptions<RabbitMQSettings> rabbitMqSettings,
        IServiceProvider serviceProvider,
        ILogger<KYCProfileApprovedEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _rabbitMQSettings = rabbitMqSettings.Value;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit for the rest of the app (DB migrations, RabbitMQ) to be ready
        await Task.Delay(5000, stoppingToken);

        // Retry loop — handles transient failures at startup
        const int maxRetries = 10;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (stoppingToken.IsCancellationRequested) return;

            InitializeRabbitMQ();

            if (_channel != null)
                break;

            var delay = TimeSpan.FromSeconds(Math.Min(attempt * 5, 60));
            _logger.LogWarning(
                "KYCProfileApprovedEventConsumer: RabbitMQ not ready (attempt {Attempt}/{Max}). Retrying in {Delay}s…",
                attempt, maxRetries, delay.TotalSeconds);

            if (attempt == maxRetries)
            {
                _logger.LogError("KYCProfileApprovedEventConsumer: Exhausted {Max} retries. Consumer will NOT start.", maxRetries);
                return;
            }

            await Task.Delay(delay, stoppingToken);
        }

        try
        {
            if (_channel == null)
            {
                _logger.LogWarning("RabbitMQ channel not available, KYCProfileApprovedEventConsumer will not start");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    _logger.LogInformation("Received KYCProfileStatusChangedEvent: {Message}", message);

                    var @event = JsonSerializer.Deserialize<KYCProfileStatusChangedEvent>(message, _jsonOptions);
                    if (@event != null)
                    {
                        await ProcessEventAsync(@event, stoppingToken);
                        _channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("Successfully processed KYCProfileStatusChangedEvent for user {UserId}", @event.UserId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize KYCProfileStatusChangedEvent");
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing KYCProfileStatusChangedEvent");
                    _channel.BasicNack(ea.DeliveryTag, false, true); // Requeue
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("KYCProfileApprovedEventConsumer started, listening on queue: {Queue}", QueueName);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in KYCProfileApprovedEventConsumer");
        }
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMQSettings.Host,
                Port = _rabbitMQSettings.Port,
                UserName = _rabbitMQSettings.Username,
                Password = _rabbitMQSettings.Password,
                VirtualHost = _rabbitMQSettings.VirtualHost,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation(
                "RabbitMQ KYCProfileApprovedEventConsumer initialized - Exchange: {Exchange}, Queue: {Queue}, RoutingKey: {RoutingKey}",
                ExchangeName, QueueName, RoutingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for KYCProfileApprovedEventConsumer");
        }
    }

    private async Task ProcessEventAsync(KYCProfileStatusChangedEvent @event, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await dbContext.Users.FindAsync(new object[] { @event.UserId }, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found in UserService, cannot update IsVerified", @event.UserId);
            return;
        }

        switch (@event.NewStatus)
        {
            case "Approved":
                user.IsVerified = true;
                user.VerifiedAt = @event.ChangedAt;
                user.UpdatedAt = DateTime.UtcNow;
#pragma warning disable CS0618 // Obsolete Email — Ley 172-13 migration in progress
                _logger.LogInformation("Set IsVerified=true for user {UserId} ({Email}) after KYC approval", @event.UserId, @event.Email);
#pragma warning restore CS0618
                break;

            case "Rejected":
            case "Expired":
            case "Suspended":
                user.IsVerified = false;
                user.VerifiedAt = null;
                user.UpdatedAt = DateTime.UtcNow;
#pragma warning disable CS0618 // Obsolete Email — Ley 172-13 migration in progress
                _logger.LogInformation("Set IsVerified=false for user {UserId} ({Email}) after KYC status={Status}", @event.UserId, @event.Email, @event.NewStatus);
#pragma warning restore CS0618
                break;

            default:
                // UnderReview, InProgress, Pending — no change to IsVerified
                _logger.LogDebug("KYC status {Status} for user {UserId} — no IsVerified change needed", @event.NewStatus, @event.UserId);
                return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
