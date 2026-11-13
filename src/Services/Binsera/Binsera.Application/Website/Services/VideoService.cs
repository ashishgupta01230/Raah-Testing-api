using Binsera.Application.Website.Contracts;
using Binsera.Core.Website.Domain;
using Binsera.Core.Website.Model;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Website.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class VideoService : IVideoService
    {
        private readonly IRepository<VideoCategory> _categoryRepository;
        private readonly IRepository<VideoMaster> _videoRepository;

        public VideoService(IUnitOfWork unitOfWork)
        {
            _categoryRepository = unitOfWork.Repository<VideoCategory>();
            _videoRepository = unitOfWork.Repository<VideoMaster>();
        }

        //--------- Video Category-----------------------------------------

        public async Task<List<VideoCategoryDto>> GetVideoCategoriesAsync(CancellationToken cancellationToken)
        {
            return await _categoryRepository.TableNoTracking
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new VideoCategoryDto
                {
                    Id = x.Id,
                    CategoryName = x.CategoryName,
                    IsActive = x.IsActive,
                    CreatedDate = x.CreatedDate
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<VideoCategoryDto?> GetVideoCategoryByIdAsync(long id, CancellationToken cancellationToken)
        {
            return await _categoryRepository.TableNoTracking
                .Where(x => x.Id == id)
                .Select(x => new VideoCategoryDto
                {
                    Id = x.Id,
                    CategoryName = x.CategoryName,
                    IsActive = x.IsActive,
                    CreatedDate = x.CreatedDate
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<VideoCategoryDto> CreateVideoCategoryAsync(CreateVideoCategoryRequest request, CancellationToken cancellationToken)
        {
            var category = new VideoCategory(request.CategoryName);

            await _categoryRepository.Save(category);

            return new VideoCategoryDto
            {
                Id = category.Id,
                CategoryName = category.CategoryName,
                IsActive = category.IsActive,
                CreatedDate = category.CreatedDate
            };
        }

        public async Task<VideoCategoryDto?> UpdateVideoCategoryAsync(long id, UpdateVideoCategoryRequest request, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.Table
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (category == null) return null;

            category.Update(request.CategoryName, request.IsActive);

            await _categoryRepository.Save(category);

            return new VideoCategoryDto
            {
                Id = category.Id,
                CategoryName = category.CategoryName,
                IsActive = category.IsActive,
                CreatedDate = category.CreatedDate
            };
        }

        public async Task<bool> DeleteVideoCategoryAsync(long id, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.Table
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (category == null) return false;

            var hasVideos = await _videoRepository.TableNoTracking
                .AnyAsync(x => x.VideoCategoryId == id, cancellationToken);

            if (hasVideos) return false;

            await _categoryRepository.Delete(category);
            return true;
        }

          //---------  Video Master --------------------------------

        public async Task<List<VideoMasterDto>> GetVideoMasterListAsync(long? categoryId, CancellationToken cancellationToken)
        {
            var query = _videoRepository.TableNoTracking
                .Include(x => x.VideoCategory)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(x => x.VideoCategoryId == categoryId.Value);

            return await query
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new VideoMasterDto
                {
                    Id = x.Id,
                    VideoCategoryId = x.VideoCategoryId,
                    CategoryName = x.VideoCategory.CategoryName,
                    Title = x.Title,
                    Description = x.Description,
                    YouTubeEmbedUrl = x.YouTubeEmbedUrl,
                    IsActive = x.IsActive,
                    CreatedDate = x.CreatedDate
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<VideoMasterDto?> GetVideoMasterByIdAsync(long id, CancellationToken cancellationToken)
        {
            return await _videoRepository.TableNoTracking
                .Include(x => x.VideoCategory)
                .Where(x => x.Id == id)
                .Select(x => new VideoMasterDto
                {
                    Id = x.Id,
                    VideoCategoryId = x.VideoCategoryId,
                    CategoryName = x.VideoCategory.CategoryName,
                    Title = x.Title,
                    Description = x.Description,
                    YouTubeEmbedUrl = x.YouTubeEmbedUrl,
                    IsActive = x.IsActive,
                    CreatedDate = x.CreatedDate
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<VideoMasterDto> CreateVideoMasterAsync(CreateVideoMasterRequest request, CancellationToken cancellationToken)
        {
            var video = new VideoMaster(request.VideoCategoryId, request.Title, request.Description, request.YouTubeEmbedUrl);

            await _videoRepository.Save(video);

            var category = await _categoryRepository.TableNoTracking
                .Where(x => x.Id == video.VideoCategoryId)
                .Select(x => x.CategoryName)
                .FirstOrDefaultAsync(cancellationToken);

            return new VideoMasterDto
            {
                Id = video.Id,
                VideoCategoryId = video.VideoCategoryId,
                CategoryName = category ?? string.Empty,
                Title = video.Title,
                Description = video.Description,
                YouTubeEmbedUrl = video.YouTubeEmbedUrl,
                IsActive = video.IsActive,
                CreatedDate = video.CreatedDate
            };
        }

        public async Task<VideoMasterDto?> UpdateVideoMasterAsync(long id, UpdateVideoMasterRequest request, CancellationToken cancellationToken)
        {
            var video = await _videoRepository.Table
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (video == null) return null;

            video.Update(request.VideoCategoryId, request.Title, request.Description, request.YouTubeEmbedUrl, request.IsActive);

            await _videoRepository.Save(video);

            var category = await _categoryRepository.TableNoTracking
                .Where(x => x.Id == video.VideoCategoryId)
                .Select(x => x.CategoryName)
                .FirstOrDefaultAsync(cancellationToken);

            return new VideoMasterDto
            {
                Id = video.Id,
                VideoCategoryId = video.VideoCategoryId,
                CategoryName = category ?? string.Empty,
                Title = video.Title,
                Description = video.Description,
                YouTubeEmbedUrl = video.YouTubeEmbedUrl,
                IsActive = video.IsActive,
                CreatedDate = video.CreatedDate
            };
        }

        public async Task<bool> DeleteVideoMasterAsync(long id, CancellationToken cancellationToken)
        {
            var video = await _videoRepository.Table
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (video == null) return false;

            await _videoRepository.Delete(video);
            return true;
        }

       
    }
}
