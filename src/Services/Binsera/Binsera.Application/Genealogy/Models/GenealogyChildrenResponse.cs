#nullable disable
namespace Binsera.Application.Genealogy.Models
{
    public sealed class GenealogyChildrenResponse
    {
        public List<GenealogyNodeResponse> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
