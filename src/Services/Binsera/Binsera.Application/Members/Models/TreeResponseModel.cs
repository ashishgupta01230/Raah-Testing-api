#nullable disable
namespace Binsera.Application.Members.Models
{
    public class TreeResponseModel
    {
        public TreeMemberDto Root { get; set; }
        public List<TreeMemberDto> Members { get; set; } = [];
        public TreeMetaDto Meta { get; set; }
    }
    public class TreeMetaDto
    {
        public int RootId { get; set; }
        public int Depth { get; set; }
        public int TotalMembers { get; set; }
        public int Returned { get; set; }
        public int MaxDepth { get; internal set; }
    }
    public class UplineResponse
    {
        public List<UplineMemberDto> Path { get; set; } = [];
    }
    public class SubtreeResponse
    {
        public string ParentId { get; set; }
        public List<TreeMemberDto> Members { get; set; }
        public int Depth { get; set; }
        public int Returned { get; set; }
    }
    public class UplineMemberDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string RegistrationNumber { get; set; }
    }
}
