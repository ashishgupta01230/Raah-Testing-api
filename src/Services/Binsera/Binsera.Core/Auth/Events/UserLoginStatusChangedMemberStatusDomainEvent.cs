using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Auth.Events
{
    public record UserLoginStatusChangedMemberStatusDomainEvent : DomainEventBase
    {
        public string UserId { get; init; }
        public bool IsActive { get; init; }
        public UserLoginStatusChangedMemberStatusDomainEvent(string userId, bool isActive)
        {
            UserId = userId;
            IsActive = isActive;
        }
    }
}
