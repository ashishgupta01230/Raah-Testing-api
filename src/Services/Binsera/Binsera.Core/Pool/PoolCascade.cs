namespace Binsera.Core.Pool
{
    /// <summary>One level's configuration, detached from EF so the cascade can be run without a database.</summary>
    public readonly record struct PoolLevelSettings(
        int LevelNo,
        int RequiredMembers,
        decimal ContributionPerMember,
        decimal TransferAmount,
        int? StepsUp)
    {
        public decimal IncomingTotal => RequiredMembers * ContributionPerMember;

        public decimal MainWalletIncome => IncomingTotal - TransferAmount;
    }

    /// <summary>How far a member has got: which level they are on, and how much of it is filled.</summary>
    public sealed class PoolMemberState
    {
        public int CurrentLevel { get; set; }

        public int ContributionCount { get; set; }
    }

    public enum PoolEffectKind
    {
        /// <summary>A contribution arrived and is sitting in the recipient's Upgrade Wallet.</summary>
        UpgradeWalletCredit,

        /// <summary>A level completed; the Upgrade Wallet is drained to fund the transfer and the income.</summary>
        UpgradeWalletDebit,

        /// <summary>A level completed; the remainder is the member's earnings.</summary>
        MainWalletCredit,

        /// <summary>The recipient was a seed account, or there was nothing above X7.</summary>
        CompanyWalletCredit,

        /// <summary>Bookkeeping: this member finished this level and moved to the next.</summary>
        LevelCompleted
    }

    public sealed record PoolEffect(
        PoolEffectKind Kind,
        long? MemberPosition,
        int LevelNo,
        decimal Amount,
        long? SourcePosition,
        int? SeedIndex,
        string IdempotencyKey);

    /// <summary>
    /// The money cascade: one member joining can complete an upline member's level, whose transfer
    /// can complete the next one's, and so on (requirements doc PART I).
    ///
    /// This is deliberately a pure function over an in-memory view of the tree. It reads nothing and
    /// writes nothing -- it returns the list of effects a caller should apply inside one transaction.
    /// That is what makes the doc's PART H scenario testable end to end without a database, and it
    /// keeps the arithmetic that decides who gets paid separate from the plumbing that pays them.
    /// </summary>
    public static class PoolCascade
    {
        /// <summary>
        /// A single join can only complete one level per member, and the chain climbs at most from
        /// the deepest member to above X7. This ceiling exists so a bug can never spin here forever
        /// moving money; it is far above any legitimate chain length.
        /// </summary>
        private const int MaxCascadeSteps = 512;

        /// <summary>
        /// Plans everything that follows from <paramref name="joinerPosition"/> joining the pool.
        ///
        /// <paramref name="stateOf"/> hands back the (mutable) state for a position, loading it on
        /// demand -- the cascade cannot know in advance how far up it will climb.
        /// </summary>
        public static IReadOnlyList<PoolEffect> PlanJoin(
            long joinerPosition,
            IReadOnlyDictionary<int, PoolLevelSettings> levels,
            Func<long, PoolMemberState> stateOf)
        {
            ArgumentNullException.ThrowIfNull(levels);
            ArgumentNullException.ThrowIfNull(stateOf);

            var effects = new List<PoolEffect>();

            // Level 0 is the joining fee itself. The member "completes" it on arrival: one
            // contribution of Rs.15, required count 1. It never touches their own Upgrade Wallet --
            // the money goes straight up -- so the completion below skips the drain for level 0.
            var joiner = stateOf(joinerPosition);
            joiner.ContributionCount = levels[0].RequiredMembers;

            Complete(joinerPosition, effects, levels, stateOf);

            return effects;
        }

        private static void Complete(
            long position,
            List<PoolEffect> effects,
            IReadOnlyDictionary<int, PoolLevelSettings> levels,
            Func<long, PoolMemberState> stateOf)
        {
            var steps = 0;
            long? current = position;

            while (current.HasValue)
            {
                if (++steps > MaxCascadeSteps)
                    throw new InvalidOperationException(
                        $"Pool cascade exceeded {MaxCascadeSteps} steps starting from position {position}; refusing to continue moving money.");

                var completing = current.Value;
                var state = stateOf(completing);
                var level = levels[state.CurrentLevel];

                // The joining level never accumulated anything in the member's own wallet, so
                // there is nothing to drain and no income to take from it.
                if (level.LevelNo > 0)
                {
                    effects.Add(new PoolEffect(
                        PoolEffectKind.UpgradeWalletDebit, completing, level.LevelNo,
                        level.IncomingTotal, null, null, LevelUpgradeKey(completing, level.LevelNo)));

                    if (level.MainWalletIncome > 0)
                        effects.Add(new PoolEffect(
                            PoolEffectKind.MainWalletCredit, completing, level.LevelNo,
                            level.MainWalletIncome, null, null, MainWalletKey(completing, level.LevelNo)));
                }

                effects.Add(new PoolEffect(
                    PoolEffectKind.LevelCompleted, completing, level.LevelNo,
                    0m, null, null, LevelCompletedKey(completing, level.LevelNo)));

                state.CurrentLevel = level.LevelNo + 1;
                state.ContributionCount = 0;

                // Level 7 is final: nothing transfers, so the chain stops here.
                if (level.StepsUp is not int stepsUp || level.TransferAmount <= 0)
                    return;

                var recipient = PoolTreePosition.ResolveStepsUp(completing, stepsUp);

                if (recipient.Kind != PoolRecipientKind.Member)
                {
                    // A seed account, or nothing above X7 -- either way the company collects it and
                    // the chain ends, because seeds have no levels of their own (PART A rule 7).
                    effects.Add(new PoolEffect(
                        PoolEffectKind.CompanyWalletCredit, null, level.LevelNo,
                        level.TransferAmount, completing,
                        recipient.Kind == PoolRecipientKind.SeedAccount ? recipient.SeedIndex : null,
                        TransferKey(completing, level.LevelNo)));

                    return;
                }

                current = Contribute(
                    recipient.MemberPosition, level.TransferAmount, completing, level.LevelNo,
                    effects, levels, stateOf);
            }
        }

        /// <summary>
        /// Credits one contribution to a member's current level. Returns their position if that
        /// completed the level, so the caller continues the chain from there.
        /// </summary>
        private static long? Contribute(
            long recipientPosition,
            decimal amount,
            long sourcePosition,
            int sourceLevelNo,
            List<PoolEffect> effects,
            IReadOnlyDictionary<int, PoolLevelSettings> levels,
            Func<long, PoolMemberState> stateOf)
        {
            var state = stateOf(recipientPosition);
            var level = levels[state.CurrentLevel];

            // By PART F's design the amount arriving always equals what the recipient's current
            // level expects -- level N's transfer is level N+1's contribution. If that ever fails
            // the accounting has drifted, and continuing would credit the wrong counter.
            if (amount != level.ContributionPerMember)
                throw new InvalidOperationException(
                    $"Pool contribution of {amount} from position {sourcePosition} does not match level " +
                    $"{level.LevelNo}'s expected contribution of {level.ContributionPerMember} for position {recipientPosition}.");

            effects.Add(new PoolEffect(
                PoolEffectKind.UpgradeWalletCredit, recipientPosition, level.LevelNo,
                amount, sourcePosition, null, TransferKey(sourcePosition, sourceLevelNo)));

            state.ContributionCount++;

            return state.ContributionCount >= level.RequiredMembers ? recipientPosition : null;
        }

        // Idempotency keys, per PART J rule 1. Each is unique to the event that produced it, so a
        // replayed Outbox message re-derives the same keys and the unique index rejects the writes.
        private static string TransferKey(long position, int levelNo) => $"XFER_{position}_L{levelNo}";

        private static string LevelUpgradeKey(long position, int levelNo) => $"LVLUP_{position}_L{levelNo}";

        private static string MainWalletKey(long position, int levelNo) => $"INCOME_{position}_L{levelNo}";

        private static string LevelCompletedKey(long position, int levelNo) => $"DONE_{position}_L{levelNo}";
    }
}
