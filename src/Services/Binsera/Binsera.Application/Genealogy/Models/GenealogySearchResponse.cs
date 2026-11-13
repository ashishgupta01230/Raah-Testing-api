#nullable disable
namespace Binsera.Application.Genealogy.Models
{
    public sealed class GenealogySearchResponse
    {
        public List<GenealogyNodeResponse> Items { get; set; } = [];
        public int TotalCount { get; set; }
    }
}
