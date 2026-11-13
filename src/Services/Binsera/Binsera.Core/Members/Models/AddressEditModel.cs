#nullable disable
using Binsera.Core.Locations.Models;

namespace Binsera.Core.Members.Models
{
    public class AddressEditModel
    {
        public string HouseNo { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int CountryId { get; set; }
        public int? StateId { get; set; }
        public int? CityId { get; set; }
        public string ZipCode { get; set; }
        public int? DistId { get; set; }

        public void SetAddressModel(AddressItemModel[] addressItemModels)
        {
            var city = addressItemModels.Where(a => a.CityName.Equals(City, StringComparison.OrdinalIgnoreCase) && a.ZipCode == ZipCode)
                .Select(a => a).FirstOrDefault();
            if (city == null)
                city = addressItemModels.Where(a => a.CityName.Equals(City, StringComparison.OrdinalIgnoreCase))
                .Select(a => a).FirstOrDefault();
            if (city != null)
            {
                CityId = (int)city.Id;
                StateId = city.StateId;
                State = city.StateName;
                CountryId = city.CountryId;
                DistId = city.DistId;
            }
            else
            {
                StateId = 10;
                CountryId = 96;
            }
        }
    }
}
