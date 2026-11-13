using HautSphere.BuildingBlocks.Domain.Models;

namespace Binsera.Application.Members.Models
{
    public class TeamFilterModel : PaginationModel
    {
        public int Page
        {
            get => PageIndex;
            set => PageIndex = value;
        }

        public string? Search { get; set; }

        public int? Status { get; set; }

        public string? SortBy { get; set; }

        public bool Descending { get; set; }
    }
}
