using Binsera.Core.AdminDashboard.Contracts;
using Binsera.Core.Auth.Domain;
using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.AdminDashboard
{
    [Route("admin/dashboard")]
    public class AdminDashboardController(IAdminDashboardService dashboardService) : ApiControllerBase
    {
        private bool IsAdmin => RoleId == (int)RoleType.Admin || RoleId == (int)RoleType.SubAdmin;

        [HttpGet("reconciliation")]
        public async Task<IActionResult> GetReconciliation(DateTime? date, DateTime? from, DateTime? to, CancellationToken cancellationToken)
        {
            if (!IsAdmin) return StatusCode(403);

            var effectiveFrom = from ?? date ?? DateTime.Today;
            var effectiveTo = to ?? date ?? DateTime.Today;

            return Ok(await dashboardService.GetReconciliationAsync(effectiveFrom, effectiveTo, cancellationToken));
        }

        [HttpGet("queue")]
        public async Task<IActionResult> GetQueue(CancellationToken cancellationToken)
        {
            if (!IsAdmin) return StatusCode(403);

            return Ok(await dashboardService.GetQueueAsync(cancellationToken));
        }

        [HttpGet("position")]
        public async Task<IActionResult> GetPosition(CancellationToken cancellationToken)
        {
            if (!IsAdmin) return StatusCode(403);

            return Ok(await dashboardService.GetPositionAsync(cancellationToken));
        }

        [HttpGet("joinings")]
        public async Task<IActionResult> GetJoinings(int days, CancellationToken cancellationToken)
        {
            if (!IsAdmin) return StatusCode(403);

            var effectiveDays = days <= 0 ? 14 : days;
            return Ok(await dashboardService.GetJoiningsAsync(effectiveDays, cancellationToken));
        }

        [HttpGet("level-distribution")]
        public async Task<IActionResult> GetLevelDistribution(CancellationToken cancellationToken)
        {
            if (!IsAdmin) return StatusCode(403);

            return Ok(await dashboardService.GetLevelDistributionAsync(cancellationToken));
        }

        [HttpGet("tree-health")]
        public async Task<IActionResult> GetTreeHealth(CancellationToken cancellationToken)
        {
            if (!IsAdmin) return StatusCode(403);

            return Ok(await dashboardService.GetTreeHealthAsync(cancellationToken));
        }

        [HttpGet("stuck-members")]
        public async Task<IActionResult> GetStuckMembers(int minDays, int limit, CancellationToken cancellationToken)
        {
            if (!IsAdmin) return StatusCode(403);

            var effectiveMinDays = minDays <= 0 ? 25 : minDays;
            var effectiveLimit = limit <= 0 ? 10 : limit;
            return Ok(await dashboardService.GetStuckMembersAsync(effectiveMinDays, effectiveLimit, cancellationToken));
        }
    }
}
