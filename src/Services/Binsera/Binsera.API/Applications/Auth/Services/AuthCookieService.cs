using Binsera.API.Applications.Auth.Contracts;
using HautSphere.BuildingBlocks.Application.Attributes;

namespace Binsera.API.Applications.Auth.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class AuthCookieService(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration) : IAuthCookieService
    {
        private string AccessTokenCookieName =>
            configuration["AuthCookie:AccessTokenCookieName"] ?? "accessToken";

        private string RefreshTokenCookieName =>
            configuration["AuthCookie:RefreshTokenCookieName"] ?? "refreshToken";

        private string LegacyRefreshTokenCookieName =>
            configuration["AuthCookie:LegacyRefreshTokenCookieName"] ?? "binsera_refresh_token";

        public string? GetAccessToken()
        {
            return httpContextAccessor.HttpContext?.Request.Cookies[AccessTokenCookieName];
        }

        public string? GetRefreshToken(bool includeLegacy = false)
        {
            var refreshToken = httpContextAccessor.HttpContext?.Request.Cookies[RefreshTokenCookieName];
            if (!string.IsNullOrWhiteSpace(refreshToken) || !includeLegacy)
            {
                return refreshToken;
            }

            return httpContextAccessor.HttpContext?.Request.Cookies[LegacyRefreshTokenCookieName];
        }

        public void SetAccessToken(string accessToken, DateTime expiresAtUtc)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Path = "/",
                Expires = new DateTimeOffset(expiresAtUtc),
                MaxAge = expiresAtUtc > DateTime.UtcNow ? expiresAtUtc - DateTime.UtcNow : TimeSpan.Zero
            };

            httpContextAccessor.HttpContext?.Response.Cookies.Append(AccessTokenCookieName, accessToken, options);
        }

        public void SetRefreshToken(string refreshToken, bool keepMeSignedIn)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Path = "/"
            };

            if (keepMeSignedIn)
            {
                options.Expires = DateTimeOffset.UtcNow.AddDays(30);
                options.MaxAge = TimeSpan.FromDays(30);
            }

            var responseCookies = httpContextAccessor.HttpContext?.Response.Cookies;
            responseCookies?.Append(RefreshTokenCookieName, refreshToken, options);
            if (responseCookies != null)
            {
                ExpireCookie(responseCookies, LegacyRefreshTokenCookieName);
            }
        }

        public void ClearAuthCookies(bool includeLegacy = false)
        {
            var responseCookies = httpContextAccessor.HttpContext?.Response.Cookies;
            if (responseCookies == null)
            {
                return;
            }

            ExpireCookie(responseCookies, AccessTokenCookieName);
            ExpireCookie(responseCookies, RefreshTokenCookieName);

            if (includeLegacy)
            {
                ExpireCookie(responseCookies, LegacyRefreshTokenCookieName);
            }
        }

        private void ExpireCookie(IResponseCookies responseCookies, string name)
        {
            var host = httpContextAccessor.HttpContext?.Request.Host.Host;
            var paths = new[] { "/", "/auth" };

            foreach (var path in paths)
            {
                var baseOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    IsEssential = true,
                    Path = path,
                    Expires = DateTimeOffset.UnixEpoch,
                    MaxAge = TimeSpan.Zero
                };

                responseCookies.Delete(name, baseOptions);
                responseCookies.Append(name, string.Empty, baseOptions);

                if (!string.IsNullOrWhiteSpace(host))
                {
                    var domainOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        IsEssential = true,
                        Path = path,
                        Domain = host,
                        Expires = DateTimeOffset.UnixEpoch,
                        MaxAge = TimeSpan.Zero
                    };

                    responseCookies.Delete(name, domainOptions);
                    responseCookies.Append(name, string.Empty, domainOptions);
                }
            }

            // Fallback for older cookies created without explicit options.
            responseCookies.Delete(name);
        }
    }
}
