using Binsera.Core.Orders.Domain;
using Binsera.Core.Orders.Enums;
using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Orders.Events
{
    public record OrderCreatedDomainEvent(
        Order Order,
        PaymentMode PaymentMode,
        PaymentMethod PaymentMethod,
        string PaymentReferenceNo,
        DateTime PaymentDate,
        long MediaFileId,
        string PaymentRemarks) : DomainEventBase
    {
    }
}
