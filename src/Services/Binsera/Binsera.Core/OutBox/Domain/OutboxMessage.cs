using HautSphere.BuildingBlocks.Domain;

#nullable disable
namespace Binsera.Core.OutBox.Domain
{
    public class OutboxMessage : AuditableDomainBase
    {
        public DateTime OccurredOn { get; set; }
        public string MessageType { get; set; }
        public string Payload { get; set; }
        public int Attempts { get; set; }
        public DateTime? LastAttemptOn { get; set; }
        public bool Processed { get; set; }
        public DateTime? ProcessedOn { get; set; }
        public bool DeadLetter { get; set; }
        public string Error { get; set; }
    }
}
