#nullable disable
namespace Binsera.Application.Members.Models
{
    public class UserNode
    {
        public string Id { get; set; }
        public long MemberId { get; set; }
        public long RegNo { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }
        public string PhoneNumber { get; set; }
        public string State { get; set; }
        public string PinCode { get; set; }
        public string PlacementSide { get; set; }
        public int Depth { get; set; }
        public string Path { get; set; }
        public DateTime JoiningDate { get; set; }
        public int Status { get; set; }
        public string SponsorId { get; set; }
        public string PlacementParentId { get; set; }
        public int? RankId { get; set; }
        public string ProfileFileId { get; set; }
        public string ClientId { get; set; }
        public string IntroMemberGraphId { get; internal set; }
    }
}
