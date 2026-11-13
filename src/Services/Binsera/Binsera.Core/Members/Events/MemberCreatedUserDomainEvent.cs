using Binsera.Core.Members.Domain;
using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Members.Events
{
    public record MemberCreatedUserDomainEvent(Member member) : DomainEventBase
    {
    }
}
