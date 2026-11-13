using Binsera.Core.Orders.Models;
using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Orders.Domain
{
    public class OrderItem : DomainBase
    {
        private OrderItem() { }

        internal OrderItem(CreateOrderItemRequest request)
        {
            ItemId = request.ItemId;
            ItemName = request.ItemName;
            Quantity = request.Quantity;
            UnitPrice = request.UnitPrice;
            Amount = request.Quantity * request.UnitPrice;
        }

        public long OrderId { get; private set; }
        public long ItemId { get; private set; }
        public string ItemName { get; private set; } = null!;
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal Amount { get; private set; }

        public Order Order { get; private set; } = null!;
    }
}
