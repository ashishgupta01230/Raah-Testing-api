using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Pool.Domain
{
    /// <summary>
    /// Maps a seed index (X1..X7) to the member_Master row standing in for it.
    ///
    /// The seeds live in member_Master so the tree has real rows to hang placement pointers off and
    /// the admin tree view can render the chain, but they are not members: no login, no wallet, no
    /// levels. This table is what tells the rest of the system which rows those are, without adding
    /// a flag column to member_Master.
    /// </summary>
    public class PoolSeedAccount : AuditableDomainBase
    {
        private PoolSeedAccount() { }

        /// <summary>1 for X1 (bottom of the chain, directly above the first member) through 7 for X7.</summary>
        public int SeedIndex { get; private set; }

        public long MemberRegNo { get; private set; }
    }
}
