using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Pool.Domain
{
    public enum PoolWalletType
    {
        /// <summary>Fuel. Credited by downstream contributions, drained when a level completes. Never withdrawable.</summary>
        Upgrade = 0,

        /// <summary>The member's actual earnings. Mirrored into Member_Account, which withdrawals read.</summary>
        Main = 1,

        /// <summary>Money that reached a seed account or ran off the top of the chain.</summary>
        Company = 2
    }

    /// <summary>
    /// The pool's own ledger: every rupee that moves, and the idempotency key that stops it moving
    /// twice.
    ///
    /// This is a new table rather than the legacy Member_level_JoinDetails, which cannot carry an
    /// idempotency key (it is excluded from EF migrations, so no column can be added) and whose
    /// MLID column is neither unique nor an identity, so EF cannot track more than one row per
    /// value. The unique index on <see cref="IdempotencyKey"/> is the real guard against double
    /// payment -- the in-code check is only the fast path.
    ///
    /// Upgrade Wallet rows live here and nowhere else. They must never reach Member_Account:
    /// WithdrawalService sums every credit and debit for a member, so non-withdrawable fuel sitting
    /// in that table would inflate what the member can withdraw.
    /// </summary>
    public class PoolWalletTransaction : AuditableDomainBase
    {
        private PoolWalletTransaction() { }

        private PoolWalletTransaction(
            long? memberRegNo, PoolWalletType walletType, decimal credit, decimal debit,
            int levelNo, long? sourceRegNo, int? seedIndex, string idempotencyKey, string remark)
        {
            MemberRegNo = memberRegNo;
            WalletType = walletType;
            Credit = credit;
            Debit = debit;
            LevelNo = levelNo;
            SourceRegNo = sourceRegNo;
            SeedIndex = seedIndex;
            IdempotencyKey = idempotencyKey;
            Remark = remark;
            OccurredOn = DateTime.Now;
        }

        /// <summary>Null for company-wallet rows, which belong to no member.</summary>
        public long? MemberRegNo { get; private set; }

        public PoolWalletType WalletType { get; private set; }

        public decimal Credit { get; private set; }

        public decimal Debit { get; private set; }

        /// <summary>The level this movement belongs to. Level 0 is the joining fee.</summary>
        public int LevelNo { get; private set; }

        /// <summary>Who the money came from, where that is a member.</summary>
        public long? SourceRegNo { get; private set; }

        /// <summary>Which seed account collected it, for company rows that reached X1-X7.</summary>
        public int? SeedIndex { get; private set; }

        public string IdempotencyKey { get; private set; }

        public string Remark { get; private set; }

        public DateTime OccurredOn { get; private set; }

        public static PoolWalletTransaction CreditTo(
            long memberRegNo, PoolWalletType walletType, decimal amount, int levelNo,
            long? sourceRegNo, string idempotencyKey, string remark)
            => new(memberRegNo, walletType, amount, 0m, levelNo, sourceRegNo, null, idempotencyKey, remark);

        public static PoolWalletTransaction DebitFrom(
            long memberRegNo, PoolWalletType walletType, decimal amount, int levelNo,
            string idempotencyKey, string remark)
            => new(memberRegNo, walletType, 0m, amount, levelNo, null, null, idempotencyKey, remark);

        public static PoolWalletTransaction ToCompany(
            decimal amount, int levelNo, long sourceRegNo, int? seedIndex, string idempotencyKey, string remark)
            => new(null, PoolWalletType.Company, amount, 0m, levelNo, sourceRegNo, seedIndex, idempotencyKey, remark);
    }
}
