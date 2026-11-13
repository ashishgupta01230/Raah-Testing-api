using Binsera.Core.Master.Model;
using HautSphere.BuildingBlocks.Domain.Models;

namespace Binsera.Core.Master.Contract
{
    public interface IDistrictService
    {
        Task<IPagedList<DistrictModel>> GetAllAsync(DistrictFilterModel filterModel);
        Task<DistrictModel?> GetByIdAsync(int id);
        Task UpdateStatusAsync(int id, bool active);
    }


}
