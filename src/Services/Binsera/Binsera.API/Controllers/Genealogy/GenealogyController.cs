using Binsera.Application.Genealogy;
using Binsera.Core.Auth.Domain;
using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Genealogy
{
    [Route("genealogy")]
    [AllowAnonymous]
    public class GenealogyController(IGenealogyService genealogyService) : ApiControllerBase
    {
        [HttpGet("node/{memberId?}")]
        public async Task<IActionResult> GetNode(string? memberId)
        {
            var isAdmin = IsAdmin;
            var currentMemberId = MemberGraphId ?? Guid.Empty;

            Guid targetId;
            if (string.IsNullOrEmpty(memberId))
            {
                targetId = currentMemberId;
            }
            else if (Guid.TryParse(memberId, out var parsedGuid))
            {
                targetId = parsedGuid;
            }
            else if (isAdmin)
            {
                // Legacy admin caller still sending a plain numeric id (e.g. the old
                // "1" default) instead of the graph node's GUID -- fall back to the
                // real company root rather than failing the request.
                var resolved = await genealogyService.GetCompanyRootIdAsync();
                if (resolved is null)
                    throw new KeyNotFoundException("Company root member not found.");

                targetId = resolved.Value;
            }
            else
            {
                // Same legacy case for a member caller -- default to their own node.
                targetId = currentMemberId;
            }

            var result = await genealogyService.GetNodeAsync(targetId, currentMemberId, isAdmin);
            return Ok(result);
        }

        [HttpGet("expand/{memberId}")]
        public async Task<IActionResult> ExpandNode(Guid memberId)
        {
            var result = await genealogyService.ExpandNodeAsync(memberId, MemberGraphId ?? Guid.Empty, IsAdmin);
            return Ok(result);
        }

        [HttpGet("members/search")]
        public async Task<IActionResult> SearchMembers(
            [FromQuery] string q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var currentMemberId = MemberGraphId ?? Guid.Empty;
            var result = await genealogyService.SearchMembersAsync(currentMemberId, IsAdmin, q, page, pageSize);
            return Ok(result);
        }

        [HttpGet("path/{memberId}")]
        public async Task<IActionResult> GetMemberPath(Guid memberId)
        {
            var result = await genealogyService.GetMemberPathAsync(memberId, MemberGraphId ?? Guid.Empty, IsAdmin);
            return Ok(result);
        }

        [HttpGet("children/{memberId}")]
        public async Task<IActionResult> GetChildrenPaged(
            Guid memberId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await genealogyService.GetChildrenPagedAsync(memberId, MemberGraphId ?? Guid.Empty, IsAdmin, page, pageSize);
            return Ok(result);
        }

        private bool IsAdmin => RoleId == (int)RoleType.Admin || RoleId == (int)RoleType.SubAdmin;
    }
}
