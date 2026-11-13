using Binsera.Core.Members.Domain;
using Binsera.Core.Orders;
using Binsera.Core.Orders.Events;
using HautSphere.BuildingBlocks.Application;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Orders.EventHandlers
{
    public class OrderApprovedDomainEventHandler(IUnitOfWork unitOfWork)
        : IDomainEventHandler<OrderApprovedDomainEvent>
    {
        public async Task HandleAsync(OrderApprovedDomainEvent domainEvent, CancellationToken ct = default)
        {
            // Only the joining package activates a member. Approving anything else -- a repurchase,
            // any future product -- must not set ConfPayNo, because that is what puts the member
            // into the Global Auto Pool and takes the pool's cut. Without this check any approved
            // order at all granted pool entry, regardless of what was actually paid.
            if (domainEvent.ItemId != JoiningPackage.KitId)
                return;

            // Order.MemberId stores the member's business RegNo (see OrderController.CreateOrder,
            // which passes the JWT's RegNo claim), not the Member entity's internal Id -- matching
            // on Id here silently found the wrong member (or none at all).
            var memberRepo = unitOfWork.Repository<Member>();
            var member = await memberRepo.Table
                .FirstOrDefaultAsync(x => x.RegNo == domainEvent.MemberId, ct);

            if (member is null)
                return;

            member.UpdatePayNo(1);
            await memberRepo.Save(member);
        }
    }
}
