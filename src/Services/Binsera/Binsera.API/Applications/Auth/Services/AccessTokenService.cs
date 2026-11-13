using Binsera.API.Applications.Auth.Contracts;
using Binsera.API.Applications.Auth.Models;
using Binsera.API.Applications.Helpers;
using Binsera.Core.Auth.Model;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Configuration.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Binsera.API.Applications.Auth.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class AccessTokenService(JwtSetting jwtSetting) : IAccessTokenService
    {
        public UserTokens CreateToken(long userId, Guid sessionId, string emailId, string userName, int roleId, long? regNo, Guid? memberGraphId)
        {
            return JwtHelpers.GenTokenkey(new UserTokens
            {
                Id = userId,
                SessionId = sessionId,
                EmailId = emailId,
                UserName = userName,
                RoleId = roleId,
                RegNo = regNo,
                MemberGraphId = memberGraphId
            }, jwtSetting, sessionId);
        }

        public AccessTokenValidationResult Validate(string accessToken, bool validateLifetime)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(accessToken, BuildValidationParameters(validateLifetime), out var validatedToken);
                var jwtToken = validatedToken as JwtSecurityToken;
                if (principal == null || jwtToken == null || !TryGetIdentity(principal, out var identity))
                {
                    return new AccessTokenValidationResult { IsValid = false };
                }

                return new AccessTokenValidationResult
                {
                    IsValid = true,
                    IsExpired = false,
                    ExpiresAtUtc = jwtToken.ValidTo,
                    Identity = identity
                };
            }
            catch (SecurityTokenExpiredException)
            {
                return new AccessTokenValidationResult { IsValid = false, IsExpired = true };
            }
            catch (Exception)
            {
                return new AccessTokenValidationResult { IsValid = false };
            }
        }

        private TokenValidationParameters BuildValidationParameters(bool validateLifetime)
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = jwtSetting.ValidateIssuerSigningKey,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSetting.IssuerSigningKey)),
                ValidateIssuer = jwtSetting.ValidateIssuer,
                ValidIssuer = jwtSetting.ValidIssuer,
                ValidateAudience = jwtSetting.ValidateAudience,
                ValidAudience = jwtSetting.ValidAudience,
                RequireExpirationTime = true,
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.Zero
            };
        }

        private static bool TryGetIdentity(ClaimsPrincipal principal, out AccessTokenIdentity? identity)
        {
            identity = null;

            var roleIdValue = principal.Claims.FirstOrDefault(c => c.Type == nameof(UserTokens.RoleId))?.Value;
            var userIdValue = principal.Claims.FirstOrDefault(c => c.Type == "Id")?.Value
                ?? principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var sessionIdValue = principal.Claims.FirstOrDefault(c => c.Type == nameof(UserTokens.SessionId))?.Value;
            var userName = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName)?.Value
                ?? principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                ?? principal.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value;

            if (!int.TryParse(roleIdValue, out var roleId) ||
                !long.TryParse(userIdValue, out var userId) ||
                !Guid.TryParse(sessionIdValue, out var sessionId) ||
                string.IsNullOrWhiteSpace(userName))
            {
                return false;
            }

            identity = new AccessTokenIdentity
            {
                RoleId = roleId,
                UserId = userId,
                SessionId = sessionId,
                UserName = userName
            };
            return true;
        }
    }
}
