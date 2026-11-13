using Binsera.API.Applications.Auth.Contracts;
using Binsera.Core.Auth.Contracts;
using Binsera.Core.Auth.Model;
using HautSphere.BuildingBlocks.Application.Attributes;

namespace Binsera.API.Applications.Auth.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class AuthSessionIssuerService(
        IUserLogService userLogService,
        IAccessTokenService accessTokenService,
        IAuthCookieService authCookieService,
        IHttpContextAccessor httpContextAccessor) : IAuthSessionIssuerService
    {
        public async Task<UserTokens?> IssueSessionAsync(ValidateUserDto validateUser, string userName, bool keepMeSignedIn = false)
        {
            var httpContext = httpContextAccessor.HttpContext;
            var browser = httpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;
            var clientIp = httpContext?.Connection.RemoteIpAddress?.ToString() ?? httpContext?.Request.Host.ToString() ?? string.Empty;

            var sessionId = await userLogService.LogUserLoginAsync(validateUser.UserId, userName, validateUser.EmailId, validateUser.RoleId, clientIp, browser);
            var refreshToken = await userLogService.CreateRefreshTokenAsync(sessionId, keepMeSignedIn);
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            authCookieService.SetRefreshToken(refreshToken, keepMeSignedIn);

            var token = accessTokenService.CreateToken(
                validateUser.UserId,
                sessionId,
                validateUser.EmailId,
                userName,
                (int)validateUser.RoleId,
                validateUser.RegNo,
                validateUser.MemberGraphId);

            authCookieService.SetAccessToken(token.AccessToken, token.ExpiredTime);
            return token;
        }
    }
}
