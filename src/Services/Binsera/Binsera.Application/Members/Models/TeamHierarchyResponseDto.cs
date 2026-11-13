#nullable disable
namespace Binsera.Application.Members.Models
{
    public class TeamHierarchyResponseDto
    {
        public TeamHierarchyNodeDto CurrentMember { get; set; }
        public IReadOnlyList<TeamHierarchyNodeDto> Children { get; set; } = [];
    }
}
