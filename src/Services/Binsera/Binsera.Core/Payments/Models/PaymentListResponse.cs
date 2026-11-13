using Binsera.Core.Payments.Enums;

namespace Binsera.Core.Payments.Models
{
    public sealed class PaymentListResponse
    {
        public long Id { get; set; }
        public string PaymentNo { get; set; }
        public ReferenceType ReferenceType { get; set; }
        public long ReferenceId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMode PaymentMode { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime PaidOn { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
