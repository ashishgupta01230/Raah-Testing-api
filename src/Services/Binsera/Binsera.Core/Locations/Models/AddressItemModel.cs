#nullable disable
namespace Binsera.Core.Locations.Models
{
    public record AddressItemModel
    {
        public long Id { get; set; }
        public int CountryId { get; set; }
        public int StateId { get; set; }
        public string CountryName { get; set; }
        public string StateName { get; set; }
        public string CityName { get; set; }
        public string ZipCode { get; set; }
        public string DistName { get; set; }
        public int DistId { get; set; }
    }
}
