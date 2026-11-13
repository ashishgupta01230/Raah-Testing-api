#nullable disable
namespace Binsera.Application.Genealogy.Models
{
    public sealed class GenealogyNodeResponse
    {
        public string Id { get; set; }
        public string RegNo { get; set; }
        public string FullName { get; set; }
        public string ParentId { get; set; }
        public bool HasChildren { get; set; }
        public int ChildrenCount { get; set; }
        public string RegistrationNumber { get; internal set; }
        public string JoiningDate { get; internal set; }
    }
}
