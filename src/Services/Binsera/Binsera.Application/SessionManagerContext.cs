using Microsoft.AspNetCore.Http;

namespace Binsera.Application
{
    public class SessionManagerContext
    {
        public required string UserName { get; set; }
        public required string RoleName { get; set; }
        public required long UserId { get; set; }
        public required long RoleId { get; set; }
        public required Guid SessionId { get; set; }
        public SessionManagerContext(IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor == null || httpContextAccessor.HttpContext == null 
                || !httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
            {
                return;
            }
            var values = httpContextAccessor.HttpContext.User.Claims.Select(x => new { x.Type, x.Value });
            //UserName = values.First(x => x.Type == "FullName").Value;
            UserId = Convert.ToInt64(values.First(x => x.Type == "Id").Value);
            RoleId = Convert.ToInt64(values.First(x => x.Type == "RoleId").Value);
            SessionId = Guid.Parse(values.First(x => x.Type == "SessionId").Value);
            //RoleName = values.First(x => x.Type == "RoleName").Value;
        }
    }
}
