using Binsera.Core.Orders.Enums;

namespace Binsera.Core.Orders.Models
{
    public sealed class OrderListRequest
    {
        public string Search { get; set; }
        public OrderType? OrderType { get; set; }
        public OrderStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
