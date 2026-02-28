using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CarDealer.Contracts.Events.Auth;
using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Consumer que escucha eventos de inicio de sesión desde AuthService
/// y crea notificaciones in-app de seguridad para el usuario.
///
/// Flujo:
/// 1. Usuario inicia sesión → AuthService publica UserLoggedInEvent (auth.user.loggedin)
/// 2. Este consumer recibe el evento y crea una UserNotification de seguridad
/// 3. La notificación aparece en el header (campana) y en /cuenta/notificaciones
///
/// SEGURIDAD: La notificación in-app NO expone IP, User-Agent ni dispositivo.
/// Solo informa que hubo un inicio de sesión y redirige a /cuenta/seguridad.
///
/// Nota: AuthService también envía un email con detalles del dispositivo/IP
/// mediante RabbitMQNotificationConsumer (routing key: notification.auth).
/// Este consumer es independiente y usa la cola: notificationservice.user.loggedin
/// </summary>
public class UserLoggedInEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserLoggedInEventConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "notificationservice.user.loggedin";
    private const string RoutingKey = "auth.user.loggedin";
    private const string DeadLetterExchange = "cardealer.events.dlx";
    private const string DeadLetterQueue = "notificationservice.user.loggedin.dlq";
    private const int MaxRetryAttempts = 3;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public UserLoggedInEventConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<UserLoggedInEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitEnabled = _configuration.GetValue<bool>("RabbitMQ:Enabled");
        if (!rabbitEnabled)
        {
            _logger.LogInformation("RabbitMQ is disabled. UserLoggedInEventConsumer will not start.");
            return;
        }

        // Pequeña espera para que RabbitMQ esté listo
        await Task.Delay(5000, stoppingToken);

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
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Dead Letter Exchange (Direct — debe coincidir con topología existente)
            _channel.ExchangeDeclare(
                exchange: DeadLetterExchange,
                type: ExchangeType.Direct,
                durable: true);

            // Dead Letter Queue
            _channel.QueueDeclare(
                queue: DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);
            _channel.QueueBind(DeadLetterQueue, DeadLetterExchange, RoutingKey);

            // Exchange principal (Topic)
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true);

            // Cola con DLX configurado
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", DeadLetterExchange },
                { "x-dead-letter-routing-key", RoutingKey }
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

            _logger.LogInformation(
                "UserLoggedInEventConsumer initialized. Queue: {Queue}, RoutingKey: {RoutingKey}",
                QueueName, RoutingKey);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var retryCount = GetRetryCount(ea.BasicProperties);

                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    var loginEvent = JsonSerializer.Deserialize<UserLoggedInEvent>(message, _jsonOptions);

                    if (loginEvent == null || loginEvent.UserId == Guid.Empty)
                    {
                        _logger.LogWarning(
                            "UserLoggedInEvent deserialization failed or UserId is empty. Sending to DLQ.");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                        return;
                    }

                    _logger.LogInformation(
                        "Received UserLoggedInEvent: UserId={UserId}, LoggedInAt={LoggedInAt}, Retry={Retry}",
                        loginEvent.UserId, loginEvent.LoggedInAt, retryCount);

                    await HandleEventAsync(loginEvent, stoppingToken);

                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    _logger.LogInformation(
                        "Successfully processed UserLoggedInEvent for UserId={UserId}", loginEvent.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing UserLoggedInEvent. Attempt {Attempt}/{Max}",
                        retryCount + 1, MaxRetryAttempts);

                    if (retryCount >= MaxRetryAttempts - 1)
                    {
                        _logger.LogWarning(
                            "Max retries ({Max}) reached for UserLoggedInEvent. Sending to DLQ.",
                            MaxRetryAttempts);
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                    else
                    {
                        var delayMs = (int)Math.Pow(2, retryCount + 1) * 1000; // 2s, 4s, 8s
                        _logger.LogInformation("Waiting {Delay}ms before retry...", delayMs);
                        await Task.Delay(delayMs, stoppingToken);
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("UserLoggedInEventConsumer started successfully");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("UserLoggedInEventConsumer stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize UserLoggedInEventConsumer");
        }
    }

    private async Task HandleEventAsync(UserLoggedInEvent loginEvent, CancellationToken cancellationToken)
    {
        // SEGURIDAD: No exponer IP ni dispositivo en la notificación in-app.
        // El email de sesión (enviado por RabbitMQNotificationConsumer) ya incluye ese detalle.
        // Aquí solo notificamos que hubo un acceso y dirigimos a /cuenta/seguridad.
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var userNotifService = scope.ServiceProvider.GetService<IUserNotificationService>();

            if (userNotifService == null)
            {
                _logger.LogWarning(
                    "IUserNotificationService not available in DI scope for UserLoggedInEvent. UserId={UserId}",
                    loginEvent.UserId);
                return;
            }

            var loginTimeFormatted = loginEvent.LoggedInAt.ToString("dd/MM/yyyy HH:mm") + " UTC";

            await userNotifService.CreateAsync(
                userId: loginEvent.UserId,
                type: "system",
                title: "🔐 Nuevo inicio de sesión",
                message: $"Se detectó un nuevo acceso a tu cuenta el {loginTimeFormatted}. " +
                         "Si no fuiste tú, asegura tu cuenta de inmediato.",
                icon: "🔐",
                link: "/cuenta/seguridad",
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Security in-app notification created for UserId={UserId} at {LoginTime}",
                loginEvent.UserId, loginEvent.LoggedInAt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to create security notification for UserId={UserId}. Non-critical.",
                loginEvent.UserId);
        }
    }

    private static int GetRetryCount(IBasicProperties? properties)
    {
        if (properties?.Headers == null)
            return 0;

        if (properties.Headers.TryGetValue("x-death", out var deathHeader))
        {
            if (deathHeader is List<object> deaths && deaths.Count > 0)
            {
                if (deaths[0] is Dictionary<string, object> death &&
                    death.TryGetValue("count", out var count))
                {
                    return Convert.ToInt32(count);
                }
            }
        }

        if (properties.Headers.TryGetValue("x-retry-count", out var retryCount))
            return Convert.ToInt32(retryCount);

        return 0;
    }

    public override void Dispose()
    {
        try { _channel?.Close(); } catch { /* ignore */ }
        try { _connection?.Close(); } catch { /* ignore */ }
        try { _channel?.Dispose(); } catch { /* ignore */ }
        try { _connection?.Dispose(); } catch { /* ignore */ }
        base.Dispose();
    }
}
