using CarDealer.Contracts.Events.KYC;

namespace KYCService.Domain.Interfaces;

/// <summary>
/// Publica eventos de dominio KYC al message broker (RabbitMQ).
/// Desacopla completamente KYCService de los consumidores (e.g. NotificationService).
/// </summary>
public interface IKYCEventPublisher
{
    /// <summary>
    /// Publica un evento de cambio de estado de perfil KYC.
    /// Exchange: cardealer.events | routing key: kyc.profile.status_changed
    /// </summary>
    Task PublishStatusChangedAsync(
        KYCProfileStatusChangedEvent @event,
        CancellationToken cancellationToken = default);
}
