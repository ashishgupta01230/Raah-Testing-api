using HautSphere.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HautSphere.BuildingBlocks.Persistence.Extensions
{
    public static class ChangeTrackerConfigurationExtensions
    {
        /// <summary>
        /// Handle Change tracking if any updated and insertion of tables 
        /// </summary>
        /// <param name="changeTracker"></param>
        /// <param name="userId"> create by user and update by user id</param>
        public static void HandleAuditBeforeSave(this ChangeTracker changeTracker, long userId, DateTime currentTime)
        {
            var entities = changeTracker.Entries()
                .Where(e => (e.Entity is AuditableDomainBase || e.Entity is TransactionDomainBase)
                    && e.State != EntityState.Unchanged
                   );

            foreach (var entry in entities)
            {
                var entity = entry.Entity;
                var entityBase = entity as TransactionDomainBase;

                if (entry.State == EntityState.Added)
                {
                    if (entityBase.CreatedDate == default)
                    {
                        entityBase.CreatedDate = currentTime;
                    }

                    if (entityBase.CreatedBy <= 0)
                        entityBase.CreatedBy = userId;
                }
                else if (entity is AuditableDomainBase)
                {
                    var auditableDomain = entity as AuditableDomainBase;
                    auditableDomain.ModifiedDate = currentTime;
                    if (!auditableDomain.ModifiedBy.HasValue || auditableDomain.ModifiedBy == null)
                        auditableDomain.ModifiedBy = userId;
                }
            }
        }

        /// <summary>
        /// It is used to mark records soft delete
        /// </summary>
        public static void HandleSoftDeleteBeforeSave(this ChangeTracker changeTracker, long userId)
        {
            foreach (var entry in changeTracker.Entries().Where(w => w.State == EntityState.Deleted))
            {
                entry.Property("IsDeleted").CurrentValue = true;
                entry.State = EntityState.Modified;
               // entry.Property("ModifiedBy").CurrentValue = userId;
                //entry.Property("ModifiedDate").CurrentValue = DateTime.Now;
            }
        }

        public static void TrackGraph(this ChangeTracker changeTracker)
        {
            foreach (var entity in changeTracker.Entries().Where(e => e.Entity is DomainBase))
            {
                changeTracker.TrackGraph(entity, node =>
                {
                    var entry = node.Entry;
                    var childEntity = (DomainBase)entry.Entity;
                    entry.State = !entry.IsKeySet && childEntity.Id <= 0 ? EntityState.Added : EntityState.Modified;
                });
            }
        }

    }
}
