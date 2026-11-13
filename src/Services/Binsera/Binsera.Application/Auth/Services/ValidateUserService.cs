using Binsera.Core.Auth.Contracts;
using Binsera.Core.Auth.Domain;
using Binsera.Core.Auth.Model;
using Binsera.Core.Members.Domain;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Persistence.Contracts;

namespace Binsera.Application.Auth.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class ValidateUserService(IUnitOfWork unitOfWork) : IValidateUserService
    {
        private readonly IRepository<UserLogin> _userLoginRepository = unitOfWork.Repository<UserLogin>();
        private readonly IRepository<Member> _memberRepository = unitOfWork.Repository<Member>();
        public async Task<bool> ValidateLogin(string userName, string password)
        {
            var result = await _userLoginRepository.GetAsync(s => s.UserName == userName && s.Password == password);
            return result != null ? result.IsActive : false;
        }

        public async Task<ValidateUserDto> ValidateUser(string userName)
        {
            var validateUserDto = new ValidateUserDto();
            var user = await _userLoginRepository.GetAsync(x => x.UserName.ToLower() == userName.ToLower());
            if (user == null)
                validateUserDto.Messages = AuthResource.InvalidUser;
            else if (user.IsActive)
            {
                validateUserDto.UserId = user.Id;
                validateUserDto.RoleId = user.RoleId;
                validateUserDto.EmailId = user.Email;
                if (user.RoleId == RoleType.Member)
                {
                    var member = await _memberRepository.GetAsync(x => x.LoginID == user.UserName.ToLower());
                    if (member != null)
                    {
                        validateUserDto.RegNo = member.RegNo;
                        validateUserDto.MemberGraphId = member.MemberGraphId;
                    }
                }
            }
            else
                validateUserDto.Messages = AuthResource.AccountDeactive;
            validateUserDto.IsAccountActive = user != null && user.IsActive;
            return validateUserDto;
        }
    }
}
