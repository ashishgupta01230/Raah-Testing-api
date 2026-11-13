using Binsera.Core.Pool.Contracts;
using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Pool
{
    /// <summary>
    /// Tasks 1-6 of docs/plan/Member_Pool_API_And_UI_Plan.md. Every action resolves the caller's
    /// own RegNo from the JWT -- never a member id from the request -- so this controller can never
    /// be asked to hand back someone else's pool data.
    /// </summary>
    [Route("pool/member")]
    public class PoolMemberController(IPoolMemberQueryService queryService) : ApiControllerBase
    {
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        {
            if (RegNo is not long regNo)
                return StatusCode(403);

            return Ok(await queryService.GetSummaryAsync(regNo, cancellationToken));
        }

        [HttpGet("activity")]
        public async Task<IActionResult> GetActivity(int limit, CancellationToken cancellationToken)
        {
            if (RegNo is not long regNo)
                return StatusCode(403);

            var effectiveLimit = limit <= 0 ? 10 : Math.Min(limit, 50);
            return Ok(await queryService.GetActivityAsync(regNo, effectiveLimit, cancellationToken));
        }

        [HttpGet("tree")]
        public async Task<IActionResult> GetTree(long? rootRegNo, int depth, CancellationToken cancellationToken)
        {
            if (RegNo is not long regNo)
                return StatusCode(403);

            var effectiveDepth = depth <= 0 ? 3 : depth;

            try
            {
                return Ok(await queryService.GetTreeAsync(regNo, rootRegNo ?? regNo, effectiveDepth, cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403);
            }
        }

        [HttpGet("team")]
        public async Task<IActionResult> GetTeam(
            string? search, int? level, int page, int pageSize, CancellationToken cancellationToken)
        {
            if (RegNo is not long regNo)
                return StatusCode(403);

            var effectivePage = page <= 0 ? 1 : page;
            var effectivePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

            return Ok(await queryService.GetTeamAsync(
                regNo, search, level, effectivePage, effectivePageSize, cancellationToken));
        }

        [HttpGet("income")]
        public async Task<IActionResult> GetIncome(CancellationToken cancellationToken)
        {
            if (RegNo is not long regNo)
                return StatusCode(403);

            return Ok(await queryService.GetIncomeAsync(regNo, cancellationToken));
        }

        [HttpGet("wallet-ledger")]
        public async Task<IActionResult> GetWalletLedger(
            string wallet, DateTime? from, DateTime? to, CancellationToken cancellationToken)
        {
            if (RegNo is not long regNo)
                return StatusCode(403);

            var mainWallet = string.Equals(wallet, "main", StringComparison.OrdinalIgnoreCase);
            return Ok(await queryService.GetWalletLedgerAsync(regNo, mainWallet, from, to, cancellationToken));
        }

        [HttpGet("upgrade-history")]
        public async Task<IActionResult> GetUpgradeHistory(CancellationToken cancellationToken)
        {
            if (RegNo is not long regNo)
                return StatusCode(403);

            return Ok(await queryService.GetUpgradeHistoryAsync(regNo, cancellationToken));
        }
    }
}
