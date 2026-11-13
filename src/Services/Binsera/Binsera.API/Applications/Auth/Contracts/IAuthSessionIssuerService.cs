using Binsera.Core.Auth.Model;

namespace Binsera.API.Applications.Auth.Contracts
{
    public interface IAuthSessionIssuerService
    {
        Task<UserTokens?> IssueSessionAsync(ValidateUserDto validateUser, string userName, bool keepMeSignedIn = false);
    }
}
