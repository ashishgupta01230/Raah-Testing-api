using HautSphere.BuildingBlocks.Domain;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable
namespace Binsera.Core.Members.Domain
{
    /// <summary>
    /// The company's own ledger (legacy Company_account table). Everything the X1-X7 seed accounts
    /// collect lands here, as does anything that runs off the top of the seed chain.
    /// </summary>
    public class CompanyAccount : DomainBase
    {
        [NotMapped]
        public override bool IsDeleted { get; set; }

        public DateTime TransDate { get; private set; }
        public decimal Credit { get; private set; }
        public decimal Debit { get; private set; }
        public string TransType { get; private set; }
        public string Remark { get; private set; }
        public int TrStatus { get; private set; }

        public static CompanyAccount Collect(decimal amount, string transType, string remark)
            => new()
            {
                TransDate = DateTime.Now,
                Credit = amount,
                Debit = 0m,
                TransType = transType,
                Remark = remark,
                TrStatus = 1
            };
    }
}
