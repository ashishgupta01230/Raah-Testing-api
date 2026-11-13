using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Website.Domain
{
    public class VideoCategory : AuditableDomainBase
    {
        public string CategoryName { get; private set; }

        public bool IsActive { get; private set; }

        private VideoCategory()
        {

        }

        public VideoCategory(string categoryName)
        {
            CategoryName = categoryName;
            IsActive = true;
        }

        public void Update(string categoryName, bool isActive)
        {
            CategoryName = categoryName;
            IsActive = isActive;
        }
    }
}
