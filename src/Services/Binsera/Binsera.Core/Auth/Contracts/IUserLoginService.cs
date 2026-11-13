using Binsera.Core.Auth.Domain;

namespace Binsera.Core.Auth.Contracts
{
    public interface IUserLoginService
    {
        Task<long> CrateUser(string userName, string email, RoleType roleType);
        Task ActivateDeactivateAccount(string userName, bool active);
        Task<(bool, string)> ChangePassword(string userName, string oldPassword, string newPassword);
    }
}
