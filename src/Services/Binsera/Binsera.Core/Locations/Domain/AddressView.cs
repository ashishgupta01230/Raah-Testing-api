using HautSphere.BuildingBlocks.Domain;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable
namespace Binsera.Core.Locations.Domain
{
    public class AddressView : DomainBase
    {
        [NotMapped]
        public override bool IsDeleted { get; set; }
        public int CountryId { get; set; }
        public int StateId { get; set; }
        public string CountryName { get; set; }
        public string StateName { get; set; }
        public string CityName { get; set; }
        public string Pincode { get; set; }
        public string DistName { get; set; }
        public int DistId { get; set; }
    }
}
