using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Members.Domain
{
    public class Document : AuditableDomainBase
    {
       

        
        public string Name { get; private set; } = null!;
        public string? Description { get; private set; }
        public bool IsActive { get; private set; }

        private Document() { }

        private Document(string name, string? description)
        {
            Name = name;
            Description = description;
            IsActive = true;
        }


        public static Document CreateDomain(string name, string? description)
        {
            return new Document(name, description);
        }
    }
}
