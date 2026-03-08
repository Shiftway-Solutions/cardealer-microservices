using CarDealer.Contracts.Abstractions;

namespace RecoAgent.Domain.Interfaces;

/// <summary>
/// Interface for publishing domain events to a message broker (RabbitMQ).
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event to the message broker.
    /// </summary>
    /// <typeparam name="TEvent">Type of event that implements IEvent</typeparam>
    /// <param name="event">The event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}
