using Binsera.Core.Pool;

namespace Binsera.Pool.Tests;

/// <summary>
/// Replays the requirements doc's PART H scenario -- fifteen members joining in order, A through O --
/// and asserts the exact end state the doc states. This is the acceptance test for the whole
/// cascade: the doc hands us a fully worked example with concrete numbers, so if this passes the
/// engine agrees with the plan.
/// </summary>
public class PoolCascadeScenarioTests
{
    // PART F, plus the level 0 joining row from the UI design doc's Level Master screen.
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

    private sealed class Pool
    {
        private readonly Dictionary<long, PoolMemberState> _states = [];

        public readonly Dictionary<long, decimal> MainWallet = [];
        public readonly Dictionary<long, decimal> UpgradeWallet = [];
        public decimal CompanyWallet;
        public readonly List<PoolEffect> AllEffects = [];

        public PoolMemberState StateOf(long position)
        {
            if (!_states.TryGetValue(position, out var state))
                _states[position] = state = new PoolMemberState();

            return state;
        }

        public int LevelOf(long position) => StateOf(position).CurrentLevel;

        public void Join(long position)
        {
            var effects = PoolCascade.PlanJoin(position, Levels, StateOf);
            AllEffects.AddRange(effects);

            foreach (var effect in effects)
            {
                switch (effect.Kind)
                {
                    case PoolEffectKind.UpgradeWalletCredit:
                        UpgradeWallet[effect.MemberPosition!.Value] =
                            UpgradeWallet.GetValueOrDefault(effect.MemberPosition.Value) + effect.Amount;
                        break;

                    case PoolEffectKind.UpgradeWalletDebit:
                        UpgradeWallet[effect.MemberPosition!.Value] =
                            UpgradeWallet.GetValueOrDefault(effect.MemberPosition.Value) - effect.Amount;
                        break;

                    case PoolEffectKind.MainWalletCredit:
                        MainWallet[effect.MemberPosition!.Value] =
                            MainWallet.GetValueOrDefault(effect.MemberPosition.Value) + effect.Amount;
                        break;

                    case PoolEffectKind.CompanyWalletCredit:
                        CompanyWallet += effect.Amount;
                        break;
                }
            }
        }
    }

    private static Pool RunFifteenMemberScenario()
    {
        var pool = new Pool();

        // A=1, B=2 ... O=15, joining in the order PART C's fill diagram gives.
        for (long position = 1; position <= 15; position++)
            pool.Join(position);

        return pool;
    }

    [Fact]
    public void After_fifteen_members_the_levels_match_the_doc_summary()
    {
        var pool = RunFifteenMemberScenario();

        // PART H's closing table: A on L3, B-G on L2, H-O on L1.
        Assert.Equal(3, pool.LevelOf(1));                       // A

        foreach (long position in new long[] { 2, 3 })           // B, C
            Assert.Equal(2, pool.LevelOf(position));

        foreach (long position in new long[] { 4, 5, 6, 7 })     // D, E, F, G
            Assert.Equal(2, pool.LevelOf(position));

        for (long position = 8; position <= 15; position++)      // H-O
            Assert.Equal(1, pool.LevelOf(position));
    }

    [Fact]
    public void Only_the_root_member_earns_anything_and_it_is_exactly_ten_rupees()
    {
        var pool = RunFifteenMemberScenario();

        // "A ke Main Wallet me Rs.10 <- A ki pehli kamai", and every other member still on zero.
        Assert.Equal(10m, pool.MainWallet.GetValueOrDefault(1));

        for (long position = 2; position <= 15; position++)
            Assert.Equal(0m, pool.MainWallet.GetValueOrDefault(position));
    }

    [Fact]
    public void Every_rupee_is_accounted_for()
    {
        var pool = RunFifteenMemberScenario();

        // 15 joinings at Rs.15 each is everything that entered the pool.
        const decimal totalIn = 15 * 15m;

        var paidToMembers = pool.MainWallet.Values.Sum();
        var stillHeldAsFuel = pool.UpgradeWallet.Values.Sum();

        // "15 members join hue. Kul Rs.10 kisi member ko mila. Baaki sab company wallet me."
        Assert.Equal(10m, paidToMembers);
        Assert.Equal(215m, pool.CompanyWallet);
        Assert.Equal(totalIn, paidToMembers + pool.CompanyWallet + stillHeldAsFuel);
    }

    [Fact]
    public void Upgrade_wallets_never_go_negative()
    {
        var pool = RunFifteenMemberScenario();

        // PART J rule 6. A negative balance would mean a level was funded by money that never
        // arrived, so this is a correctness check rather than a defensive one.
        Assert.All(pool.UpgradeWallet.Values, balance => Assert.True(balance >= 0, $"Upgrade wallet went negative: {balance}"));
    }

    [Fact]
    public void A_completed_level_drains_the_upgrade_wallet_exactly()
    {
        var pool = RunFifteenMemberScenario();

        // PART F: IncomingTotal = Transfer + MainWallet, so completing a level leaves nothing
        // behind -- "is design me excess bachta hi nahi" (PART L point 5).
        Assert.Equal(0m, pool.UpgradeWallet.GetValueOrDefault(1));  // A, completed L1 and L2
        Assert.Equal(0m, pool.UpgradeWallet.GetValueOrDefault(2));  // B, completed L1
        Assert.Equal(0m, pool.UpgradeWallet.GetValueOrDefault(4));  // D, completed L1
    }

    [Fact]
    public void Partly_filled_levels_hold_their_contributions_as_fuel()
    {
        var pool = new Pool();

        // A and B only: B's joining Rs.15 reaches A, whose level 1 needs two.
        pool.Join(1);
        pool.Join(2);

        Assert.Equal(15m, pool.UpgradeWallet.GetValueOrDefault(1));
        Assert.Equal(1, pool.LevelOf(1));
        Assert.Equal(0m, pool.MainWallet.GetValueOrDefault(1));
    }

    [Fact]
    public void Row_one_transfers_go_to_the_company_not_the_root_member()
    {
        var pool = RunFifteenMemberScenario();

        // The doc's explicit warning: B and C's Rs.30 goes to X1, not to A. If it went to A, A's
        // level 2 would fill from the wrong members and the whole plan's counting breaks.
        var bTransfer = pool.AllEffects.Single(e =>
            e.Kind == PoolEffectKind.CompanyWalletCredit && e.SourcePosition == 2 && e.LevelNo == 1);

        Assert.Equal(30m, bTransfer.Amount);
        Assert.Equal(1, bTransfer.SeedIndex);  // X1
    }

    [Fact]
    public void The_root_members_level_two_is_filled_by_the_four_members_two_rows_below()
    {
        var pool = RunFifteenMemberScenario();

        // PART E: A's level 2 is filled by D, E, F and G completing their level 1 -- not by B and C.
        var contributors = pool.AllEffects
            .Where(e => e.Kind == PoolEffectKind.UpgradeWalletCredit && e.MemberPosition == 1 && e.LevelNo == 2)
            .Select(e => e.SourcePosition)
            .ToList();

        Assert.Equal([4L, 5L, 6L, 7L], contributors.Order().ToList());
        Assert.All(contributors, source => Assert.Equal(30m, pool.AllEffects.First(e => e.SourcePosition == source && e.LevelNo == 2).Amount));
    }

    [Fact]
    public void Idempotency_keys_are_unique_across_the_whole_run()
    {
        var pool = RunFifteenMemberScenario();

        // Every effect carries the key that will be written with a unique index behind it. A
        // collision here would mean a legitimate credit gets rejected as a duplicate replay.
        var moneyEffects = pool.AllEffects
            .Where(e => e.Kind != PoolEffectKind.LevelCompleted)
            .Select(e => $"{e.Kind}:{e.IdempotencyKey}")
            .ToList();

        Assert.Equal(moneyEffects.Count, moneyEffects.Distinct().Count());
    }

    [Fact]
    public void A_mismatched_contribution_amount_stops_everything()
    {
        // Decision: on drift, fail loudly rather than credit the wrong counter. Forcing A onto
        // level 2 while B's joining still sends level 1's Rs.15 reproduces that mismatch.
        var pool = new Pool();
        pool.Join(1);
        pool.StateOf(1).CurrentLevel = 2;

        var error = Assert.Throws<InvalidOperationException>(() => pool.Join(2));

        Assert.Contains("does not match level", error.Message);
    }
}
