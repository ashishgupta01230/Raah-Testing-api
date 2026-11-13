using Binsera.Persistence.Interceptors;

namespace Binsera.API.Applications
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public long UserId
        {
            get
            {
                // The JWT carries the user's id under claim type "Id", not "UserId" -- see
                // JwtHelpers.GenTokenkey (new Claim("Id", ...)) and ClaimsExtension.GetUserId(),
                // which every other caller in this codebase already reads correctly. This was the
                // one place still looking for "UserId", which never matches, so CreatedBy/ModifiedBy
                // silently stamped 0 for every authenticated request regardless of who was logged in.
                var userId = _httpContextAccessor.HttpContext?.User?
                    .FindFirst("Id")?.Value;

                return long.TryParse(userId, out var id) ? id : 0;
            }
        }
    }
}
