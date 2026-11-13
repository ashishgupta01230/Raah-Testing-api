using Binsera.Core.Locations.Contracts;
using Binsera.Core.Locations.Domain;
using Binsera.Core.Locations.Models;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Locations.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class AddressViewService : IAddressViewService
    {
        private readonly IRepository<AddressView> _addressViewRepository;
        public AddressViewService(IUnitOfWork unitOfWork)
        {
            _addressViewRepository = unitOfWork.Repository<AddressView>();
        }
        public async Task<AddressItemModel[]> GetAddressesByCityName(string cityName)
        {
            var addresses = await _addressViewRepository.TableNoTracking
                .Where(a => a.CityName == cityName)
                .Select(a => new AddressItemModel
                {
                    CountryId = a.CountryId,
                    CityName = a.CityName,
                    StateName = a.StateName,
                    CountryName = a.CountryName,
                    ZipCode = a.Pincode,
                    StateId = a.StateId
                }).ToArrayAsync();
            return addresses;
        }

        public async Task<AddressItemModel[]> GetAddressesByPinCode(string pinCode)
        {
            var addresses = await _addressViewRepository.TableNoTracking
                .Where(a => a.Pincode == pinCode)
                .Select(a => new AddressItemModel
                {
                    CountryId = a.CountryId,
                    CityName = a.CityName,
                    StateName = a.StateName,
                    CountryName = a.CountryName,
                    ZipCode = a.Pincode,
                    StateId = a.StateId,
                    DistName = a.DistName,
                    DistId = a.DistId
                }).ToArrayAsync();
            return addresses;
        }
    }
}
