namespace Binsera.Application.Members
{
    public interface IGraphDatabaseMemberRegistrationService
    {
        Task RegisterMember(long memberId);
        Task UpdateMember(long memberId);
        Task UpdateMemberProfileImage(long memberId, string profileFileId);
        Task UpdateMemberStatus(Guid memberGraphId, int? confPayNo);
    }
}
