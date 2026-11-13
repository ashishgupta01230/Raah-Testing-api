using Binsera.Core.Locations.Models;

namespace Binsera.Core.Locations.Contracts
{
    public interface IAddressViewService
    {
        Task<AddressItemModel[]> GetAddressesByPinCode(string pinCode);
        Task<AddressItemModel[]> GetAddressesByCityName(string cityName);
    }
}
