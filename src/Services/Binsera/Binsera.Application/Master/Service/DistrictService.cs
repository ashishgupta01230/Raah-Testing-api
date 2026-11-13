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
    public class DistrictService : IDistrictService
    {
        private readonly IRepository<DistrictView> _districtRepository;
        private readonly IRepository<CountryView> _countryRepository;
        private readonly IRepository<StateView> _stateRepository;
        private readonly BinseraDbContext _context;

        public DistrictService(IUnitOfWork unitOfWork, BinseraDbContext context)
        {
            _districtRepository = unitOfWork.Repository<DistrictView>();
            _countryRepository = unitOfWork.Repository<CountryView>();
            _stateRepository = unitOfWork.Repository<StateView>();
            _context = context;
        }

        public async Task<IPagedList<DistrictModel>> GetAllAsync(DistrictFilterModel filterModel)
        {
            var query = from d in _districtRepository.TableNoTracking
                        join c in _countryRepository.TableNoTracking on d.CID equals c.Id
                        join s in _stateRepository.TableNoTracking on d.SID equals s.Id
                        where (string.IsNullOrEmpty(filterModel.Keyword) || d.DistName.Contains(filterModel.Keyword))
                           && (!filterModel.CID.HasValue || d.CID == filterModel.CID.Value)
                           && (!filterModel.SID.HasValue || d.SID == filterModel.SID.Value)
                           && (!filterModel.Active.HasValue || d.Active == filterModel.Active.Value)
                        select new DistrictModel
                        {
                            DistId = (int)d.Id,
                            CID = d.CID,
                            SID = d.SID,
                            DistName = d.DistName,
                            CountryName = c.CountryName,
                            StateName = s.StateName,
                            Active = d.Active,
                        };

            query = filterModel.SortingOrder == SortingOrder.Desc
                ? query.OrderByDescending(d => d.DistName)
                : query.OrderBy(d => d.DistName);

            return await query.ToPagedListAsync(filterModel.PageIndex, filterModel.PageSize);
        }

        public async Task<DistrictModel?> GetByIdAsync(int id)
        {
            return await (from d in _districtRepository.TableNoTracking
                          join c in _countryRepository.TableNoTracking on d.CID equals c.Id
                          join s in _stateRepository.TableNoTracking on d.SID equals s.Id
                          where d.Id == id
                          select new DistrictModel
                          {
                              DistId = (int)d.Id,
                              CID = d.CID,
                              SID = d.SID,
                              DistName = d.DistName,
                              CountryName = c.CountryName,
                              StateName = s.StateName,
                              Active = d.Active,
                          }).FirstOrDefaultAsync();
        }

        public async Task UpdateStatusAsync(int id, bool active)
        {
            var district = await _context.Set<DistrictView>()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (district == null)
                throw new KeyNotFoundException($"District with Id {id} not found.");

            district.Active = active;
            _context.Entry(district).Property(x => x.Active).IsModified = true;
            await _context.SaveChangesAsync();
        }
    }
}
