using Binsera.Core.Orders.Events;
using Binsera.Core.Payments.Contracts;
using Binsera.Core.Payments.Enums;
using Binsera.Core.Payments.Models;
using HautSphere.BuildingBlocks.Application;

namespace Binsera.Application.Orders.EventHandlers
{
    public class OrderCreatedDomainEventHandler(IPaymentService paymentService)
        : IDomainEventHandler<OrderCreatedDomainEvent>
    {
        public async Task HandleAsync(OrderCreatedDomainEvent domainEvent, CancellationToken ct = default)
        {
            var request = new CreatePaymentRequest
            {
                ReferenceType = ReferenceType.ORDER,
                ReferenceId = domainEvent.Order.Id,
                Amount = domainEvent.Order.TotalAmount,
                PaymentMode = (Binsera.Core.Payments.Enums.PaymentMode)(int)domainEvent.PaymentMode,
                PaymentMethod = (Binsera.Core.Payments.Enums.PaymentMethod)(int)domainEvent.PaymentMethod,
                TransactionReference = domainEvent.PaymentReferenceNo,
                MediaFileId = domainEvent.MediaFileId,
                Remarks = domainEvent.PaymentRemarks,
                PaidOn = domainEvent.PaymentDate
            };

            await paymentService.CreatePaymentAsync(request, domainEvent.Order.MemberId);
        }
    }
}
