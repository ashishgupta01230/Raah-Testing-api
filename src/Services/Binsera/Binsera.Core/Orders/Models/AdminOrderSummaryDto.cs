namespace Binsera.Core.Orders.Models
{
    public sealed class AdminOrderSummaryDto
    {
        public int TotalOrders { get; set; }
        public int PendingPaymentOrders { get; set; }
        public int PaymentReceivedOrders { get; set; }
        public int ApprovedOrders { get; set; }
        public int RejectedOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int TodayOrders { get; set; }
        public int ThisMonthOrders { get; set; }
        public decimal TotalOrderAmount { get; set; }
        public decimal PendingPaymentAmount { get; set; }
        public decimal ReceivedPaymentAmount { get; set; }
    }
}
