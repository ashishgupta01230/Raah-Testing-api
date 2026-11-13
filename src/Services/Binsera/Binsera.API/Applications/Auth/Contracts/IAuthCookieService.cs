namespace Binsera.API.Applications.Auth.Contracts
{
    public interface IAuthCookieService
    {
        string? GetAccessToken();
        string? GetRefreshToken(bool includeLegacy = false);
        void SetAccessToken(string accessToken, DateTime expiresAtUtc);
        void SetRefreshToken(string refreshToken, bool keepMeSignedIn);
        void ClearAuthCookies(bool includeLegacy = false);
    }
}
