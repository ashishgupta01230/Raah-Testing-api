using Binsera.Core.Payments.Enums;

namespace Binsera.Core.Payments.Models
{
    public sealed class PaymentListRequest
    {
        public string Search { get; set; }
        public ReferenceType? ReferenceType { get; set; }
        public PaymentStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
