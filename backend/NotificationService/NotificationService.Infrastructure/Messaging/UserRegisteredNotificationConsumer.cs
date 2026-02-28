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
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Consumer que escucha eventos de registro de usuarios desde AuthService.
/// 
/// NOTA: Este consumer ya NO envía email de bienvenida.
/// El email de bienvenida se envía DESPUÉS de que el usuario verifica su email.
/// Este consumer solo loguea el registro para analytics/métricas.
/// 
/// Flujo correcto:
/// 1. Usuario se registra → AuthService envía email de VERIFICACIÓN
/// 2. Usuario hace click en link → AuthService verifica email
/// 3. AuthService envía email de BIENVENIDA (solo después de verificar)
/// 
/// IMPORTANT: Este consumer implementa manejo robusto de errores:
/// - Retry con exponential backoff
/// - Dead Letter Queue para mensajes que fallan repetidamente
/// - Límite máximo de reintentos para evitar loops infinitos
/// </summary>
public class UserRegisteredNotificationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserRegisteredNotificationConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "notificationservice.user.registered";
    private const string RoutingKey = "auth.user.registered";
    private const string DeadLetterExchange = "cardealer.events.dlx";
    private const string DeadLetterQueue = "notificationservice.user.registered.dlq";
    private const int MaxRetryAttempts = 3;

    public UserRegisteredNotificationConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<UserRegisteredNotificationConsumer> logger)
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
            _logger.LogInformation("RabbitMQ is disabled. UserRegisteredNotificationConsumer will not start.");
            return;
        }

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"] ?? throw new InvalidOperationException("RabbitMQ:Username is not configured"),
                Password = _configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured"),
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // ✅ Declarar Dead Letter Exchange para mensajes fallidos
            // cardealer.events.dlx is Direct type (must match live RabbitMQ topology)
            _channel.ExchangeDeclare(
                exchange: DeadLetterExchange,
                type: ExchangeType.Direct,
                durable: true
            );

            // ✅ Declarar Dead Letter Queue
            _channel.QueueDeclare(
                queue: DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false
            );
            _channel.QueueBind(DeadLetterQueue, DeadLetterExchange, RoutingKey);

            // Declarar exchange principal
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true
            );

            // ✅ Declarar queue CON Dead Letter Exchange configurado
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
                arguments: queueArgs
            );

            // Bind queue al exchange con routing key
            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey
            );

            _logger.LogInformation("UserRegisteredNotificationConsumer initialized for queue: {Queue} with DLQ: {DLQ}", QueueName, DeadLetterQueue);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var retryCount = GetRetryCount(ea.BasicProperties);
                
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var userRegisteredEvent = JsonSerializer.Deserialize<UserRegisteredEvent>(message);

                    if (userRegisteredEvent != null)
                    {
                        // ✅ Validar que el email no esté vacío
                        if (string.IsNullOrWhiteSpace(userRegisteredEvent.Email))
                        {
                            _logger.LogWarning("Received UserRegisteredEvent with empty email. UserId={UserId}. Sending to DLQ.", userRegisteredEvent.UserId);
                            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); // Goes to DLQ
                            return;
                        }
                        
                        _logger.LogInformation(
                            "Received UserRegisteredEvent: UserId={UserId}, Email={Email}, RetryCount={RetryCount}",
                            userRegisteredEvent.UserId,
                            userRegisteredEvent.Email,
                            retryCount);

                        await HandleEventAsync(userRegisteredEvent, stoppingToken);

                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        _logger.LogInformation("Successfully processed UserRegisteredEvent for {Email}", userRegisteredEvent.Email);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize UserRegisteredEvent. Sending to DLQ.");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); // Goes to DLQ
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing UserRegisteredEvent. Attempt {RetryCount}/{MaxRetries}", 
                        retryCount + 1, MaxRetryAttempts);

                    if (retryCount >= MaxRetryAttempts - 1)
                    {
                        // ✅ Max retries reached - send to Dead Letter Queue (no requeue)
                        _logger.LogWarning("Max retry attempts ({MaxRetries}) reached. Sending message to DLQ.", MaxRetryAttempts);
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                    else
                    {
                        // ✅ Retry with delay - wait before requeue to prevent tight loop
                        var delayMs = (int)Math.Pow(2, retryCount + 1) * 1000; // Exponential backoff: 2s, 4s, 8s
                        _logger.LogInformation("Waiting {Delay}ms before retry...", delayMs);
                        await Task.Delay(delayMs, stoppingToken);
                        
                        // Requeue for retry
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation("UserRegisteredNotificationConsumer started successfully");

            // Mantener el consumer activo
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize UserRegisteredNotificationConsumer");
        }
    }

    private async Task HandleEventAsync(UserRegisteredEvent eventData, CancellationToken cancellationToken)
    {
        // ✅ IMPORTANTE: Ya NO enviamos email de bienvenida aquí
        // El email de bienvenida se envía DESPUÉS de que el usuario verifica su email
        // Este handler solo loguea el evento para analytics/métricas
        
        _logger.LogInformation(
            "UserRegistered event processed - User: {Email}, UserId: {UserId}, AccountType: {AccountType}, RegisteredAt: {RegisteredAt}",
            eventData.Email,
            eventData.UserId,
            eventData.Metadata?.GetValueOrDefault("AccountType", "Unknown"),
            eventData.RegisteredAt
        );

        // ✅ Send admin alert for new user registration
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var adminAlertService = scope.ServiceProvider.GetRequiredService<IAdminAlertService>();
            
            await adminAlertService.SendAlertAsync(
                alertType: "new_user_registered",
                title: "Nuevo usuario registrado",
                message: $"Se ha registrado un nuevo usuario: {eventData.Email} ({eventData.FullName ?? "N/A"})",
                severity: "Info",
                metadata: new Dictionary<string, string>
                {
                    ["UserId"] = eventData.UserId.ToString(),
                    ["Email"] = eventData.Email,
                    ["Name"] = eventData.FullName ?? "N/A",
                    ["RegisteredAt"] = eventData.RegisteredAt.ToString("yyyy-MM-dd HH:mm:ss UTC")
                },
                ct: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send admin alert for new user registration. Non-critical.");
        }

        // Persist welcome in-app notification for the new user
        if (eventData.UserId != Guid.Empty)
        {
            try
            {
                using var welcomeScope = _serviceProvider.CreateScope();
                var userNotifService = welcomeScope.ServiceProvider.GetService<IUserNotificationService>();
                if (userNotifService != null)
                {
                    await userNotifService.CreateAsync(
                        userId: eventData.UserId,
                        type: "system",
                        title: "🎉 ¡Bienvenido/a a OKLA!",
                        message: $"¡Hola {eventData.FullName ?? eventData.Email}! Tu cuenta ha sido creada exitosamente. Explora miles de vehículos en la República Dominicana.",
                        icon: "🎉",
                        link: "/",
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist welcome notification for UserId: {UserId}. Non-critical.", eventData.UserId);
            }
        }
    }

    /// <summary>
    /// Obtiene el conteo de reintentos del mensaje desde los headers de RabbitMQ.
    /// El header x-death contiene información sobre reintentos previos.
    /// </summary>
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

        // Fallback: check custom retry header
        if (properties.Headers.TryGetValue("x-retry-count", out var retryCount))
        {
            return Convert.ToInt32(retryCount);
        }

        return 0;
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
