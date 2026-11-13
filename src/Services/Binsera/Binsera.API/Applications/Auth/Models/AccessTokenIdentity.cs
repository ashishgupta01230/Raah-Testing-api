namespace Binsera.API.Applications.Auth.Models
{
    public sealed class AccessTokenIdentity
    {
        public required long UserId { get; init; }
        public required string UserName { get; init; }
        public required Guid SessionId { get; init; }
        public required int RoleId { get; init; }
    }
}
