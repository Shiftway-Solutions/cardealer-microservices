using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CarDealer.Contracts.Events.Auth;
using BillingService.Domain.Interfaces;

namespace BillingService.Infrastructure.Messaging;

/// <summary>
/// Listens for UserRegisteredEvent and links any guest ReportPurchase records
/// (matched by email) to the newly registered user's account.
/// </summary>
public class ReportPurchaseLinkingConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReportPurchaseLinkingConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "billingservice.user.registered.link-reports";
    private const string RoutingKey = "auth.user.registered";

    public ReportPurchaseLinkingConsumer(
        IServiceProvider serviceProvider,
        ILogger<ReportPurchaseLinkingConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitMQEnabled = _configuration.GetValue<bool>("RabbitMQ:Enabled");
        if (!rabbitMQEnabled)
        {
            _logger.LogInformation("RabbitMQ is disabled. ReportPurchaseLinkingConsumer will not start.");
            return;
        }

        try
        {
            InitializeRabbitMQ();

            if (_channel == null)
            {
                _logger.LogWarning("RabbitMQ channel is null. ReportPurchaseLinkingConsumer will not start");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (_, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var userEvent = JsonSerializer.Deserialize<UserRegisteredEvent>(message);

                    if (userEvent != null && !string.IsNullOrWhiteSpace(userEvent.Email))
                    {
                        _logger.LogInformation(
                            "Received UserRegisteredEvent: UserId={UserId}, Email={Email}",
                            userEvent.UserId,
                            userEvent.Email);

                        await LinkGuestPurchasesAsync(userEvent.UserId, userEvent.Email, stoppingToken);
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        _logger.LogWarning("Received UserRegisteredEvent with missing data, skipping");
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing UserRegisteredEvent for report linking");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("ReportPurchaseLinkingConsumer started on queue: {Queue}", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in ReportPurchaseLinkingConsumer");
        }
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"] ?? throw new InvalidOperationException("RabbitMQ:Username is not configured"),
                Password = _configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured"),
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
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

            _logger.LogInformation("RabbitMQ initialized for ReportPurchaseLinkingConsumer");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for ReportPurchaseLinkingConsumer");
            throw;
        }
    }

    private async Task LinkGuestPurchasesAsync(Guid userId, string email, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IReportPurchaseRepository>();

        try
        {
            var guestPurchases = await repo.GetByEmailAsync(email.ToLowerInvariant(), cancellationToken);
            var unlinked = guestPurchases.Where(p => p.UserId == null).ToList();

            if (unlinked.Count == 0)
            {
                _logger.LogDebug("No unlinked report purchases found for email {Email}", email);
                return;
            }

            foreach (var purchase in unlinked)
            {
                purchase.LinkToUser(userId);
                await repo.UpdateAsync(purchase, cancellationToken);
            }

            _logger.LogInformation(
                "Linked {Count} guest report purchases to UserId={UserId} (email={Email})",
                unlinked.Count,
                userId,
                email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to link guest purchases for UserId={UserId}, Email={Email}", userId, email);
            throw;
        }
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ for ReportPurchaseLinkingConsumer");
        }

        base.Dispose();
    }
}
