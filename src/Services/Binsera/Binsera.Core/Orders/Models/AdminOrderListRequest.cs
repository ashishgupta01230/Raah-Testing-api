using Binsera.Core.Orders.Enums;

namespace Binsera.Core.Orders.Models
{
    public sealed class AdminOrderListRequest
    {
        public string Search { get; set; }
        public OrderType? OrderType { get; set; }
        public OrderStatus? OrderStatus { get; set; }
        public Binsera.Core.Payments.Enums.PaymentStatus? PaymentStatus { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string LoginId { get; set; }

        public string SortBy { get; set; } = "CreatedDate";
        public string SortDirection { get; set; } = "Desc";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
