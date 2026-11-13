namespace Binsera.Application.Incomes.Model
{
    /// <summary>
    /// Member-facing headline total -- never expose raw level numbers or Company
    /// Root to a member (see docs/requirement/Level_Income_Display_Approach.md).
    /// The per-event activity feed itself is paged separately via
    /// GetTeamIncomeActivity, so this never grows into an unbounded list.
    /// </summary>
    public class MemberTeamIncomeSummaryModel
    {
        public decimal TotalTeamIncome { get; set; }
    }

    public class TeamIncomeActivityItemModel
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public DateTime OccurredOn { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>Paged "view all" activity list, for when the overview preview isn't enough.</summary>
    public class TeamIncomeActivityPageModel
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public List<TeamIncomeActivityItemModel> Items { get; set; } = new();
    }
}
