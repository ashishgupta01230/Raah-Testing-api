#nullable disable
namespace Binsera.Application.Members.Models
{
    public class LevelSummaryDto
    {
        public int Level { get; set; }
        public int MemberCount { get; set; }
        public int ActiveMembers { get; set; }
        public int InactiveMembers { get; set; }
        public decimal TotalBusiness { get; set; }
        public decimal TotalIncome { get; set; }
    }
}
