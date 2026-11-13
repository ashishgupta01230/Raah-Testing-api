using Binsera.Core.Payments.Enums;
using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Payments.Domain
{
    public class PaymentHistory : DomainBase
    {
        private PaymentHistory() { }

        internal PaymentHistory(PaymentStatus? fromStatus, PaymentStatus toStatus,
            string remarks, long changedBy)
        {
            FromStatus = fromStatus;
            ToStatus = toStatus;
            Remarks = remarks;
            ChangedOn = DateTime.UtcNow;
            ChangedBy = changedBy;
        }

        public long PaymentId { get; private set; }
        public PaymentStatus? FromStatus { get; private set; }
        public PaymentStatus ToStatus { get; private set; }
        public string Remarks { get; private set; }
        public DateTime ChangedOn { get; private set; }
        public long ChangedBy { get; private set; }

        public Payment Payment { get; private set; }

        public static PaymentHistory Create(PaymentStatus? fromStatus,
            PaymentStatus toStatus, string remarks, long changedBy)
            => new(fromStatus, toStatus, remarks, changedBy);
    }
}
