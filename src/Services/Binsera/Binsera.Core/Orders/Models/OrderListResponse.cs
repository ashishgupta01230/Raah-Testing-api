using Binsera.Core.Orders.Enums;

namespace Binsera.Core.Orders.Models
{
    public sealed class OrderListResponse
    {
        public long Id { get; set; }
        public string OrderNo { get; set; } = null!;
        public OrderType OrderType { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
