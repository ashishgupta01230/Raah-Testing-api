#nullable disable
namespace Binsera.Application.Members.Models
{
    public class TeamHierarchyNodeDto
    {
        public long RegNo { get; set; }
        public string LoginId { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string ProfileImage { get; set; }
        public string SponsorName { get; set; }
        public string SponsorLoginId { get; set; }
        public DateTime? JoinDate { get; set; }
        public bool HasChildren { get; set; }
        public int ChildrenCount { get; set; }
        public int DirectReferralCount { get; set; }
        public int TotalTeamCount { get; set; }
        public int Status { get; set; }
        public int Level { get; set; }
        public string RelationshipType { get; set; }
    }
}
