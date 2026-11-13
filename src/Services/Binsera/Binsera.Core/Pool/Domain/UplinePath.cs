using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Pool.Domain
{
    /// <summary>
    /// One row per (member, depth) giving the ancestor that many steps above them, for depths 1
    /// through 8 -- requirements doc PART J rule 4.
    ///
    /// This is materialised on the client's instruction. It is derivable from the member's own
    /// position (the ancestor k steps up is position / 2^k, or a seed account once the walk passes
    /// the first member), so it is a cache rather than a source of truth: if the two ever disagree,
    /// the position is right and this table is wrong.
    ///
    /// Depth 8 is included because the deepest transfer -- the first member's level 6, seven steps
    /// up -- needs one row beyond the seven seeds to be representable.
    /// </summary>
    public class UplinePath : AuditableDomainBase
    {
        private UplinePath() { }

        private UplinePath(long memberRegNo, long ancestorRegNo, int depth)
        {
            MemberRegNo = memberRegNo;
            AncestorRegNo = ancestorRegNo;
            Depth = depth;
        }

        public long MemberRegNo { get; private set; }

        /// <summary>May be a seed account's RegNo, once the walk climbs past the first real member.</summary>
        public long AncestorRegNo { get; private set; }

        public int Depth { get; private set; }

        public static UplinePath Create(long memberRegNo, long ancestorRegNo, int depth)
            => new(memberRegNo, ancestorRegNo, depth);
    }
}
