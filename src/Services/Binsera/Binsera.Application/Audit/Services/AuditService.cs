using Binsera.Core.Audit.Contracts;
using Binsera.Core.Audit.Domain;
using Binsera.Core.Audit.Enums;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Persistence.Contracts;

namespace Binsera.Application.Audit.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class AuditService(IUnitOfWork unitOfWork) : IAuditService
    {
        private readonly IRepository<AuditLog> _auditLogRepository = unitOfWork.Repository<AuditLog>();

        public async Task LogAsync(AuditEventType eventType, string description, long? relatedEntityId,
            string entityType, long performedBy, string performedByName = null, string metadata = null)
        {
            var log = AuditLog.Create(eventType, description, relatedEntityId, entityType, performedBy, performedByName, metadata);
            await _auditLogRepository.Save(log);
        }
    }
}
