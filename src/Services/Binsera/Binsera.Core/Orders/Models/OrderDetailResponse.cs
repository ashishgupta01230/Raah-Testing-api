using Binsera.Core.Orders.Enums;

namespace Binsera.Core.Orders.Models
{
    public sealed class OrderDetailResponse
    {
        public long Id { get; set; }
        public string OrderNo { get; set; }
        public OrderType OrderType { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string Remarks { get; set; }
        public PaymentMode? PaymentMode { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public string RejectedReason { get; set; }

        public List<OrderItemResponse> Items { get; set; } = new List<OrderItemResponse>();
        public List<OrderStatusHistoryResponse> StatusHistory { get; set; } = new List<OrderStatusHistoryResponse>();
    }

    public sealed class OrderItemResponse
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    public sealed class OrderStatusHistoryResponse
    {
        public OrderStatus FromStatus { get; set; }
        public OrderStatus ToStatus { get; set; }
        public string Remarks { get; set; }
        public DateTime ChangedOn { get; set; }
    }
}
