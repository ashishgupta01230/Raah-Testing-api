using Binsera.API.Applications.Auth.Contracts;
using Binsera.Core.Auth.Contracts;
using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Auth
{
    [Route("")]
    public class AuthSessionController(
        IUserLogService userLogService,
        IValidateUserService validateUserService,
        IAccessTokenService accessTokenService,
        IAuthCookieService authCookieService,
        ILogger<AuthSessionController> logger) : ApiControllerBase
    {
        private static readonly HashSet<int> AllowedRoleIds = new HashSet<int> { 1, 2, 3, 4 };

        [HttpGet("auth/validate")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateSession()
        {
            try
            {
                var accessToken = authCookieService.GetAccessToken();
                var refreshToken = authCookieService.GetRefreshToken();

                if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
                {
                    return Unauthorized(new { message = "Session is invalid or expired" });
                }

                var tokenValidation = accessTokenService.Validate(accessToken, validateLifetime: true);
                if (tokenValidation.IsValid && tokenValidation.Identity != null && tokenValidation.ExpiresAtUtc.HasValue)
                {
                    var identity = tokenValidation.Identity;

                    if (!AllowedRoleIds.Contains(identity.RoleId))
                    {
                        logger.LogWarning("Invalid role id '{RoleId}' found in access token for user id '{UserId}'.", identity.RoleId, identity.UserId);
                        return Unauthorized(new { message = "Session is invalid or expired" });
                    }

                    var activeSession = await userLogService.GetActiveSessionAsync(identity.SessionId);
                    if (activeSession == null || activeSession.UserId != identity.UserId)
                    {
                        return Unauthorized(new { message = "Session is invalid or expired" });
                    }

                    var validateUser = await validateUserService.ValidateUser(identity.UserName);
                    if (!validateUser.IsAccountActive)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, new { message = "User account is disabled" });
                    }

                    var expiresIn = GetExpiresInSeconds(tokenValidation.ExpiresAtUtc.Value);
                    return Ok(new
                    {
                        roleId = identity.RoleId,
                        userId = identity.UserId.ToString(),
                        expiresIn
                    });
                }

                if (!tokenValidation.IsExpired)
                {
                    return Unauthorized(new { message = "Session is invalid or expired" });
                }

                var (session, newRefreshToken) = await userLogService.RotateRefreshTokenAsync(refreshToken);
                if (session == null || string.IsNullOrWhiteSpace(newRefreshToken))
                {
                    authCookieService.ClearAuthCookies();
                    return Unauthorized(new { message = "Session is invalid or expired" });
                }

                var userState = await validateUserService.ValidateUser(session.UserName);
                if (!userState.IsAccountActive)
                {
                    authCookieService.ClearAuthCookies();
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "User account is disabled" });
                }

                var refreshedRoleId = (int)session.RoleType;
                if (!AllowedRoleIds.Contains(refreshedRoleId))
                {
                    logger.LogWarning("Invalid role id '{RoleId}' found during refresh for session '{SessionId}'.", refreshedRoleId, session.SessionId);
                    authCookieService.ClearAuthCookies();
                    return Unauthorized(new { message = "Session is invalid or expired" });
                }

                var refreshedToken = accessTokenService.CreateToken(
                    session.UserId,
                    session.SessionId,
                    session.Email,
                    session.UserName,
                    refreshedRoleId,
                   userState.RegNo,
                   userState.MemberGraphId
                    );

                authCookieService.SetAccessToken(refreshedToken.AccessToken, refreshedToken.ExpiredTime);
                authCookieService.SetRefreshToken(newRefreshToken, session.KeepMeSignedIn);

                return Ok(new
                {
                    roleId = refreshedRoleId,
                    userId = session.UserId.ToString(),
                    expiresIn = GetExpiresInSeconds(refreshedToken.ExpiredTime)
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while validating user session.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected server error occurred" });
            }
        }

        private static long GetExpiresInSeconds(DateTime expiresAtUtc)
        {
            var remaining = expiresAtUtc - DateTime.UtcNow;
            return remaining.TotalSeconds > 0 ? (long)Math.Floor(remaining.TotalSeconds) : 0;
        }
    }
}
