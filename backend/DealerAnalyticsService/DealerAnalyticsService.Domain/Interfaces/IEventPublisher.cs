using DealerAnalyticsService.Domain.Events;

namespace DealerAnalyticsService.Domain.Interfaces;

/// <summary>
/// Interface for publishing domain events to external systems (RabbitMQ, etc.)
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a domain event to the message broker
    /// </summary>
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent;

    /// <summary>
    /// Publishes multiple domain events
    /// </summary>
    Task PublishManyAsync<T>(IEnumerable<T> domainEvents, CancellationToken cancellationToken = default) where T : IDomainEvent;
}
