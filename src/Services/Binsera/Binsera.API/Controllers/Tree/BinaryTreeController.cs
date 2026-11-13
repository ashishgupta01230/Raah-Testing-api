using Binsera.Core.Auth.Domain;
using Binsera.Core.Pool.Contracts;
using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Tree
{
    /// <summary>
    /// The binary tree behind the vis-network visualisation (MlmTreeVisComponent).
    ///
    /// Two things changed here. It now reads the Global Auto Pool tree out of SQL rather than the
    /// Neo4j graph, which registration stopped writing to -- the old source returned an empty tree
    /// for every member. And it is no longer anonymous: every action used to be [AllowAnonymous]
    /// while taking the member's regno straight from the URL, so anyone at all could read any
    /// member's downline by guessing a number. A member is now restricted to their own subtree;
    /// only Admin/SubAdmin may root the view at an arbitrary member, which is what the admin tree
    /// pages need.
    /// </summary>
    [Route("binary-tree")]
    public class BinaryTreeController(IPoolMemberQueryService poolQueryService) : ApiControllerBase
    {
        private bool IsAdmin => RoleId == (int)RoleType.Admin || RoleId == (int)RoleType.SubAdmin;

        [HttpGet("{regno:long}")]
        public async Task<IActionResult> GetTree(long regno, int depth, CancellationToken cancellationToken)
        {
            if (RegNo is not long callerRegNo && !IsAdmin)
                return StatusCode(403);

            var effectiveDepth = depth <= 0 ? 3 : depth;

            try
            {
                return Ok(await poolQueryService.GetBinaryTreeAsync(
                    RegNo ?? 0, IsAdmin, regno, effectiveDepth, cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403);
            }
        }

        [HttpGet("subtree")]
        public async Task<IActionResult> GetSubtree(long parentId, int depth, CancellationToken cancellationToken)
        {
            if (RegNo is not long && !IsAdmin)
                return StatusCode(403);

            var effectiveDepth = depth <= 0 ? 3 : depth;

            try
            {
                return Ok(await poolQueryService.GetBinarySubtreeAsync(
                    RegNo ?? 0, IsAdmin, parentId, effectiveDepth, cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403);
            }
        }

        /// <summary>
        /// Breadcrumb path. Pool-backed like the rest -- the Neo4j version returned an empty path
        /// for every member, which is why the breadcrumb never rendered.
        /// </summary>
        [HttpGet("members/{regno:long}/upline")]
        public async Task<IActionResult> GetUpline(long regno, CancellationToken cancellationToken)
        {
            if (RegNo is not long && !IsAdmin)
                return StatusCode(403);

            try
            {
                return Ok(await poolQueryService.GetBinaryUplineAsync(RegNo ?? 0, IsAdmin, regno, cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403);
            }
        }

        /// <summary>The tree's detail side panel. Same access rule as the tree itself.</summary>
        [HttpGet("members/{regno:long}")]
        public async Task<IActionResult> GetMember(long regno, CancellationToken cancellationToken)
        {
            if (RegNo is not long && !IsAdmin)
                return StatusCode(403);

            try
            {
                var member = await poolQueryService.GetBinaryMemberAsync(RegNo ?? 0, IsAdmin, regno, cancellationToken);
                return member is null ? NotFound() : Ok(member);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403);
            }
        }

        /// <summary>
        /// The tree's search box. Results are restricted to the caller's own downline, so search
        /// cannot be used to reach members the tree itself would refuse to open.
        /// </summary>
        [HttpGet("members/search")]
        public async Task<IActionResult> Search(string q, int limit, CancellationToken cancellationToken)
        {
            if (RegNo is not long && !IsAdmin)
                return StatusCode(403);

            var effectiveLimit = limit <= 0 ? 15 : limit;
            return Ok(await poolQueryService.SearchBinaryMembersAsync(
                RegNo ?? 0, IsAdmin, q ?? string.Empty, effectiveLimit, cancellationToken));
        }
    }
}
