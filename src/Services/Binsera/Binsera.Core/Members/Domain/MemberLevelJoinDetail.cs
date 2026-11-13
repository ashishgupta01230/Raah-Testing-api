using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Members.Domain
{
    /// <summary>
    /// The legacy dbo.Member_level_JoinDetails table -- the per-credit ledger (who
    /// triggered it, when, how much), one row per commission credit. Mapped to
    /// DomainBase.Id the same way MemberLevel maps to "MLID", but note MLID is a FK
    /// back to Member_Level here (not unique per row -- many join-detail rows can
    /// share one MLID), so this entity is only safe to query via TableNoTracking.
    /// </summary>
    public class MemberLevelJoinDetail : DomainBase
    {
        public long ToRegNo { get; private set; }
        public long FromRegNo { get; private set; }
        public DateTime EntryDate { get; private set; }
        public decimal Amount { get; private set; }
    }
}
