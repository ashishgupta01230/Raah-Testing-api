namespace Binsera.Core.AdminDashboard.Models
{
    /// <summary>Backs the reconciliation hero -- the one panel the spec says must never be cached.</summary>
    public sealed class ReconciliationResponse
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public ReconciliationInflow Inflow { get; set; } = new();
        public ReconciliationOutflow Outflow { get; set; } = new();
        public bool IsBalanced { get; set; }
        public decimal Difference { get; set; }

        /// <summary>
        /// Always empty today. PoolWalletTransaction carries no per-row expected-balance field to
        /// check a row against, so there is nothing yet to point at individually -- only the
        /// day-level Difference above says whether something is wrong.
        /// </summary>
        public List<long> MismatchTransactionIds { get; set; } = new();
    }

    public sealed class ReconciliationInflow
    {
        public int NewMembers { get; set; }

        /// <summary>
        /// Unavailable: no table in this codebase records the full registration amount charged per
        /// member (only the pool's own Rs.15-per-member slice is tracked). Returned as 0 rather than
        /// guessed -- see the plan's open questions.
        /// </summary>
        public decimal JoiningCollected { get; set; }

        /// <summary>Unavailable for the same reason as JoiningCollected. Returned as 0.</summary>
        public decimal ProductAndMargin { get; set; }

        /// <summary>NewMembers times the joining level's contribution amount, from PoolLevelMaster.</summary>
        public decimal PoolContribution { get; set; }
    }

    public sealed class ReconciliationOutflow
    {
        public decimal ToMemberMainWallets { get; set; }
        public decimal ToCompanyWallet { get; set; }

        /// <summary>Inflow.PoolContribution minus the two lines above. Zero unless something broke.</summary>
        public decimal Unallocated { get; set; }

        public decimal TotalOut { get; set; }
    }

    public sealed class DashboardQueueResponse
    {
        public int WithdrawalsPending { get; set; }
        public decimal WithdrawalsPendingAmount { get; set; }
        public int KycPending { get; set; }

        /// <summary>Always 0: no bank-details table exists yet (flagged in Backlog.md/Withdrawal_Plan.md).</summary>
        public int BankVerifyPending { get; set; }

        public int FailedJobs { get; set; }
        public int OldestPendingWithdrawalDays { get; set; }
    }

    public sealed class DashboardPositionResponse
    {
        public decimal MemberLiability { get; set; }
        public decimal CompanyWallet { get; set; }
        public decimal TotalWithdrawn { get; set; }
        public decimal StuckInLevelPools { get; set; }
        public DateTime AsOn { get; set; }
    }

    public sealed class JoiningsResponse
    {
        public List<JoiningDayCount> Series { get; set; } = new();
        public int Total { get; set; }
        public decimal DailyAverage { get; set; }

        /// <summary>UP / DOWN / FLAT -- second half of the window vs. the first half.</summary>
        public string Trend { get; set; } = "FLAT";
    }

    public sealed class JoiningDayCount
    {
        public DateOnly Date { get; set; }
        public int Count { get; set; }
    }

    public sealed class DashboardLevelDistributionResponse
    {
        public int TotalMembers { get; set; }
        public List<LevelDistributionRow> Levels { get; set; } = new();
    }

    public sealed class LevelDistributionRow
    {
        public int Level { get; set; }
        public int Count { get; set; }
        public decimal Percent { get; set; }
    }

    public sealed class TreeHealthResponse
    {
        public int TreeDepth { get; set; }
        public TreeHealthLastRow LastRow { get; set; } = new();
        public TreeHealthNextPlacement? NextPlacement { get; set; }
        public int MembersInBottomTwoRows { get; set; }
        public decimal BottomTwoRowsPercent { get; set; }
    }

    public sealed class TreeHealthLastRow
    {
        public int Row { get; set; }
        public long Capacity { get; set; }
        public int Filled { get; set; }
        public decimal FillPercent { get; set; }
    }

    public sealed class TreeHealthNextPlacement
    {
        public string? ParentCode { get; set; }
        public string Side { get; set; } = string.Empty; // LEFT | RIGHT
        public int Row { get; set; }
    }

    public sealed class StuckMembersResponse
    {
        public List<StuckMemberRow> Items { get; set; } = new();
        public int TotalStuck { get; set; }
    }

    public sealed class StuckMemberRow
    {
        public long MemberRegNo { get; set; }
        public string? RegistrationNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CurrentLevel { get; set; }
        public int QualifiedMembers { get; set; }
        public int RequiredMembers { get; set; }
        public int DaysAtLevel { get; set; }
        public decimal TotalEarned { get; set; }

        /// <summary>RISK / WATCH / NORMAL, from StuckMemberRiskThresholds.</summary>
        public string RiskBand { get; set; } = string.Empty;

        public string? Mobile { get; set; }
    }
}
