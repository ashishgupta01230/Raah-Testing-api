using Binsera.Core.Orders;
using Binsera.Core.Orders.Domain;
using Binsera.Core.Orders.Enums;
using Binsera.Core.Orders.Events;
using Binsera.Core.Orders.Models;

namespace Binsera.Pool.Tests;

/// <summary>
/// What may and may not put a member into the pool.
///
/// Pool entry is triggered by OrderApprovedDomainEvent, so what that event carries decides whether
/// a member is activated. OrderApprovedDomainEventHandler acts only when the approved order was
/// for the joining package -- these pin the payload that decision reads.
/// </summary>
public class PoolEntryTriggerTests
{
    private static Order NewOrder(long kitId, decimal unitPrice)
    {
        var request = new CreateOrderRequest
        {
            OrderType = OrderType.PACKAGE,
            PaymentMode = PaymentMode.Offline,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ItemId = kitId,
                    ItemName = kitId == JoiningPackage.KitId ? "Global Auto Pool Joining Package" : "Something else",
                    Quantity = 1,
                    UnitPrice = unitPrice
                }
            ]
        };

        return Order.Create(request, memberId: 42, orderNo: "ORD-1");
    }

    private static OrderApprovedDomainEvent ApproveAndCaptureEvent(Order order)
    {
        order.SetPaymentReceived(adminId: 1);
        order.ClearDomainEvents();

        order.Approve(adminId: 1);

        return order.DomainEvents.OfType<OrderApprovedDomainEvent>().Single();
    }

    [Fact]
    public void Approving_the_joining_package_carries_the_kit_id_pool_entry_looks_for()
    {
        var approved = ApproveAndCaptureEvent(NewOrder(JoiningPackage.KitId, 200m));

        Assert.Equal(JoiningPackage.KitId, approved.ItemId);
        Assert.Equal(200m, approved.UnitPrice);
        Assert.Equal(42, approved.MemberId);
    }

    [Fact]
    public void Approving_anything_else_does_not_carry_the_joining_kit_id()
    {
        // The handler compares ItemId against the joining kit, so any other product has to come
        // through with a different id -- otherwise approving a repurchase would grant pool entry
        // and take the pool's cut of a payment that was never the joining fee.
        var approved = ApproveAndCaptureEvent(NewOrder(kitId: 99, unitPrice: 5_000m));

        Assert.NotEqual(JoiningPackage.KitId, approved.ItemId);
    }

    /// <summary>
    /// The guard against approving the same order twice lives in OrderService, not on the
    /// aggregate: the service refuses anything whose status is not PaymentReceived. Order.Approve
    /// itself will happily run again and raise a second event, so nothing else should ever call it
    /// directly.
    /// </summary>
    [Fact]
    public void The_aggregate_itself_does_not_guard_re_approval()
    {
        var order = NewOrder(JoiningPackage.KitId, 200m);

        order.SetPaymentReceived(adminId: 1);
        order.Approve(adminId: 1);
        Assert.Equal(OrderStatus.Approved, order.Status);

        order.ClearDomainEvents();
        order.Approve(adminId: 1);

        // A second event is raised. Harmless as things stand -- OrderService's status check stops
        // this path, and placement skips a member who is already placed -- but it means the status
        // check is the only thing standing between a double approval and a duplicate activation.
        Assert.Single(order.DomainEvents.OfType<OrderApprovedDomainEvent>());
    }
}
