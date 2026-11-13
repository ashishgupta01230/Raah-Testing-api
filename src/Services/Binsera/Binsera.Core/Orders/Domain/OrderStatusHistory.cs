using Binsera.Core.Orders.Enums;
using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Orders.Domain
{
    public class OrderStatusHistory : DomainBase
    {
        private OrderStatusHistory() { }

        internal OrderStatusHistory(OrderStatus fromStatus, OrderStatus toStatus,
            string remarks, long changedBy)
        {
            FromStatus = fromStatus;
            ToStatus = toStatus;
            Remarks = remarks;
            ChangedOn = DateTime.UtcNow;
            ChangedBy = changedBy;
        }

        public long OrderId { get; private set; }
        public OrderStatus FromStatus { get; private set; }
        public OrderStatus ToStatus { get; private set; }
        public string Remarks { get; private set; }
        public DateTime ChangedOn { get; private set; }
        public long ChangedBy { get; private set; }

        public Order Order { get; private set; }

        public static OrderStatusHistory Create(OrderStatus fromStatus,
            OrderStatus toStatus, string remarks, long changedBy)
            => new(fromStatus, toStatus, remarks, changedBy);
    }
}
