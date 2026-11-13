namespace HautSphere.BuildingBlocks.Configuration.Models
{
    public class JwtSetting
    {
        public bool ValidateIssuerSigningKey { get; init; }
        public string IssuerSigningKey { get; init; }
        public bool ValidateIssuer { get; init; }
        public string ValidIssuer { get; init; }
        public bool ValidateAudience { get; init; }
        public string ValidAudience { get; init; }
        public bool RequireExpirationTime { get; init; }
        public bool ValidateLifetime { get; init; }
    }
}
