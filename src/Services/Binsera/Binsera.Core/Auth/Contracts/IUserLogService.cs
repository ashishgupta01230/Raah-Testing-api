using Binsera.Core.Auth.Domain;

namespace Binsera.Core.Auth.Contracts
{
    public interface IUserLogService
    {
        Task<Guid> LogUserLoginAsync(long userId, string userName, string email, RoleType roleType, string clientIp, string browser);
        Task<string?> CreateRefreshTokenAsync(Guid sessionId, bool keepMeSignedIn);
        Task<(UserLog? Session, string? NewRefreshToken)> RotateRefreshTokenAsync(string refreshToken);
        Task<UserLog?> GetActiveSessionAsync(Guid sessionId);
        Task RevokeSessionAsync(Guid sessionId);
    }
}
