using Binsera.Core.Pool;

namespace Binsera.Pool.Tests;

/// <summary>
/// Every expectation here comes from Global_Auto_Pool_Scenario_Complete_Flow.md, not from the
/// implementation. Positions map to the doc's PART C tree diagram: A=1, B=2, C=3, D=4 ... O=15.
/// </summary>
public class PoolTreePositionTests
{
    // PART C fill order: B,C under A; D,E under B; F,G under C; H,I under D; J,K under E;
    // L,M under F; N,O under G.
    [Theory]
    [InlineData(2, 1)]   // B -> A
    [InlineData(3, 1)]   // C -> A
    [InlineData(4, 2)]   // D -> B
    [InlineData(5, 2)]   // E -> B
    [InlineData(6, 3)]   // F -> C
    [InlineData(7, 3)]   // G -> C
    [InlineData(8, 4)]   // H -> D
    [InlineData(9, 4)]   // I -> D
    [InlineData(10, 5)]  // J -> E
    [InlineData(11, 5)]  // K -> E
    [InlineData(12, 6)]  // L -> F
    [InlineData(13, 6)]  // M -> F
    [InlineData(14, 7)]  // N -> G
    [InlineData(15, 7)]  // O -> G
    public void Parent_matches_the_doc_tree_diagram(long position, long expectedParent)
    {
        Assert.Equal(expectedParent, PoolTreePosition.ParentOf(position));
    }

    [Fact]
    public void Root_member_has_no_parent()
    {
        Assert.Null(PoolTreePosition.ParentOf(PoolTreePosition.RootPosition));
    }

    [Theory]
    [InlineData(2, PoolSide.Left)]
    [InlineData(3, PoolSide.Right)]
    [InlineData(8, PoolSide.Left)]
    [InlineData(15, PoolSide.Right)]
    public void Even_positions_fill_the_left_slot_and_odd_the_right(long position, PoolSide expected)
    {
        Assert.Equal(expected, PoolTreePosition.SideOf(position));
    }

    [Theory]
    [InlineData(1, 0)]   // A
    [InlineData(2, 1)]   // B, row 1
    [InlineData(3, 1)]   // C, row 1
    [InlineData(4, 2)]   // D, row 2
    [InlineData(7, 2)]   // G, row 2
    [InlineData(8, 3)]   // H, row 3
    [InlineData(15, 3)]  // O, row 3
    public void Depth_counts_rows_below_the_root_member(long position, int expectedDepth)
    {
        Assert.Equal(expectedDepth, PoolTreePosition.DepthOf(position));
    }

    [Fact]
    public void Depth_stays_exact_where_floating_point_log_would_round()
    {
        // 2^52 is where double precision starts losing integers; Math.Log2 round-trips can land
        // a position on the wrong side of a power of two here, which would misplace a member.
        const long justBelowPowerOfTwo = (1L << 52) - 1;

        Assert.Equal(51, PoolTreePosition.DepthOf(justBelowPowerOfTwo));
        Assert.Equal(52, PoolTreePosition.DepthOf(1L << 52));
    }

    // PART H, worked step by step. The joining Rs.15 always travels exactly one step up.
    [Theory]
    [InlineData(2, 1)]   // "B join hua -> Rs.15 A ko"
    [InlineData(4, 2)]   // "D join: Rs.15 -> B"
    [InlineData(8, 4)]   // "H join -> Rs.15 D ko"
    public void Joining_contribution_goes_one_step_up_to_the_parent(long joiner, long expectedRecipient)
    {
        var recipient = PoolTreePosition.ResolveStepsUp(joiner, steps: 1);

        Assert.Equal(PoolRecipientKind.Member, recipient.Kind);
        Assert.Equal(expectedRecipient, recipient.MemberPosition);
    }

    // PART H: "D, E, F, G apna L1 complete karenge" -> each sends Rs.30 two steps up, to A.
    [Theory]
    [InlineData(4)]  // D
    [InlineData(5)]  // E
    [InlineData(6)]  // F
    [InlineData(7)]  // G
    public void Row_two_level_one_transfers_reach_the_root_member(long position)
    {
        var recipient = PoolTreePosition.ResolveStepsUp(position, steps: 2);

        Assert.Equal(PoolRecipientKind.Member, recipient.Kind);
        Assert.Equal(PoolTreePosition.RootPosition, recipient.MemberPosition);
    }

    /// <summary>
    /// The doc calls this out explicitly, because it is the counter-intuitive case:
    /// "Dhyan dein: B aur C ka Rs.30 A ko nahi gaya -- X1 ko gaya. Kyunki B se 2 step upar
    /// X1 hai, A nahi."
    /// </summary>
    [Theory]
    [InlineData(2)]  // B
    [InlineData(3)]  // C
    public void Row_one_level_one_transfers_skip_past_the_root_into_the_seed_chain(long position)
    {
        var recipient = PoolTreePosition.ResolveStepsUp(position, steps: 2);

        Assert.Equal(PoolRecipientKind.SeedAccount, recipient.Kind);
        Assert.Equal(1, recipient.SeedIndex); // X1
    }

    // PART H, from the root member: each of A's transfers lands one further up the seed chain.
    [Theory]
    [InlineData(1, 1)]  // joining Rs.15 -> X1
    [InlineData(2, 2)]  // L1 Rs.30 -> X2
    [InlineData(3, 3)]  // L2 Rs.110 -> X3
    [InlineData(7, 7)]  // L6 Rs.1,00,000 -> X7
    public void Transfers_from_the_root_member_walk_the_seed_chain(int steps, int expectedSeedIndex)
    {
        var recipient = PoolTreePosition.ResolveStepsUp(PoolTreePosition.RootPosition, steps);

        Assert.Equal(PoolRecipientKind.SeedAccount, recipient.Kind);
        Assert.Equal(expectedSeedIndex, recipient.SeedIndex);
    }

    /// <summary>
    /// PART B's justification for there being exactly seven seeds: the root member's level 6
    /// transfer travels 7 steps up, and something has to be there to receive it.
    /// </summary>
    [Fact]
    public void Seven_seeds_are_exactly_enough_for_the_root_members_deepest_transfer()
    {
        var deepest = PoolTreePosition.ResolveStepsUp(PoolTreePosition.RootPosition, steps: 7);
        Assert.Equal(PoolTreePosition.SeedAccountCount, deepest.SeedIndex);

        // PART J rule 3: nothing above X7, so that money goes to the company wallet.
        var aboveTheChain = PoolTreePosition.ResolveStepsUp(PoolTreePosition.RootPosition, steps: 8);
        Assert.Equal(PoolRecipientKind.None, aboveTheChain.Kind);
    }

    [Fact]
    public void Walking_up_from_a_deep_member_crosses_the_root_into_the_seed_chain()
    {
        // O sits at depth 3, so three steps reach A and the fourth leaves the member tree.
        var atRoot = PoolTreePosition.ResolveStepsUp(15, steps: 3);
        Assert.Equal(PoolRecipientKind.Member, atRoot.Kind);
        Assert.Equal(PoolTreePosition.RootPosition, atRoot.MemberPosition);

        var pastRoot = PoolTreePosition.ResolveStepsUp(15, steps: 4);
        Assert.Equal(PoolRecipientKind.SeedAccount, pastRoot.Kind);
        Assert.Equal(1, pastRoot.SeedIndex);
    }

    /// <summary>
    /// PART E is the doc's proof that "N+1 steps up" is the only rule that makes the plan work:
    /// level N needs 2^N contributors, and exactly 2^N members are N steps below any given member.
    /// This reproduces that table from the arithmetic alone.
    /// </summary>
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    [InlineData(4, 16)]
    [InlineData(5, 32)]
    [InlineData(6, 64)]
    [InlineData(7, 128)]
    public void Each_level_is_fed_by_exactly_the_number_of_members_it_requires(int level, int requiredMembers)
    {
        var contributors = 0;

        // Everyone at depth `level` below the root -- a contiguous run of positions.
        for (var position = 1L << level; position <= (1L << (level + 1)) - 1; position++)
        {
            var recipient = PoolTreePosition.ResolveStepsUp(position, steps: level);

            if (recipient.Kind == PoolRecipientKind.Member && recipient.MemberPosition == PoolTreePosition.RootPosition)
                contributors++;
        }

        Assert.Equal(requiredMembers, contributors);
    }

    [Theory]
    [InlineData(0, 1)]    // nothing placed yet -> the root member
    [InlineData(15, 16)]  // O placed -> next member opens row 4
    public void Next_position_follows_join_order(long placedCount, long expected)
    {
        Assert.Equal(expected, PoolTreePosition.NextPosition(placedCount));
    }

    [Fact]
    public void Positions_below_one_are_rejected()
    {
        // A silently-wrong position would resolve to a real but wrong recipient, i.e. pay the
        // wrong member, so these throw rather than returning something plausible.
        Assert.Throws<ArgumentOutOfRangeException>(() => PoolTreePosition.DepthOf(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PoolTreePosition.ParentOf(-5));
        Assert.Throws<ArgumentOutOfRangeException>(() => PoolTreePosition.ResolveStepsUp(0, 1));
    }

    [Fact]
    public void Root_member_has_no_side_and_zero_steps_is_not_a_transfer()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PoolTreePosition.SideOf(PoolTreePosition.RootPosition));
        Assert.Throws<ArgumentOutOfRangeException>(() => PoolTreePosition.ResolveStepsUp(4, steps: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PoolTreePosition.NextPosition(-1));
    }
}
