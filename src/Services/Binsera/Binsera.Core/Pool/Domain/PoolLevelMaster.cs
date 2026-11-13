using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Pool.Domain
{
    /// <summary>
    /// The Global Auto Pool level configuration -- one row per level, and the only place pool
    /// amounts come from. Nothing in the pool engine may hardcode a rupee value.
    ///
    /// Level 0 is the joining contribution itself: a member "completes" it the moment they join,
    /// sending Rs.15 one step up. Modelling it as a level means the single rule "level N complete
    /// -> transfer N+1 steps up" covers joining too, instead of joining being a special case.
    /// </summary>
    public class PoolLevelMaster : AuditableDomainBase
    {
        private PoolLevelMaster() { }

        /// <summary>0-7. Level 0 is joining; 7 is the final level.</summary>
        public int LevelNo { get; private set; }

        /// <summary>How many contributions complete this level (1, 2, 4, 8, 16, 32, 64, 128).</summary>
        public int RequiredMembers { get; private set; }

        /// <summary>What each contribution to this level is worth. Equals the previous level's TransferAmount.</summary>
        public decimal ContributionPerMember { get; private set; }

        /// <summary>Paid upward when the level completes. Zero at level 7 -- there is nothing after it.</summary>
        public decimal TransferAmount { get; private set; }

        /// <summary>How many steps up the transfer travels. Null at level 7, which transfers nothing.</summary>
        public int? StepsUp { get; private set; }

        public bool IsActive { get; private set; }

        /// <summary>
        /// Derived, not stored. The requirements doc's PART F gives these as columns, and the admin
        /// Level Master screen displays them, but storing them would let an edit to RequiredMembers
        /// or ContributionPerMember leave them stale -- and the cascade pays real money off
        /// MainWalletIncome. Computing them makes PART F's invariant
        /// (IncomingTotal = TransferAmount + MainWalletIncome) impossible to violate.
        /// </summary>
        public decimal IncomingTotal => RequiredMembers * ContributionPerMember;

        public decimal MainWalletIncome => IncomingTotal - TransferAmount;
    }
}
