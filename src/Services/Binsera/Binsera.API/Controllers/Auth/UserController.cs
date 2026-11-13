using Binsera.Core.Auth.Contracts;
using Binsera.Core.Auth.Domain;
using Binsera.Core.Auth.Model;
using HautSphere.BuildingBlocks.Configuration.Controller;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Binsera.API.Controllers.Auth
{
    [Route("users")]
    public class UserController(IUnitOfWork unitOfWork) : ApiControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var repository = unitOfWork.Repository<UserLogin>();
            var users = await repository.TableNoTracking.OrderBy(x => x.UserName)
                .Select(x => new UserModel
                {
                    Id = x.Id,
                    MemberId = x.UserName,
                    EmailId = x.Email,
                    RoleType = x.RoleId,
                    FirstName = x.UserName
                }).ToListAsync();

            var memberLoginIds = users.Where(s => s.RoleType == RoleType.Member)
                .Select(u => u.MemberId).ToList();

            var members = await unitOfWork.Repository<Core.Members.Domain.Member>()
                .TableNoTracking.Where(m => memberLoginIds.Contains(m.LoginID))
                .Select(m => new
                {
                    LoginID = m.LoginID,
                    firstName = m.FName,
                    lastName = m.LName,
                }).ToListAsync();

            users.ForEach(u =>
             {
                 if (u.RoleType == RoleType.Member)
                 {
                     var member = members.FirstOrDefault(m => m.LoginID == u.MemberId);
                     if (member != null)
                     {
                         u.FirstName = member.firstName;
                         u.LastName = member.lastName;
                     }
                 }
             });
            return Ok(users);
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetUserDetails()
        {
            UserModel userModel = null!;
            if (this.RoleId == (int)RoleType.Member)
            {
                var member = await unitOfWork.Repository<Core.Members.Domain.Member>()
                    .TableNoTracking.FirstOrDefaultAsync(m => m.LoginID == this.UserName);
                if (member != null)
                {
                    userModel = new UserModel
                    {
                        Id = member.Id,
                        EmailId = member.EmailID,
                        RoleType = RoleType.Member,
                        FirstName = member.FName,
                        LastName = member.LName,
                        MemberId = member.MemberGraphId.ToString(),
                        UserName = member.LoginID,
                        ProfileImageId = member.ProfileImage
                    };
                    return Ok(userModel);
                }
            }

            var repository = unitOfWork.Repository<UserLogin>();
            var user = await repository.TableNoTracking.FirstOrDefaultAsync(x => x.UserName == UserName);
            if (user == null)
            {
                return NotFound();
            }

            userModel = new UserModel
            {
                Id = user.Id,
                MemberId = user.UserName,
                EmailId = user.Email,
                RoleType = user.RoleId,
                FirstName = user.UserName,
                UserName = user.UserName
            };


            return Ok(userModel);
        }

        [HttpPut("{userName}/status")]
        public async Task<IActionResult> UpdateUserStatus([FromServices] IUserLoginService userLoginService, string userName, bool active)
        {
            await userLoginService.ActivateDeactivateAccount(userName, active);
            PostResponseModel.SetSuccessMessage("Member user status changed successfully");
            return Ok(true);
        }
    }
}
