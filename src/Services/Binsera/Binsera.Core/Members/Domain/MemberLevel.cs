using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Members.Domain
{
    /// <summary>
    /// One row per (member, level), tracking how full that level is and what it earned.
    ///
    /// This is the legacy Member_Level table, reused by the Global Auto Pool rather than a new one
    /// being added. Its <c>rl*</c> columns belonged to the previous plan's repurchase levels and are
    /// left untouched. <c>mlActive</c> is read here as "this level is complete".
    /// </summary>
    public class MemberLevel : DomainBase
    {
        public long RegNo { get; private set; }

        /// <summary>The level number. Level 0 is the joining fee.</summary>
        public int Levels { get; private set; }

        /// <summary>Contributions received toward this level so far.</summary>
        public int TlCount { get; private set; }

        public int RlCount { get; private set; }

        /// <summary>What completing this level paid into the member's Main Wallet.</summary>
        public decimal TlIncome { get; set; }

        public decimal RlIncome { get; set; }

        /// <summary>True once the level is complete. Null/false while it is still filling.</summary>
        public bool? MlActive { get; private set; }

        public static MemberLevel StartLevel(long regNo, int levelNo)
            => new()
            {
                RegNo = regNo,
                Levels = levelNo,
                TlCount = 0,
                TlIncome = 0m,
                MlActive = false
            };

        public void RecordContribution() => TlCount++;

        public void RecordIncome(decimal amount) => TlIncome += amount;

        public void MarkComplete() => MlActive = true;
    }
}
