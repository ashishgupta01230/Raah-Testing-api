using Binsera.Core.Orders.Enums;

namespace Binsera.Core.Orders.Models
{
    public sealed class AdminOrderListDto
    {
        public long Id { get; set; }
        public string OrderNo { get; set; }
        public long MemberId { get; set; }
        public string RegistrationNumber { get; set; }
        public string MemberName { get; set; }
        public long? SponsorId { get; set; }
        public string SponsorName { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Package { get; set; }
        public int? Rank { get; set; }
        public string MemberStatus { get; set; }
        public OrderType OrderType { get; set; }
        public decimal TotalAmount { get; set; }
        public PaymentMode? PaymentMode { get; set; }
        public Binsera.Core.Payments.Enums.PaymentStatus? PaymentStatus { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public int TotalItems { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? PendingSinceDays { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
    }
}
