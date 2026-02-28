using CarDealer.Contracts.Abstractions;

namespace AlertService.Domain.Interfaces;

/// <summary>
/// Publishes domain events to the message broker (RabbitMQ).
/// Used by AlertService to notify other services (e.g., NotificationService)
/// when a price alert is triggered.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event to the message broker.
    /// The routing key is derived from the event's EventType property.
    /// </summary>
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : EventBase;
}
