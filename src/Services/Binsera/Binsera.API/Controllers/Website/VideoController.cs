using Binsera.Application.Website.Contracts;
using Binsera.Core.Website.Model;
using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Website
{
    [Route("website/videos")]
    public class VideoController(IVideoService videoService) : ApiControllerBase
    {
        //------------ Video Category -----------------------------

        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        {
            var result = await videoService.GetVideoCategoriesAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet("categories/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryById(long id, CancellationToken cancellationToken)
        {
            var result = await videoService.GetVideoCategoryByIdAsync(id, cancellationToken);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory(CreateVideoCategoryRequest request, CancellationToken cancellationToken)
        {
            var result = await videoService.CreateVideoCategoryAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(long id, UpdateVideoCategoryRequest request, CancellationToken cancellationToken)
        {
            var result = await videoService.UpdateVideoCategoryAsync(id, request, cancellationToken);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(long id, CancellationToken cancellationToken)
        {
            var result = await videoService.DeleteVideoCategoryAsync(id, cancellationToken);
            if (!result) return BadRequest("Category not found or has associated videos.");
            return Ok();
        }

        

       //-------------------------- Video Master-------------------------------

        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVideos([FromQuery] long? categoryId, CancellationToken cancellationToken)
        {
            var result = await videoService.GetVideoMasterListAsync(categoryId, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVideoById(long id, CancellationToken cancellationToken)
        {
            var result = await videoService.GetVideoMasterByIdAsync(id, cancellationToken);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateVideo(CreateVideoMasterRequest request, CancellationToken cancellationToken)
        {
            var result = await videoService.CreateVideoMasterAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVideo(long id, UpdateVideoMasterRequest request, CancellationToken cancellationToken)
        {
            var result = await videoService.UpdateVideoMasterAsync(id, request, cancellationToken);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVideo(long id, CancellationToken cancellationToken)
        {
            var result = await videoService.DeleteVideoMasterAsync(id, cancellationToken);
            if (!result) return NotFound();
            return Ok();
        }

       
    }
}
