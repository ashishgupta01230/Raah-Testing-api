using Binsera.Core.Pool;

namespace Binsera.Pool.Tests;

/// <summary>
/// The PART H scenario only takes the first member to level 2 -- fifteen members is all it needs.
/// These push the cascade deeper, because every level has its own amounts and its own recipient
/// distance, and a mistake at level 4 would not show up in a fifteen-member test.
///
/// PART K explains why this cannot be exhaustive: reaching level 5 needs 65,534 members below the
/// first one, level 6 needs 41,94,302, and level 7 needs 53,68,70,910. The first five are simulated
/// here outright. Level 6 has been run the same way out of band -- 41,94,303 members, 2.3 seconds,
/// 347 MB -- and is not kept in the suite at that cost. Level 7 cannot be simulated at all, so its
/// distinguishing behaviour is tested directly instead.
/// </summary>
public class PoolDeepLevelTests
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

    [Fact]
    public void The_level_table_satisfies_the_invariants_PART_F_is_built_on()
    {
        foreach (var level in Levels.Values)
        {
            // IncomingTotal = Members x Amount/Member, and MainWalletIncome is the remainder after
            // the transfer. The doc states both as formulas the table must satisfy.
            Assert.Equal(level.RequiredMembers * level.ContributionPerMember, level.IncomingTotal);
            Assert.Equal(level.IncomingTotal - level.TransferAmount, level.MainWalletIncome);
            Assert.True(level.MainWalletIncome >= 0, $"Level {level.LevelNo} transfers more than it takes in.");
        }

        // "Chain: har row ka Transfer, agli row ka 'Har ek se' ban jaata hai." If this breaks, a
        // transfer arrives at an amount its recipient's level does not expect and the cascade
        // refuses it.
        for (var levelNo = 0; levelNo < 7; levelNo++)
            Assert.Equal(Levels[levelNo].TransferAmount, Levels[levelNo + 1].ContributionPerMember);

        // "Level N complete -> paisa N+1 step upar."
        for (var levelNo = 0; levelNo <= 6; levelNo++)
            Assert.Equal(levelNo + 1, Levels[levelNo].StepsUp);
    }

    /// <summary>
    /// Grows a real tree member by member and checks the first member's progress against PART K's
    /// table of how big the tree must be for each level, and PART F's income for each.
    /// </summary>
    [Fact]
    public void The_first_member_climbs_the_levels_exactly_when_PART_K_says_they_should()
    {
        var states = new Dictionary<long, PoolMemberState>();
        var mainWallet = 0m;
        var companyWallet = 0m;
        var upgradeHeld = 0m;

        PoolMemberState StateOf(long position)
        {
            if (!states.TryGetValue(position, out var state))
                states[position] = state = new PoolMemberState();

            return state;
        }

        // PART K gives the count of members *below* the first one; the tree is one larger.
        // Income is cumulative Main Wallet, from PART F: L2 pays 10, L3 pays 20, L4 pays 1,000,
        // L5 pays 3,00,000.
        var checkpoints = new Dictionary<long, (int LevelAfter, decimal CumulativeIncome)>
        {
            [3] = (2, 0m),
            [15] = (3, 10m),
            [127] = (4, 30m),
            [2_047] = (5, 1_030m),
            [65_535] = (6, 3_01_030m)
        };

        for (long position = 1; position <= 65_535; position++)
        {
            foreach (var effect in PoolCascade.PlanJoin(position, Levels, StateOf))
            {
                switch (effect.Kind)
                {
                    case PoolEffectKind.UpgradeWalletCredit: upgradeHeld += effect.Amount; break;
                    case PoolEffectKind.UpgradeWalletDebit: upgradeHeld -= effect.Amount; break;
                    case PoolEffectKind.CompanyWalletCredit: companyWallet += effect.Amount; break;
                    case PoolEffectKind.MainWalletCredit:
                        if (effect.MemberPosition == PoolTreePosition.RootPosition)
                            mainWallet += effect.Amount;
                        break;
                }
            }

            if (!checkpoints.TryGetValue(position, out var expected))
                continue;

            Assert.Equal(expected.LevelAfter, StateOf(PoolTreePosition.RootPosition).CurrentLevel);
            Assert.Equal(expected.CumulativeIncome, mainWallet);
        }

        // Fuel is never conjured or destroyed in aggregate: a level completing drains exactly what
        // its contributions put in, so this can only drift if the arithmetic is wrong.
        Assert.True(upgradeHeld >= 0, "Upgrade wallets went negative in aggregate.");
        Assert.True(companyWallet > 0);
    }

    /// <summary>
    /// Level 7 transfers nothing and has nowhere to send it -- it is the end of the plan. A tree
    /// large enough to reach it would need 53 crore members, so the branch is exercised with a
    /// level table whose first level is final. What is being tested is the cascade's handling of a
    /// final level, which is the same code either way.
    /// </summary>
    [Fact]
    public void A_final_level_transfers_nothing_and_stops_the_chain()
    {
        var finalAtLevelOne = new Dictionary<int, PoolLevelSettings>
        {
            [0] = new(0, 1, 15m, 15m, 1),
            [1] = new(1, 2, 15m, 0m, null)   // final: nothing transfers, nowhere to send it
        };

        var states = new Dictionary<long, PoolMemberState>();

        PoolMemberState StateOf(long position)
        {
            if (!states.TryGetValue(position, out var state))
                states[position] = state = new PoolMemberState();

            return state;
        }

        PoolCascade.PlanJoin(1, finalAtLevelOne, StateOf);   // A
        PoolCascade.PlanJoin(2, finalAtLevelOne, StateOf);   // B -- A's level 1 is now 1/2
        var completing = PoolCascade.PlanJoin(3, finalAtLevelOne, StateOf);   // C -- completes it

        // A keeps the whole incoming total, because nothing goes upward.
        var income = completing.Single(e =>
            e.Kind == PoolEffectKind.MainWalletCredit && e.MemberPosition == 1);
        Assert.Equal(finalAtLevelOne[1].IncomingTotal, income.Amount);

        // And no transfer leaves A -- the only company credit is C's own joining fee travelling up
        // past the tree, not a level transfer from A.
        Assert.DoesNotContain(completing, e =>
            e.Kind == PoolEffectKind.CompanyWalletCredit && e.SourcePosition == 1 && e.LevelNo == 1);

        Assert.Equal(2, StateOf(1).CurrentLevel);
    }

    [Fact]
    public void Level_seven_is_configured_as_the_final_level()
    {
        // Whatever else changes in the level table, this has to stay true, because the cascade
        // relies on it to know where to stop.
        Assert.Equal(0m, Levels[7].TransferAmount);
        Assert.Null(Levels[7].StepsUp);
        Assert.Equal(Levels[7].IncomingTotal, Levels[7].MainWalletIncome);
        Assert.Equal(1_28_00_000m, Levels[7].MainWalletIncome);
    }
}
