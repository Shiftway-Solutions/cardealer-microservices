#nullable disable
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CarDealer.Contracts.Events.Auth;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace AuthService.Tests.Integration.Events;

/// <summary>
/// Integration tests para UserRegisteredEventConsumer.
/// Valida la comunicación completa: Publisher → RabbitMQ → Consumer.
/// </summary>
public class UserRegisteredEventConsumerTests : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly Mock<ILogger<UserRegisteredEventConsumerTests>> _loggerMock;
    private readonly string _queueName;
    private readonly bool _rabbitMqAvailable;
    private const string ExchangeName = "cardealer.events";
    private const string RoutingKey = "auth.user.registered";

    public UserRegisteredEventConsumerTests()
    {
        _loggerMock = new Mock<ILogger<UserRegisteredEventConsumerTests>>();
        _queueName = $"test.{Guid.NewGuid():N}";

        // Configurar conexión a RabbitMQ (usar RabbitMQ de docker-compose)
        var factory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest",
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
            RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declarar exchange
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declarar queue temporal para tests
            _channel.QueueDeclare(
                queue: _queueName,
                durable: false,
                exclusive: true,
                autoDelete: true);

            // Bind queue al exchange con routing key
            _channel.QueueBind(
                queue: _queueName,
                exchange: ExchangeName,
                routingKey: RoutingKey);

            _rabbitMqAvailable = true;
        }
        catch (BrokerUnreachableException)
        {
            // RabbitMQ not available (e.g., CI environment without RabbitMQ service).
            // Tests will be skipped gracefully.
            _rabbitMqAvailable = false;
        }
    }

    [Fact]
    public async Task Consumer_ReceivesPublishedEvent_Successfully()
    {
        if (!_rabbitMqAvailable) return;

        // Arrange
        var expectedEvent = new UserRegisteredEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            Email = "integration-test@example.com",
            FullName = "Integration Test User",
            RegisteredAt = DateTime.UtcNow
        };

        var receivedEvent = default(UserRegisteredEvent);
        var messageReceived = new TaskCompletionSource<bool>();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                receivedEvent = JsonSerializer.Deserialize<UserRegisteredEvent>(message);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                messageReceived.TrySetResult(true);
            }
            catch (Exception ex)
            {
                _loggerMock.Object.LogError(ex, "Error processing message");
                messageReceived.TrySetException(ex);
            }
        };

        _channel.BasicConsume(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);

        // Act - Publicar evento
        var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(expectedEvent));
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = expectedEvent.EventId.ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.ContentType = "application/json";

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: RoutingKey,
            basicProperties: properties,
            body: messageBody);

        // Assert - Esperar hasta 5 segundos por el mensaje
        var received = await messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));

        received.Should().BeTrue();
        receivedEvent.Should().NotBeNull();
        receivedEvent!.EventId.Should().Be(expectedEvent.EventId);
        receivedEvent.UserId.Should().Be(expectedEvent.UserId);
        receivedEvent.Email.Should().Be(expectedEvent.Email);
        receivedEvent.FullName.Should().Be(expectedEvent.FullName);
        receivedEvent.RegisteredAt.Should().BeCloseTo(expectedEvent.RegisteredAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Consumer_DeserializesAllProperties_Correctly()
    {
        if (!_rabbitMqAvailable) return;

        // Arrange
        var expectedEvent = new UserRegisteredEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            Email = "deserialize-test@example.com",
            FullName = "John Doe",
            RegisteredAt = DateTime.UtcNow
        };

        var receivedEvent = default(UserRegisteredEvent);
        var messageReceived = new TaskCompletionSource<bool>();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            receivedEvent = JsonSerializer.Deserialize<UserRegisteredEvent>(message);
            _channel.BasicAck(ea.DeliveryTag, multiple: false);
            messageReceived.TrySetResult(true);
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        // Act
        var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(expectedEvent));
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: RoutingKey,
            basicProperties: properties,
            body: messageBody);

        // Assert
        await messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));

        receivedEvent.Should().NotBeNull();
        receivedEvent!.EventType.Should().Be("auth.user.registered");
        receivedEvent.OccurredAt.Should().BeCloseTo(expectedEvent.OccurredAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Consumer_HandlesMultipleMessages_InSequence()
    {
        if (!_rabbitMqAvailable) return;

        // Arrange
        var messageCount = 5;
        var receivedMessages = 0;
        var allMessagesReceived = new TaskCompletionSource<bool>();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            receivedMessages++;
            _channel.BasicAck(ea.DeliveryTag, multiple: false);

            if (receivedMessages >= messageCount)
            {
                allMessagesReceived.TrySetResult(true);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        // Act - Publicar múltiples eventos
        for (int i = 0; i < messageCount; i++)
        {
            var testEvent = new UserRegisteredEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                UserId = Guid.NewGuid(),
                Email = $"test{i}@example.com",
                FullName = $"Test User {i}",
                RegisteredAt = DateTime.UtcNow.AddSeconds(i)
            };

            var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testEvent));
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: RoutingKey,
                basicProperties: properties,
                body: messageBody);
        }

        // Assert
        var allReceived = await allMessagesReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));

        allReceived.Should().BeTrue();
        receivedMessages.Should().Be(messageCount);
    }

    [Fact]
    public void Consumer_WithInvalidMessage_DoesNotCrash()
    {
        if (!_rabbitMqAvailable) return;

        // Arrange
        var invalidMessage = "{ invalid json }";
        var errorLogged = false;

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var receivedEvent = JsonSerializer.Deserialize<UserRegisteredEvent>(message);

                // Si llegamos aquí con mensaje inválido, es un problema
                receivedEvent.Should().BeNull(); // Esperamos que falle la deserialización
            }
            catch (JsonException)
            {
                errorLogged = true;
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        // Act
        var messageBody = Encoding.UTF8.GetBytes(invalidMessage);
        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: RoutingKey,
            basicProperties: properties,
            body: messageBody);

        // Assert - Esperar un momento para procesar
        Thread.Sleep(1000);

        // El consumer debe haber capturado el error sin crashear
        errorLogged.Should().BeTrue();
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
