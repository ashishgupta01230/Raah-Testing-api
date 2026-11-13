using Binsera.Core.Auth.Contracts;
using Binsera.Core.Auth.Domain;
using Binsera.Core.Members.Events;
using HautSphere.BuildingBlocks.Application;

namespace Binsera.Application.Auth.EventHandler
{
    public class MemberCreatedUserDomainEventHandler(IUserLoginService userLoginService) : IDomainEventHandler<MemberCreatedUserDomainEvent>
    {
        public async Task HandleAsync(MemberCreatedUserDomainEvent domainEvent, CancellationToken ct = default)
        {
            await userLoginService.CrateUser(domainEvent.member.LoginID, domainEvent.member.EmailID, RoleType.Member);
        }
    }
}
