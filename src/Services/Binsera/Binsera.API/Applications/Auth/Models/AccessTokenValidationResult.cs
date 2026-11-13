namespace Binsera.API.Applications.Auth.Models
{
    public sealed class AccessTokenValidationResult
    {
        public bool IsValid { get; init; }
        public bool IsExpired { get; init; }
        public DateTime? ExpiresAtUtc { get; init; }
        public AccessTokenIdentity? Identity { get; init; }
    }
}
