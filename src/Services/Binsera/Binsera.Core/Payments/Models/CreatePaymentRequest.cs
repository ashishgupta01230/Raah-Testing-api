using Binsera.Core.Payments.Enums;

namespace Binsera.Core.Payments.Models
{
    public sealed class CreatePaymentRequest
    {
        public ReferenceType ReferenceType { get; set; }
        public long ReferenceId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMode PaymentMode { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string TransactionReference { get; set; }
        public string GatewayTransactionId { get; set; }
        public long? MediaFileId { get; set; }
        public string Remarks { get; set; }
        public DateTime? PaidOn { get; set; }
    }
}
