using Binsera.Core.Orders.Events;
using Binsera.Core.Payments.Contracts;
using Binsera.Core.Payments.Enums;
using HautSphere.BuildingBlocks.Application;

namespace Binsera.Application.Orders.EventHandlers
{
    public class OrderCancelledDomainEventHandler(IPaymentService paymentService)
        : IDomainEventHandler<OrderCancelledDomainEvent>
    {
        public async Task HandleAsync(OrderCancelledDomainEvent domainEvent, CancellationToken ct = default)
        {
            await paymentService.CancelPaymentByReferenceAsync(
                ReferenceType.ORDER,
                domainEvent.Order.Id,
                "Order cancelled");
        }
    }
}
