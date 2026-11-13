using Binsera.Core.Auth.Contracts;
using Binsera.Core.Auth.Domain;
using Binsera.Core.Auth.Model;
using Binsera.Core.Members.Domain;
using HautSphere.BuildingBlocks.Configuration.Controller;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Binsera.API.Controllers.Auth
{
    [Route("user-login-history")]
    public class UserLoginHistoryController(
        IUserLoginHistoryService userLoginHistoryService,
        IUnitOfWork unitOfWork
    ) : ApiControllerBase
    {
        private readonly IRepository<Member> _memberRepository = unitOfWork.Repository<Member>();
        [HttpGet("list")]
        public async Task<IActionResult> GetLoginHistory(
            [FromQuery] UserLoginHistoryRequest request,
            Guid? memberId,
            CancellationToken cancellationToken)
        {
            var isAdmin = this.RoleId == (int)RoleType.Admin || this.RoleId == (int)RoleType.SubAdmin;
            if (!isAdmin)
            {
                request.UserName = this.UserName;
            }
            else if (memberId.HasValue)
            {
                var userName = await GetUserName(memberId.Value, cancellationToken);
                if (string.IsNullOrEmpty(userName))
                    return NotFound(new { message = "Member not found." });

                request.UserName = userName;
            }

            var history = await userLoginHistoryService.UserLoginHistory(request);
            return Ok(history);
        }

        private async Task<string?> GetUserName(Guid memberId, CancellationToken cancellationToken)
        {
            return await _memberRepository.TableNoTracking
                .Where(x => x.MemberGraphId == memberId)
                .Select(x => x.LoginID)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
