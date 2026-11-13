namespace Binsera.Core.Website.Model
{
    public class VideoMasterDto
    {
        public long Id { get; set; }
        public long VideoCategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string YouTubeEmbedUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateVideoMasterRequest
    {
        public long VideoCategoryId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string YouTubeEmbedUrl { get; set; }
    }

    public class UpdateVideoMasterRequest
    {
        public long VideoCategoryId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string YouTubeEmbedUrl { get; set; }
        public bool IsActive { get; set; }
    }
}
