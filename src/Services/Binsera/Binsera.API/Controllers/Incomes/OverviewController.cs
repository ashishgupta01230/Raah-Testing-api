using Binsera.Application.Incomes.Contracts;
using HautSphere.BuildingBlocks.Configuration.Controller;
using HautSphere.BuildingBlocks.Configuration.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Incomes
{
    [Route("incomes")]
    public class OverviewController(IMemberIncomeService memberIncomeService) : ApiControllerBase
    {
        [HttpGet("{memberId}/overview-income-amount")]
        public async Task<IActionResult> GetOverviewIncomeAmount(Guid memberId, CancellationToken cancellationToken)
        {
            var result = await memberIncomeService.GetOverviewIncomeAmount(memberId, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Admin-only: full Level 1..N breakdown. Per
        /// docs/requirement/Level_Income_Display_Approach.md, raw level numbers must
        /// never reach a member (confusing, and reads as a pyramid structure), so
        /// this is gated to admin/sub-admin -- members use team-income-summary instead.
        /// </summary>
        [HttpGet("{memberId}/level-income-overview")]
        [AdminOnly]
        public async Task<IActionResult> GetLevelIncomeOverview(Guid memberId, CancellationToken cancellationToken)
        {
            var result = await memberIncomeService.GetLevelIncomeOverview(memberId, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Member-facing: Level 1 collapsed to "direct referral", Level 2+ collapsed
        /// to "team income" -- no level numbers, no Company Root.
        /// </summary>
        [HttpGet("{memberId}/team-income-summary")]
        public async Task<IActionResult> GetTeamIncomeSummary(Guid memberId, CancellationToken cancellationToken)
        {
            var result = await memberIncomeService.GetTeamIncomeSummary(memberId, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Member-facing, paged: the full activity list behind the overview
        /// preview, for when a member has too many direct/team credits to fit a
        /// short glance card.
        /// </summary>
        [HttpGet("{memberId}/team-income-activity")]
        public async Task<IActionResult> GetTeamIncomeActivity(
            Guid memberId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var result = await memberIncomeService.GetTeamIncomeActivity(memberId, page, pageSize, cancellationToken);
            return Ok(result);
        }
    }
}
