using Binsera.Core.Auth.Contracts;
using Binsera.Core.Auth.Domain;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Persistence.Contracts;

namespace Binsera.Application.Auth.Services
{
    [AutoRegister(serviceType: typeof(IUserLoginService))]
    public class UserLoginService(IUnitOfWork unitOfWork) : IUserLoginService
    {
        private readonly IRepository<UserLogin> _userLoginRepository = unitOfWork.Repository<UserLogin>();

        public async Task ActivateDeactivateAccount(string userName, bool active)
        {
            var userLogin = await _userLoginRepository.GetAsync(s => s.UserName == userName);
            userLogin.UpdateStatus(active);
            await _userLoginRepository.Save(userLogin);
        }

        public async Task<long> CrateUser(string userName, string email, RoleType roleType)
        {
            var domain = UserLogin.CreateDomain(userName, email, roleType);
            await _userLoginRepository.Save(domain);
            return domain.Id;
        }

        public async Task<(bool, string)> ChangePassword(string userName, string oldPassword, string newPassword)
        {
            var userLogin = await _userLoginRepository.GetAsync(s => s.UserName == userName && s.Password == oldPassword);
            if (userLogin == null)
                return (false, AuthResource.InvalidOldPassword);

            userLogin.Password = newPassword;
            await _userLoginRepository.Save(userLogin);
            return (true, string.Empty);
        }
    }
}
