using Binsera.Core.Audit.Enums;
using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Audit.Domain
{
    /// <summary>
    /// Immutable chronological record of every significant system event -- approvals, rejections,
    /// commission runs, wallet operations. Append-only; never updated or deleted. Required for
    /// compliance and dispute resolution (D.7 requirement).
    /// </summary>
    public class AuditLog : DomainBase
    {
        private AuditLog() { }

        private AuditLog(AuditEventType eventType, string description, long? relatedEntityId, string entityType, long performedBy)
        {
            EventType = eventType;
            Description = description;
            RelatedEntityId = relatedEntityId;
            EntityType = entityType;
            PerformedBy = performedBy;
            OccurredOn = DateTime.UtcNow;
        }

        public AuditEventType EventType { get; private set; }
        public string Description { get; private set; }
        public long? RelatedEntityId { get; private set; }
        public string EntityType { get; private set; }
        public long PerformedBy { get; private set; }
        public string PerformedByName { get; private set; }
        public DateTime OccurredOn { get; private set; }
        public string Metadata { get; private set; }

        public static AuditLog Create(AuditEventType eventType, string description,
            long? relatedEntityId, string entityType, long performedBy, string performedByName = null, string metadata = null)
            => new(eventType, description, relatedEntityId, entityType, performedBy)
            {
                PerformedByName = performedByName,
                Metadata = metadata
            };
    }
}
