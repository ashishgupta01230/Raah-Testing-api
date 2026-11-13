using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Members.Events
{
    public record MemberPayNoUpdatedDomainEvent(
        long MemberId,
        Guid MemberGraphId,
        int? ConfPayNo) : DomainEventBase;
}
