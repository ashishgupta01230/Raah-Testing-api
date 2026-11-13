using Binsera.Core.Members.Events;
using Binsera.Core.Pool.Contracts;
using HautSphere.BuildingBlocks.Application;

namespace Binsera.Application.Pool.EventHandlers
{
    /// <summary>
    /// Puts a newly activated member into the pool.
    ///
    /// This hangs off MemberPayNoUpdatedDomainEvent rather than OrderApprovedDomainEvent because
    /// ConfPayNo is the system-wide meaning of "this member has paid and is active", and the pool
    /// should follow that single signal.
    ///
    /// It is an external (Outbox) handler, so placement runs in its own transaction after the order
    /// approval has committed -- matching how every other money-moving handler in this codebase
    /// works. The requirements doc's demand that the cascade be atomic is satisfied by the
    /// service's own transaction; it does not need to share the approval's. The trade-off is a
    /// short window where a member is approved but not yet placed, and the possibility of an
    /// Outbox message dead-lettering, which is why the plan calls for an admin view of members who
    /// are active but unplaced.
    /// </summary>
    public class MemberActivatedPoolPlacementHandler(IPoolPlacementService poolPlacementService)
        : IExternalEventHandler<MemberPayNoUpdatedDomainEvent>
    {
        public async Task HandleAsync(MemberPayNoUpdatedDomainEvent domainEvent, CancellationToken ct = default)
        {
            if (domainEvent.ConfPayNo != 1)
                return;

            // Re-running this is safe: the service exits early if the member is already placed, and
            // every ledger write carries an idempotency key with a unique index behind it.
            await poolPlacementService.PlaceMemberAsync(domainEvent.MemberId, ct);
        }
    }
}
