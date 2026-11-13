#nullable disable
namespace Binsera.Core
{
    public class CompanySetting
    {
        public static CompanySetting Default { get; set; }
        public MemberCodeSettings MemberCodeSettings { get; init; }
        public int DefaultKitId { get; init; }
        public string OtherWebRedirectPage { get; init; }
        public string MediaRoot { get; init; }
        public string ClientId { get; init; }
    }
    public class MemberCodeSettings
    {
        public required string Prefix { get; init; }
        public required int Length { get; init; }
    }
}
