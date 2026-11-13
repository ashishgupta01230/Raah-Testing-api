using Binsera.Core.Payments.Enums;

namespace Binsera.Core.Payments.Models
{
    public sealed class AdminPaymentDetailResponse
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
        public DateTime CreatedDate { get; set; }

        public PaymentMemberInfo Member { get; set; }
        public List<PaymentHistoryResponse> History { get; set; }
        public AdminPaymentAuditInfo Audit { get; set; }
    }

    public sealed class PaymentMemberInfo
    {
        public long Id { get; set; }
        public long? RegNo { get; set; }
        public string MemberName { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
    }

    public sealed class AdminPaymentAuditInfo
    {
        public string CreatedByName { get; set; }
        public string VerifiedByName { get; set; }
    }
}
