using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Orders.Events
{
    // Carries only primitive fields, not the Order aggregate itself: this event is serialized
    // into the Outbox table for OutboxPublisher to replay later, and System.Text.Json's
    // ReferenceHandler.Preserve (needed elsewhere for cyclic object graphs) cannot deserialize
    // a reference-type constructor parameter like a full entity -- it throws
    // NotSupportedException ("Reference metadata is not supported when deserializing
    // constructor parameters"). Plain value types/strings sidestep that entirely.
    public record OrderApprovedDomainEvent(
        long OrderId,
        string OrderNo,
        long MemberId,
        long? ItemId,
        decimal? UnitPrice) : DomainEventBase
    {
    }
}
