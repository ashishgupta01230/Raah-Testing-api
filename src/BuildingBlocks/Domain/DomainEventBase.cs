namespace HautSphere.BuildingBlocks.Domain
{
    public record DomainEventBase : IDomainEvent
    {
        public Guid Id { get; }
        public DateTime OccurredOn { get; }
    }
}
