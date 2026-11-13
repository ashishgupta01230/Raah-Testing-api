using Binsera.Core.Audit.Enums;

namespace Binsera.Core.Audit.Contracts
{
    public interface IAuditService
    {
        Task LogAsync(AuditEventType eventType, string description, long? relatedEntityId,
            string entityType, long performedBy, string performedByName = null, string metadata = null);
    }
}
