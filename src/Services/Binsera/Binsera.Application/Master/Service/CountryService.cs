using Binsera.Core.Master.Contract;
using Binsera.Core.Master.Domain;
using Binsera.Core.Master.Model;
using Binsera.Persistence;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Domain.Models;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using HautSphere.BuildingBlocks.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Master.Service
{
    [AutoRegister(includeInterfaces: true)]
    public class CountryService : ICountryService
    {
        private readonly IRepository<CountryView> _countryRepository;
        private readonly BinseraDbContext _context;

        public CountryService(IUnitOfWork unitOfWork, BinseraDbContext context)
        {
            _countryRepository = unitOfWork.Repository<CountryView>();
            _context = context;
        }

        public async Task<IPagedList<CountryModel>> GetCountries(CountryFilterModel filterModel)
        {
            var query = _countryRepository.TableNoTracking
                .Where(c => string.IsNullOrEmpty(filterModel.Keyword) || c.CountryName.Contains(filterModel.Keyword))
                .Where(c => !filterModel.cActive.HasValue || c.cActive == filterModel.cActive.Value)
                .Select(c => new CountryModel
                {
                    CID = (int)c.Id,
                    CountryName = c.CountryName,
                    cActive = c.cActive
                });

            query = filterModel.SortingOrder == SortingOrder.Desc
                ? query.OrderByDescending(c => c.CountryName)
                : query.OrderBy(c => c.CountryName);
           
            return await query.ToPagedListAsync(filterModel.PageIndex, filterModel.PageSize);
        }

        public async Task UpdateCountryStatus(int id, bool cActive)
        {
            var country = await _context.Set<CountryView>()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (country == null)
                throw new KeyNotFoundException($"Country with CID {id} not found.");

            country.UpdateStatus(cActive);
            await _context.SaveChangesAsync();
        }
    }
}
