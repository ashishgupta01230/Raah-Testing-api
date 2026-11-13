using HautSphere.BuildingBlocks.Configuration.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HautSphere.BuildingBlocks.Configuration.Filters
{
    /// <summary>
    /// Restricts an action or controller to Admin/SubAdmin roles.
    ///
    /// ApiControllerBase's class-level [Authorize] only proves the caller has SOME valid,
    /// unexpired JWT -- it says nothing about role. Every "admin/..." route in this API is
    /// otherwise reachable by any authenticated member's token: verifying/rejecting payments,
    /// approving/rejecting orders were all callable by a plain member before this attribute
    /// existed. Apply
    /// [AdminOnly] on every admin-prefixed controller (or a specific action, if only part of a
    /// controller needs it) rather than hand-copying a RoleId check into each action -- that
    /// per-action pattern is exactly how several of those endpoints ended up unrestricted.
    ///
    /// Role ids are the same ones defined in Binsera.Core.Auth.Domain.RoleType (Admin = 1,
    /// SubAdmin = 2) -- duplicated here as literals rather than referencing that type, since this
    /// project (BuildingBlocks.Configuration) sits below Binsera.Core in the dependency graph and
    /// must not take a dependency on it.
    /// </summary>
    public class AdminOnlyAttribute : Attribute, IAuthorizationFilter
    {
        private const int AdminRoleId = 1;
        private const int SubAdminRoleId = 2;

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var roleId = context.HttpContext.User.Claims.GetUserRoleId();

            if (roleId != AdminRoleId && roleId != SubAdminRoleId)
            {
                context.Result = new ObjectResult(new { message = "This action is available to admin only." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
