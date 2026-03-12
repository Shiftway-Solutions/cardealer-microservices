using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MediaService.Application.Features.Media.Commands.ProcessMedia; // ✅ Agregar using
using MediaService.Shared; // ✅ Agregar using

namespace MediaService.Infrastructure.Messaging;

public class RabbitMQMediaConsumer : BackgroundService
{
    private readonly RabbitMQSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQMediaConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed = false;

    public RabbitMQMediaConsumer(
        IOptions<RabbitMQSettings> settings,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQMediaConsumer> logger)
    {
        _settings = settings.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeConnectionWithRetryAsync(stoppingToken);

        if (_channel == null || !_channel.IsOpen)
        {
            _logger.LogWarning("RabbitMQ connection not available, consumer will not start");
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            await ProcessMessageAsync(ea);
        };

        // Start consuming from the process media queue
        _channel!.BasicConsume( // ✅ Agregar null-forgiving operator
            queue: _settings.ProcessMediaQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQ Media Consumer started listening on queue: {Queue}", _settings.ProcessMediaQueue);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Verificar si la conexión sigue activa
            if (_connection == null || !_connection.IsOpen)
            {
                _logger.LogWarning("RabbitMQ connection lost, attempting to reconnect...");
                await InitializeConnectionWithRetryAsync(stoppingToken);

                if (_channel != null && _channel.IsOpen)
                {
                    var newConsumer = new AsyncEventingBasicConsumer(_channel);
                    newConsumer.Received += async (model, ea) =>
                    {
                        await ProcessMessageAsync(ea);
                    };
                    _channel.BasicConsume(
                        queue: _settings.ProcessMediaQueue,
                        autoAck: false,
                        consumer: newConsumer);
                    _logger.LogInformation("Reconnected to RabbitMQ successfully");
                }
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task InitializeConnectionWithRetryAsync(CancellationToken stoppingToken)
    {
        int retryCount = 0;
        const int maxRetryDelay = 60; // segundos máximo entre reintentos
        const int maxRetries = 10; // máximo número de reintentos antes de continuar sin RabbitMQ

        while (!stoppingToken.IsCancellationRequested && retryCount < maxRetries)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    Port = _settings.Port,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    VirtualHost = _settings.VirtualHost,
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Ensure exchanges and queues are declared (same as producer)
                _channel.ExchangeDeclare(_settings.MediaCommandsExchange, ExchangeType.Direct, durable: true);

                // Dead Letter Exchange for persistent DLQ (CRIT-01: replaces InMemoryDeadLetterQueue)
                var dlxExchange = $"{_settings.MediaCommandsExchange}.dlx";
                var dlqQueue = $"{_settings.ProcessMediaQueue}.dlq";
                _channel.ExchangeDeclare(dlxExchange, ExchangeType.Direct, durable: true);
                _channel.QueueDeclare(dlqQueue, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(dlqQueue, dlxExchange, _settings.ProcessMediaRoutingKey);

                // Main queue with DLX arguments — rejected messages are routed to DLQ automatically
                var queueArgs = new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", dlxExchange },
                    { "x-dead-letter-routing-key", _settings.ProcessMediaRoutingKey }
                };
                _channel.QueueDeclare(_settings.ProcessMediaQueue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
                _channel.QueueBind(_settings.ProcessMediaQueue, _settings.MediaCommandsExchange, _settings.ProcessMediaRoutingKey);

                // Configure quality of service
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _logger.LogInformation("Successfully connected to RabbitMQ on {Host}:{Port}", _settings.HostName, _settings.Port);
                return; // Conexión exitosa, salir del loop
            }
            catch (Exception ex)
            {
                retryCount++;
                var delaySeconds = Math.Min(maxRetryDelay, (int)Math.Pow(2, Math.Min(retryCount, 6)));

                _logger.LogWarning(ex,
                    "Failed to connect to RabbitMQ (attempt {RetryCount}/{MaxRetries}). Retrying in {Delay} seconds...",
                    retryCount, maxRetries, delaySeconds);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("RabbitMQ connection retry cancelled");
                    return;
                }
            }
        }

        if (retryCount >= maxRetries)
        {
            _logger.LogWarning("Failed to connect to RabbitMQ after {MaxRetries} attempts. Service will continue in degraded mode without RabbitMQ messaging.", maxRetries);
        }
    }

    // Keep legacy method for backward compatibility
    private Task InitializeConnectionAsync()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Ensure exchanges and queues are declared (same as producer)
            _channel.ExchangeDeclare(_settings.MediaCommandsExchange, ExchangeType.Direct, durable: true);

            // Dead Letter Exchange for persistent DLQ
            var dlxExchange = $"{_settings.MediaCommandsExchange}.dlx";
            var dlqQueue = $"{_settings.ProcessMediaQueue}.dlq";
            _channel.ExchangeDeclare(dlxExchange, ExchangeType.Direct, durable: true);
            _channel.QueueDeclare(dlqQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(dlqQueue, dlxExchange, _settings.ProcessMediaRoutingKey);

            // Main queue with DLX arguments
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", dlxExchange },
                { "x-dead-letter-routing-key", _settings.ProcessMediaRoutingKey }
            };
            _channel.QueueDeclare(_settings.ProcessMediaQueue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
            _channel.QueueBind(_settings.ProcessMediaQueue, _settings.MediaCommandsExchange, _settings.ProcessMediaRoutingKey);

            // Configure quality of service
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection for consumer");
            throw;
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea)
    {
        string message = string.Empty;
        ulong deliveryTag = ea.DeliveryTag;

        try
        {
            var body = ea.Body.ToArray();
            message = Encoding.UTF8.GetString(body);

            _logger.LogDebug("Received message from RabbitMQ: {Message}", message);

            // Process based on routing key
            switch (ea.RoutingKey)
            {
                case var key when key == _settings.ProcessMediaRoutingKey:
                    await ProcessMediaCommandAsync(message);
                    break;
                default:
                    _logger.LogWarning("Unknown routing key: {RoutingKey}", ea.RoutingKey);
                    break;
            }

            // Acknowledge the message - ✅ Corregir referencia nula
            if (_channel != null && _channel.IsOpen)
            {
                _channel.BasicAck(deliveryTag, multiple: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from RabbitMQ. Message: {Message}", message);

            // Reject and requeue the message - ✅ Corregir referencia nula
            if (_channel != null && _channel.IsOpen)
            {
                _channel.BasicNack(deliveryTag, multiple: false, requeue: false);
            }
        }
    }

    private async Task ProcessMediaCommandAsync(string message)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // ✅ Usar la clase de comando real en lugar de la clase helper
            var commandData = JsonSerializer.Deserialize<ProcessMediaCommandData>(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (commandData == null)
            {
                _logger.LogWarning("Failed to deserialize process media command: {Message}", message);
                return;
            }

            _logger.LogInformation("Processing media command for {MediaId}", commandData.MediaId);

            // ✅ Crear el comando real de MediatR
            var command = new ProcessMediaCommand(
                commandData.MediaId,
                commandData.ProcessingType,
                commandData.ProcessingOptions);

            // Send the command through MediatR pipeline
            var result = await mediator.Send(command);

            // ✅ CORRECCIÓN: Acceder a las propiedades correctamente
            if (result.Success) // ✅ Ya no da error CS1061
            {
                _logger.LogInformation("Successfully processed media {MediaId}", commandData.MediaId);
            }
            else
            {
                _logger.LogWarning("Failed to process media {MediaId}: {Error}", commandData.MediaId, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process media command: {Message}", message);
            throw;
        }
    }

    // ✅ Clase auxiliar para deserialización
    private class ProcessMediaCommandData
    {
        public string MediaId { get; set; } = string.Empty;
        public string? ProcessingType { get; set; }
        public Dictionary<string, object>? ProcessingOptions { get; set; }
        public DateTime Timestamp { get; set; }
        public string CommandId { get; set; } = string.Empty;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQMediaConsumer stopping gracefully...");

        try { _channel?.Close(); } catch { /* swallow on shutdown */ }
        try { _connection?.Close(); } catch { /* swallow on shutdown */ }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("RabbitMQMediaConsumer stopped");
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _channel?.Close();
            _connection?.Close();
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
            _disposed = true;
        }
    }
}