#nullable disable
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Xunit;
using CarDealer.Contracts.Events.Vehicle;

namespace VehiclesSaleService.Tests.Integration.Events;

/// <summary>
/// Integration tests for VehicleCreatedEvent consumer.
/// Tests RabbitMQ message consumption with real broker.
/// Requires RabbitMQ active — skipped automatically if not available (CI without RabbitMQ).
/// </summary>
public class VehicleCreatedEventConsumerTests : IDisposable
{
    private readonly Mock<ILogger<VehicleCreatedEventConsumerTests>> _loggerMock;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queueName;
    private readonly bool _rabbitMqAvailable;
    private const string ExchangeName = "test.events.exchange";
    private const string RoutingKey = "vehicle.created";

    public VehicleCreatedEventConsumerTests()
    {
        _loggerMock = new Mock<ILogger<VehicleCreatedEventConsumerTests>>();
        _queueName = $"test.{Guid.NewGuid():N}";

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

            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _channel.QueueDeclare(
                queue: _queueName,
                durable: false,
                exclusive: true,
                autoDelete: true);

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
        var expectedEvent = new VehicleCreatedEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            VehicleId = Guid.NewGuid(),
            Make = "Toyota",
            Model = "Camry",
            Year = 2024,
            Price = 35000.00m,
            VIN = "1HGBH41JXMN109186",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var receivedEvent = default(VehicleCreatedEvent);
        var messageReceived = new TaskCompletionSource<bool>();
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                receivedEvent = JsonSerializer.Deserialize<VehicleCreatedEvent>(message);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                messageReceived.TrySetResult(true);
            }
            catch (Exception ex)
            {
                _loggerMock.Object.LogError(ex, "Error processing message");
                messageReceived.TrySetException(ex);
            }
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
        var received = await messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(received);
        Assert.NotNull(receivedEvent);
        Assert.Equal(expectedEvent.EventId, receivedEvent.EventId);
        Assert.Equal(expectedEvent.VehicleId, receivedEvent.VehicleId);
        Assert.Equal(expectedEvent.Make, receivedEvent.Make);
        Assert.Equal(expectedEvent.Model, receivedEvent.Model);
        Assert.Equal(expectedEvent.Year, receivedEvent.Year);
        Assert.Equal(expectedEvent.Price, receivedEvent.Price);
        Assert.Equal(expectedEvent.VIN, receivedEvent.VIN);
        Assert.Equal(expectedEvent.CreatedBy, receivedEvent.CreatedBy);
        Assert.Equal(expectedEvent.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                     receivedEvent.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [Fact]
    public async Task Consumer_DeserializesAllProperties_Correctly()
    {
        if (!_rabbitMqAvailable) return;

        // Arrange
        var expectedEvent = new VehicleCreatedEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            VehicleId = Guid.NewGuid(),
            Make = "Honda",
            Model = "Accord",
            Year = 2023,
            Price = 32000.00m,
            VIN = "2HGFC2F59MH507688",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var receivedEvent = default(VehicleCreatedEvent);
        var messageReceived = new TaskCompletionSource<bool>();
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                receivedEvent = JsonSerializer.Deserialize<VehicleCreatedEvent>(message);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                messageReceived.TrySetResult(true);
            }
            catch (Exception ex)
            {
                messageReceived.TrySetException(ex);
            }
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
        var received = await messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(received);
        Assert.NotNull(receivedEvent);

        // Validate EventBase properties
        Assert.Equal(expectedEvent.EventId, receivedEvent.EventId);
        Assert.Equal("vehicle.created", receivedEvent.EventType);
        Assert.Equal(expectedEvent.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss"),
                     receivedEvent.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss"));

        // Validate VehicleCreatedEvent specific properties
        Assert.Equal(expectedEvent.VehicleId, receivedEvent.VehicleId);
        Assert.Equal(expectedEvent.Make, receivedEvent.Make);
        Assert.Equal(expectedEvent.Model, receivedEvent.Model);
        Assert.Equal(expectedEvent.Year, receivedEvent.Year);
        Assert.Equal(expectedEvent.Price, receivedEvent.Price);
        Assert.Equal(expectedEvent.VIN, receivedEvent.VIN);
        Assert.Equal(expectedEvent.CreatedBy, receivedEvent.CreatedBy);
        Assert.Equal(expectedEvent.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                     receivedEvent.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [Fact]
    public async Task Consumer_HandlesMultipleMessages_InSequence()
    {
        if (!_rabbitMqAvailable) return;

        // Arrange
        const int messageCount = 5;
        var receivedEvents = new List<VehicleCreatedEvent>();
        var messagesReceived = new TaskCompletionSource<bool>();
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var evt = JsonSerializer.Deserialize<VehicleCreatedEvent>(message);
            receivedEvents.Add(evt!);

            _channel.BasicAck(ea.DeliveryTag, multiple: false);

            if (receivedEvents.Count == messageCount)
            {
                messagesReceived.TrySetResult(true);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        // Act
        for (int i = 0; i < messageCount; i++)
        {
            var testEvent = new VehicleCreatedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                VehicleId = Guid.NewGuid(),
                Make = "Toyota",
                Model = $"Model {i}",
                Year = 2020 + i,
                Price = 25000.00m + (i * 1000),
                VIN = $"VIN{i:D14}",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddSeconds(i)
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
        var received = await messagesReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(received);
        Assert.Equal(messageCount, receivedEvents.Count);

        for (int i = 0; i < messageCount; i++)
        {
            Assert.Equal($"Model {i}", receivedEvents[i].Model);
            Assert.Equal(2020 + i, receivedEvents[i].Year);
            Assert.Equal(25000.00m + (i * 1000), receivedEvents[i].Price);
        }
    }

    [Fact]
    public async Task Consumer_WithInvalidMessage_DoesNotCrash()
    {
        if (!_rabbitMqAvailable) return;

        // Arrange
        var errorOccurred = false;
        var messageProcessed = new TaskCompletionSource<bool>();
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                // This should throw JsonException
                var evt = JsonSerializer.Deserialize<VehicleCreatedEvent>(message);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (JsonException)
            {
                errorOccurred = true;
                // In real consumer, would send to DLQ
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
            finally
            {
                messageProcessed.TrySetResult(true);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        // Act - Send invalid JSON
        var invalidMessage = "{\"invalid\": \"json structure\"";
        var messageBody = Encoding.UTF8.GetBytes(invalidMessage);

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: RoutingKey,
            basicProperties: null,
            body: messageBody);

        // Assert
        var processed = await messageProcessed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(processed);
        Assert.True(errorOccurred, "Consumer should have handled invalid JSON error");
    }

    public void Dispose()
    {
        if (_rabbitMqAvailable)
        {
            _channel?.Close();
            _connection?.Close();
        }
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
