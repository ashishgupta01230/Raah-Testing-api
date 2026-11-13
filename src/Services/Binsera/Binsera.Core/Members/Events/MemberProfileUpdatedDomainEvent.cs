using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Members.Events
{
    public record MemberProfileUpdatedDomainEvent(
        long MemberId,
        Guid MemberGraphId,
        string Title,
        string FirstName,
        string LastName,
        string EmailId,
        string PhoneNumber,
        string State,
        string PinCode,
        string ProfileFileId) : DomainEventBase;
}
