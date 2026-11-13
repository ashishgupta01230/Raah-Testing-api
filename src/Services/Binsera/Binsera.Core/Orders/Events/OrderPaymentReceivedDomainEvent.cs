using Binsera.Core.Orders.Domain;
using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Orders.Events
{
    public record OrderPaymentReceivedDomainEvent(Order Order, long AdminId) : DomainEventBase
    {
    }
}
