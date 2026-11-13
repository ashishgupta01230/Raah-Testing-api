using Binsera.Core.Payments.Enums;

namespace Binsera.Core.Payments.Models
{
    public sealed class AdminPaymentListResponse
    {
        public long Id { get; set; }
        public string PaymentNo { get; set; }
        public long MemberId { get; set; }
        /// <summary>The LoginID. This used to carry the numeric RegNo, which is an internal key.</summary>
        public string RegistrationNumber { get; set; }
        public string MemberName { get; set; }
        public string Mobile { get; set; }
        public ReferenceType ReferenceType { get; set; }
        public long ReferenceId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMode PaymentMode { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public string TransactionReference { get; set; }
        public DateTime PaidOn { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
