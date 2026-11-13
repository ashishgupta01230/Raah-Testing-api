using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Website.Domain
{
    public class VideoMaster : AuditableDomainBase
    {
        public long VideoCategoryId { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string YouTubeEmbedUrl { get; private set; }
        public bool IsActive { get; private set; }
        public VideoCategory VideoCategory { get; private set; }

        private VideoMaster()
        {
        }

        public VideoMaster(long videoCategoryId, string title, string description, string youTubeEmbedUrl)
        {
            VideoCategoryId = videoCategoryId;
            Title = title;
            Description = description;
            YouTubeEmbedUrl = youTubeEmbedUrl;
            IsActive = true;
        }

        public void Update(long videoCategoryId, string title, string description, string youTubeEmbedUrl, bool isActive)
        {
            VideoCategoryId = videoCategoryId;
            Title = title;
            Description = description;
            YouTubeEmbedUrl = youTubeEmbedUrl;
            IsActive = isActive;
        }
    }
}
