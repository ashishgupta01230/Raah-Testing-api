using HautSphere.BuildingBlocks.Domain;
using System.Security.Cryptography;
using System.Text;

namespace Binsera.Core.Auth.Domain
{
    public class UserLog : DomainBase
    {
        private UserLog()
        {
        }
        private UserLog(long userId, string userName, string email, RoleType roleType, string clientIp, string browser)
        {
            UserId = userId;
            UserName = userName;
            Email = email;
            SessionId = Guid.NewGuid();
            ClientIp = clientIp;
            Browser = browser;
            LoginDateTime = DateTime.UtcNow;
            RoleType = roleType;
            IsSessionActivated = true;
        }
        public long UserId { get; private set; }
        public string UserName { get; private set; }
        public string Email { get; private set; }
        public Guid SessionId { get; private set; }
        public string ClientIp { get; private set; }
        public string Browser { get; private set; }
        public DateTime LoginDateTime { get; private set; }
        public DateTime? LogoutDateTime { get; private set; }
        public bool IsSessionActivated { get; private set; }
        public RoleType RoleType { get; set; }
        public string RefreshTokenHash { get; private set; }
        public string PreviousRefreshTokenHash { get; private set; }
        public DateTime? RefreshTokenExpiresAt { get; private set; }
        public DateTime? RefreshTokenIssuedAt { get; private set; }
        public DateTime? RefreshTokenRevokedAt { get; private set; }
        public bool KeepMeSignedIn { get; private set; }

        public static UserLog CreateDomain(long userId, string userName, string email, RoleType roleType, string clientIp, string browser)
        {
            return new UserLog(userId, userName, email, roleType, clientIp, browser);
        }

        public void SetRefreshToken(string refreshToken, DateTime expiresAt, bool keepMeSignedIn)
        {
            RefreshTokenHash = ComputeHash(refreshToken);
            RefreshTokenIssuedAt = DateTime.UtcNow;
            RefreshTokenExpiresAt = expiresAt;
            RefreshTokenRevokedAt = null;
            KeepMeSignedIn = keepMeSignedIn;
            IsSessionActivated = true;
        }

        public bool IsRefreshTokenValid(string refreshToken)
        {
            if (!IsSessionActivated || string.IsNullOrWhiteSpace(RefreshTokenHash) || RefreshTokenRevokedAt.HasValue || !RefreshTokenExpiresAt.HasValue)
            {
                return false;
            }

            if (RefreshTokenExpiresAt.Value < DateTime.UtcNow)
            {
                return false;
            }

            return string.Equals(RefreshTokenHash, ComputeHash(refreshToken), StringComparison.Ordinal);
        }

        public bool IsRotatedTokenReuse(string refreshToken)
        {
            if (!IsSessionActivated || string.IsNullOrWhiteSpace(PreviousRefreshTokenHash))
            {
                return false;
            }

            return string.Equals(PreviousRefreshTokenHash, ComputeHash(refreshToken), StringComparison.Ordinal);
        }

        public void RotateRefreshToken(string newRefreshToken, DateTime newExpiresAt)
        {
            PreviousRefreshTokenHash = RefreshTokenHash;
            SetRefreshToken(newRefreshToken, newExpiresAt, KeepMeSignedIn);
        }

        public void RevokeSession()
        {
            LogoutDateTime = DateTime.UtcNow;
            IsSessionActivated = false;
            RefreshTokenRevokedAt = DateTime.UtcNow;
        }

        private static string ComputeHash(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}
