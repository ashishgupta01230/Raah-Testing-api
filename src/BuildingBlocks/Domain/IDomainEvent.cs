namespace HautSphere.BuildingBlocks.Domain
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
}
