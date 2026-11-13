#nullable disable
namespace Binsera.Application.Members.Models
{
    public class TreeMemberDto
    {
        public string Id { get; set; }
        public string RegistrationNumber { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public string Package { get; set; }
        public string Image { get; set; }
        public string Status { get; set; }
        public string ParentId { get; set; }
        public string Placement { get; set; }
        public int Level { get; set; }
        public bool IsEmpty { get; set; }
        public bool HasChildren { get; set; }
        public bool LeftChildExists { get; set; }
        public bool RightChildExists { get; set; }
        public int ChildrenCount { get; set; }
    }
}
