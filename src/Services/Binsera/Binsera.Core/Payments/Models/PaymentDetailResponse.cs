using Binsera.Core.Payments.Enums;

namespace Binsera.Core.Payments.Models
{
    public sealed class PaymentDetailResponse
    {
        public long Id { get; set; }
        public string PaymentNo { get; set; }
        public ReferenceType ReferenceType { get; set; }
        public long ReferenceId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMode PaymentMode { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public string TransactionReference { get; set; }
        public string GatewayTransactionId { get; set; }
        public long? MediaFileId { get; set; }
        public string MediaFileUrl { get; set; }
        public string Remarks { get; set; }
        public DateTime PaidOn { get; set; }
        public DateTime? VerifiedOn { get; set; }
        public string VerifiedByName { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<PaymentHistoryResponse> History { get; set; }
    }

    public sealed class PaymentHistoryResponse
    {
        public PaymentStatus? FromStatus { get; set; }
        public PaymentStatus ToStatus { get; set; }
        public string Remarks { get; set; }
        public DateTime ChangedOn { get; set; }
        public string ChangedByName { get; set; }
    }
}
