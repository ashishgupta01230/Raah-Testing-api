using HautSphere.BuildingBlocks.Domain;

namespace HautSphere.BuildingBlocks.Application
{
    public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
    }
}
