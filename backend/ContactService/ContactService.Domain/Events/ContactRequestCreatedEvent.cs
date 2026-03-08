using CarDealer.Contracts.Abstractions;

namespace ContactService.Domain.Events;

/// <summary>
/// Domain event raised when a new contact request is created.
/// Published via RabbitMQ for notification and audit purposes.
/// </summary>
public class ContactRequestCreatedEvent : EventBase
{
    public override string EventType => "contact.contactrequest.created";

    public Guid ContactRequestId { get; init; }
    public Guid BuyerId { get; init; }
    public Guid SellerId { get; init; }
    public Guid? VehicleId { get; init; }
    public string BuyerName { get; init; } = string.Empty;
    public string BuyerEmail { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;

    public ContactRequestCreatedEvent() { }

    public ContactRequestCreatedEvent(
        Guid contactRequestId,
        Guid buyerId,
        Guid sellerId,
        Guid? vehicleId,
        string buyerName,
        string buyerEmail,
        string subject)
    {
        ContactRequestId = contactRequestId;
        BuyerId = buyerId;
        SellerId = sellerId;
        VehicleId = vehicleId;
        BuyerName = buyerName;
        BuyerEmail = buyerEmail;
        Subject = subject;
    }
}
