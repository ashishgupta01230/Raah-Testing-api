using Binsera.Core.Pool;

namespace Binsera.Pool.Tests;

/// <summary>
/// Rules the requirements doc states outright that the other test files only cover incidentally.
/// Each test names the rule and where it comes from.
/// </summary>
public class PoolDocRuleTests
{
    private static readonly IReadOnlyDictionary<int, PoolLevelSettings> Levels =
        new Dictionary<int, PoolLevelSettings>
        {
            [0] = new(0, 1, 15m, 15m, 1),
            [1] = new(1, 2, 15m, 30m, 2),
            [2] = new(2, 4, 30m, 110m, 3),
            [3] = new(3, 8, 110m, 860m, 4),
            [4] = new(4, 16, 860m, 12_760m, 5),
            [5] = new(5, 32, 12_760m, 1_08_320m, 6),
            [6] = new(6, 64, 1_08_320m, 1_00_000m, 7),
            [7] = new(7, 128, 1_00_000m, 0m, null)
        };

    private static List<PoolEffect> RunTree(int members, out Dictionary<long, PoolMemberState> states)
    {
        var captured = new Dictionary<long, PoolMemberState>();
        var effects = new List<PoolEffect>();

        PoolMemberState StateOf(long position)
        {
            if (!captured.TryGetValue(position, out var state))
                captured[position] = state = new PoolMemberState();

            return state;
        }

        for (long position = 1; position <= members; position++)
            effects.AddRange(PoolCascade.PlanJoin(position, Levels, StateOf));

        states = captured;
        return effects;
    }

    /// <summary>
    /// PART A rule 7 and PART J rule 2: the seed accounts are collectors. They have no levels of
    /// their own, so money reaching one becomes company income and the chain stops there.
    /// </summary>
    [Fact]
    public void Money_reaching_a_seed_account_ends_the_chain()
    {
        var effects = RunTree(127, out _);

        foreach (var companyCredit in effects.Where(e => e.Kind == PoolEffectKind.CompanyWalletCredit))
        {
            var index = effects.IndexOf(companyCredit);

            // Nothing after a company credit may be attributed to a seed: seeds never appear as a
            // member position, never complete a level, never hold fuel.
            Assert.All(effects.Skip(index),
                e => Assert.True(e.MemberPosition is null or > 0,
                    "A seed account was treated as a member."));
        }

        // Every company credit either names the seed that collected it, or ran off the top of the
        // chain -- PART J rule 3, which routes that to the company as well.
        Assert.All(
            effects.Where(e => e.Kind == PoolEffectKind.CompanyWalletCredit),
            e => Assert.True(e.SeedIndex is null or (>= 1 and <= PoolTreePosition.SeedAccountCount)));
    }

    /// <summary>
    /// PART G: the Upgrade Wallet is fuel. It is credited by downstream contributions and debited
    /// when the member's own level upgrades -- it never becomes withdrawable earnings, which is why
    /// it is kept out of Member_Account entirely.
    /// </summary>
    [Fact]
    public void Upgrade_wallet_movements_never_turn_into_main_wallet_movements()
    {
        var effects = RunTree(127, out _);

        // Main Wallet is only ever credited as the remainder of a completed level, never as a
        // transfer of Upgrade Wallet fuel.
        foreach (var income in effects.Where(e => e.Kind == PoolEffectKind.MainWalletCredit))
        {
            Assert.Equal(Levels[income.LevelNo].MainWalletIncome, income.Amount);
            Assert.Null(income.SourcePosition);
        }
    }

    /// <summary>
    /// PART F: completing a level drains the Upgrade Wallet exactly -- the incoming total is the
    /// transfer plus the income, so nothing is left over. PART L point 5 says the same thing from
    /// the other direction: "is design me excess bachta hi nahi".
    /// </summary>
    [Fact]
    public void Completing_a_level_drains_exactly_what_that_level_took_in()
    {
        var effects = RunTree(127, out var states);

        var creditedPerMember = effects
            .Where(e => e.Kind == PoolEffectKind.UpgradeWalletCredit)
            .GroupBy(e => e.MemberPosition!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var debitedPerMember = effects
            .Where(e => e.Kind == PoolEffectKind.UpgradeWalletDebit)
            .GroupBy(e => e.MemberPosition!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        foreach (var (position, credited) in creditedPerMember)
        {
            var debited = debitedPerMember.GetValueOrDefault(position);
            var state = states[position];

            // Whatever has not been drained is a partly-filled current level, and must be exactly
            // the contributions counted toward it -- never more, never negative.
            var held = credited - debited;
            var expectedHeld = state.ContributionCount * Levels[state.CurrentLevel].ContributionPerMember;

            Assert.Equal(expectedHeld, held);
            Assert.True(held >= 0, $"Position {position} holds negative fuel.");
        }
    }

    /// <summary>
    /// PART D: the joining Rs.15 goes one step up and counts toward the receiver's level 1. Level 0
    /// is the joiner's own fee, so it never touches their own wallet -- it only ever appears as a
    /// credit on somebody else's.
    /// </summary>
    [Fact]
    public void A_members_joining_fee_never_enters_their_own_wallet()
    {
        var effects = RunTree(127, out _);

        var levelZeroWalletMovements = effects.Where(e =>
            e.LevelNo == 0 &&
            e.Kind is PoolEffectKind.UpgradeWalletCredit or PoolEffectKind.UpgradeWalletDebit or PoolEffectKind.MainWalletCredit);

        Assert.Empty(levelZeroWalletMovements);

        // The fee still moves -- as a level 1 contribution to the parent, or to the company when the
        // parent is a seed.
        Assert.Contains(effects, e => e.Kind == PoolEffectKind.CompanyWalletCredit && e.LevelNo == 0);
        Assert.Contains(effects, e => e.Kind == PoolEffectKind.UpgradeWalletCredit && e.LevelNo == 1);
    }

    /// <summary>
    /// PART J rule 1: every financial event carries a key unique to it, so a replayed message
    /// re-derives the same keys and the unique index rejects the duplicate writes.
    /// </summary>
    [Fact]
    public void Replaying_the_same_join_produces_identical_keys()
    {
        var first = RunTree(15, out _);
        var second = RunTree(15, out _);

        // Keys are derived from position and level alone -- nothing time- or sequence-dependent --
        // so the same tree always produces the same keys. That is what makes the unique index a
        // reliable guard rather than an accident of ordering.
        Assert.Equal(
            first.Select(e => $"{e.Kind}:{e.IdempotencyKey}"),
            second.Select(e => $"{e.Kind}:{e.IdempotencyKey}"));
    }
}
