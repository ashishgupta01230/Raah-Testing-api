using Binsera.API.Applications.Auth.Models;
using Binsera.Core.Auth.Model;

namespace Binsera.API.Applications.Auth.Contracts
{
    public interface IAccessTokenService
    {
        UserTokens CreateToken(long userId, Guid sessionId, string emailId, string userName, int roleId, long? regNo, Guid? memberGraphId);
        AccessTokenValidationResult Validate(string accessToken, bool validateLifetime);
    }
}
