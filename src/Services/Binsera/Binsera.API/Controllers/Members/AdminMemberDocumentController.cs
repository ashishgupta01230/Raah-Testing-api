using Binsera.Application.Members.Contracts;
using Binsera.Core.Members.Models;
using HautSphere.BuildingBlocks.Configuration.Controller;
using HautSphere.BuildingBlocks.Configuration.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Members
{
    [Route("admin/member-documents")]
    [AdminOnly]
    public class AdminMemberDocumentController(IMemberDocumentService memberDocumentService) : ApiControllerBase
    {
        [HttpGet("{memberGraphId:guid}")]
        public async Task<IActionResult> GetMemberDocuments(Guid memberGraphId)
        {
            var documents = await memberDocumentService.GetMemberDocumentsByGraphIdAsync(memberGraphId);
            return Ok(documents);
        }

        [HttpPost("{id:long}/approve")]
        public async Task<IActionResult> ApproveDocument(long id)
        {
            await memberDocumentService.ApproveDocumentAsync(id, UserId);
            return Ok(new { message = "Document approved successfully." });
        }

        [HttpPost("{id:long}/reject")]
        public async Task<IActionResult> RejectDocument(long id, RejectDocumentRequest request)
        {
            await memberDocumentService.RejectDocumentAsync(id, UserId, request);
            return Ok(new { message = "Document rejected successfully." });
        }
    }
}
