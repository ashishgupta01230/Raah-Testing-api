using Binsera.Core.Locations.Models;
using Binsera.Core.Members.Models;
using HautSphere.BuildingBlocks.Domain.Models;

namespace Binsera.Core.Members.Contracts
{
    public interface IMemberDataService
    {
        Task<IPagedList<MemberItemModel>> GetMembers(MemberFilterModel filterModel);
        Task<MemberLoginDetailsModel> GetMemberLoginDetails(long memberId);
    }
}
