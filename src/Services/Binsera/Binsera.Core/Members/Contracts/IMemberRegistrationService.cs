using Binsera.Core.Members.Domain;
using Binsera.Core.Members.Models;

namespace Binsera.Core.Members.Contracts
{
    public interface IMemberRegistrationService
    {
        Task<MemberProfileModel> GetMemberProfileAsync(string userName);
        Task<MemberProfileModel?> GetMemberProfileByGraphIdAsync(Guid memberGraphId);
        Task<Member> RegisterMemberAsync(MemberRegistrationModel memberRegistration);
        Task<SponsorModel> GetSponsor(string sponsorId);
        Task<bool> UpdateMemberProfileAsync(string userName, PersonInfoModel model);
        Task<bool> UpdateMemberProfileImageAsync(string userName, long profileId);
    }
}
