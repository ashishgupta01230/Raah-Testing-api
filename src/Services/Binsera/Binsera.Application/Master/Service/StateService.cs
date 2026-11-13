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
    public class StateService : IStateService
    {
        private readonly IRepository<StateView> _stateRepository;
        private readonly IRepository<CountryView> _countryRepository;
        private readonly BinseraDbContext _context;

        public StateService(IUnitOfWork unitOfWork, BinseraDbContext context)
        {
            _stateRepository = unitOfWork.Repository<StateView>();
            _countryRepository = unitOfWork.Repository<CountryView>();
            _context = context;
        }

        public async Task<IPagedList<StateModel>> GetStates(StateFilterModel filterModel)
        {
            var query = from s in _stateRepository.TableNoTracking
                        join c in _countryRepository.TableNoTracking on s.CID equals c.Id
                        where (string.IsNullOrEmpty(filterModel.Keyword) || s.StateName.Contains(filterModel.Keyword))
                           && (!filterModel.CID.HasValue || s.CID == filterModel.CID.Value)
                           && (!filterModel.sActive.HasValue || s.sActive == filterModel.sActive.Value)
                        select new StateModel
                        {
                            SID = (int)s.Id,
                            StateName = s.StateName,
                            CID = s.CID,
                            CountryName = c.CountryName,
                            sActive = s.sActive
                        };

            query = filterModel.SortingOrder == SortingOrder.Desc
                ? query.OrderByDescending(s => s.StateName)
                : query.OrderBy(s => s.StateName);

            return await query.ToPagedListAsync(filterModel.PageIndex, filterModel.PageSize);
        }

        public async Task UpdateStateStatus(int id, bool sActive)
        {
            var state = await _context.Set<StateView>()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (state == null)
                throw new KeyNotFoundException($"State with SID {id} not found.");

            state.UpdateStatus(sActive);
            _context.Entry(state).Property(x => x.sActive).IsModified = true;
            await _context.SaveChangesAsync();
        }
    }
}
