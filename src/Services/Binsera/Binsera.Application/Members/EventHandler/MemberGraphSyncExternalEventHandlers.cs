using Binsera.Application.Members;
using Binsera.Core.Members.Events;
using HautSphere.BuildingBlocks.Application;

namespace Binsera.Application.Members.EventHandler
{
    public class MemberProfileUpdatedExternalEventHandler(
        IGraphDatabaseMemberRegistrationService graphDatabaseMemberRegistrationService)
        : IExternalEventHandler<MemberProfileUpdatedDomainEvent>
    {
        public async Task HandleAsync(MemberProfileUpdatedDomainEvent domainEvent, CancellationToken ct = default)
        {
            await graphDatabaseMemberRegistrationService.UpdateMember(domainEvent.MemberId);
        }
    }

    public class MemberProfileImageUpdatedExternalEventHandler(
        IGraphDatabaseMemberRegistrationService graphDatabaseMemberRegistrationService)
        : IExternalEventHandler<MemberProfileImageUpdatedDomainEvent>
    {
        public async Task HandleAsync(MemberProfileImageUpdatedDomainEvent domainEvent, CancellationToken ct = default)
        {
            await graphDatabaseMemberRegistrationService.UpdateMemberProfileImage(
                domainEvent.MemberId, domainEvent.ProfileFileId);
        }
    }

    public class MemberPayNoUpdatedExternalEventHandler(
        IGraphDatabaseMemberRegistrationService graphDatabaseMemberRegistrationService)
        : IExternalEventHandler<MemberPayNoUpdatedDomainEvent>
    {
        public async Task HandleAsync(MemberPayNoUpdatedDomainEvent domainEvent, CancellationToken ct = default)
        {
            await graphDatabaseMemberRegistrationService.UpdateMemberStatus(
                domainEvent.MemberGraphId, domainEvent.ConfPayNo);
        }
    }
}
