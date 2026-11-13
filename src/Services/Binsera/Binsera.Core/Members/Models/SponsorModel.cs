
namespace Binsera.Core.Members.Models
{
    public class SponsorModel
    {
        public long? IntoRegNo { get; set; }
        public string Title { get; set; } = default!;
        public string FName { get; set; } = default!;
        public string LName { get; set; } = default!;
        public string LoginId { get; set; } = default!;
        public string PrimaryContactNumber { get; set; }
        public Guid MemberId { get; set; }
        public string ProfileImage { get; set; }
    }
}
