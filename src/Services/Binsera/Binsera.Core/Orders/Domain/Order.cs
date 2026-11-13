using Binsera.Core.Auth.Events;
using Binsera.Core.Orders.Enums;
using Binsera.Core.Orders.Events;
using Binsera.Core.Orders.Models;
using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Orders.Domain
{
    public class Order : AuditableDomainBase
    {
        private Order() { }

        private Order(CreateOrderRequest request, long memberId, string orderNo)
        {
            OrderNo = orderNo;
            MemberId = memberId;
            OrderType = request.OrderType;
            PaymentMode = request.PaymentMode;
            Remarks = request.Remarks;
            TotalAmount = request.Items.Sum(x => x.Quantity * x.UnitPrice);

            foreach (var item in request.Items)
            {
                Items.Add(new OrderItem(item));
            }

            if (request.OfflinePayment != null)
            {
                Status = OrderStatus.PaymentReceived;
                StatusHistory.Add(OrderStatusHistory.Create(
                    OrderStatus.Draft, OrderStatus.PendingPayment,
                    "Payment proof uploaded", memberId));

                AddDomainEvent(new OrderCreatedDomainEvent(
                    this,
                    request.PaymentMode,
                    request.OfflinePayment.PaymentMethod,
                    request.OfflinePayment.ReferenceNumber,
                    request.OfflinePayment.PaymentDate,
                    request.OfflinePayment.MediaFileId,
                    request.OfflinePayment.Remarks));
            }
            else
            {
                Status = OrderStatus.Draft;
            }
        }

        public string OrderNo { get; private set; } = null!;
        public long MemberId { get; private set; }
        public OrderType OrderType { get; private set; }
        public OrderStatus Status { get; private set; }
        public decimal TotalAmount { get; private set; }
        public string Remarks { get; private set; }
        public DateTime? ApprovedOn { get; private set; }
        public long? ApprovedBy { get; private set; }
        public string RejectedReason { get; private set; }
        public PaymentMode? PaymentMode { get; private set; }

        public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
        public ICollection<OrderStatusHistory> StatusHistory { get; private set; } = new List<OrderStatusHistory>();

        public static Order Create(CreateOrderRequest request, long memberId, string orderNo)
            => new(request, memberId, orderNo);

        public void SetPaymentReceived(long adminId)
        {
            var previous = Status;
            Status = OrderStatus.PaymentReceived;
            StatusHistory.Add(OrderStatusHistory.Create(
                previous, OrderStatus.PaymentReceived, "Payment verified", adminId));
            AddDomainEvent(new OrderPaymentReceivedDomainEvent(this, adminId));
        }

        public void Approve(long adminId)
        {
            var previous = Status;
            Status = OrderStatus.Approved;
            ApprovedOn = DateTime.UtcNow;
            ApprovedBy = adminId;
            StatusHistory.Add(OrderStatusHistory.Create(
                previous, OrderStatus.Approved, "Approved by admin", adminId));

            var firstItem = Items.FirstOrDefault();
            AddDomainEvent(new OrderApprovedDomainEvent(
                Id, OrderNo, MemberId, firstItem?.ItemId, firstItem?.UnitPrice));
        }

        public void Reject(long adminId, string reason)
        {
            var previous = Status;
            Status = OrderStatus.Rejected;
            RejectedReason = reason;
            StatusHistory.Add(OrderStatusHistory.Create(
                previous, OrderStatus.Rejected, reason, adminId));
        }

        public void Complete()
        {
            var previous = Status;
            Status = OrderStatus.Completed;
            StatusHistory.Add(OrderStatusHistory.Create(
                previous, OrderStatus.Completed, "Order completed", 0));
        }

        public void Cancel(long memberId)
        {
            if (Status != OrderStatus.Draft && Status != OrderStatus.PendingPayment)
                throw new InvalidOperationException("Only draft or pending payment orders can be cancelled.");

            var previous = Status;
            Status = OrderStatus.Cancelled;
            StatusHistory.Add(OrderStatusHistory.Create(
                previous, OrderStatus.Cancelled, "Cancelled by member", memberId));

            AddDomainEvent(new OrderCancelledDomainEvent(this));
        }
    }
}
