using CarDealer.Contracts.Events.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using UserService.Domain.Entities;
using UserService.Infrastructure.Persistence;

namespace UserService.Infrastructure.Services.Messaging;

/// <summary>
/// Consumer for UserRegisteredEvent from AuthService.
/// Automatically creates a User record in UserService when a user registers.
/// </summary>
public class UserRegisteredEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserRegisteredEventConsumer> _logger;
    private readonly RabbitMQSettings _rabbitMQSettings;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "userservice.user.registered";
    private const string RoutingKey = "auth.user.registered";

    public UserRegisteredEventConsumer(
        IOptions<RabbitMQSettings> rabbitMqSettings,
        IServiceProvider serviceProvider,
        ILogger<UserRegisteredEventConsumer> logger)
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

        // Retry loop — handles transient failures at startup (DB overloaded, RabbitMQ not yet ready)
        const int maxRetries = 10;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (stoppingToken.IsCancellationRequested) return;

            InitializeRabbitMQ();

            if (_channel != null)
                break; // Successfully connected

            var delay = TimeSpan.FromSeconds(Math.Min(attempt * 5, 60)); // 5s, 10s, … up to 60s
            _logger.LogWarning(
                "UserRegisteredEventConsumer: RabbitMQ not ready (attempt {Attempt}/{Max}). Retrying in {Delay}s…",
                attempt, maxRetries, delay.TotalSeconds);

            if (attempt == maxRetries)
            {
                _logger.LogError("UserRegisteredEventConsumer: Exhausted {Max} retries. Consumer will NOT start.", maxRetries);
                return;
            }

            await Task.Delay(delay, stoppingToken);
        }

        try
        {
            if (_channel == null)
            {
                _logger.LogWarning("RabbitMQ channel not available, UserRegisteredEventConsumer will not start");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    _logger.LogInformation("Received UserRegisteredEvent: {Message}", message);

                    var @event = JsonSerializer.Deserialize<UserRegisteredEvent>(message, _jsonOptions);

                    if (@event != null)
                    {
                        await ProcessEventAsync(@event, stoppingToken);
                        _channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("Successfully processed UserRegisteredEvent for user {UserId}", @event.UserId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize UserRegisteredEvent");
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing UserRegisteredEvent");
                    _channel.BasicNack(ea.DeliveryTag, false, true); // Requeue
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("UserRegisteredEventConsumer started, listening on queue: {Queue}", QueueName);

            // Keep running until cancellation
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UserRegisteredEventConsumer");
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

            // Declare exchange (topic type for event routing)
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare queue
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Bind queue to exchange with routing key
            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ UserRegisteredEventConsumer initialized - Exchange: {Exchange}, Queue: {Queue}, RoutingKey: {RoutingKey}",
                ExchangeName, QueueName, RoutingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for UserRegisteredEventConsumer");
        }
    }

    private async Task ProcessEventAsync(UserRegisteredEvent @event, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Idempotency: event replays can carry the same email with either the same
        // or a different user id. Acknowledge duplicates instead of requeueing them.
        var normalizedEmail = @event.Email.Trim().ToLowerInvariant();
        var existingUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Id == @event.UserId || u.Email.ToLower() == normalizedEmail,
                cancellationToken);

        if (existingUser != null)
        {
            if (existingUser.Id == @event.UserId)
            {
                _logger.LogInformation("User {UserId} already exists in UserService, skipping", @event.UserId);
            }
            else
            {
                _logger.LogWarning(
                    "Skipping duplicate UserRegisteredEvent for email {Email}. Existing user {ExistingUserId} already owns that email; incoming user id was {IncomingUserId}.",
                    @event.Email,
                    existingUser.Id,
                    @event.UserId);
            }

            return;
        }

        // Use FirstName/LastName if provided, otherwise parse from FullName (backwards compatibility)
        var firstName = !string.IsNullOrWhiteSpace(@event.FirstName)
            ? @event.FirstName
            : ParseFirstName(@event.FullName);
        var lastName = !string.IsNullOrWhiteSpace(@event.LastName)
            ? @event.LastName
            : ParseLastName(@event.FullName);

        // Determine AccountType from event Metadata (set by AdminManagementController for staff)
        var accountType = AccountType.Buyer; // Default for normal registrations
        if (@event.Metadata != null && @event.Metadata.TryGetValue("AccountType", out var accountTypeStr))
        {
            if (Enum.TryParse<AccountType>(accountTypeStr, ignoreCase: true, out var parsedType))
            {
                accountType = parsedType;
                _logger.LogInformation("Setting AccountType to {AccountType} for user {UserId} (from event Metadata)",
                    accountType, @event.UserId);
            }
        }

        // Create user in UserService
        var user = new User
        {
            Id = @event.UserId,
            Email = @event.Email,
            PasswordHash = "SYNCED_FROM_AUTH", // Password is managed by AuthService
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = @event.PhoneNumber ?? string.Empty, // Use phone from event
            IsActive = true,
            EmailConfirmed = false, // Will be updated when user confirms email
            AccountType = accountType, // From Metadata or default Buyer
            CreatedAt = @event.RegisteredAt,
            CreatedBy = null
        };

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            if (await UserAlreadySyncedAsync(dbContext, @event, cancellationToken))
            {
                _logger.LogWarning(
                    ex,
                    "Ignoring duplicate UserRegisteredEvent detected at save time for user {UserId} ({Email}).",
                    @event.UserId,
                    @event.Email);
                return;
            }

            throw;
        }

        _logger.LogInformation("Successfully synced user {UserId} ({Email}) from AuthService to UserService with FirstName={FirstName}, LastName={LastName}",
            @event.UserId, @event.Email, firstName, lastName);
    }

    private static Task<bool> UserAlreadySyncedAsync(
        ApplicationDbContext dbContext,
        UserRegisteredEvent @event,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = @event.Email.Trim().ToLowerInvariant();
        return dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.Id == @event.UserId || u.Email.ToLower() == normalizedEmail,
                cancellationToken);
    }

    /// <summary>
    /// Parse first name from full name string (for backwards compatibility)
    /// </summary>
    private static string ParseFirstName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var parts = fullName.Split(' ', 2);
        return parts.Length > 0 ? parts[0] : fullName;
    }

    /// <summary>
    /// Parse last name from full name string (for backwards compatibility)
    /// </summary>
    private static string ParseLastName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var parts = fullName.Split(' ', 2);
        return parts.Length > 1 ? parts[1] : string.Empty;
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
