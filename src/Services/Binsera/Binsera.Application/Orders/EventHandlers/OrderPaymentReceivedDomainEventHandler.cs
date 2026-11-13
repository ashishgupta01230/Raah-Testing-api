using Binsera.Core.Orders.Events;
using Binsera.Core.Payments.Contracts;
using Binsera.Core.Payments.Enums;
using HautSphere.BuildingBlocks.Application;

namespace Binsera.Application.Orders.EventHandlers
{
    public class OrderPaymentReceivedDomainEventHandler(IPaymentService paymentService)
        : IDomainEventHandler<OrderPaymentReceivedDomainEvent>
    {
        public async Task HandleAsync(OrderPaymentReceivedDomainEvent domainEvent, CancellationToken ct = default)
        {
            await paymentService.VerifyPaymentByReferenceAsync(
                ReferenceType.ORDER,
                domainEvent.Order.Id,
                domainEvent.AdminId,
                "Payment verified");
        }
    }
}
