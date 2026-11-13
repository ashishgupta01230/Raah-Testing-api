using Binsera.Core.Orders.Enums;

namespace Binsera.Core.Orders.Models
{
    public sealed class AdminOrderDetailResponse
    {
        public long Id { get; set; }
        public string OrderNo { get; set; }
        public OrderType OrderType { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string Remarks { get; set; }
        public PaymentMode? PaymentMode { get; set; }
        public Binsera.Core.Payments.Enums.PaymentStatus? PaymentStatus { get; set; }
        public int TotalItems { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public string RejectedReason { get; set; }

        public List<OrderItemResponse> Items { get; set; } = new List<OrderItemResponse>();
        public AdminOrderMemberInfo Member { get; set; }
        public AdminOrderSponsorInfo Sponsor { get; set; }
        public List<OrderStatusHistoryResponse> StatusHistory { get; set; } = new List<OrderStatusHistoryResponse>();
        public AdminOrderAuditInfo Audit { get; set; }
    }

    public sealed class AdminOrderMemberInfo
    {
        public long Id { get; set; }
        public long? RegNo { get; set; }
        /// <summary>The LoginID. RegNo is an internal key and must not be shown to a user.</summary>
        public string RegistrationNumber { get; set; }
        public string MemberName { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PinCode { get; set; }
        public string PackageName { get; set; }
        public int? Rank { get; set; }
        public string MemberStatus { get; set; }
    }

    public sealed class AdminOrderSponsorInfo
    {
        public long? Id { get; set; }
        public long? RegNo { get; set; }
        /// <summary>The LoginID. RegNo is an internal key and must not be shown to a user.</summary>
        public string RegistrationNumber { get; set; }
        public string SponsorName { get; set; }
        public string Mobile { get; set; }
    }

    public sealed class AdminOrderAuditInfo
    {
        public string CreatedByName { get; set; }
        public string ApprovedByName { get; set; }
    }
}
