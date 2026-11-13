using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Members.Events
{
    public record MemberProfileImageUpdatedDomainEvent(
        long MemberId,
        Guid MemberGraphId,
        string ProfileFileId) : DomainEventBase;
}
