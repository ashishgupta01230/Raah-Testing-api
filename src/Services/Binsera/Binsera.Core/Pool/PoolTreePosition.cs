using System.Numerics;

namespace Binsera.Core.Pool
{
    public enum PoolSide
    {
        Left = 0,
        Right = 1
    }

    public enum PoolRecipientKind
    {
        /// <summary>A real member, identified by their pool position.</summary>
        Member = 0,

        /// <summary>One of the X1-X7 company seed accounts. Money goes to the company wallet.</summary>
        SeedAccount = 1,

        /// <summary>Above X7 -- nothing there. Money goes to the company wallet.</summary>
        None = 2
    }

    public readonly record struct PoolRecipient(PoolRecipientKind Kind, long MemberPosition, int SeedIndex)
    {
        public static PoolRecipient Member(long position) => new(PoolRecipientKind.Member, position, 0);

        public static PoolRecipient Seed(int seedIndex) => new(PoolRecipientKind.SeedAccount, 0, seedIndex);

        public static readonly PoolRecipient None = new(PoolRecipientKind.None, 0, 0);
    }

    /// <summary>
    /// Placement arithmetic for the Global Auto Pool tree.
    ///
    /// The requirements doc states placement as "sabse upar wali khaali jagah, left se right"
    /// (PART C) -- topmost empty slot, filled left to right, one row completed before the next
    /// begins. That is the definition of level-order fill of a complete binary tree, which means
    /// the tree is always complete by construction and a member's whole position in it follows
    /// from a single number.
    ///
    /// Give each member a sequential position in join order -- the first real member is 1, then
    /// 2, 3, ... -- and parent, side and depth are arithmetic rather than a tree search:
    ///
    ///     position   1    2    3    4    5    6    7    8  ...   15
    ///     member     A    B    C    D    E    F    G    H  ...   O
    ///     parent     -    A    A    B    B    C    C    D  ...   G
    ///     side       -    L    R    L    R    L    R    L  ...   R
    ///
    /// which is exactly the fill order the doc's diagram gives (B, C, D, E, F, G, H ... O).
    ///
    /// The X1-X7 seed accounts sit in a straight chain above the first member and are not part of
    /// this numbering -- stepping past the first member lands on them, see
    /// <see cref="ResolveStepsUp"/>.
    ///
    /// Everything here is a pure function of the position: no database, no state.
    /// </summary>
    public static class PoolTreePosition
    {
        /// <summary>The first real member's position. X1-X7 sit above this and have no position.</summary>
        public const long RootPosition = 1;

        /// <summary>X1..X7 -- seven, because the root member's level 6 transfer travels 7 steps up.</summary>
        public const int SeedAccountCount = 7;

        /// <summary>
        /// The position a new member takes. Placement order is join order and positions are
        /// contiguous from 1, so this is simply the next number -- the caller's job is to make
        /// reading the highest position and writing the new one atomic, not to search the tree.
        /// </summary>
        public static long NextPosition(long highestAssignedPosition)
        {
            if (highestAssignedPosition < 0)
                throw new ArgumentOutOfRangeException(nameof(highestAssignedPosition), highestAssignedPosition, "Positions are never negative.");

            return highestAssignedPosition + 1;
        }

        /// <summary>The parent's position, or null for the root member, who has no parent inside the tree.</summary>
        public static long? ParentOf(long position)
        {
            GuardPosition(position);

            return position == RootPosition ? null : position / 2;
        }

        /// <summary>
        /// Which side of its parent this position hangs off. Even positions fill the left slot,
        /// odd positions the right, which is what makes the fill order left-to-right within a row.
        /// </summary>
        public static PoolSide SideOf(long position)
        {
            GuardPosition(position);

            if (position == RootPosition)
                throw new ArgumentOutOfRangeException(nameof(position), position, "The root member has no parent, so no side.");

            return position % 2 == 0 ? PoolSide.Left : PoolSide.Right;
        }

        /// <summary>Depth below the root member, who is depth 0.</summary>
        public static int DepthOf(long position)
        {
            GuardPosition(position);

            // floor(log2(position)) -- done on bits rather than Math.Log2 so it stays exact at
            // positions where double precision would start rounding.
            return BitOperations.Log2((ulong)position);
        }

        /// <summary>
        /// Who receives money sent <paramref name="steps"/> steps up from <paramref name="position"/>.
        ///
        /// Within the member tree this is just the position divided by 2 per step. Once the walk
        /// passes the root member it continues into the seed chain: one step above the root is X1,
        /// two is X2, and so on. Above X7 there is nothing, and the doc sends that money to the
        /// company wallet -- as it does for the seeds themselves.
        /// </summary>
        public static PoolRecipient ResolveStepsUp(long position, int steps)
        {
            GuardPosition(position);

            if (steps < 1)
                throw new ArgumentOutOfRangeException(nameof(steps), steps, "Steps up must be at least 1.");

            var depth = DepthOf(position);

            if (steps <= depth)
                return PoolRecipient.Member(position >> steps);

            var seedIndex = steps - depth;

            return seedIndex <= SeedAccountCount
                ? PoolRecipient.Seed(seedIndex)
                : PoolRecipient.None;
        }

        /// <summary>The left child's position -- occupied by the next member placed under this one.</summary>
        public static long LeftChildOf(long position)
        {
            GuardPosition(position);
            return position * 2;
        }

        /// <summary>The right child's position.</summary>
        public static long RightChildOf(long position)
        {
            GuardPosition(position);
            return position * 2 + 1;
        }

        private static void GuardPosition(long position)
        {
            if (position < RootPosition)
                throw new ArgumentOutOfRangeException(nameof(position), position, "Pool positions start at 1.");
        }
    }
}
