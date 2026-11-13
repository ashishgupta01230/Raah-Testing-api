using Binsera.API.Applications.Auth.Contracts;
using Binsera.Core.Auth.Contracts;
using Binsera.Core.Auth.Domain;
using Binsera.Core.Auth.Model;
using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Auth
{
    [Route("auth")]
    public class LoginController(
        IValidateUserService validateUserService,
        IUserLogService userLogService,
        IAccessTokenService accessTokenService,
        IAuthCookieService authCookieService,
        IAuthSessionIssuerService authSessionIssuerService,
        IUserLoginService userLoginService) : ApiControllerBase
    {
        [HttpPost("")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateLogin(AuthenicateUserModel request)
        {
            var validateUser = await validateUserService.ValidateUser(request.UserName);
            if (validateUser.IsAccountActive)
            {
                var validateLogin = await validateUserService.ValidateLogin(request.UserName, request.Password);
                if (validateLogin)
                {
                    var token = await authSessionIssuerService.IssueSessionAsync(validateUser, request.UserName, request.KeepMeSignedIn);
                    if (token == null)
                    {
                        PostResponseModel.SetErrorMessage("Unable to create refresh token.");
                        return BadRequest("Unable to create refresh token.");
                    }

                    return Ok(token);
                }
                else
                {
                    PostResponseModel.SetErrorMessage("Invalid Users");
                    return BadRequest("Invalid Users");
                }
            }
            PostResponseModel.SetErrorMessage(validateUser.Messages);
            return BadRequest();
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = authCookieService.GetRefreshToken(includeLegacy: true);
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Unauthorized("Missing refresh token.");
            }

            var (session, newRefreshToken) = await userLogService.RotateRefreshTokenAsync(refreshToken);
            if (session == null || string.IsNullOrWhiteSpace(newRefreshToken))
            {
                authCookieService.ClearAuthCookies(includeLegacy: true);
                return Unauthorized("Invalid refresh token.");
            }

            authCookieService.SetRefreshToken(newRefreshToken, session.KeepMeSignedIn);

            var token = accessTokenService.CreateToken(
                session.UserId,
                session.SessionId,
                session.Email,
                session.UserName,
                (int)session.RoleType,
                this.RegNo,
                this.MemberGraphId);

            authCookieService.SetAccessToken(token.AccessToken, token.ExpiredTime);
            return Ok(token);
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            authCookieService.ClearAuthCookies(includeLegacy: true);
            var sessionId = GetCurrentSessionId();
            if (sessionId != Guid.Empty)
            {
                await userLogService.RevokeSessionAsync(sessionId);
            }
            return Ok();
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel request)
        {
            var (success, message) = await userLoginService.ChangePassword(UserName, request.OldPassword, request.NewPassword);
            if (!success)
            {
                PostResponseModel.SetErrorMessage(message);
                return BadRequest(true);
            }

            PostResponseModel.SetSuccessMessage("Password changed successfully.");
            return Ok(true);
        }

        private Guid GetCurrentSessionId()
        {
            var sessionIdClaim = User.Claims.FirstOrDefault(c => c.Type == nameof(UserTokens.SessionId))?.Value;
            return Guid.TryParse(sessionIdClaim, out var sessionId) ? sessionId : Guid.Empty;
        }
    }
}
