using Binsera.Core.Auth.Model;
using HautSphere.BuildingBlocks.Configuration.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Binsera.API.Applications.Helpers
{
    public static class JwtHelpers
    {
        public static IEnumerable<Claim> GetClaims(this UserTokens userAccounts, long Id)
        {
            IEnumerable<Claim> claims = new Claim[] {
                new Claim("Id", userAccounts.Id.ToString()),
                    new Claim(nameof(userAccounts.UserName), userAccounts.UserName),
                    new Claim(JwtRegisteredClaimNames.UniqueName, userAccounts.UserName),
                    new Claim(ClaimTypes.Email, userAccounts.EmailId),
                    new Claim(JwtRegisteredClaimNames.Sub, Id.ToString()),
                    new Claim(nameof(userAccounts.RegNo), userAccounts.RegNo?.ToString()??""),
                    new Claim(nameof(userAccounts.MemberGraphId), userAccounts.MemberGraphId?.ToString()??""),
                    new Claim(nameof(userAccounts.RoleId), userAccounts.RoleId.ToString()),
                    new Claim(nameof(userAccounts.SessionId), userAccounts.SessionId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, userAccounts.SessionId.ToString()),
                    new Claim(ClaimTypes.Expiration, DateTime.UtcNow.AddDays(1).ToString("MMM ddd dd yyyy HH:mm:ss tt"))
            };
            return claims;
        }
        public static IEnumerable<Claim> GetClaims(this UserTokens userAccounts, out long Id)
        {
            Id = userAccounts.Id;
            return GetClaims(userAccounts, Id);
        }
        public static UserTokens GenTokenkey(UserTokens model, JwtSetting jwtSettings, Guid sessionId)
        {
            long Id;
            var UserToken = model;
            if (model == null) throw new ArgumentException(nameof(model));
            // Get secret key
            var key = System.Text.Encoding.ASCII.GetBytes(jwtSettings.IssuerSigningKey);

            DateTime expireTime = DateTime.UtcNow.AddDays(1);
            var JWToken = new JwtSecurityToken(issuer: jwtSettings.ValidIssuer, audience: jwtSettings.ValidAudience,
                claims: GetClaims(model, out Id), notBefore: DateTime.UtcNow, expires: expireTime,
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));
            UserToken.ExpiredTime = expireTime;
            UserToken.AccessToken = new JwtSecurityTokenHandler().WriteToken(JWToken);
            UserToken.UserName = model.UserName;
            UserToken.Id = model.Id;
            UserToken.SessionId = sessionId;
            return UserToken;
        }
    }
}
