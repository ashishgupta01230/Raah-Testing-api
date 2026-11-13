namespace Binsera.Core.Website.Model
{
    public class VideoCategoryDto
    {
        public long Id { get; set; }
        public string CategoryName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateVideoCategoryRequest
    {
        public string CategoryName { get; set; }
    }

    public class UpdateVideoCategoryRequest
    {
        public string CategoryName { get; set; }
        public bool IsActive { get; set; }
    }
}
