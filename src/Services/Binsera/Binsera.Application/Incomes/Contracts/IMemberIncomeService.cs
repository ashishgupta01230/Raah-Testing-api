using Binsera.Application.Incomes.Model;

namespace Binsera.Application.Incomes.Contracts
{
    public interface IMemberIncomeService
    {
        Task<MemberIncomeStatsModel> GetOverviewIncomeAmount(Guid memberId, CancellationToken cancellationToken);

        /// <summary>Admin-only: full Level 1..N breakdown. Never call this for a member-facing view.</summary>
        Task<LevelIncomeOverviewModel[]> GetLevelIncomeOverview(Guid memberId, CancellationToken cancellationToken);

        /// <summary>Member-facing: Level 1 collapsed to "direct referral", Level 2+ collapsed to "team income".</summary>
        Task<MemberTeamIncomeSummaryModel> GetTeamIncomeSummary(Guid memberId, CancellationToken cancellationToken);

        /// <summary>Member-facing, paged: the full activity list behind the overview preview.</summary>
        Task<TeamIncomeActivityPageModel> GetTeamIncomeActivity(Guid memberId, int page, int pageSize, CancellationToken cancellationToken);
    }
}
