using HautSphere.BuildingBlocks.Domain;

namespace HautSphere.BuildingBlocks.Application
{
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(IDomainEvent domainEvent);
    }

}
