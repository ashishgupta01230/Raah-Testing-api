using Binsera.Core.Members.Models;

namespace Binsera.Core.Members.Contracts
{
    public interface IKitMasterService
    {
        Task<KitItemModel> GetKit(long id);
        Task<List<KitItemModel>> GetKits();
    }
}