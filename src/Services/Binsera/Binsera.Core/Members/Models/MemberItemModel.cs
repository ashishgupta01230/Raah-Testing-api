using HautSphere.BuildingBlocks.Domain.Models;

namespace Binsera.Core.Members.Models
{
    public class MemberItemModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string RegNo { get; set; }
        public DateTime? JoiningDate { get; set; }
        public string MobileNo { get; set; }
        public string Email { get; set; }
        public string ReferralMember { get; set; }
        public string ReferralRegNo { get; set; }
        public long ReferralMemberId { get; set; }
        public int Status { get; set; }
        public string Password { get; set; }
        public bool IsAcountActive { get; set; }
        public Guid MemberId { get; set; }
        public Guid ReferralId { get; set; }
        public string ProfileImage { get; set; }
        public string ReferralProfileImage { get; set; }
    }
    public class MemberFilterModel: PaginationModel
    {
        public string Keyword { get; set; }
        public int? Status { get; set; } 
        public long? SponsorId { get; set; } 
    }
}
