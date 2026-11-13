using HautSphere.BuildingBlocks.Domain;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable
namespace Binsera.Core.Members.Domain
{
    public class KitMaster : DomainBase
    {
        [NotMapped]
        public override bool IsDeleted { get; set; }
        public string KitCode { get; private set; }
        public int KitType { get; private set; }
        public string KitDesc { get; private set; }
        public string KitProducts { get; private set; }
        public decimal KitPV { get; private set; }
        public decimal KitBV { get; private set; }
        public decimal KitPrice { get; private set; }
        public decimal? BoardBV { get; private set; }
        public string KitColor { get; private set; }
        public bool? KitActive { get; private set; }
        public int OnlyAdmin { get; private set; }
        public int? JoinActive { get; private set; }
        public string KitImage { get; private set; }
        public decimal? BinaryPVCapping { get; private set; }
        public decimal? KitCapping { get; private set; }
        public decimal? BinaryBuffer { get; private set; }
        public decimal? LevelBuffer { get; private set; }
        public decimal? DirectIncome { get; private set; }
        public decimal? ProductCost { get; private set; }
        public decimal? ReturnPer { get; private set; }
        public long? ReturnDays { get; private set; }
        public decimal? ReturnAmt { get; private set; }
        public int? Validaity { get; private set; }
        public int? Weekly { get; private set; }
        public decimal? Coupan { get; private set; }
        public decimal? PerClassIncome { get; private set; }

    }
}
