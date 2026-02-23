#nullable disable
using System.Text;
using System.Text.Json;
using CarDealer.Contracts.Events.Billing;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace BillingService.Tests.Integration.Events;

/// <summary>
/// Tests de integración para verificar que el consumer de PaymentCompletedEvent
/// recibe y procesa correctamente eventos publicados en RabbitMQ.
/// Requiere RabbitMQ activo — se omiten automáticamente si no está disponible (CI sin RabbitMQ).
/// </summary>
public class PaymentCompletedEventConsumerTests : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queueName;
    private readonly Mock<ILogger<PaymentCompletedEventConsumerTests>> _loggerMock;
    private readonly bool _rabbitMqAvailable;
    private const string ExchangeName = "test.events.exchange";
    private const string RoutingKey = "billing.payment.completed";

    public PaymentCompletedEventConsumerTests()
    {
        _loggerMock = new Mock<ILogger<PaymentCompletedEventConsumerTests>>();
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
        if (!_rabbitMqAvailable)
        {
            // RabbitMQ not available in this environment — skipping integration test.
            return;
        }

        var expectedEvent = new PaymentCompletedEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            PaymentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            UserEmail = "john.doe@example.com",
            UserName = "John Doe",
            Amount = 299.99m,
            Currency = "USD",
            StripePaymentIntentId = "pi_1234567890abcdef",
            Description = "Premium Plan - Monthly Subscription",
            SubscriptionPlan = "Premium",
            PaidAt = DateTime.UtcNow
        };

        var receivedEvent = default(PaymentCompletedEvent);
        var messageReceived = new TaskCompletionSource<bool>();
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                receivedEvent = JsonSerializer.Deserialize<PaymentCompletedEvent>(message);
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
        Assert.Equal(expectedEvent.PaymentId, receivedEvent.PaymentId);
        Assert.Equal(expectedEvent.UserId, receivedEvent.UserId);
        Assert.Equal(expectedEvent.UserEmail, receivedEvent.UserEmail);
        Assert.Equal(expectedEvent.UserName, receivedEvent.UserName);
        Assert.Equal(expectedEvent.Amount, receivedEvent.Amount);
        Assert.Equal(expectedEvent.Currency, receivedEvent.Currency);
        Assert.Equal(expectedEvent.StripePaymentIntentId, receivedEvent.StripePaymentIntentId);
        Assert.Equal(expectedEvent.Description, receivedEvent.Description);
        Assert.Equal(expectedEvent.SubscriptionPlan, receivedEvent.SubscriptionPlan);
        Assert.Equal(expectedEvent.PaidAt.ToString("yyyy-MM-dd HH:mm:ss"),
                     receivedEvent.PaidAt.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [Fact]
    public async Task Consumer_DeserializesAllProperties_Correctly()
    {
        if (!_rabbitMqAvailable) return;

        // Arrange
        var expectedEvent = new PaymentCompletedEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            PaymentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            UserEmail = "jane.smith@example.com",
            UserName = "Jane Smith",
            Amount = 99.99m,
            Currency = "EUR",
            StripePaymentIntentId = "pi_0987654321fedcba",
            Description = "Basic Plan - Monthly Subscription",
            SubscriptionPlan = "Basic",
            PaidAt = DateTime.UtcNow
        };

        var receivedEvent = default(PaymentCompletedEvent);
        var messageReceived = new TaskCompletionSource<bool>();
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                receivedEvent = JsonSerializer.Deserialize<PaymentCompletedEvent>(message);
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

        // Validate EventBase properties
        Assert.Equal(expectedEvent.EventId, receivedEvent.EventId);
        Assert.Equal("billing.payment.completed", receivedEvent.EventType);
        Assert.Equal(expectedEvent.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss"),
                     receivedEvent.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss"));

        // Validate PaymentCompletedEvent specific properties
        Assert.Equal(expectedEvent.PaymentId, receivedEvent.PaymentId);
        Assert.Equal(expectedEvent.UserId, receivedEvent.UserId);
        Assert.Equal(expectedEvent.UserEmail, receivedEvent.UserEmail);
        Assert.Equal(expectedEvent.UserName, receivedEvent.UserName);
        Assert.Equal(expectedEvent.Amount, receivedEvent.Amount);
        Assert.Equal(expectedEvent.Currency, receivedEvent.Currency);
        Assert.Equal(expectedEvent.StripePaymentIntentId, receivedEvent.StripePaymentIntentId);
        Assert.Equal(expectedEvent.Description, receivedEvent.Description);
        Assert.Equal(expectedEvent.SubscriptionPlan, receivedEvent.SubscriptionPlan);
        Assert.Equal(expectedEvent.PaidAt.ToString("yyyy-MM-dd HH:mm:ss"),
                     receivedEvent.PaidAt.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [Fact]
    public async Task Consumer_HandlesMultipleMessages_InSequence()
    {
        if (!_rabbitMqAvailable) return;

        // Arrange
        const int messageCount = 5;
        var receivedEvents = new List<PaymentCompletedEvent>();
        var messagesReceived = new TaskCompletionSource<bool>();
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var evt = JsonSerializer.Deserialize<PaymentCompletedEvent>(message);
            receivedEvents.Add(evt!);

            _channel.BasicAck(ea.DeliveryTag, multiple: false);

            if (receivedEvents.Count == messageCount)
            {
                messagesReceived.TrySetResult(true);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        // Act - Publish 5 payments in sequence
        for (int i = 0; i < messageCount; i++)
        {
            var testEvent = new PaymentCompletedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                PaymentId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                UserEmail = $"user{i}@example.com",
                UserName = $"User {i}",
                Amount = 50.00m + (i * 10.00m),
                Currency = "USD",
                StripePaymentIntentId = $"pi_{i:D16}",
                Description = $"Payment {i}",
                SubscriptionPlan = i % 2 == 0 ? "Basic" : "Premium",
                PaidAt = DateTime.UtcNow.AddSeconds(i)
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
            Assert.Equal($"user{i}@example.com", receivedEvents[i].UserEmail);
            Assert.Equal($"User {i}", receivedEvents[i].UserName);
            Assert.Equal(50.00m + (i * 10.00m), receivedEvents[i].Amount);
            Assert.Equal($"Payment {i}", receivedEvents[i].Description);
            Assert.Equal(i % 2 == 0 ? "Basic" : "Premium", receivedEvents[i].SubscriptionPlan);
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
                var evt = JsonSerializer.Deserialize<PaymentCompletedEvent>(message);
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (JsonException)
            {
                errorOccurred = true;
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
