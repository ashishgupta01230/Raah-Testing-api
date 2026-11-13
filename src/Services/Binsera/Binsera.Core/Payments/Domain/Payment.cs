using Binsera.Core.Payments.Enums;
using Binsera.Core.Payments.Models;
using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Payments.Domain
{
    public class Payment : AuditableDomainBase
    {
        private Payment() { }

        private Payment(CreatePaymentRequest request, long memberId, string paymentNo)
        {
            PaymentNo = paymentNo;
            ReferenceType = request.ReferenceType;
            ReferenceId = request.ReferenceId;
            MemberId = memberId;
            Amount = request.Amount;
            PaymentMode = request.PaymentMode;
            PaymentMethod = request.PaymentMethod;
            TransactionReference = request.TransactionReference;
            GatewayTransactionId = request.GatewayTransactionId;
            MediaFileId = request.MediaFileId;
            Remarks = request.Remarks;
            PaidOn = request.PaidOn ?? DateTime.UtcNow;
            Status = PaymentStatus.Pending;

            History.Add(PaymentHistory.Create(
                null, PaymentStatus.Pending,
                "Payment created", memberId));
        }

        public string PaymentNo { get; private set; } = null!;
        public ReferenceType ReferenceType { get; private set; }
        public long ReferenceId { get; private set; }
        public long MemberId { get; private set; }
        public decimal Amount { get; private set; }
        public PaymentMode PaymentMode { get; private set; }
        public PaymentMethod PaymentMethod { get; private set; }
        public PaymentStatus Status { get; private set; }
        public string TransactionReference { get; private set; }
        public string GatewayTransactionId { get; private set; }
        public long? MediaFileId { get; private set; }
        public string Remarks { get; private set; }
        public DateTime PaidOn { get; private set; }
        public DateTime? VerifiedOn { get; private set; }
        public long? VerifiedBy { get; private set; }

        public ICollection<PaymentHistory> History { get; private set; } = new List<PaymentHistory>();

        public static Payment Create(CreatePaymentRequest request, long memberId, string paymentNo)
            => new(request, memberId, paymentNo);

        public void Verify(long adminId, string remarks)
        {
            var previous = Status;
            Status = PaymentStatus.Verified;
            VerifiedOn = DateTime.UtcNow;
            VerifiedBy = adminId;
            Remarks = remarks ?? Remarks;
            History.Add(PaymentHistory.Create(
                previous, PaymentStatus.Verified, remarks ?? "Payment verified", adminId));
        }

        public void Reject(long adminId, string reason)
        {
            var previous = Status;
            Status = PaymentStatus.Rejected;
            Remarks = reason;
            History.Add(PaymentHistory.Create(
                previous, PaymentStatus.Rejected, reason, adminId));
        }

        public void MarkFailed(string reason)
        {
            var previous = Status;
            Status = PaymentStatus.Failed;
            Remarks = reason;
            History.Add(PaymentHistory.Create(
                previous, PaymentStatus.Failed, reason, 0));
        }

        public void Cancel(long memberId, string reason)
        {
            var previous = Status;
            Status = PaymentStatus.Cancelled;
            Remarks = reason;
            History.Add(PaymentHistory.Create(
                previous, PaymentStatus.Cancelled, reason, memberId));
        }

        public void Refund(long adminId, string reason)
        {
            var previous = Status;
            Status = PaymentStatus.Refunded;
            Remarks = reason;
            History.Add(PaymentHistory.Create(
                previous, PaymentStatus.Refunded, reason, adminId));
        }

        public void UpdateGatewayStatus(string gatewayTransactionId, PaymentStatus status, string remarks)
        {
            var previous = Status;
            Status = status;
            GatewayTransactionId = gatewayTransactionId;
            Remarks = remarks ?? Remarks;
            History.Add(PaymentHistory.Create(
                previous, status, remarks ?? $"Gateway status updated to {status}", 0));
        }
    }
}
