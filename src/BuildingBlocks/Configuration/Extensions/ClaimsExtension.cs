using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HautSphere.BuildingBlocks.Configuration.Extensions
{
    public static class ClaimsExtension
    {
        public static long GetUserId(this IEnumerable<Claim> claims)
        {
            var values = claims.Select(x => new { x.Type, x.Value });
            if (values.Any())
                return Convert.ToInt32(values.First(x => x.Type == "Id").Value);
            return 0;
        }
        public static string GetUserRole(this IEnumerable<Claim> claims)
        {
            var values = claims.Select(x => new { x.Type, x.Value });
            if (values.Any())
                return values.First(x => x.Type == "RoleName").Value;
            return null;
        }
        public static int GetUserRoleId(this IEnumerable<Claim> claims)
        {
            var values = claims.Select(x => new { x.Type, x.Value });
            if (values.Any())
                return Convert.ToInt32(values.First(x => x.Type == "RoleId").Value);
            return 0;
        }
        public static long? GetUserRegNo(this IEnumerable<Claim> claims)
        {
            var values = claims.Select(x => new { x.Type, x.Value });
            if (values.Any())
                return int.TryParse(values.FirstOrDefault(x => x.Type == "RegNo")?.Value, out var regNo) ? regNo : (long?)null;
            return null;
        }
        public static Guid? GetMemberGraphId(this IEnumerable<Claim> claims)
        {
            var values = claims.Select(x => new { x.Type, x.Value });
            if (values.Any())
                return Guid.TryParse(values.FirstOrDefault(x => x.Type == "MemberGraphId")?.Value, out var memberId) ? memberId : (Guid?)null;
            return null;
        }
        public static string GetUserName(this IEnumerable<Claim> claims)
        {
            var values = claims.Select(x => new { x.Type, x.Value });
            if (values.Any())
                return values.First(x => x.Type == "UserName").Value;
            return null;
        }
        public static Guid GetLoginSessionId(this IEnumerable<Claim> claims)
        {
            var values = claims.Select(x => new { x.Type, x.Value });
            if (values.Any())
                return Guid.Parse(values.First(x => x.Type == "SessionId").Value);
            return Guid.Empty;
        }
    }
}
