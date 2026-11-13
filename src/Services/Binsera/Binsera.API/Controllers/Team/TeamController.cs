using Binsera.Application.Members.Models;
using Binsera.Application.Members.Team;
using Binsera.Core.Auth.Domain;
using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Team
{
    [Route("team")]
    public class TeamController(ITeamService teamService) : ApiControllerBase
    {
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] Guid? memberId, CancellationToken cancellationToken)
        {
            var id = ResolveMemberId(memberId);
            if (id == Guid.Empty) return BadRequest(new { message = "memberId is required for non-member roles." });
            var result = await teamService.GetTeamSummaryAsync(id, cancellationToken);
            return Ok(result);
        }

        [HttpGet("direct")]
        public async Task<IActionResult> GetDirectMembers([FromQuery] TeamFilterModel filter, [FromQuery] Guid? memberId, CancellationToken cancellationToken)
        {
            var id = ResolveMemberId(memberId);
            if (id == Guid.Empty) return BadRequest(new { message = "memberId is required for non-member roles." });
            var result = await teamService.GetDirectMembersAsync(id, filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("indirect")]
        public async Task<IActionResult> GetIndirectMembers([FromQuery] TeamFilterModel filter, [FromQuery] Guid? memberId, CancellationToken cancellationToken)
        {
            var id = ResolveMemberId(memberId);
            if (id == Guid.Empty) return BadRequest(new { message = "memberId is required for non-member roles." });
            var result = await teamService.GetIndirectMembersAsync(id, filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllMembers([FromQuery] TeamFilterModel filter, [FromQuery] Guid? memberId, CancellationToken cancellationToken)
        {
            var id = ResolveMemberId(memberId);
            if (id == Guid.Empty) return BadRequest(new { message = "memberId is required for non-member roles." });
            var result = await teamService.GetTotalTeamMembersAsync(id, filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("hierarchy")]
        public async Task<IActionResult> GetHierarchy([FromQuery] Guid? memberId, CancellationToken cancellationToken)
        {
            var id = ResolveMemberId(memberId);
            if (id == Guid.Empty) return BadRequest(new { message = "memberId is required for non-member roles." });
            var result = await teamService.GetHierarchyAsync(id, cancellationToken);
            return Ok(result);
        }

        [HttpGet("hierarchy/{parentRegNo}/children")]
        public async Task<IActionResult> GetChildren(long parentRegNo, CancellationToken cancellationToken)
        {
            var result = await teamService.GetChildrenAsync(parentRegNo, cancellationToken);
            return Ok(result);
        }

        [HttpGet("levels")]
        public async Task<IActionResult> GetLevels([FromQuery] Guid? memberId, CancellationToken cancellationToken)
        {
            var id = ResolveMemberId(memberId);
            if (id == Guid.Empty) return BadRequest(new { message = "memberId is required for non-member roles." });
            var result = await teamService.GetLevelSummariesAsync(id, cancellationToken);
            return Ok(result);
        }

        [HttpGet("levels/{level}")]
        public async Task<IActionResult> GetLevelMembers(int level, [FromQuery] TeamFilterModel filter, [FromQuery] Guid? memberId, CancellationToken cancellationToken)
        {
            var id = ResolveMemberId(memberId);
            if (id == Guid.Empty) return BadRequest(new { message = "memberId is required for non-member roles." });
            var result = await teamService.GetLevelMembersAsync(id, level, filter, cancellationToken);
            return Ok(result);
        }

        private Guid ResolveMemberId(Guid? queryMemberId)
        {
            if (RoleId == (int)RoleType.Member)
                return MemberGraphId ?? Guid.Empty;

            return queryMemberId ?? Guid.Empty;
        }
    }
}
