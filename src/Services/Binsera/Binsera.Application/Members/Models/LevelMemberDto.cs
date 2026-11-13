#nullable disable
namespace Binsera.Application.Members.Models
{
    public class LevelMemberDto
    {
        public long RegNo { get; set; }
        public string LoginId { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string ProfileImage { get; set; }
        public DateTime? JoinDate { get; set; }
        public int Status { get; set; }
        public int Level { get; set; }
        public int DirectReferralCount { get; set; }
        public int TotalTeamCount { get; set; }
        public string KitName { get; set; }
        public double? KitPV { get; set; }
    }
}
