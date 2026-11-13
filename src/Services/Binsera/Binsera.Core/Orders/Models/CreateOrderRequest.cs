using Binsera.Core.Orders.Enums;

namespace Binsera.Core.Orders.Models
{
    public sealed class CreateOrderRequest
    {
        public OrderType OrderType { get; set; }
        public PaymentMode PaymentMode { get; set; }
        public string Remarks { get; set; }
        public List<CreateOrderItemRequest> Items { get; set; } = new List<CreateOrderItemRequest>();
        public OfflinePaymentInfo OfflinePayment { get; set; }
    }

    public sealed class CreateOrderItemRequest
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class OfflinePaymentInfo
    {
        public PaymentMethod PaymentMethod { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public long MediaFileId { get; set; }
        public string Remarks { get; set; }
    }
}
