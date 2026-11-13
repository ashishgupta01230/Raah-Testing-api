using Binsera.Core.Auth.Contracts;
using Binsera.Core.Auth.Domain;
using Binsera.Core.Auth.Model;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Domain.Models;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using HautSphere.BuildingBlocks.Persistence.Extensions;

namespace Binsera.Application.Auth.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class UserLoginHistoryService(IUnitOfWork unitOfWork) : IUserLoginHistoryService
    {
        private readonly IRepository<UserLog> _userLogRepository = unitOfWork.Repository<UserLog>();

        public async Task<IPagedList<UserLoginHistoryResponse>> UserLoginHistory(UserLoginHistoryRequest request)
        {
            var query = _userLogRepository.TableNoTracking
                //.Where(x => !x.IsDeleted)
                .Where(x => string.IsNullOrEmpty(request.UserName) || x.UserName == request.UserName)
                .Where(x => string.IsNullOrEmpty(request.Keyword)
                    || x.UserName.Contains(request.Keyword)
                    || x.Email.Contains(request.Keyword)
                    || x.ClientIp.Contains(request.Keyword)
                    || x.Browser.Contains(request.Keyword))
                .Where(x => !request.FromDate.HasValue || x.LoginDateTime >= request.FromDate)
                .Where(x => !request.ToDate.HasValue || x.LoginDateTime <= request.ToDate)
                .OrderByDescending(x => x.LoginDateTime)
                .Select(x => new UserLoginHistoryResponse
                {
                    UserName = x.UserName,
                    LoginDateTime = x.LoginDateTime,
                    Browser = x.Browser,
                    ClientIp = x.ClientIp,
                });

            return await query.ToPagedListAsync(request.PageIndex, request.PageSize);
        }
    }
}
