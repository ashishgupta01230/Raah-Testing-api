using Binsera.Core.Auth.Model;
using HautSphere.BuildingBlocks.Domain.Models;

namespace Binsera.Core.Auth.Contracts
{
    public interface IUserLoginHistoryService
    {
        Task<IPagedList<UserLoginHistoryResponse>> UserLoginHistory(UserLoginHistoryRequest request);
    }
}
