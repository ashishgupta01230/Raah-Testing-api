using Binsera.Core.Auth.Model;

namespace Binsera.Core.Auth.Contracts
{
    public interface IValidateUserService
    {
        Task<ValidateUserDto> ValidateUser(string userName);
        Task<bool> ValidateLogin(string userName, string password);
    }
}
