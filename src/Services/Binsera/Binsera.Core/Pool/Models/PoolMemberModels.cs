namespace Binsera.Core.Pool.Models
{
    /// <summary>
    /// Backs Task D1 (docs/plan/Member_Dashboard_Rebuild_Plan.md) -- the Overview tab's hero,
    /// shaped to match docs/requirements/Member_Dashboard_API_Spec.md exactly.
    /// </summary>
    public sealed class MemberSummaryResponse
    {
        public MemberInfo Member { get; set; } = new();
        public int CurrentLevel { get; set; }

        /// <summary>Always 7 entries, Level 1-7, even when the member is still on Level 1.</summary>
        public List<LadderRung> Ladder { get; set; } = new();

        public LevelProgress CurrentLevelProgress { get; set; } = new();
        public MainWalletSummary MainWallet { get; set; } = new();

        /// <summary>Null at Level 7 -- there is no further level to be collecting into.</summary>
        public LevelPoolSummary? LevelPool { get; set; }

        public TeamStats Team { get; set; } = new();

        /// <summary>
        /// What moved since the member last looked. Everything else on the Overview is a standing
        /// figure -- balance, level, team size -- which reads the same on every visit.
        /// </summary>
        public WeeklyMovement ThisWeek { get; set; } = new();
    }

    public sealed class WeeklyMovement
    {
        public int NewMembers { get; set; }
        public decimal Earned { get; set; }
    }

    public sealed class MemberInfo
    {
        public long RegNo { get; set; }
        /// <summary>The member's LoginID -- the only identifier the UI is allowed to show.</summary>
        public string? RegistrationNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? JoinedOn { get; set; }
        public string Status { get; set; } = string.Empty; // ACTIVE | PENDING
    }

    public sealed class LadderRung
    {
        public int Level { get; set; }
        public string State { get; set; } = string.Empty; // DONE | CURRENT | LOCKED
        public decimal? IncomeEarned { get; set; }
        public decimal? IncomeOnComplete { get; set; }
        public DateTime? CompletedOn { get; set; }
    }

    public sealed class LevelProgress
    {
        public int Level { get; set; }
        public decimal CollectedAmount { get; set; }
        public decimal RequiredAmount { get; set; }
        public int QualifiedMembers { get; set; }
        public int RequiredMembers { get; set; }

        /// <summary>Computed here, never on the client -- rounding must be consistent for every viewer.</summary>
        public decimal FillPercent { get; set; }

        public decimal IncomeOnComplete { get; set; }
        public int? NextLevel { get; set; }
    }

    public sealed class MainWalletSummary
    {
        public decimal Balance { get; set; }
        public bool CanWithdraw { get; set; } = true;
        public List<LevelBreakupRow> LevelBreakup { get; set; } = new();
    }

    public sealed class LevelBreakupRow
    {
        public int Level { get; set; }
        public decimal Amount { get; set; }
    }

    public sealed class LevelPoolSummary
    {
        public int Level { get; set; }
        public decimal Balance { get; set; }

        /// <summary>Always false. Hardcoded, not computed -- this must never depend on any input.</summary>
        public bool Withdrawable { get; set; }

        public List<PoolContribution> Contributions { get; set; } = new();
    }

    public sealed class PoolContribution
    {
        public string MemberCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ReceivedOn { get; set; }
    }

    public sealed class TeamStats
    {
        public int Direct { get; set; }
        public int Total { get; set; }
        public int Left { get; set; }
        public int Right { get; set; }
    }

    /// <summary>Backs Task D2 -- the Overview tab's Recent section.</summary>
    public sealed class ActivityItem
    {
        public string Type { get; set; } = string.Empty; // POOL_CREDIT | LEVEL_COMPLETE | WITHDRAWAL_*
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public int? Level { get; set; }
        public string Display { get; set; } = string.Empty;
        public bool AffectsMainWallet { get; set; }
    }

    /// <summary>Backs Task 2 -- one node in the tree diagram.</summary>
    public sealed class TreeNodeResponse
    {
        public long RegNo { get; set; }
        /// <summary>The member's LoginID -- the only identifier the UI is allowed to show.</summary>
        public string? RegistrationNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Position { get; set; }
        public long? ParentPosition { get; set; }
        public int CurrentLevel { get; set; }
        public int Depth { get; set; }
        public bool HasChildren { get; set; }
    }

    /// <summary>Backs Task 3 -- My Team's flat list.</summary>
    public sealed class TeamListResponse
    {
        public int DirectCount { get; set; }
        public int TotalTeamCount { get; set; }
        public int LeftLegCount { get; set; }
        public int RightLegCount { get; set; }
        public int TotalItems { get; set; }
        public List<TeamMemberRow> Items { get; set; } = new();
    }

    public sealed class TeamMemberRow
    {
        public long RegNo { get; set; }
        /// <summary>Media id from member_Master, or blank -- the list falls back to initials.</summary>
        public string? ProfileImage { get; set; }
        /// <summary>The member's LoginID -- the only identifier the UI is allowed to show.</summary>
        public string? RegistrationNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CurrentLevel { get; set; }
        public int Depth { get; set; }
        public DateTime? JoinedOn { get; set; }
        public string Side { get; set; } = string.Empty; // Left | Right
    }

    /// <summary>Backs Task 4 -- the Income tab.</summary>
    /// <summary>
    /// The level-by-level earnings report. Deliberately carries no transaction list: those rows are
    /// the Main Wallet ledger, served by the wallet-ledger endpoint and shown on the Wallet screen.
    /// Returning them here too is what had the Income and Wallet screens showing the same rows
    /// under different headings.
    /// </summary>
    public sealed class IncomeStatementResponse
    {
        public decimal TotalEarned { get; set; }

        /// <summary>What the member's current level will pay when it completes.</summary>
        public decimal PendingFromCurrentLevel { get; set; }

        public int CurrentLevel { get; set; }
        public List<LevelIncomeRow> LevelSummary { get; set; } = new();
    }

    /// <summary>
    /// One level's income line. <see cref="Earned"/> and <see cref="PayableOnComplete"/> are kept
    /// apart deliberately: a single "income" number that means "received" on a finished level and
    /// "will receive" on an unfinished one cannot be totalled, and read as though the member had
    /// already been paid for levels they have not reached.
    /// </summary>
    public sealed class LevelIncomeRow
    {
        public int LevelNo { get; set; }

        /// <summary>DONE / CURRENT / LOCKED -- same vocabulary as the dashboard ladder.</summary>
        public string State { get; set; } = string.Empty;

        /// <summary>Actually paid into the Main Wallet. Null until the level completes.</summary>
        public decimal? Earned { get; set; }

        /// <summary>What completing this level pays. Null once it has completed.</summary>
        public decimal? PayableOnComplete { get; set; }

        public DateTime? CompletedOn { get; set; }
        public int ReceivedMembers { get; set; }
        public int RequiredMembers { get; set; }

        /// <summary>Collected into this level's pool so far.</summary>
        public decimal CollectedAmount { get; set; }

        /// <summary>What the level needs in total before it completes.</summary>
        public decimal RequiredAmount { get; set; }

        /// <summary>Sent upward when the level completes -- the rest is what the member keeps.</summary>
        public decimal TransferAmount { get; set; }

        /// <summary>How far up that transfer travels. The recipient's identity is never exposed.</summary>
        public int StepsUp { get; set; }
    }

    /// <summary>Backs Task 5 -- one row of a wallet ledger (Main or Upgrade), running balance included.</summary>
    public sealed class WalletLedgerEntry
    {
        public DateTime OccurredOn { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Credit { get; set; }
        public decimal Debit { get; set; }
        public decimal Balance { get; set; }
        public int LevelNo { get; set; }
    }

    /// <summary>Backs Task 6 -- one completed level's upward transfer, recipient identity never included.</summary>
    public sealed class UpgradeHistoryEntry
    {
        public int LevelNo { get; set; }
        public int NextLevelNo { get; set; }
        public decimal AmountTransferred { get; set; }
        public int StepsUp { get; set; }
        public decimal MainWalletIncome { get; set; }
        public DateTime? CompletedOn { get; set; }
    }
}
