using Binsera.Core.Website.Model;

namespace Binsera.Application.Website.Contracts
{
    public interface IVideoService
    {
        // Video Category
        Task<List<VideoCategoryDto>> GetVideoCategoriesAsync(CancellationToken cancellationToken);
        Task<VideoCategoryDto?> GetVideoCategoryByIdAsync(long id, CancellationToken cancellationToken);
        Task<VideoCategoryDto> CreateVideoCategoryAsync(CreateVideoCategoryRequest request, CancellationToken cancellationToken);
        Task<VideoCategoryDto?> UpdateVideoCategoryAsync(long id, UpdateVideoCategoryRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteVideoCategoryAsync(long id, CancellationToken cancellationToken);

        // Video Master
        Task<List<VideoMasterDto>> GetVideoMasterListAsync(long? categoryId, CancellationToken cancellationToken);
        Task<VideoMasterDto?> GetVideoMasterByIdAsync(long id, CancellationToken cancellationToken);
        Task<VideoMasterDto> CreateVideoMasterAsync(CreateVideoMasterRequest request, CancellationToken cancellationToken);
        Task<VideoMasterDto?> UpdateVideoMasterAsync(long id, UpdateVideoMasterRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteVideoMasterAsync(long id, CancellationToken cancellationToken);
    }
}
