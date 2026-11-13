using HautSphere.BuildingBlocks.Domain;
using System.ComponentModel.DataAnnotations.Schema;

namespace Binsera.Core.Members.Domain
{
    public class MemberAccount : DomainBase
    {
        [NotMapped]
        public override bool IsDeleted { get;  set; }
        public long RegNo { get; private set; }
        public string TransType { get; private set; }
        public int TrStatus { get; private set; }
        public string Remark { get; private set; }
        public DateTime TransDate { get;private  set; }
        public decimal Credit { get; private set; }
        public decimal Debit { get; private set; }
        public char CRDB { get; set; }

        /// <summary>
        /// Credits a member's wallet directly, without going through sp_IA2.
        ///
        /// sp_IA2 also deducts TDS, a handling charge, and a further 15% from members with no PAN.
        /// The pool's amounts are exact by design -- PART F only balances if the credited amount is
        /// the credited amount -- and the seed accounts have no PAN at all. Deductions, if the
        /// business wants them, belong at withdrawal time rather than on every internal credit.
        /// </summary>
        public static MemberAccount Credited(long regNo, decimal amount, string transType, string remark)
            => new()
            {
                RegNo = regNo,
                Credit = amount,
                Debit = 0m,
                CRDB = 'C',
                TransType = transType,
                Remark = remark,
                TransDate = DateTime.Now,
                TrStatus = 1
            };

        /// <summary>
        /// Takes money out of a member's wallet. Balance is read as credits minus debits, so this
        /// is the only thing that actually reduces what a member can withdraw.
        /// </summary>
        public static MemberAccount Debited(long regNo, decimal amount, string transType, string remark)
            => new()
            {
                RegNo = regNo,
                Credit = 0m,
                Debit = amount,
                CRDB = 'D',
                TransType = transType,
                Remark = remark,
                TransDate = DateTime.Now,
                TrStatus = 1
            };
    }
}
