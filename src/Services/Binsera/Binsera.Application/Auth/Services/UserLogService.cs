using Binsera.Core.Auth.Contracts;
using Binsera.Core.Auth.Domain;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Persistence.Contracts;

namespace Binsera.Application.Auth.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class UserLogService(IUnitOfWork unitOfWork) : IUserLogService
    {
        private const int ShortSessionHours = 24;
        private const int KeepSignedInDays = 30;

        private readonly IRepository<UserLog> _userLogRepository = unitOfWork.Repository<UserLog>();

        public async Task<Guid> LogUserLoginAsync(long userId, string userName, string email, RoleType roleType, string clientIp, string browser)
        {
            var domain = UserLog.CreateDomain(userId, userName, email, roleType, clientIp, browser);
            await _userLogRepository.Save(domain);
            return domain.SessionId;
        }

        public async Task<string?> CreateRefreshTokenAsync(Guid sessionId, bool keepMeSignedIn)
        {
            var session = await _userLogRepository.GetFristOrDefault(x => x.SessionId == sessionId && x.IsSessionActivated);
            if (session == null)
            {
                return null;
            }

            var refreshToken = CreateSecureToken();
            var expiry = GetRefreshTokenExpiry(keepMeSignedIn);
            session.SetRefreshToken(refreshToken, expiry, keepMeSignedIn);
            await _userLogRepository.Save(session);
            return refreshToken;
        }

        public async Task<(UserLog? Session, string? NewRefreshToken)> RotateRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return (null, null);
            }

            var activeSessions = _userLogRepository.Table.Where(x => x.IsSessionActivated && x.RefreshTokenRevokedAt == null).ToList();

            var reusedSession = activeSessions.FirstOrDefault(x => x.IsRotatedTokenReuse(refreshToken));
            if (reusedSession != null)
            {
                reusedSession.RevokeSession();
                await _userLogRepository.Save(reusedSession);
                return (null, null);
            }

            var currentSession = activeSessions.FirstOrDefault(x => x.IsRefreshTokenValid(refreshToken));
            if (currentSession == null)
            {
                return (null, null);
            }

            var newToken = CreateSecureToken();
            currentSession.RotateRefreshToken(newToken, GetRefreshTokenExpiry(currentSession.KeepMeSignedIn));
            await _userLogRepository.Save(currentSession);
            return (currentSession, newToken);
        }

        public async Task<UserLog?> GetActiveSessionAsync(Guid sessionId)
        {
            return await _userLogRepository.GetFristOrDefault(x =>
                x.SessionId == sessionId &&
                x.IsSessionActivated &&
                x.RefreshTokenRevokedAt == null);
        }

        public async Task RevokeSessionAsync(Guid sessionId)
        {
            var session = await _userLogRepository.GetFristOrDefault(x => x.SessionId == sessionId && x.IsSessionActivated);
            if (session == null)
            {
                return;
            }

            session.RevokeSession();
            await _userLogRepository.Save(session);
        }

        private static DateTime GetRefreshTokenExpiry(bool keepMeSignedIn)
        {
            return keepMeSignedIn ? DateTime.UtcNow.AddDays(KeepSignedInDays) : DateTime.UtcNow.AddHours(ShortSessionHours);
        }

        private static string CreateSecureToken()
        {
            var bytes = new byte[32];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
